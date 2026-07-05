// This file is part of Chadwick.NET, a C#/.NET port of Chadwick's cwbox
// (http://chadwick-bureau.com/), derived from C source written and maintained
// by T. L. Turocy (ted.turocy at gmail.com) at Chadwick Baseball Bureau.
//
// Chadwick.NET is free software; you can redistribute it and/or modify it
// under the terms of the GNU General Public License as published by the Free
// Software Foundation; either version 2 of the License, or (at your option)
// any later version.
//
// This program is distributed in the hope that it will be useful, but
// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
// or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License,
// included in this repository as LICENSE, for more details.

using Chadwick.Core.Model;
using Chadwick.Core.Parsing;
using Chadwick.Core.Simulation;

namespace Chadwick.Core.Boxscores;

/// <summary>
/// Builds a <see cref="Boxscore"/> by replaying a <see cref="Game"/>'s play-by-play through a
/// <see cref="GameIterator"/> and accumulating batting, pitching, and fielding statistics.
/// </summary>
/// <remarks>
/// A close port of Chadwick's <c>box.c</c>. Where the C original reports data-consistency
/// problems (a missing player/pitcher/fielder entry for an ID referenced by the play-by-play) by
/// writing to <c>stderr</c> and either returning early (tolerating the bad play) or calling
/// <c>exit(1)</c> (aborting entirely), this port preserves the same two behaviors - skip the
/// current play's stat update, or throw - but without the console output, since library code
/// must stay UI-agnostic. Chadwick's alternate "boxscore event file" format (aggregate stat
/// lines with no play-by-play, read by <c>cw_box_process_boxscore_file</c>) is not yet ported;
/// <see cref="Create"/> throws if given a game with no events.
/// </remarks>
public static class BoxscoreBuilder
{
    /// <summary>Builds a boxscore for <paramref name="game"/>.</summary>
    /// <exception cref="NotSupportedException"><paramref name="game"/> has no play-by-play events.</exception>
    public static Boxscore Create(Game game)
    {
        ArgumentNullException.ThrowIfNull(game);

        var boxscore = new Boxscore();
        EnterStarters(boxscore, game);

        if (game.Events.Count == 0)
        {
            throw new NotSupportedException(
                "This game has no play-by-play events. Boxscore-only event files (aggregate stat lines without play-by-play) are not yet supported.");
        }

        IterateGame(boxscore, game);

        for (var team = 0; team <= 1; team++)
        {
            var pitchers = boxscore.GetPitchers(team);
            if (pitchers.Count == 0)
            {
                continue;
            }

            if (pitchers.Count == 1)
            {
                pitchers[0].Pitching.Cg = 1;
                if (pitchers[0].Pitching.R == 0)
                {
                    pitchers[0].Pitching.Sho = 1;
                }
            }
            else
            {
                pitchers[^1].Pitching.Gf = 1;
            }
        }

        var winningPitcherId = game.GetInfo("wp");
        if (winningPitcherId is not null && boxscore.FindPitcher(winningPitcherId) is { } winningPitcher)
        {
            winningPitcher.Pitching.W = true;
        }

        var losingPitcherId = game.GetInfo("lp");
        if (losingPitcherId is not null && boxscore.FindPitcher(losingPitcherId) is { } losingPitcher)
        {
            losingPitcher.Pitching.L = true;
        }

        var savePitcherId = game.GetInfo("save");
        if (savePitcherId is not null && boxscore.FindPitcher(savePitcherId) is { } savePitcher)
        {
            savePitcher.Pitching.Sv = true;
        }

        var gwrbiPlayerId = game.GetInfo("gwrbi");
        if (gwrbiPlayerId is not null && boxscore.FindPlayer(gwrbiPlayerId, battersOnly: true) is { } gwrbiBatter)
        {
            gwrbiBatter.Batting.Gw = 1;
        }

        return boxscore;
    }

