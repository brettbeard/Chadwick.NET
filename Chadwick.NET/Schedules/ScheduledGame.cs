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

namespace Chadwick.Core.Schedules;

/// <summary>
/// One game from Retrosheet's originally planned regular-season schedule, including whether it
/// was postponed and, if so, when (or whether) it was made up.
/// </summary>
/// <remarks>
/// Unlike <see cref="Chadwick.Core.Model.Game"/>, which is built incrementally from many lines of
/// an event file, a scheduled game is fully described by a single row of the schedule file - so this type
/// is a plain immutable record rather than a mutable builder-style class.
/// </remarks>
public sealed class ScheduledGame
{
    /// <summary>The date the game was originally scheduled for.</summary>
    public required DateOnly Date { get; init; }

    /// <summary>Which game of the day this is (a single game, or one game of a doubleheader).</summary>
    public required ScheduledGameSlot Slot { get; init; }

    /// <summary>The day of the week <see cref="Date"/> falls on, as recorded in the schedule file.</summary>
    public required DayOfWeek DayOfWeek { get; init; }

    /// <summary>The visiting team's Retrosheet ID.</summary>
    public required string VisitingTeamId { get; init; }

    /// <summary>The visiting team's league.</summary>
    public required string VisitingTeamLeague { get; init; }

    /// <summary>The visiting team's game number within its own season (its Nth game played).</summary>
    public required int VisitingTeamGameNumber { get; init; }

    /// <summary>The home team's Retrosheet ID.</summary>
    public required string HomeTeamId { get; init; }

    /// <summary>The home team's league.</summary>
    public required string HomeTeamLeague { get; init; }

    /// <summary>The home team's game number within its own season (its Nth game played).</summary>
    public required int HomeTeamGameNumber { get; init; }

    /// <summary>When the game was scheduled to be played.</summary>
    public required ScheduledGameTimeOfDay TimeOfDay { get; init; }

    /// <summary>
    /// The reason(s) the game was not played as originally scheduled, in file order. Empty if
    /// the game was played as scheduled.
    /// </summary>
    public required IReadOnlyList<string> PostponementNotes { get; init; }

    /// <summary>
    /// The date(s) the game was actually played, if it was postponed and made up. Empty if the
    /// game was played as scheduled or was never made up. A second date means the first makeup
    /// attempt was itself postponed, and the game was played on the second date instead.
    /// </summary>
    public required IReadOnlyList<DateOnly> MakeupDates { get; init; }

    /// <summary>
    /// Non-date entries from the makeup field, such as the replacement code for a team that
    /// folded mid-season or was renamed. Empty in the ordinary case where the makeup field holds
    /// only date(s) or is blank.
    /// </summary>
    public required IReadOnlyList<string> MakeupFieldNotes { get; init; }

    /// <summary>Whether the game was postponed from its originally scheduled date.</summary>
    public bool WasPostponed => PostponementNotes.Count > 0;
}
