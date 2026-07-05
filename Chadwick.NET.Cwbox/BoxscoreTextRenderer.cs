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

using Chadwick.Core.Boxscores;
using Chadwick.Core.Model;
using Chadwick.Core.Rosters;

namespace Chadwick.NET.Cwbox;

/// <summary>
/// Renders a <see cref="Boxscore"/> as traditional plaintext, in the same layout as Chadwick's
/// own <c>cwbox</c> tool.
/// </summary>
/// <remarks>
/// A close port of <c>cwbox.c</c>'s <c>cwbox_print_text</c> and its helpers. One upstream
/// Chadwick quirk is preserved deliberately: the day/night suffix check compares the game's
/// <c>daynight</c> info field against the literal string <c>"g_day"</c> instead of <c>"day"</c>,
/// so a day game's boxscore header never actually gets a <c>(D)</c> suffix in the reference tool
/// - only night games show a suffix, via <c>(N)</c>. Reproduced here for output parity with real
/// <c>cwbox</c> output.
/// </remarks>
internal static class BoxscoreTextRenderer
{
    private static readonly string[] PositionAbbreviations =
        ["", "p", "c", "1b", "2b", "3b", "ss", "lf", "cf", "rf", "dh", "ph", "pr"];

    private static readonly string[] NoteMarkers = ["*", "+", "#"];

    /// <summary>Renders <paramref name="boxscore"/> for <paramref name="game"/> to <paramref name="writer"/>.</summary>
    /// <param name="writer">The destination for the rendered text.</param>
    /// <param name="game">The game the boxscore was built from.</param>
    /// <param name="boxscore">The boxscore to render.</param>
    /// <param name="visitors">The visiting team's roster, if available (used to look up full names); otherwise names fall back to those recorded in the boxscore itself.</param>
    /// <param name="home">The home team's roster, if available.</param>
    public static void Render(TextWriter writer, Game game, Boxscore boxscore, Roster? visitors, Roster? home)
    {
        RenderHeader(writer, game, visitors, home);
        RenderBattingTable(writer, game, boxscore, visitors, home);
        RenderLineScore(writer, game, boxscore, visitors, home);
        writer.WriteLine();
        RenderPitchingTable(writer, game, boxscore, visitors, home);
        RenderPitcherApparatus(writer, boxscore);
        writer.WriteLine();
        RenderApparatus(writer, game, boxscore, visitors, home);
        writer.Write('\f');
    }

    private static void RenderHeader(TextWriter writer, Game game, Roster? visitors, Roster? home)
    {
        var dateParts = (game.GetInfo("date") ?? "").Split('/');
        var year = dateParts.Length > 0 && int.TryParse(dateParts[0], out var y) ? y : 0;
        var month = dateParts.Length > 1 && int.TryParse(dateParts[1], out var m) ? m : 0;
        var day = dateParts.Length > 2 && int.TryParse(dateParts[2], out var d) ? d : 0;

        var visitorCity = visitors?.City ?? game.GetInfo("visteam") ?? "";
        var homeCity = home?.City ?? game.GetInfo("hometeam") ?? "";
        var gameNumber = game.GetInfo("number") ?? "0";

        writer.Write(gameNumber == "0"
            ? $"     Game of {month}/{day}/{year} -- {visitorCity} at {homeCity}"
            : $"     Game of {month}/{day}/{year}, game {gameNumber} -- {visitorCity} at {homeCity}");

        var dayNight = game.GetInfo("daynight");
        if (dayNight == "g_day")
        {
            writer.WriteLine(" (D)");
        }
        else if (dayNight == "night")
        {
            writer.WriteLine(" (N)");
        }
        else
        {
            writer.WriteLine();
        }

        writer.WriteLine();
    }

