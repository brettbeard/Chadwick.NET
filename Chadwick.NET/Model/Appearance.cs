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

namespace Chadwick.Core.Model;

/// <summary>
/// A player's appearance in a lineup - either a starting lineup slot (a <c>start</c> record) or
/// a mid-game substitution (a <c>sub</c> record); both share the same fields.
/// </summary>
public sealed class Appearance
{
    /// <summary>
    /// The appearing player's Retrosheet ID.
    /// </summary>
    public required string PlayerId { get; init; }

    /// <summary>
    /// The player's name, as recorded in the event file.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The team the player appears for: 0 for the visiting team, 1 for the home team.
    /// </summary>
    public int Team { get; init; }

    /// <summary>
    /// The batting order slot (1-9), or 0 if the player does not bat (e.g. a pinch-running-only
    /// appearance under some conventions).
    /// </summary>
    public int Slot { get; init; }

    /// <summary>
    /// The fielding position code: 1-9 for the standard positions, 10 for designated hitter,
    /// 11 for pinch hitter, 12 for pinch runner.
    /// </summary>
    public int Position { get; init; }
}
