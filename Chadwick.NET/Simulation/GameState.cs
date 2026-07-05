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
using Chadwick.Core.Rosters;

namespace Chadwick.Core.Simulation;

/// <summary>
/// The full state of a game in progress: score, outs, lineups, and baserunners, along with who
/// is responsible for each baserunner for scoring purposes.
/// </summary>
/// <remarks>
/// This is a close port of Chadwick's <c>CWGameState</c> in <c>gameiter.c</c> - the module the
/// port's design notes flagged as highest-risk, since it implements the pitcher/catcher
/// responsibility hand-off rules of rule 10.18(g)/(h) (fielder's-choice and mid-plate-appearance
/// substitution responsibility reassignment). Kept structurally close to the original rather
/// than redesigned, for the same fidelity reasons as the play-string parser.
/// </remarks>
public sealed class GameState
{
    private const int MaxFieldingPosition = 9;
    private const int PinchHitterPosition = 11;
    private const int PinchRunnerPosition = 12;

    /// <summary>The game date in <c>yyyymmdd</c> form; updates if the game resumes after a suspension on a later date.</summary>
    public string Date { get; set; } = "";

    /// <summary>The number of events processed so far.</summary>
    public int EventCount { get; set; }

    /// <summary>The current inning number (1-based).</summary>
    public int Inning { get; set; } = 1;

    /// <summary>The team currently batting: 0 for the visiting team, 1 for the home team.</summary>
    public int BattingTeam { get; set; }

    /// <summary>The number of outs in the current half-inning.</summary>
    public int Outs { get; set; }

    /// <summary>The number of batters who have come to the plate in the current half-inning.</summary>
    public int InningBatters { get; set; }

    /// <summary>The number of runs scored in the current half-inning.</summary>
    public int InningScore { get; set; }

    /// <summary>Cumulative runs scored, indexed by team (0 = visiting, 1 = home).</summary>
    public int[] Score { get; } = new int[2];

    /// <summary>Cumulative hits, indexed by team.</summary>
    public int[] Hits { get; } = new int[2];

    /// <summary>Cumulative errors charged against each team's defense, indexed by team.</summary>
    public int[] Errors { get; } = new int[2];

    /// <summary>Cumulative outs made while batting, indexed by team.</summary>
    public int[] TimesOut { get; } = new int[2];

    /// <summary>The batting-order slot due up next, indexed by team.</summary>
    public int[] NextBatter { get; } = new int[2];

    /// <summary>The number of batters who have appeared, indexed by team.</summary>
    public int[] NumBatters { get; } = new int[2];

    /// <summary>The batting-order slot occupied by the designated hitter, indexed by team; 0 if no DH is in effect.</summary>
    public int[] DhSlot { get; } = new int[2];

    /// <summary>The number of automatic (tiebreaker) runners placed on base, indexed by team.</summary>
    public int[] NumAutoRunners { get; } = new int[2];

    /// <summary>Whether the next batter is the leadoff batter of the half-inning.</summary>
    public bool IsLeadoff { get; set; } = true;

    /// <summary>Whether the next event begins a new plate appearance (as opposed to, e.g., a stolen base mid-count).</summary>
    public bool IsNewPlateAppearance { get; set; } = true;

    /// <summary>Whether the current batter is a pinch hitter.</summary>
    public bool PinchHitFlag { get; set; }

    /// <summary>The runner (if any) on each base: index 0 is the batter-runner (used transiently while processing advances), 1-3 are first through third base.</summary>
    public RunnerState[] Runners { get; } = new RunnerState[4];

    /// <summary>The current lineup: <c>Lineups[slot, team]</c>, slots 0 (a non-batting pitcher when a DH is in effect) through 9, teams 0 (visiting) and 1 (home).</summary>
    public LineupEntry[,] Lineups { get; } = new LineupEntry[10, 2];

    /// <summary>The player currently at each fielding position: <c>Fielders[position, team]</c>, positions 1-9.</summary>
    public string?[,] Fielders { get; } = new string?[10, 2];