    private static void RenderBattingTable(TextWriter writer, Game game, Boxscore boxscore, Roster? visitors, Roster? home)
    {
        var visitorCity = visitors?.City ?? game.GetInfo("visteam") ?? "";
        var homeCity = home?.City ?? game.GetInfo("hometeam") ?? "";
        writer.WriteLine($"  {visitorCity,-18} AB  R  H RBI    {homeCity,-18} AB  R  H RBI");

        // Flatten each team's batting-order slots (1-9), in slot order and within-slot
        // chronological order, then print one row per team per iteration - equivalent to
        // walking cw_box_get_starter()/->next in the C original, just without pointer-chasing.
        var teamPlayers = new List<BoxPlayer>[2];
        for (var team = 0; team <= 1; team++)
        {
            teamPlayers[team] = [];
            for (var slot = 1; slot <= 9; slot++)
            {
                teamPlayers[team].AddRange(boxscore.GetSlot(slot, team));
            }
        }

        var totals = new (int Ab, int R, int H, int Bi)[2];
        var rowCount = Math.Max(teamPlayers[0].Count, teamPlayers[1].Count);

        for (var row = 0; row < rowCount; row++)
        {
            for (var team = 0; team <= 1; team++)
            {
                if (row < teamPlayers[team].Count)
                {
                    var (ab, r, h, bi) = RenderPlayerLine(writer, teamPlayers[team][row], team == 0 ? visitors : home);
                    totals[team] = (totals[team].Ab + ab, totals[team].R + r, totals[team].H + h, totals[team].Bi + bi);
                }
                else
                {
                    writer.Write("{0,-32}", "");
                }

                writer.Write("   ");
            }

            writer.WriteLine();
        }

        writer.WriteLine("{0,-20} -- -- -- -- {1,-22} -- -- -- --", "", "");
        writer.WriteLine("{0,-20} {1,2} {2,2} {3,2} {4,2} {5,-22} {6,2} {7,2} {8,2} {9,2}",
            "", totals[0].Ab, totals[0].R, totals[0].H, totals[0].Bi,
            "", totals[1].Ab, totals[1].R, totals[1].H, totals[1].Bi);
        writer.WriteLine();
    }

    private static (int Ab, int R, int H, int Bi) RenderPlayerLine(TextWriter writer, BoxPlayer player, Roster? roster)
    {
        var name = FormatRosterName(roster?.FindPlayer(player.PlayerId)) ?? player.Name;

        string positionString;
        if (player.PhInning > 0 && (player.Positions.Count == 0 || player.Positions[0] != 11))
        {
            positionString = "ph";
        }
        else if (player.PrInning > 0 && (player.Positions.Count == 0 || player.Positions[0] != 12))
        {
            positionString = "pr";
        }
        else
        {
            positionString = "";
        }

        foreach (var position in player.Positions)
        {
            if (positionString.Length > 0)
            {
                positionString += "-";
            }

            positionString += PositionAbbreviations[position];
        }

        string displayText;
        if (positionString.Length <= 10)
        {
            var nameWidth = 18 - positionString.Length;
            var truncatedName = name.Length > nameWidth ? name[..nameWidth] : name;
            displayText = $"{truncatedName}, {positionString}";
        }
        else
        {
            // When there are too many positions to list sensibly, just show the first.
            displayText = $"{name}, {PositionAbbreviations[player.Positions[0]]},...";
        }

        writer.Write($"{displayText,-20} {player.Batting.Ab,2} {player.Batting.R,2} {player.Batting.H,2} {player.Batting.Bi,2}");

        return (player.Batting.Ab, player.Batting.R, player.Batting.H, player.Batting.Bi);
    }

