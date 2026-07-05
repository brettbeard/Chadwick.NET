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
/// A complete traditional boxscore for one game: batting, pitching, and fielding lines for every
/// player who appeared, the line score, and the lists of notable events (extra-base hits,
/// stolen bases, double plays, etc.) that a boxscore's footer reports.
/// </summary>
/// <remarks>
/// Where Chadwick's <c>CWBoxscore</c> stores each batting-order slot and each team's pitching
/// staff as a backwards-linked list (walking <c>prev</c> pointers from the current occupant back
/// to the starter), this stores each as a <c>List&lt;T&gt;</c> in chronological order (the
/// starter first, the current occupant last) - equivalent history, without exposing raw links as
/// public API, per this port's domain-model design.
/// </remarks>
public sealed class Boxscore
{
    private readonly List<BoxPlayer>[,] _slots = new List<BoxPlayer>[10, 2];
    private readonly List<BoxPitcher>[] _pitchers = [[], []];
    private readonly int?[,] _lineScore = new int?[50, 2];

    /// <summary>Cumulative runs scored, indexed by team (0 visiting, 1 home).</summary>
    public int[] Score { get; } = new int[2];

    /// <summary>Cumulative hits, indexed by team.</summary>
    public int[] Hits { get; } = new int[2];

    /// <summary>Cumulative errors charged, indexed by team.</summary>
    public int[] Errors { get; } = new int[2];

    /// <summary>Cumulative double plays turned by each team's defense, indexed by team.</summary>
    public int[] DoublePlays { get; } = new int[2];

    /// <summary>Cumulative triple plays turned by each team's defense, indexed by team.</summary>
    public int[] TriplePlays { get; } = new int[2];

    /// <summary>Runners left on base, indexed by team.</summary>
    public int[] LeftOnBase { get; } = new int[2];

    /// <summary>Earned runs allowed, indexed by team's pitching staff.</summary>
    public int[] EarnedRuns { get; } = new int[2];

    /// <summary>At-bats with a runner in scoring position, indexed by team.</summary>
    public int[] RispAtBats { get; } = new int[2];

    /// <summary>Hits with a runner in scoring position, indexed by team.</summary>
    public int[] RispHits { get; } = new int[2];

    /// <summary>
    /// The number of outs recorded when the game ended. Less than 3 means the game either was
    /// called mid-inning, or ended on a walk-off (see <see cref="WalkOff"/>).
    /// </summary>
    public int OutsAtEnd { get; set; }

    /// <summary>Whether the game ended on a walk-off (the home team took the lead in its last at-bat, ending the game before three outs).</summary>
    public bool WalkOff { get; set; }

    /// <summary>Doubles hit, in chronological order.</summary>
    public List<BoxEvent> Doubles { get; } = [];

    /// <summary>Triples hit, in chronological order.</summary>
    public List<BoxEvent> Triples { get; } = [];

    /// <summary>Home runs hit, in chronological order.</summary>
    public List<BoxEvent> HomeRuns { get; } = [];

    /// <summary>Stolen bases, in chronological order.</summary>
    public List<BoxEvent> StolenBases { get; } = [];

    /// <summary>Caught-stealing plays, in chronological order.</summary>
    public List<BoxEvent> CaughtStealing { get; } = [];

    /// <summary>Pickoffs, in chronological order.</summary>
    public List<BoxEvent> Pickoffs { get; } = [];

    /// <summary>Sacrifice hits, in chronological order.</summary>
    public List<BoxEvent> SacrificeHits { get; } = [];

    /// <summary>Sacrifice flies, in chronological order.</summary>
    public List<BoxEvent> SacrificeFlies { get; } = [];

    /// <summary>Hit-by-pitch events, in chronological order.</summary>
    public List<BoxEvent> HitByPitches { get; } = [];

    /// <summary>Intentional walks, in chronological order.</summary>
    public List<BoxEvent> IntentionalWalks { get; } = [];

    /// <summary>Wild pitches, in chronological order.</summary>
    public List<BoxEvent> WildPitches { get; } = [];

    /// <summary>Balks, in chronological order.</summary>
    public List<BoxEvent> Balks { get; } = [];

    /// <summary>Errors, in chronological order.</summary>
    public List<BoxEvent> FieldingErrors { get; } = [];

    /// <summary>Passed balls, in chronological order.</summary>
    public List<BoxEvent> PassedBalls { get; } = [];

    /// <summary>Double plays, in chronological order.</summary>
    public List<BoxEvent> DoublePlayEvents { get; } = [];

    /// <summary>Triple plays, in chronological order.</summary>
    public List<BoxEvent> TriplePlayEvents { get; } = [];

