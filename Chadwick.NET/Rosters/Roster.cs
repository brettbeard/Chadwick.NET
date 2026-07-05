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
/// One team's roster for a season: its identity as listed in the season's team file, plus the
/// players read from its <c>.ROS</c> file (if one was available).
/// </summary>
public sealed class Roster
{
    private readonly List<Player> _players = new();

    /// <summary>
    /// The team's Retrosheet ID (e.g. <c>BAL</c>).
    /// </summary>
    public required string TeamId { get; init; }

    /// <summary>
    /// The season year this roster applies to.
    /// </summary>
    public int Year { get; init; }

    /// <summary>
    /// The league the team belongs to (e.g. <c>A</c> or <c>N</c>).
    /// </summary>
    public required string League { get; init; }

    /// <summary>
    /// The city the team represents.
    /// </summary>
    public required string City { get; init; }

    /// <summary>
    /// The team's nickname.
    /// </summary>
    public required string Nickname { get; init; }

    /// <summary>
    /// The players on this roster, in the order they were read from the roster file.
    /// </summary>
    public IReadOnlyList<Player> Players => _players;

    /// <summary>
    /// Adds a player to the roster.
    /// </summary>
    /// <param name="player">The player to add.</param>
    public void AddPlayer(Player player)
    {
        ArgumentNullException.ThrowIfNull(player);
        _players.Add(player);
    }

    /// <summary>
    /// Finds the player with the given ID, or <see langword="null"/> if none is on this roster.
    /// </summary>
    /// <param name="playerId">The player's Retrosheet ID.</param>
    public Player? FindPlayer(string playerId)
    {
        return _players.FirstOrDefault(player => player.PlayerId == playerId);
    }

    /// <summary>
    /// The batting hand of the player with the given ID, or <c>?</c> if the player is not on
    /// this roster.
    /// </summary>
    /// <param name="playerId">The player's Retrosheet ID.</param>
    public char GetBattingHand(string playerId) => FindPlayer(playerId)?.BattingHand ?? '?';

    /// <summary>
    /// The throwing hand of the player with the given ID, or <c>?</c> if the player is not on
    /// this roster.
    /// </summary>
    /// <param name="playerId">The player's Retrosheet ID.</param>
    public char GetThrowingHand(string playerId) => FindPlayer(playerId)?.ThrowingHand ?? '?';
}
