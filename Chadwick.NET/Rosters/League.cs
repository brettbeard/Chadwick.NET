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

namespace Chadwick.Core.Rosters;

/// <summary>
/// The full set of team rosters for a season, as read from a Retrosheet team file
/// (<c>TEAMyyyy</c>).
/// </summary>
public sealed class League
{
    private readonly List<Roster> _rosters = new();

    /// <summary>
    /// The team rosters in this league, in the order they were read from the team file.
    /// </summary>
    public IReadOnlyList<Roster> Rosters => _rosters;

    /// <summary>
    /// Adds a team's roster to the league.
    /// </summary>
    /// <param name="roster">The roster to add.</param>
    public void AddRoster(Roster roster)
    {
        ArgumentNullException.ThrowIfNull(roster);
        _rosters.Add(roster);
    }

    /// <summary>
    /// Finds the roster for the team with the given ID, or <see langword="null"/> if no such
    /// team is in this league.
    /// </summary>
    /// <param name="teamId">The team's Retrosheet ID.</param>
    public Roster? FindRoster(string teamId)
    {
        return _rosters.FirstOrDefault(roster => roster.TeamId == teamId);
    }
}
