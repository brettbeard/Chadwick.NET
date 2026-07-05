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
/// A pitcher's accumulated pitching statistics for a game.
/// </summary>
public sealed class BoxPitching
{
    /// <summary>Games played (always 1 for a single-game boxscore).</summary>
    public int G { get; set; }

    /// <summary>Games started.</summary>
    public int Gs { get; set; }

    /// <summary>Whether the pitcher completed the game.</summary>
    public int Cg { get; set; }

    /// <summary>Whether the pitcher threw a shutout.</summary>
    public int Sho { get; set; }

    /// <summary>Whether the pitcher finished the game in relief.</summary>
    public int Gf { get; set; }

    /// <summary>Outs recorded.</summary>
    public int Outs { get; set; }

    /// <summary>At-bats against.</summary>
    public int Ab { get; set; }

    /// <summary>Runs allowed.</summary>
    public int R { get; set; }

    /// <summary>Earned runs allowed.</summary>
    public int Er { get; set; }

    /// <summary>Hits allowed.</summary>
    public int H { get; set; }

    /// <summary>Doubles allowed.</summary>
    public int B2 { get; set; }

    /// <summary>Triples allowed.</summary>
    public int B3 { get; set; }

    /// <summary>Home runs allowed.</summary>
    public int Hr { get; set; }

    /// <summary>Grand slams allowed.</summary>
    public int HrSlam { get; set; }

    /// <summary>Walks allowed.</summary>
    public int Bb { get; set; }

    /// <summary>Intentional walks allowed.</summary>
    public int Ibb { get; set; }

    /// <summary>Strikeouts.</summary>
    public int So { get; set; }

    /// <summary>Batters faced.</summary>
    public int Bf { get; set; }

    /// <summary>Balks.</summary>
    public int Bk { get; set; }

    /// <summary>Wild pitches.</summary>
    public int Wp { get; set; }

    /// <summary>Batters hit by a pitch.</summary>
    public int Hb { get; set; }

    /// <summary>Double plays induced.</summary>
    public int Gdp { get; set; }

    /// <summary>Sacrifice hits allowed.</summary>
    public int Sh { get; set; }

    /// <summary>Sacrifice flies allowed.</summary>
    public int Sf { get; set; }

    /// <summary>Times charged with catcher's interference allowed.</summary>
    public int Xi { get; set; }

    /// <summary>Pickoffs.</summary>
    public int Pk { get; set; }

    /// <summary>Inherited runners.</summary>
    public int Inr { get; set; }

    /// <summary>Inherited runners who scored (charged even if this pitcher was relieved before the runner scored).</summary>
    public int Inrs { get; set; }

    /// <summary>The number of "extra batters" faced in an inning without recording an out.</summary>
    public int Xb { get; set; }

    /// <summary>The inning in which <see cref="Xb"/> occurred.</summary>
    public int XbInn { get; set; }

    /// <summary>Ground-ball outs induced.</summary>
    public int Gb { get; set; }

    /// <summary>Fly-ball outs induced.</summary>
    public int Fb { get; set; }

    /// <summary>Pitches thrown.</summary>
    public int Pitches { get; set; }

    /// <summary>Strikes thrown.</summary>
    public int Strikes { get; set; }

    /// <summary>Whether this pitcher is credited with the win.</summary>
    public bool W { get; set; }

    /// <summary>Whether this pitcher is charged with the loss.</summary>
    public bool L { get; set; }

    /// <summary>Whether this pitcher is credited with a save.</summary>
    public bool Sv { get; set; }
}
