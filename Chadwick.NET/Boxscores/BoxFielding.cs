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

namespace Chadwick.Core.Boxscores;

/// <summary>
/// A player's accumulated fielding statistics at one position for a game.
/// </summary>
public sealed class BoxFielding
{
    /// <summary>
    /// Whether the player is credited with a game played at this position. Under modern rules,
    /// this requires appearing at the position for at least one event - merely being announced
    /// there is not enough.
    /// </summary>
    public bool G { get; set; }

    /// <summary>Outs recorded while playing this position.</summary>
    public int Outs { get; set; }

    /// <summary>Balls put in play while playing this position.</summary>
    public int Bip { get; set; }

    /// <summary>Batters faced as the fielder credited with the putout, while playing this position.</summary>
    public int Bf { get; set; }

    /// <summary>Putouts.</summary>
    public int Po { get; set; }

    /// <summary>Assists.</summary>
    public int A { get; set; }

    /// <summary>Errors.</summary>
    public int E { get; set; }

    /// <summary>Double plays participated in.</summary>
    public int Dp { get; set; }

    /// <summary>Triple plays participated in.</summary>
    public int Tp { get; set; }

    /// <summary>Passed balls (catchers only).</summary>
    public int Pb { get; set; }

    /// <summary>Times charged with catcher's interference.</summary>
    public int Xi { get; set; }
}