    /// <summary>The player removed for a pinch hitter still due up this half-inning, or <see langword="null"/>.</summary>
    public string? RemovedForPinchHitter { get; set; }

    /// <summary>The players removed for a pinch runner, indexed by the base (1-3) they were removed from.</summary>
    public string?[] RemovedForPinchRunner { get; } = new string?[4];

    /// <summary>The fielding position of the player removed for a pinch hitter.</summary>
    public int RemovedPosition { get; set; }

    /// <summary>
    /// The pitcher who should be charged with a walk when a pitching change happens mid-count,
    /// per rule 10.18(h)(1) - the pitcher who threw most of the bad pitches, not the one who
    /// happened to throw ball four.
    /// </summary>
    public string? WalkPitcher { get; set; }

    /// <summary>
    /// The batter who should be charged with a strikeout when a pinch hitter is announced with
    /// two strikes already on the previous batter, per rule 10.17(b).
    /// </summary>
    public string? StrikeoutBatter { get; set; }

    /// <summary>The batting hand of <see cref="StrikeoutBatter"/>, if known.</summary>
    public char? StrikeoutBatterHand { get; set; }

    /// <summary>The batter credited with the RBI that most recently gave a team the lead, or <see langword="null"/> if none.</summary>
    public string? GoAheadRbiPlayerId { get; set; }

    /// <summary>A <c>badj</c> override of the current batter's hand, or <see langword="null"/> if none applies.</summary>
    public char? BatterHand { get; set; }

    /// <summary>A <c>padj</c> override of the current pitcher's hand, or <see langword="null"/> if none applies.</summary>
    public char? PitcherHand { get; set; }

    /// <summary>Creates a new game state at the start of a game (top of the 1st, no outs, empty bases).</summary>
    public GameState()
    {
        NextBatter[0] = 1;
        NextBatter[1] = 1;

        for (var baseIndex = 0; baseIndex < 4; baseIndex++)
        {
            Runners[baseIndex] = new RunnerState();
        }

        for (var team = 0; team <= 1; team++)
        {
            for (var slot = 0; slot <= 9; slot++)
            {
                Lineups[slot, team] = new LineupEntry();
            }
        }
    }

    /// <summary>Creates an independent copy of this state, safe to mutate without affecting the original.</summary>
    public GameState Clone()
    {
        var clone = new GameState
        {
            Date = Date,
            EventCount = EventCount,
            Inning = Inning,
            BattingTeam = BattingTeam,
            Outs = Outs,
            InningBatters = InningBatters,
            InningScore = InningScore,
            IsLeadoff = IsLeadoff,
            IsNewPlateAppearance = IsNewPlateAppearance,
            PinchHitFlag = PinchHitFlag,
            RemovedForPinchHitter = RemovedForPinchHitter,
            RemovedPosition = RemovedPosition,
            WalkPitcher = WalkPitcher,
            StrikeoutBatter = StrikeoutBatter,
            StrikeoutBatterHand = StrikeoutBatterHand,
            GoAheadRbiPlayerId = GoAheadRbiPlayerId,
            BatterHand = BatterHand,
            PitcherHand = PitcherHand,
        };

        for (var team = 0; team <= 1; team++)
        {
            clone.Score[team] = Score[team];
            clone.Hits[team] = Hits[team];
            clone.Errors[team] = Errors[team];
            clone.TimesOut[team] = TimesOut[team];
            clone.NextBatter[team] = NextBatter[team];
            clone.NumBatters[team] = NumBatters[team];
            clone.DhSlot[team] = DhSlot[team];
            clone.NumAutoRunners[team] = NumAutoRunners[team];
        }

        for (var baseIndex = 0; baseIndex < 4; baseIndex++)
        {
            clone.Runners[baseIndex].RunnerId = Runners[baseIndex].RunnerId;
            clone.Runners[baseIndex].PitcherId = Runners[baseIndex].PitcherId;
            clone.Runners[baseIndex].CatcherId = Runners[baseIndex].CatcherId;
            clone.Runners[baseIndex].SourceEventIndex = Runners[baseIndex].SourceEventIndex;
            clone.Runners[baseIndex].IsAuto = Runners[baseIndex].IsAuto;
        }

        for (var baseIndex = 1; baseIndex <= 3; baseIndex++)
        {
            clone.RemovedForPinchRunner[baseIndex] = RemovedForPinchRunner[baseIndex];
        }

        for (var team = 0; team <= 1; team++)
        {
            for (var slot = 0; slot <= 9; slot++)
            {
                clone.Lineups[slot, team].PlayerId = Lineups[slot, team].PlayerId;
                clone.Lineups[slot, team].Name = Lineups[slot, team].Name;
                clone.Lineups[slot, team].Position = Lineups[slot, team].Position;
                clone.Fielders[slot, team] = Fielders[slot, team];
            }
        }

        return clone;
    }