    /// <summary>Creates an empty boxscore.</summary>
    public Boxscore()
    {
        for (var team = 0; team <= 1; team++)
        {
            for (var slot = 0; slot <= 9; slot++)
            {
                _slots[slot, team] = [];
            }

            for (var inning = 0; inning < 50; inning++)
            {
                _lineScore[inning, team] = null;
            }
        }
    }

    /// <summary>
    /// The runs scored by <paramref name="team"/> in <paramref name="inning"/> (1-based), or
    /// <see langword="null"/> if that inning was not played (e.g. the home team didn't need its
    /// last at-bat).
    /// </summary>
    public int? GetLineScore(int inning, int team) => _lineScore[inning, team];

    /// <summary>Sets the runs scored by <paramref name="team"/> in <paramref name="inning"/> (1-based).</summary>
    public void SetLineScore(int inning, int team, int? runs) => _lineScore[inning, team] = runs;

    /// <summary>
    /// The players who have occupied batting-order <paramref name="slot"/> (0-9; slot 0 is
    /// reserved for a non-batting pitcher when the DH rule is in effect) for <paramref name="team"/>,
    /// in chronological order (starter first, current occupant last).
    /// </summary>
    public List<BoxPlayer> GetSlot(int slot, int team) => _slots[slot, team];

    /// <summary>
    /// <paramref name="team"/>'s pitchers, in the order they entered the game (starter first,
    /// current pitcher last).
    /// </summary>
    public List<BoxPitcher> GetPitchers(int team) => _pitchers[team];

    /// <summary>The starting player in <paramref name="slot"/> for <paramref name="team"/>, or <see langword="null"/> if the slot was never used.</summary>
    public BoxPlayer? GetStarter(int slot, int team)
    {
        var occupants = _slots[slot, team];
        return occupants.Count > 0 ? occupants[0] : null;
    }

    /// <summary>The starting pitcher for <paramref name="team"/>, or <see langword="null"/> if none has been recorded yet.</summary>
    public BoxPitcher? GetStartingPitcher(int team)
    {
        var pitchers = _pitchers[team];
        return pitchers.Count > 0 ? pitchers[0] : null;
    }

    /// <summary>
    /// Finds the boxscore entry for the player with ID <paramref name="playerId"/>, searching
    /// all slots that have ever been occupied (not just current occupants).
    /// </summary>
    /// <param name="playerId">The player's Retrosheet ID.</param>
    /// <param name="battersOnly">
    /// If <see langword="true"/>, searches only batting-order slots 1-9 - implementing the
    /// "Ohtani rule" for a two-way player under the DH: a player who DHs for himself has two
    /// distinct entries in the game, one in the batting order and one as the DH'd-for pitcher
    /// (slot 0).
    /// </param>
    public BoxPlayer? FindPlayer(string? playerId, bool battersOnly)
    {
        if (playerId is null)
        {
            return null;
        }

        for (var team = 0; team <= 1; team++)
        {
            for (var slot = battersOnly ? 1 : 0; slot <= 9; slot++)
            {
                foreach (var player in _slots[slot, team])
                {
                    if (player.PlayerId == playerId)
                    {
                        return player;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Finds the boxscore entry for the player with ID <paramref name="playerId"/> who currently
    /// occupies a slot - unlike <see cref="FindPlayer"/>, this does not match a player who
    /// previously occupied a slot but has since been replaced (matters for illegal, or
    /// sanctioned-but-unorthodox, player re-entry in a different slot).
    /// </summary>
    /// <param name="playerId">The player's Retrosheet ID.</param>
    /// <param name="battingSlotsOnly">If <see langword="true"/>, restricts the search to batting-order slots 1-9.</param>
    public BoxPlayer? FindCurrentPlayer(string? playerId, bool battingSlotsOnly)
    {
        if (playerId is null)
        {
            return null;
        }

        for (var team = 0; team <= 1; team++)
        {
            for (var slot = battingSlotsOnly ? 1 : 0; slot <= 9; slot++)
            {
                var occupants = _slots[slot, team];
                if (occupants.Count > 0 && occupants[^1].PlayerId == playerId)
                {
                    return occupants[^1];
                }
            }
        }

        return null;
    }

    /// <summary>Finds the boxscore entry for the pitcher with ID <paramref name="playerId"/>, searching all pitchers who have ever appeared (not just the current one).</summary>
    public BoxPitcher? FindPitcher(string? playerId)
    {
        if (playerId is null)
        {
            return null;
        }

        for (var team = 0; team <= 1; team++)
        {
            foreach (var pitcher in _pitchers[team])
            {
                if (pitcher.PlayerId == playerId)
                {
                    return pitcher;
                }
            }
        }

        return null;
    }
}