    private static void RenderLineScore(TextWriter writer, Game game, Boxscore boxscore, Roster? visitors, Roster? home)
    {
        for (var team = 0; team <= 1; team++)
        {
            var runs = 0;
            var teamCity = team == 0 ? (visitors?.City ?? game.GetInfo("visteam") ?? "") : (home?.City ?? game.GetInfo("hometeam") ?? "");
            writer.Write($"{teamCity,-17}");

            var inning = 1;
            for (; inning < 50; inning++)
            {
                if (boxscore.GetLineScore(inning, 0) is null && boxscore.GetLineScore(inning, 1) is null)
                {
                    break;
                }

                var runsThisInning = boxscore.GetLineScore(inning, team);
                if (runsThisInning >= 10)
                {
                    writer.Write($"({runsThisInning})");
                    runs += runsThisInning.Value;
                }
                else if (runsThisInning >= 0)
                {
                    writer.Write(runsThisInning.Value);
                    runs += runsThisInning.Value;
                }
                else
                {
                    writer.Write("x");
                }

                if (inning % 3 == 0)
                {
                    writer.Write(" ");
                }
            }

            if ((inning - 1) % 3 != 0)
            {
                writer.Write(" ");
            }

            writer.WriteLine($"-- {runs,2}");
        }

        if (boxscore.OutsAtEnd != 3)
        {
            var outsWord = boxscore.OutsAtEnd == 1 ? "" : "s";
            writer.WriteLine(boxscore.WalkOff
                ? $"  {boxscore.OutsAtEnd} out{outsWord} when winning run was scored."
                : $"  {boxscore.OutsAtEnd} out{outsWord} when game ended.");
        }
    }

    private static void RenderPitchingTable(TextWriter writer, Game game, Boxscore boxscore, Roster? visitors, Roster? home)
    {
        var noteCount = 0;
        for (var team = 0; team <= 1; team++)
        {
            var teamCity = team == 0 ? (visitors?.City ?? game.GetInfo("visteam") ?? "") : (home?.City ?? game.GetInfo("hometeam") ?? "");
            writer.WriteLine($"  {teamCity,-18}   IP  H  R ER BB SO");

            foreach (var pitcher in boxscore.GetPitchers(team))
            {
                RenderPitcherLine(writer, game, pitcher, team == 0 ? visitors : home, ref noteCount);
            }

            if (team == 0)
            {
                writer.WriteLine();
            }
        }
    }

    private static void RenderPitcherLine(TextWriter writer, Game game, BoxPitcher pitcher, Roster? roster, ref int noteCount)
    {
        var name = FormatRosterName(roster?.FindPlayer(pitcher.PlayerId)) ?? pitcher.Name;

        if (game.GetInfo("wp") == pitcher.PlayerId)
        {
            name += " (W)";
        }
        else if (game.GetInfo("lp") == pitcher.PlayerId)
        {
            name += " (L)";
        }
        else if (game.GetInfo("save") == pitcher.PlayerId)
        {
            name += " (S)";
        }

        if (pitcher.Pitching.XbInn > 0 && pitcher.Pitching.Xb > 0)
        {
            for (var i = 0; i <= noteCount / 3; i++)
            {
                name += NoteMarkers[noteCount % 3];
            }

            noteCount++;
        }

        writer.Write($"{name,-20} {pitcher.Pitching.Outs / 3,2}.{pitcher.Pitching.Outs % 3,1} {pitcher.Pitching.H,2} {pitcher.Pitching.R,2}");
        writer.WriteLine($" {pitcher.Pitching.Er,2} {pitcher.Pitching.Bb,2} {pitcher.Pitching.So,2}");
    }

    private static void RenderPitcherApparatus(TextWriter writer, Boxscore boxscore)
    {
        var count = 0;
        for (var team = 0; team <= 1; team++)
        {
            foreach (var pitcher in boxscore.GetPitchers(team))
            {
                if (pitcher.Pitching.XbInn <= 0 || pitcher.Pitching.Xb <= 0)
                {
                    continue;
                }

                writer.Write("  ");
                for (var i = 0; i <= count / 3; i++)
                {
                    writer.Write(NoteMarkers[count % 3]);
                }

                var batterWord = pitcher.Pitching.Xb == 1 ? "" : "s";
                var ordinalSuffix = (pitcher.Pitching.XbInn % 10, pitcher.Pitching.XbInn) switch
                {
                    (1, not 11) => "st",
                    (2, not 12) => "nd",
                    (3, not 13) => "rd",
                    _ => "th",
                };
                writer.WriteLine($" Pitched to {pitcher.Pitching.Xb} batter{batterWord} in {pitcher.Pitching.XbInn}{ordinalSuffix}");

                count++;
            }
        }
    }