    /// <summary>The number of runners (of <paramref name="team"/>'s batters) left on base, including runners currently on base.</summary>
    public int LeftOnBase(int team) => NumBatters[team] + NumAutoRunners[team] - Score[team] - TimesOut[team];

    /// <summary>
    /// The batting-order slot currently occupied by <paramref name="playerId"/>.
    /// </summary>
    /// <returns>The slot (1-9), 0 if the player is a non-batting pitcher, or -1 if not found.</returns>
    public int LineupSlot(int team, string playerId)
    {
        // Searches in decreasing slot order so that a pitcher DHing for himself resolves to his
        // batting-order identity rather than his (also-present) slot-0 pitcher identity.
        for (var slot = 9; slot >= 0; slot--)
        {
            if (Lineups[slot, team].PlayerId == playerId)
            {
                return slot;
            }
        }

        return -1;
    }

    /// <summary>
    /// The position <paramref name="playerId"/> currently plays on defense.
    /// </summary>
    /// <returns>The position code (10 = DH, 11 = PH, 12 = PR), or -1 if not found.</returns>
    public int PlayerPosition(int team, string playerId)
    {
        for (var slot = 1; slot <= 9; slot++)
        {
            if (Lineups[slot, team].PlayerId == playerId)
            {
                if (Lineups[slot, team].Position > 10 && DhSlot[team] == slot)
                {
                    // A pinch hitter for the DH is considered the DH immediately.
                    return 10;
                }

                if (Lineups[slot, team].Position > 10 && !PinchHitFlag)
                {
                    // Pinch hitters/runners are "no position" if they bat again later in the same inning.
                    return 0;
                }

                return Lineups[slot, team].Position;
            }
        }

        // Check the pitcher last: covers cases where the pitcher bats despite a DH being in effect.
        if (Lineups[0, team].PlayerId == playerId)
        {
            return Lineups[0, team].Position;
        }

        return -1;
    }

    /// <summary>Whether <paramref name="baseIndex"/> (1-3) is currently occupied by a runner.</summary>
    public bool BaseOccupied(int baseIndex) => Runners[baseIndex].RunnerId is not null;

    /// <summary>
    /// The batter charged with the outcome of <paramref name="play"/> - almost always
    /// <paramref name="batter"/>, except per rule 10.17(b) when a pinch hitter is announced with
    /// two strikes on the previous batter and then strikes out.
    /// </summary>
    public string ChargedBatter(string batter, ParsedPlay play)
    {
        if (play.EventType == PlayEventType.Strikeout && StrikeoutBatter is not null)
        {
            return StrikeoutBatter;
        }

        return batter;
    }