    private static void EnterStarters(Boxscore boxscore, Game game)
    {
        var date = game.GetInfo("date");
        var formattedDate = date is { Length: >= 10 }
            ? $"{date[0]}{date[1]}{date[2]}{date[3]}{date[5]}{date[6]}{date[8]}{date[9]}"
            : "";

        for (var team = 0; team <= 1; team++)
        {
            for (var slot = 0; slot <= 9; slot++)
            {
                var appearance = game.FindStarter(team, slot);
                if (appearance is null)
                {
                    continue;
                }

                var player = new BoxPlayer { PlayerId = appearance.PlayerId, Name = appearance.Name, Date = formattedDate };
                player.Batting.G = 1;
                player.AddPosition(appearance.Position);
                player.StartPosition = appearance.Position;

                if (appearance.Position < 10)
                {
                    // Under modern rules, a player is only credited with a game in the field
                    // once they appear there for at least one event, not merely by being
                    // announced - so the fielding line's `G` stays false here.
                    player.SetFielding(appearance.Position, new BoxFielding());
                }

                boxscore.GetSlot(slot, team).Add(player);

                if (appearance.Position == 1)
                {
                    var pitcher = new BoxPitcher { PlayerId = appearance.PlayerId, Name = appearance.Name };
                    pitcher.Pitching.G = 1;
                    pitcher.Pitching.Gs = 1;
                    boxscore.GetPitchers(team).Add(pitcher);
                }
            }
        }
    }

    private static void IterateGame(Boxscore boxscore, Game game)
    {
        var walkOff = false;
        var gameIterator = new GameIterator(game);

        while (gameIterator.CurrentEvent is not null)
        {
            var state = gameIterator.State;
            if (boxscore.GetLineScore(state.Inning, state.BattingTeam) is null)
            {
                boxscore.SetLineScore(state.Inning, state.BattingTeam, 0);
            }

            PitchStats(boxscore, gameIterator);

            if (gameIterator.CurrentEvent.PlayText != "NP")
            {
                var play = gameIterator.EventData!;
                BatterStats(boxscore, gameIterator);
                RunnerStats(boxscore, gameIterator);
                FielderStats(boxscore, gameIterator);

                if (play.DpFlag)
                {
                    boxscore.DoublePlays[1 - state.BattingTeam]++;
                }

                if (play.TpFlag)
                {
                    boxscore.TriplePlays[1 - state.BattingTeam]++;
                }

                boxscore.SetLineScore(state.Inning, state.BattingTeam, boxscore.GetLineScore(state.Inning, state.BattingTeam)!.Value + play.RunsOnPlay);

                walkOff =
                    state.Score[state.BattingTeam] + play.RunsOnPlay > state.Score[1 - state.BattingTeam] &&
                    state.Score[state.BattingTeam] - state.Score[1 - state.BattingTeam] <= 0;
            }

            AddSubstitute(boxscore, gameIterator);
            gameIterator.Next();
        }

        boxscore.OutsAtEnd = gameIterator.State.Outs;
        boxscore.WalkOff = walkOff;

        for (var team = 0; team <= 1; team++)
        {
            boxscore.LeftOnBase[team] = gameIterator.State.NumBatters[team] + gameIterator.State.NumAutoRunners[team]
                - gameIterator.State.TimesOut[team] - gameIterator.State.Score[team];
            boxscore.Score[team] = gameIterator.State.Score[team];
            boxscore.Hits[team] = gameIterator.State.Hits[team];
            boxscore.Errors[team] = gameIterator.State.Errors[team];
        }
    }

