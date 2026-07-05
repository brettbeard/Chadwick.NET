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
/// A notable event to be listed in a boxscore's footer (an extra-base hit, stolen base, wild
/// pitch, double play, etc.). Which players appear in <see cref="Players"/>, and what they mean,
/// depends on the list this event belongs to (e.g. for a double, the batter and pitcher; for a
/// stolen base, the runner, pitcher, and catcher).
/// </summary>
public sealed class BoxEvent
{
    private readonly List<string> _players = [];

    /// <summary>The players involved, in an order specific to this event's list.</summary>
    public IReadOnlyList<string> Players => _players;

    /// <summary>Adds a player to this event.</summary>
    public void AddPlayer(string playerId) => _players.Add(playerId);

    /// <summary>The inning the event occurred in.</summary>
    public int Inning { get; set; }

    /// <summary>The batting team when the event occurred: 0 visiting, 1 home.</summary>
    public int HalfInning { get; set; }

    /// <summary>
    /// Event-specific context, such as which base a stolen base/caught stealing/pickoff involved,
    /// or how many runners scored on a home run. <see langword="null"/> if not applicable.
    /// </summary>
    public int? Runners { get; set; }

    /// <summary>For caught-stealing and pickoff events, the fielder who applied the tag, or <see langword="null"/> if not applicable.</summary>
    public int? Pickoff { get; set; }

    /// <summary>For a home run, the number of outs when it was hit. <see langword="null"/> if not applicable.</summary>
    public int? Outs { get; set; }

    /// <summary>For a home run, the hit-location code. Empty if not applicable.</summary>
    public string Location { get; set; } = "";

    /// <summary>
    /// General-purpose scratch flag used by boxscore renderers to track which events have
    /// already been printed while grouping related events together - not meaningful outside of
    /// rendering.
    /// </summary>
    public bool Mark { get; set; }
}