    /// <summary>
    /// The side the charged batter was batting from. May come from an explicit <c>badj</c>
    /// override; otherwise looked up from <paramref name="offenseRoster"/>, resolving switch
    /// hitters to the opposite of the responsible pitcher's throwing hand.
    /// </summary>
    public char ChargedBatterHand(string batter, ParsedPlay play, Roster offenseRoster, Roster defenseRoster)
    {
        if (play.EventType == PlayEventType.Strikeout && StrikeoutBatter is not null && StrikeoutBatterHand is not null)
        {
            return StrikeoutBatterHand.Value;
        }

        var resolvedBatterHand = BatterHand ?? offenseRoster.GetBattingHand(ChargedBatter(batter, play));

        if (resolvedBatterHand != 'B')
        {
            return resolvedBatterHand;
        }

        var resolvedPitcherHand = PitcherHand ?? defenseRoster.GetThrowingHand(ChargedPitcher(play));
        return resolvedPitcherHand switch
        {
            'L' => 'R',
            'R' => 'L',
            _ => '?', // needed in case the pitcher's hand is unknown
        };
    }

    /// <summary>
    /// The pitcher charged with the outcome of <paramref name="play"/> - almost always the
    /// current pitcher, except per rule 10.18(h)(1) when a pitching change happens mid-walk.
    /// </summary>
    public string ChargedPitcher(ParsedPlay play)
    {
        if ((play.EventType == PlayEventType.Walk || play.EventType == PlayEventType.IntentionalWalk) && WalkPitcher is not null)
        {
            return WalkPitcher;
        }

        return Fielders[1, 1 - BattingTeam] ?? "";
    }

    /// <summary>
    /// The pitcher charged with the eventual scoring of the runner on <paramref name="baseIndex"/>.
    /// </summary>
    /// <remarks>
    /// This is usually the pitcher responsible when the runner reached base. But on a play like
    /// <c>32(3)/FO.2-H(E2)</c>, the run should be charged to the pitcher who was originally
    /// responsible for the runner who started on third (now out on the force), so that pitcher
    /// is returned here - letting stats be computed directly from parsed play data without
    /// re-deriving responsibility chains downstream.
    /// </remarks>
    public string ResponsiblePitcher(ParsedPlay play, int baseIndex)
    {
        if (!BaseOccupied(baseIndex))
        {
            return "";
        }

        var originBase = ResponsibleBase(play, baseIndex);
        return Runners[originBase].PitcherId ?? "";
    }

    /// <summary>Whether the runner on <paramref name="baseIndex"/> is there due to an automatic-runner (tiebreaker) placement, using the same responsibility rules as <see cref="ResponsiblePitcher"/>.</summary>
    public bool RunnerIsAuto(ParsedPlay play, int baseIndex)
    {
        if (!BaseOccupied(baseIndex))
        {
            return false;
        }

        var originBase = ResponsibleBase(play, baseIndex);
        return Runners[originBase].IsAuto;
    }

    /// <summary>The catcher charged (for catcher ERA) with the eventual scoring of the runner on <paramref name="baseIndex"/>, using the same responsibility rules as <see cref="ResponsiblePitcher"/>.</summary>
    public string ResponsibleCatcher(ParsedPlay play, int baseIndex)
    {
        if (!BaseOccupied(baseIndex))
        {
            return "";
        }

        var originBase = ResponsibleBase(play, baseIndex);
        return Runners[originBase].CatcherId ?? "";
    }

    /// <summary>
    /// Generic logic for shifting pitcher responsibility on plays where a preceding runner is
    /// put out by batter action but a subsequent runner then scores.
    /// </summary>
    private int ResponsibleBase(ParsedPlay play, int baseIndex)
    {
        if (baseIndex == 3)
        {
            return 3;
        }

        if (baseIndex == 2)
        {
            if (play.RunnerPutOut(3) && play.FcFlag[3] && play.Advance[2] >= 4)
            {
                return 3;
            }

            return 2;
        }

        if (play.RunnerPutOut(3) && play.FcFlag[3] && play.Advance[2] >= 4)
        {
            return 2;
        }

        if (play.RunnerPutOut(3) && play.FcFlag[3] && !BaseOccupied(2) && play.Advance[1] >= 4)
        {
            return 3;
        }

        return 1;
    }

