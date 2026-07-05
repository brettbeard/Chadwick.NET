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
/// One player's entry in a boxscore: their batting line, the positions they played, and their
/// fielding line at each.
/// </summary>
public sealed class BoxPlayer
{
    private readonly BoxFielding?[] _fielding = new BoxFielding?[10];
    private readonly List<int> _positions = [];

    /// <summary>The player's Retrosheet ID.</summary>
    public required string PlayerId { get; init; }

    /// <summary>The player's name.</summary>
    public required string Name { get; init; }

    /// <summary>The date (in <c>yyyymmdd</c> form) this player actually appeared, accounting for games resumed after a suspension on a later date.</summary>
    public string Date { get; set; } = "";

    /// <summary>The player's batting line.</summary>
    public BoxBatting Batting { get; } = new();

    /// <summary>The inning this player entered as a pinch hitter, or 0 if not applicable.</summary>
    public int PhInning { get; set; }

    /// <summary>The inning this player entered as a pinch runner, or 0 if not applicable.</summary>
    public int PrInning { get; set; }

    /// <summary>The position this player started the game at, or <see langword="null"/> if the player was not a starter.</summary>
    public int? StartPosition { get; set; }

    /// <summary>The positions this player played, in the order first played.</summary>
    public IReadOnlyList<int> Positions => _positions;

    /// <summary>Records that this player played <paramref name="position"/>.</summary>
    public void AddPosition(int position) => _positions.Add(position);

    /// <summary>
    /// This player's fielding line at <paramref name="position"/> (1-9), or <see langword="null"/>
    /// if they never played that position.
    /// </summary>
    public BoxFielding? GetFielding(int position) => _fielding[position];

    /// <summary>Sets this player's fielding line at <paramref name="position"/> (1-9).</summary>
    public void SetFielding(int position, BoxFielding fielding) => _fielding[position] = fielding;
}