    private static void AddSubstitute(Boxscore boxscore, GameIterator gameIterator)
    {
        var currentEvent = gameIterator.CurrentEvent;
        if (currentEvent is null)
        {
            return;
        }

        foreach (var sub in currentEvent.Substitutions)
        {
            if (sub.Slot is < 0 or > 9)
            {
                throw new InvalidDataException($"In {gameIterator.Game.GameId}, invalid slot {sub.Slot} for player '{sub.PlayerId}'.");
            }

            if (sub.Team is < 0 or > 1)
            {
                throw new InvalidDataException($"In {gameIterator.Game.GameId}, invalid team {sub.Team} for player '{sub.PlayerId}'.");
            }

            if (sub.Position is < 1 or > 12)
            {
                throw new InvalidDataException($"In {gameIterator.Game.GameId}, invalid position {sub.Position} for player '{sub.PlayerId}'.");
            }

            var slotOccupants = boxscore.GetSlot(sub.Slot, sub.Team);
            BoxPlayer enteringPlayer;

            if (slotOccupants.Count == 0)
            {
                // This should never happen, but some Retrosheet files have bogus substitution
                // entries, including subbing players into slot 0 even when the DH isn't in use.
                // Try to do something reasonable.
                enteringPlayer = new BoxPlayer { PlayerId = sub.PlayerId, Name = sub.Name, Date = gameIterator.State.Date };
                enteringPlayer.Batting.G = 1;
                slotOccupants.Add(enteringPlayer);
            }
            else
            {
                var zeroSlotOccupants = boxscore.GetSlot(0, sub.Team);
                var pitcherInZeroSlot = zeroSlotOccupants.Count > 0 ? zeroSlotOccupants[^1] : null;

                if (sub.Slot != 0 && pitcherInZeroSlot is not null && pitcherInZeroSlot.PlayerId == sub.PlayerId)
                {
                    // With the DH in use, a pitcher assumes a field position (and therefore a
                    // batting-order slot). Move him out of the special slot 0 into his new slot.
                    zeroSlotOccupants.RemoveAt(zeroSlotOccupants.Count - 1);
                    slotOccupants.Add(pitcherInZeroSlot);
                    enteringPlayer = pitcherInZeroSlot;
                }
                else if (slotOccupants[^1].PlayerId != sub.PlayerId)
                {
                    enteringPlayer = new BoxPlayer { PlayerId = sub.PlayerId, Name = sub.Name, Date = gameIterator.State.Date };
                    enteringPlayer.Batting.G = 1;
                    slotOccupants.Add(enteringPlayer);

                    if (sub.Position == 11)
                    {
                        enteringPlayer.PhInning = gameIterator.State.Inning;
                    }
                    else if (sub.Position == 12)
                    {
                        enteringPlayer.PrInning = gameIterator.State.Inning;
                    }
                }
                else
                {
                    enteringPlayer = slotOccupants[^1];
                }
            }

            if (sub.Position < 10 && enteringPlayer.GetFielding(sub.Position) is null)
            {
                // The mere announcement of a position does not award a game played there;
                // that's set when processing fielding credits for events.
                enteringPlayer.SetFielding(sub.Position, new BoxFielding());
            }

            enteringPlayer.AddPosition(sub.Position);
            if (sub.Position >= 11 && sub.Slot == gameIterator.State.DhSlot[sub.Team])
            {
                // Entering as a PH or PR for the DH automatically makes the player the DH.
                enteringPlayer.AddPosition(10);
            }

            // Guard against a pitcher being subbed into the batting order when a team loses the
            // DH - don't create a duplicate pitcher record for the same player.
            var currentPitcher = boxscore.GetPitchers(sub.Team)[^1];
            if (sub.Position == 1 && currentPitcher.PlayerId != sub.PlayerId)
            {
                if (gameIterator.State.Outs == 0 && gameIterator.State.InningBatters > 0)
                {
                    currentPitcher.Pitching.Xb = Math.Min(currentPitcher.Pitching.Bf, gameIterator.State.InningBatters);
                    currentPitcher.Pitching.XbInn = gameIterator.State.Inning;
                }
                else if (currentPitcher.Pitching.Outs == 0)
                {
                    currentPitcher.Pitching.Xb = currentPitcher.Pitching.Bf;
                    currentPitcher.Pitching.XbInn = gameIterator.State.Inning;
                }

                var newPitcher = new BoxPitcher { PlayerId = sub.PlayerId, Name = sub.Name };
                newPitcher.Pitching.G = 1;
                boxscore.GetPitchers(sub.Team).Add(newPitcher);
            }

            if (sub.Position == 1)
            {
                var pitcher = boxscore.GetPitchers(sub.Team)[^1];
                for (var baseIndex = 1; baseIndex <= 3; baseIndex++)
                {
                    if (gameIterator.State.BaseOccupied(baseIndex))
                    {
                        pitcher.Pitching.Inr++;
                        if (gameIterator.RunnerFate(baseIndex) >= 4)
                        {
                            pitcher.Pitching.Inrs++;
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Updates pitch-count stats with the current event. Called even for "NP" events, since
    /// pitches may occur before a substitution record. Only counts pitches following the last
    /// period in the pitch string (earlier ones belong to a previous, since-substituted batter).
    /// </summary>
    private static void PitchStats(Boxscore boxscore, GameIterator gameIterator)
    {
        var currentEvent = gameIterator.CurrentEvent!;
        if (currentEvent.Pitches.Length == 0)
        {
            return;
        }

        var player = boxscore.FindCurrentPlayer(currentEvent.Batter, false);
        if (player is null)
        {
            return; // tolerated: matches the C original's warn-and-skip for a bogus batter ID
        }

        var defensePitchers = boxscore.GetPitchers(1 - gameIterator.State.BattingTeam);
        if (defensePitchers.Count == 0)
        {
            return; // tolerated: matches the C original's warn-and-skip
        }

        var pitcher = defensePitchers[^1];

        var lastPeriod = currentEvent.Pitches.LastIndexOf('.');
        var pitchSequence = lastPeriod >= 0 ? currentEvent.Pitches[(lastPeriod + 1)..] : currentEvent.Pitches;

        foreach (var pitch in pitchSequence)
        {
            if (PitchClassifier.IsBallThrown(pitch))
            {
                player.Batting.Pitches++;
                pitcher.Pitching.Pitches++;
            }
            else if (PitchClassifier.IsStrikeThrown(pitch))
            {
                player.Batting.Pitches++;
                player.Batting.Strikes++;
                pitcher.Pitching.Pitches++;
                pitcher.Pitching.Strikes++;
            }
        }
    }

    private static void BatterStats(Boxscore boxscore, GameIterator gameIterator)
    {
        var state = gameIterator.State;
        var play = gameIterator.EventData!;

        var player = boxscore.FindPlayer(state.ChargedBatter(gameIterator.CurrentEvent!.Batter, play), battersOnly: true);
        if (player is null)
        {
            // Tolerated: matches the C original's warn-and-skip for batter events with a bogus
            // batter ID. The C original doesn't guard non-batter events against this at all (and
            // would crash on such data) - skipping here is a safe, harmless divergence for a
            // malformed-data case the reference implementation never actually handles either.
            return;
        }

        var defensePitchers = boxscore.GetPitchers(1 - state.BattingTeam);
        if (defensePitchers.Count == 0)
        {
            throw new InvalidDataException($"In {gameIterator.Game.GameId}, no pitcher in lineup for the fielding team.");
        }

        var currentPitcher = defensePitchers[^1];
        var chargedPitcher = boxscore.FindPitcher(state.ChargedPitcher(play));
        if (chargedPitcher is null)
        {
            return; // tolerated: matches the C original's warn-and-skip
        }

        if (play.IsBatter)
        {
            player.Batting.Pa++;
            chargedPitcher.Pitching.Bf++;
        }

        chargedPitcher.Pitching.Outs += play.OutsOnPlay;

        if (play.IsOfficialAtBat)
        {
            player.Batting.Ab++;
            chargedPitcher.Pitching.Ab++;

            if (state.BaseOccupied(2) || state.BaseOccupied(3))
            {
                boxscore.RispAtBats[state.BattingTeam]++;
            }

            if (play.EventType >= PlayEventType.Single && play.EventType <= PlayEventType.HomeRun)
            {
                player.Batting.H++;
                chargedPitcher.Pitching.H++;
                if (state.BaseOccupied(2) || state.BaseOccupied(3))
                {
                    boxscore.RispHits[state.BattingTeam]++;
                }

                if (play.EventType == PlayEventType.Double)
                {
                    AddEvent(boxscore.Doubles, state.Inning, state.BattingTeam, player.PlayerId, chargedPitcher.PlayerId);
                    player.Batting.B2++;
                    chargedPitcher.Pitching.B2++;
                }
                else if (play.EventType == PlayEventType.Triple)
                {
                    AddEvent(boxscore.Triples, state.Inning, state.BattingTeam, player.PlayerId, chargedPitcher.PlayerId);
                    player.Batting.B3++;
                    chargedPitcher.Pitching.B3++;
                }
                else if (play.EventType == PlayEventType.HomeRun)
                {
                    var boxEvent = AddEvent(boxscore.HomeRuns, state.Inning, state.BattingTeam, player.PlayerId, chargedPitcher.PlayerId);
                    boxEvent.Runners = play.RunsOnPlay;
                    boxEvent.Outs = state.Outs;
                    boxEvent.Location = play.HitLocation;
                    player.Batting.Hr++;
                    chargedPitcher.Pitching.Hr++;
                    if (play.RbiOnPlay == 4)
                    {
                        player.Batting.HrSlam++;
                        chargedPitcher.Pitching.HrSlam++;
                    }
                }
            }
            else if (play.EventType == PlayEventType.Strikeout)
            {
                player.Batting.So++;
                chargedPitcher.Pitching.So++;
            }
            else if (play.GdpFlag)
            {
                player.Batting.Gdp++;
                chargedPitcher.Pitching.Gdp++;
            }
        }
        else if (play.EventType is PlayEventType.Walk or PlayEventType.IntentionalWalk)
        {
            player.Batting.Bb++;
            chargedPitcher.Pitching.Bb++;
            if (play.EventType == PlayEventType.IntentionalWalk)
            {
                player.Batting.Ibb++;
                chargedPitcher.Pitching.Ibb++;
                AddEvent(boxscore.IntentionalWalks, state.Inning, state.BattingTeam, player.PlayerId, chargedPitcher.PlayerId);
            }
        }
        else if (play.EventType == PlayEventType.HitByPitch)
        {
            player.Batting.Hp++;
            chargedPitcher.Pitching.Hb++;
            AddEvent(boxscore.HitByPitches, state.Inning, state.BattingTeam, player.PlayerId, chargedPitcher.PlayerId);
        }
        else if (play.EventType == PlayEventType.Balk)
        {
            chargedPitcher.Pitching.Bk++;
            AddEvent(boxscore.Balks, state.Inning, state.BattingTeam, chargedPitcher.PlayerId);
        }
        else if (play.EventType == PlayEventType.Interference)
        {
            player.Batting.Xi++;
            chargedPitcher.Pitching.Xi++;
        }

        if (play.EventType == PlayEventType.GenericOut && !play.BuntFlag)
        {
            if (play.BattedBallType == 'G')
            {
                chargedPitcher.Pitching.Gb++;
            }
            else if (play.BattedBallType is 'F' or 'P' or 'L')
            {
                chargedPitcher.Pitching.Fb++;
            }
        }

        if (play.WpFlag)
        {
            var catcher = boxscore.FindCurrentPlayer(state.Fielders[2, 1 - state.BattingTeam], false);
            if (catcher is null)
            {
                return; // tolerated: matches the C original's warn-and-skip
            }

            AddEvent(boxscore.WildPitches, state.Inning, state.BattingTeam, currentPitcher.PlayerId, catcher.PlayerId);
            currentPitcher.Pitching.Wp++;
        }

        if (play.ShFlag)
        {
            player.Batting.Sh++;
            chargedPitcher.Pitching.Sh++;
            AddEvent(boxscore.SacrificeHits, state.Inning, state.BattingTeam, player.PlayerId, chargedPitcher.PlayerId);
        }

        if (play.SfFlag)
        {
            player.Batting.Sf++;
            chargedPitcher.Pitching.Sf++;
            AddEvent(boxscore.SacrificeFlies, state.Inning, state.BattingTeam, player.PlayerId, chargedPitcher.PlayerId);
        }

        if (play.Advance[0] >= 4)
        {
            player.Batting.R++;
            chargedPitcher.Pitching.R++;
            if (play.Advance[0] == 4 || play.Advance[0] == 6)
            {
                chargedPitcher.Pitching.Er++;
            }

            if (play.Advance[0] == 4)
            {
                boxscore.EarnedRuns[1 - state.BattingTeam]++;
            }
        }

        if (play.IsBatter)
        {
            player.Batting.Bi += play.RbiOnPlay;
            if (state.Outs == 2)
            {
                player.Batting.Bi2Out += play.RbiOnPlay;
            }
        }

        if (state.Outs + play.OutsOnPlay == 3)
        {
            if (state.BaseOccupied(3) && play.Advance[3] < 4)
            {
                player.Batting.Lisp++;
            }

            if (state.BaseOccupied(2) && play.Advance[2] < 4)
            {
                player.Batting.Lisp++;
            }
        }
        else if (play.EventType == PlayEventType.GenericOut)
        {
            if (state.BaseOccupied(1) && play.Advance[1] > 1 &&
                (play.Advance[1] < 4 || (play.Advance[1] >= 4 && play.RbiFlag[1] == 0)))
            {
                player.Batting.MovedUp++;
            }

            if (state.BaseOccupied(2) &&
                (play.Advance[2] == 3 || (play.Advance[2] >= 4 && play.RbiFlag[2] == 0)))
            {
                player.Batting.MovedUp++;
            }
        }
    }

    private static void RunnerStats(Boxscore boxscore, GameIterator gameIterator)
    {
        var state = gameIterator.State;
        var play = gameIterator.EventData!;

        for (var baseIndex = 1; baseIndex <= 3; baseIndex++)
        {
            if (!state.BaseOccupied(baseIndex))
            {
                continue;
            }

            var player = boxscore.FindCurrentPlayer(state.Runners[baseIndex].RunnerId, true);
            if (player is null)
            {
                return; // tolerated: matches the C original's warn-and-skip
            }

            var pitcher = boxscore.FindPitcher(state.ResponsiblePitcher(play, baseIndex));
            if (pitcher is null)
            {
                return; // tolerated
            }

            var catcher = boxscore.FindCurrentPlayer(state.Fielders[2, 1 - state.BattingTeam], false);
            if (catcher is null)
            {
                return; // tolerated
            }

            if (play.Advance[baseIndex] >= 4)
            {
                player.Batting.R++;
                pitcher.Pitching.R++;
                if (play.Advance[baseIndex] == 4 || play.Advance[baseIndex] == 6)
                {
                    pitcher.Pitching.Er++;
                }

                if (play.Advance[baseIndex] == 4)
                {
                    boxscore.EarnedRuns[1 - state.BattingTeam]++;
                }
            }

            var chargedPitcher = boxscore.FindPitcher(state.ChargedPitcher(play));

            if (play.SbFlag[baseIndex])
            {
                var boxEvent = AddEvent(boxscore.StolenBases, state.Inning, state.BattingTeam, player.PlayerId, chargedPitcher!.PlayerId, catcher.PlayerId);
                boxEvent.Runners = baseIndex;
                player.Batting.Sb++;
                boxEvent.Pickoff = play.PoFlag[baseIndex] ? 1 : 0;
            }

            if (play.CsFlag[baseIndex])
            {
                var boxEvent = AddEvent(boxscore.CaughtStealing, state.Inning, state.BattingTeam, player.PlayerId, chargedPitcher!.PlayerId, catcher.PlayerId);
                boxEvent.Runners = baseIndex;
                player.Batting.Cs++;
                boxEvent.Pickoff = play.PoFlag[baseIndex] ? play.Play[baseIndex][0] - '0' : 0;

                if (boxEvent.Pickoff == 1)
                {
                    chargedPitcher!.Pitching.Pk++;
                }
            }
            else if (play.PoFlag[baseIndex])
            {
                var boxEvent = play.Play[baseIndex][0] == '2'
                    ? AddEvent(boxscore.Pickoffs, state.Inning, state.BattingTeam, player.PlayerId, catcher.PlayerId)
                    : AddEvent(boxscore.Pickoffs, state.Inning, state.BattingTeam, player.PlayerId, chargedPitcher!.PlayerId);
                boxEvent.Pickoff = play.Play[baseIndex][0] - '0';
                if (boxEvent.Pickoff == 1)
                {
                    chargedPitcher!.Pitching.Pk++;
                }

                boxEvent.Runners = baseIndex;
            }
        }
    }

    private static void FielderStats(Boxscore boxscore, GameIterator gameIterator)
    {
        var state = gameIterator.State;
        var play = gameIterator.EventData!;

        for (var pos = 1; pos <= 9; pos++)
        {
            var accepted = false;
            var player = boxscore.FindCurrentPlayer(state.Fielders[pos, 1 - state.BattingTeam], false);
            var fielding = player?.GetFielding(pos);
            if (fielding is null)
            {
                return; // tolerated: matches the C original's warn-and-abandon-play behavior
            }

            // Fielders are credited with a game played only if on the field for at least one event.
            fielding.G = true;
            fielding.Outs += play.OutsOnPlay;

            if (play.EventType is PlayEventType.Single or PlayEventType.Double or PlayEventType.Triple ||
                (play.EventType == PlayEventType.HomeRun && play.FieldedBy > 0) ||
                play.EventType is PlayEventType.Error or PlayEventType.GenericOut or PlayEventType.FieldersChoice)
            {
                fielding.Bip++;
            }

            if (play.OutsOnPlay > 0 && play.FieldedBy == pos)
            {
                fielding.Bf++;
            }

            if (play.Play[0] != "99" && play.Play[1] != "99" && play.Play[2] != "99" && play.Play[3] != "99")
            {
                // If any fielding credit on the play is listed as unknown ("99"), no putouts or
                // assists are recorded for any fielder on the play.
                foreach (var putoutFielder in play.Putouts)
                {
                    if (putoutFielder == pos)
                    {
                        fielding.Po++;
                        accepted = true;
                    }
                }

                foreach (var assistFielder in play.Assists)
                {
                    if (assistFielder == pos)
                    {
                        fielding.A++;
                        accepted = true;
                    }
                }
            }

            foreach (var error in play.Errors)
            {
                if (error.FielderPosition == pos)
                {
                    fielding.E++;
                    AddEvent(boxscore.FieldingErrors, state.Inning, state.BattingTeam, player!.PlayerId);
                }
            }

            if (accepted && play.DpFlag)
            {
                fielding.Dp++;
            }

            if (accepted && play.TpFlag)
            {
                fielding.Tp++;
            }

            if (pos == 2 && play.PbFlag)
            {
                var pitcher = boxscore.FindPitcher(state.ChargedPitcher(play));
                fielding.Pb++;
                AddEvent(boxscore.PassedBalls, state.Inning, state.BattingTeam, pitcher!.PlayerId, player!.PlayerId);
            }

            if (pos == 2 && play.EventType == PlayEventType.Interference &&
                play.Errors.Count > 0 && play.Errors[0].FielderPosition == 2)
            {
                fielding.Xi++;
            }
        }

        if (play.DpFlag)
        {
            var boxEvent = AddEvent(boxscore.DoublePlayEvents, state.Inning, state.BattingTeam);
            foreach (var touchPosition in play.Touches)
            {
                var player = boxscore.FindCurrentPlayer(state.Fielders[touchPosition, 1 - state.BattingTeam], false);
                boxEvent.AddPlayer(player!.PlayerId);
            }
        }
        else if (play.TpFlag)
        {
            var boxEvent = AddEvent(boxscore.TriplePlayEvents, state.Inning, state.BattingTeam);
            foreach (var touchPosition in play.Touches)
            {
                var player = boxscore.FindCurrentPlayer(state.Fielders[touchPosition, 1 - state.BattingTeam], false);
                boxEvent.AddPlayer(player!.PlayerId);
            }
        }
    }

    private static BoxEvent AddEvent(List<BoxEvent> list, int inning, int halfInning, params string[] players)
    {
        var boxEvent = new BoxEvent { Inning = inning, HalfInning = halfInning };
        foreach (var playerId in players)
        {
            boxEvent.AddPlayer(playerId);
        }

        list.Add(boxEvent);
        return boxEvent;
    }
}