    /// <summary>
    /// Places an automatic (tiebreaker) runner on <paramref name="baseIndex"/>, crediting
    /// responsibility to the current pitcher and catcher.
    /// </summary>
    public void PlaceRunner(int baseIndex, string runner)
    {
        Runners[baseIndex].RunnerId = runner;
        Runners[baseIndex].PitcherId = Fielders[1, 1 - BattingTeam];
        Runners[baseIndex].CatcherId = Fielders[2, 1 - BattingTeam];
        Runners[baseIndex].IsAuto = true;
        NumAutoRunners[BattingTeam]++;
    }

    /// <summary>Places the batter-runner on base 0 at the start of a plate appearance, assigning responsibility per the event type.</summary>
    private void PlaceBatter(string batter, PlayEventType eventType)
    {
        Runners[0].RunnerId = batter;
        Runners[0].PitcherId = (eventType is PlayEventType.Walk or PlayEventType.IntentionalWalk && WalkPitcher is not null)
            ? WalkPitcher
            : Fielders[1, 1 - BattingTeam];
        Runners[0].CatcherId = Fielders[2, 1 - BattingTeam];
        Runners[0].SourceEventIndex = EventCount;
        Runners[0].IsAuto = false;
    }

    /// <summary>Replaces the runner on <paramref name="baseIndex"/> without changing responsibility - used for pinch runners.</summary>
    private void ReplaceRunner(int baseIndex, string runner)
    {
        Runners[baseIndex].RunnerId = runner;
    }

    private void MoveRunner(int src, int dest)
    {
        Runners[dest].RunnerId = Runners[src].RunnerId;
        Runners[dest].PitcherId = Runners[src].PitcherId;
        Runners[dest].CatcherId = Runners[src].CatcherId;
        Runners[dest].SourceEventIndex = Runners[src].SourceEventIndex;
        Runners[dest].IsAuto = Runners[src].IsAuto;
        Runners[src].IsAuto = false;
    }

    /// <summary>
    /// Implements rule 10.18(g): when a runner belonging to pitcher X is put out on a fielder's
    /// choice, responsibility for all subsequent (trailing) runners is "pushed back" one runner.
    /// </summary>
    private void ReassignResponsibility(int baseIndex)
    {
        for (var b = baseIndex - 1; b > 0; b--)
        {
            if (BaseOccupied(b))
            {
                ReassignResponsibility(b);
                Runners[b].PitcherId = Runners[baseIndex].PitcherId;
                Runners[b].CatcherId = Runners[baseIndex].CatcherId;
                Runners[b].IsAuto = Runners[baseIndex].IsAuto;
                return;
            }
        }

        Runners[0].PitcherId = Runners[baseIndex].PitcherId;
        Runners[0].CatcherId = Runners[baseIndex].CatcherId;
        Runners[0].IsAuto = Runners[baseIndex].IsAuto;
    }

    private void ClearRunner(int baseIndex)
    {
        Runners[baseIndex].RunnerId = null;
        Runners[baseIndex].PitcherId = null;
        Runners[baseIndex].CatcherId = null;
        Runners[baseIndex].SourceEventIndex = 0;
        Runners[baseIndex].IsAuto = false;
    }

    private void ClearAllRunners()
    {
        for (var baseIndex = 0; baseIndex < 4; baseIndex++)
        {
            ClearRunner(baseIndex);
        }
    }

    /// <summary>Checks whether this play's scoring changes which batter is credited with the go-ahead RBI. Must run before the score itself is updated.</summary>
    private void CheckGoAheadRbi(string batter, ParsedPlay play)
    {
        var diff = Score[BattingTeam] - Score[1 - BattingTeam];

        for (var baseIndex = 3; baseIndex >= 0; baseIndex--)
        {
            if (play.Advance[baseIndex] >= 4)
            {
                diff++;
                if (diff == 1)
                {
                    // This was the go-ahead run.
                    GoAheadRbiPlayerId = play.RbiFlag[baseIndex] != 0 ? batter : null;
                    return;
                }

                if (diff == 0)
                {
                    // This was the tying run.
                    GoAheadRbiPlayerId = null;
                }
            }
        }
    }

