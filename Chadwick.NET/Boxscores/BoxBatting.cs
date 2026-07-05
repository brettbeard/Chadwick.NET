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
/// A player's accumulated batting statistics for a game.
/// </summary>
public sealed class BoxBatting
{
    /// <summary>Games played (always 1 for a single-game boxscore).</summary>
    public int G { get; set; }

    /// <summary>Plate appearances.</summary>
    public int Pa { get; set; }

    /// <summary>At-bats.</summary>
    public int Ab { get; set; }

    /// <summary>Runs scored.</summary>
    public int R { get; set; }

    /// <summary>Hits.</summary>
    public int H { get; set; }

    /// <summary>Doubles.</summary>
    public int B2 { get; set; }

    /// <summary>Triples.</summary>
    public int B3 { get; set; }

    /// <summary>Home runs.</summary>
    public int Hr { get; set; }

    /// <summary>Grand slams.</summary>
    public int HrSlam { get; set; }

    /// <summary>Runs batted in.</summary>
    public int Bi { get; set; }

    /// <summary>RBIs driven in with two outs.</summary>
    public int Bi2Out { get; set; }

    /// <summary>Whether this player is credited with the game-winning RBI, or <see langword="null"/> if not applicable/known.</summary>
    public int? Gw { get; set; }

    /// <summary>Walks.</summary>
    public int Bb { get; set; }

    /// <summary>Intentional walks.</summary>
    public int Ibb { get; set; }

    /// <summary>Strikeouts.</summary>
    public int So { get; set; }

    /// <summary>Grounded into double plays.</summary>
    public int Gdp { get; set; }

    /// <summary>Times hit by pitch.</summary>
    public int Hp { get; set; }

    /// <summary>Sacrifice hits (bunts).</summary>
    public int Sh { get; set; }

    /// <summary>Sacrifice flies.</summary>
    public int Sf { get; set; }

    /// <summary>Stolen bases.</summary>
    public int Sb { get; set; }

    /// <summary>Times caught stealing.</summary>
    public int Cs { get; set; }

    /// <summary>Times reached on catcher's interference.</summary>
    public int Xi { get; set; }

    /// <summary>Times left a runner in scoring position stranded to end an inning.</summary>
    public int Lisp { get; set; }

    /// <summary>Times advanced a runner into scoring position on an out.</summary>
    public int MovedUp { get; set; }

    /// <summary>Pitches seen.</summary>
    public int Pitches { get; set; }

    /// <summary>Strikes seen.</summary>
    public int Strikes { get; set; }
}