    private static void RenderApparatus(TextWriter writer, Game game, Boxscore boxscore, Roster? visitors, Roster? home)
    {
        RenderPlayerApparatus(writer, boxscore, boxscore.FieldingErrors, 0, "E", visitors, home);
        RenderDoublePlay(writer, game, boxscore, visitors, home);
        RenderTriplePlay(writer, game, boxscore, visitors, home);
        RenderLeftOnBase(writer, game, boxscore, visitors, home);
        RenderPlayerApparatus(writer, boxscore, boxscore.Doubles, 0, "2B", visitors, home);
        RenderPlayerApparatus(writer, boxscore, boxscore.Triples, 0, "3B", visitors, home);
        RenderPlayerApparatus(writer, boxscore, boxscore.HomeRuns, 0, "HR", visitors, home);
        RenderPlayerApparatus(writer, boxscore, boxscore.StolenBases, 0, "SB", visitors, home);
        RenderPlayerApparatus(writer, boxscore, boxscore.CaughtStealing, 0, "CS", visitors, home);
        RenderPlayerApparatus(writer, boxscore, boxscore.SacrificeHits, 0, "SH", visitors, home);
        RenderPlayerApparatus(writer, boxscore, boxscore.SacrificeFlies, 0, "SF", visitors, home);
        RenderHbpApparatus(writer, boxscore, boxscore.HitByPitches, visitors, home);
        RenderPlayerApparatus(writer, boxscore, boxscore.WildPitches, 0, "WP", visitors, home);
        RenderPlayerApparatus(writer, boxscore, boxscore.Balks, 0, "Balk", visitors, home);
        RenderPlayerApparatus(writer, boxscore, boxscore.PassedBalls, 1, "PB", visitors, home);
        RenderTimeOfGame(writer, game);
        RenderAttendance(writer, game);
    }

    private static void RenderDoublePlay(TextWriter writer, Game game, Boxscore boxscore, Roster? visitors, Roster? home)
        => RenderTeamCountLine(writer, "DP", boxscore.DoublePlays, game, visitors, home);

    private static void RenderTriplePlay(TextWriter writer, Game game, Boxscore boxscore, Roster? visitors, Roster? home)
        => RenderTeamCountLine(writer, "TP", boxscore.TriplePlays, game, visitors, home);

    private static void RenderTeamCountLine(TextWriter writer, string label, int[] counts, Game game, Roster? visitors, Roster? home)
    {
        if (counts[0] == 0 && counts[1] == 0)
        {
            return;
        }

        var visitorCity = visitors?.City ?? game.GetInfo("visteam") ?? "";
        var homeCity = home?.City ?? game.GetInfo("hometeam") ?? "";

        writer.Write($"{label} -- ");
        if (counts[0] > 0 && counts[1] == 0)
        {
            writer.WriteLine($"{visitorCity} {counts[0]}");
        }
        else if (counts[0] == 0 && counts[1] > 0)
        {
            writer.WriteLine($"{homeCity} {counts[1]}");
        }
        else
        {
            writer.WriteLine($"{visitorCity} {counts[0]}, {homeCity} {counts[1]}");
        }
    }

    private static void RenderLeftOnBase(TextWriter writer, Game game, Boxscore boxscore, Roster? visitors, Roster? home)
    {
        if (boxscore.LeftOnBase[0] == 0 && boxscore.LeftOnBase[1] == 0)
        {
            return;
        }

        var visitorCity = visitors?.City ?? game.GetInfo("visteam") ?? "";
        var homeCity = home?.City ?? game.GetInfo("hometeam") ?? "";
        writer.WriteLine($"LOB -- {visitorCity} {boxscore.LeftOnBase[0]}, {homeCity} {boxscore.LeftOnBase[1]}");
    }