    private void ProcessAdvance(string batter, ParsedPlay play)
    {
        PlaceBatter(batter, play.EventType);

        if (play.Advance[3] >= 4 || play.RunnerPutOut(3))
        {
            if (play.FcFlag[3] && play.RunnerPutOut(3))
            {
                ReassignResponsibility(3);
            }

            ClearRunner(3);
        }

        if (play.Advance[2] == 3)
        {
            MoveRunner(2, 3);
        }

        if (play.Advance[2] >= 3 || play.RunnerPutOut(2))
        {
            if (play.FcFlag[2] && play.RunnerPutOut(2))
            {
                ReassignResponsibility(2);
            }

            ClearRunner(2);
        }

        if (play.Advance[1] == 2)
        {
            MoveRunner(1, 2);
        }
        else if (play.Advance[1] == 3)
        {
            MoveRunner(1, 3);
        }

        if (play.Advance[1] >= 2 || play.RunnerPutOut(1))
        {
            if (play.FcFlag[1] && play.RunnerPutOut(1))
            {
                ReassignResponsibility(1);
            }

            ClearRunner(1);
        }

        // Backwards advances must be processed after forward advances, to avoid clobbering
        // runner data (e.g. a caught-stealing that returns a runner to a lower base).
        if (play.Advance[3] == 2)
        {
            MoveRunner(3, 2);
            ClearRunner(3);
        }
        else if (play.Advance[3] == 1)
        {
            MoveRunner(3, 1);
            ClearRunner(3);
        }

        if (play.Advance[2] == 1)
        {
            MoveRunner(2, 1);
            ClearRunner(2);
        }

        if (play.Advance[0] is >= 1 and <= 3)
        {
            MoveRunner(0, play.Advance[0]);
        }
    }

    /// <summary>Applies the outcome of <paramref name="play"/> (by <paramref name="batter"/>) to this state: score, outs, baserunners, and batting order.</summary>
    public void Update(string batter, ParsedPlay play)
    {
        // Checked before the score itself updates, since it depends on comparing against the
        // pre-play score differential.
        CheckGoAheadRbi(batter, play);

        EventCount++;
        Score[BattingTeam] += play.RunsOnPlay;
        InningScore += play.RunsOnPlay;
        Hits[BattingTeam] += (play.EventType >= PlayEventType.Single && play.EventType <= PlayEventType.HomeRun) ? 1 : 0;
        Errors[1 - BattingTeam] += play.Errors.Count;
        TimesOut[BattingTeam] += play.OutsOnPlay;
        Outs += play.OutsOnPlay;

        ProcessAdvance(batter, play);

        if (play.IsBatter)
        {
            NumBatters[BattingTeam]++;
            NextBatter[BattingTeam]++;
            if (NextBatter[BattingTeam] == 10)
            {
                NextBatter[BattingTeam] = 1;
            }

            InningBatters++;
            PinchHitFlag = false;
            IsLeadoff = false;
            IsNewPlateAppearance = true;

            RemovedForPinchHitter = null;
            WalkPitcher = null;
            StrikeoutBatter = null;
        }
        else
        {
            IsNewPlateAppearance = false;
        }

        for (var baseIndex = 1; baseIndex <= 3; baseIndex++)
        {
            RemovedForPinchRunner[baseIndex] = null;
        }
    }

