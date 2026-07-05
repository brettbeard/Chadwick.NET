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

namespace Chadwick.Core.Simulation;

/// <summary>
/// The player currently occupying one batting-order slot for one team, and the position they
/// are playing.
/// </summary>
public sealed class LineupEntry
{
    /// <summary>The occupying player's Retrosheet ID, or <see langword="null"/> if the slot is empty (e.g. an unused designated-hitter slot).</summary>
    public string? PlayerId { get; set; }

    /// <summary>The occupying player's name.</summary>
    public string? Name { get; set; }

    /// <summary>The fielding position code: 1-9 standard positions, 10 designated hitter, 11 pinch hitter, 12 pinch runner.</summary>
    public int Position { get; set; }
}