    /// <summary>
    /// Prints a footer line grouping a list of events by the player found at <paramref name="index"/>
    /// in each event's player list (e.g. the batter for a "2B" list), with a count suffix when a
    /// player appears more than once.
    /// </summary>
    private static void RenderPlayerApparatus(TextWriter writer, Boxscore boxscore, List<BoxEvent> events, int index, string label, Roster? visitors, Roster? home)
    {
        if (events.Count == 0)
        {
            return;
        }

        writer.Write($"{label} -- ");
        var comma = false;

        foreach (var boxEvent in events)
        {
            if (boxEvent.Mark)
            {
                continue;
            }

            var count = 0;
            foreach (var searchEvent in events)
            {
                if (searchEvent.Players[index] == boxEvent.Players[index])
                {
                    count++;
                    searchEvent.Mark = true;
                }
            }

            var name = ResolveDisplayName(boxscore, visitors, home, boxEvent.Players[index]);

            if (comma)
            {
                writer.Write(", ");
            }

            writer.Write(count == 1 ? name : $"{name} {count}");
            comma = true;
        }

        writer.WriteLine();

        foreach (var boxEvent in events)
        {
            boxEvent.Mark = false;
        }
    }

    /// <summary>Prints the hit-by-pitch footer line, grouping by (batter, pitcher) pair rather than a single player.</summary>
    private static void RenderHbpApparatus(TextWriter writer, Boxscore boxscore, List<BoxEvent> events, Roster? visitors, Roster? home)
    {
        if (events.Count == 0)
        {
            return;
        }

        writer.Write("HBP -- ");
        var comma = false;

        foreach (var boxEvent in events)
        {
            if (boxEvent.Mark)
            {
                continue;
            }

            var count = 0;
            foreach (var searchEvent in events)
            {
                if (searchEvent.Players[0] == boxEvent.Players[0] && searchEvent.Players[1] == boxEvent.Players[1])
                {
                    count++;
                    searchEvent.Mark = true;
                }
            }

            var batterName = ResolveDisplayName(boxscore, visitors, home, boxEvent.Players[0]);
            var pitcherName = ResolveDisplayName(boxscore, visitors, home, boxEvent.Players[1]);

            if (comma)
            {
                writer.Write(", ");
            }

            writer.Write($"by {pitcherName} ({batterName})");
            if (count > 1)
            {
                writer.Write($" {count}");
            }

            comma = true;
        }

        writer.WriteLine();

        foreach (var boxEvent in events)
        {
            boxEvent.Mark = false;
        }
    }

    private static void RenderTimeOfGame(TextWriter writer, Game game)
    {
        var timeOfGameText = game.GetInfo("timeofgame");
        if (timeOfGameText is not null && int.TryParse(timeOfGameText, out var minutes) && minutes > 0)
        {
            writer.WriteLine($"T -- {minutes / 60}:{minutes % 60:00}");
        }
    }

    private static void RenderAttendance(TextWriter writer, Game game)
    {
        writer.WriteLine($"A -- {game.GetInfo("attendance") ?? ""}");
    }

    /// <summary>
    /// Resolves a player ID to a display name: "Lastname F" from a roster if available, else the
    /// name recorded in the boxscore itself (the raw appearance name), else the bare ID.
    /// </summary>
    private static string ResolveDisplayName(Boxscore boxscore, Roster? visitors, Roster? home, string playerId)
    {
        var bio = visitors?.FindPlayer(playerId) ?? home?.FindPlayer(playerId);
        var formatted = FormatRosterName(bio);
        if (formatted is not null)
        {
            return formatted;
        }

        return boxscore.FindPlayer(playerId, battersOnly: false)?.Name
            ?? boxscore.FindPitcher(playerId)?.Name
            ?? playerId;
    }

    private static string? FormatRosterName(Player? bio)
    {
        if (bio is null)
        {
            return null;
        }

        var initial = bio.FirstName.Length > 0 ? bio.FirstName[0].ToString() : "";
        return $"{bio.LastName} {initial}";
    }
}