    /// <summary>
    /// Processes a substitution: <paramref name="playerId"/> (<paramref name="name"/>) entering
    /// at batting-order <paramref name="slot"/> for <paramref name="team"/>, playing
    /// <paramref name="pos"/>.
    /// </summary>
    /// <param name="batter">The batter due up when the substitution occurred (for rule 10.17(b) strikeout-batter tracking).</param>
    /// <param name="count">The ball-strike count when the substitution occurred (for rule 10.18(h)(1) walk-pitcher tracking).</param>
    /// <param name="playerId">The entering player's Retrosheet ID.</param>
    /// <param name="name">The entering player's name.</param>
    /// <param name="team">The team making the substitution: 0 visiting, 1 home.</param>
    /// <param name="slot">The batting-order slot the player enters at.</param>
    /// <param name="pos">The position the player will field.</param>
    public void Substitute(string batter, string count, string playerId, string name, int team, int slot, int pos)
    {
        var removedPlayerId = Lineups[slot, team].PlayerId;
        var removedPosition = Lineups[slot, team].Position;

        Lineups[slot, team].PlayerId = playerId;
        Lineups[slot, team].Name = name;
        Lineups[slot, team].Position = pos;

        if (count.Length == 2 && count[0] != '?' && count[1] != '?')
        {
            if (pos == 1 && (count is "20" or "21" || count[0] == '3'))
            {
                // A relief pitcher entering on a 2-0, 2-1, or 3-ball count should be charged
                // with any walk that follows, per rule 10.18(h)(1) - not the new pitcher.
                WalkPitcher = Fielders[1, team];
            }
            else if (pos == PinchHitterPosition && StrikeoutBatter is null && count[1] == '2')
            {
                // A pinch hitter announced with two strikes already on the previous batter is
                // charged with a resulting strikeout under the previous batter's name, per rule 10.17(b).
                StrikeoutBatter = batter;
                StrikeoutBatterHand = BatterHand;
            }
        }

        if (pos <= MaxFieldingPosition)
        {
            Fielders[pos, team] = playerId;
            if (pos == 1 && slot > 0 && Lineups[0, team].PlayerId is not null)
            {
                // Substituting a pitcher into the batting order eliminates the DH; clear slot 0.
                Lineups[0, team].PlayerId = null;
                Lineups[0, team].Name = null;
                DhSlot[team] = 0;
            }
        }
        else if (pos == PinchHitterPosition)
        {
            RemovedForPinchHitter = removedPlayerId;
            PinchHitFlag = true;
            RemovedPosition = removedPosition;
        }
        else if (pos == PinchRunnerPosition)
        {
            if (removedPlayerId is not null && Runners[1].RunnerId == removedPlayerId)
            {
                RemovedForPinchRunner[1] = removedPlayerId;
                ReplaceRunner(1, playerId);
            }
            else if (removedPlayerId is not null && Runners[2].RunnerId == removedPlayerId)
            {
                RemovedForPinchRunner[2] = removedPlayerId;
                ReplaceRunner(2, playerId);
            }
            else if (removedPlayerId is not null && Runners[3].RunnerId == removedPlayerId)
            {
                RemovedForPinchRunner[3] = removedPlayerId;
                ReplaceRunner(3, playerId);
            }
        }

        if (slot > 0 && Lineups[0, team].PlayerId == playerId)
        {
            // Rare case: a pitcher subs into the batting order for a non-DH slot (e.g. Catfish
            // Hunter pinch-hitting for a non-DH player on 1976/9/5). This eliminates the DH too.
            Lineups[0, team].PlayerId = null;
            Lineups[0, team].Name = null;
            DhSlot[team] = 0;
        }
    }

    /// <summary>Resets per-half-inning state at the start of a new half-inning.</summary>
    public void ChangeSides(int inning, int battingTeam)
    {
        Inning = inning;
        BattingTeam = battingTeam;
        Outs = 0;
        IsLeadoff = true;
        IsNewPlateAppearance = true;
        PinchHitFlag = false;
        InningBatters = 0;
        InningScore = 0;

        ClearAllRunners();

        // Pinch hitters/runners for the DH automatically become the DH, even though no explicit sub record occurs.
        for (var team = 0; team <= 1; team++)
        {
            if (DhSlot[team] > 0 && Lineups[DhSlot[team], team].Position > 10)
            {
                Lineups[DhSlot[team], team].Position = 10;
            }
        }

        // Clear the removed-for-pinch-hitter marker, in case the inning ended on a non-batter event.
        RemovedForPinchHitter = null;
    }
}
