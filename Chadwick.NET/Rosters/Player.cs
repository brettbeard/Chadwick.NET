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
/// A single player as listed in a Retrosheet roster (<c>.ROS</c>) file.
/// </summary>
public sealed class Player
{
    /// <summary>
    /// The player's unique Retrosheet ID (e.g. <c>blaip101</c>).
    /// </summary>
    public required string PlayerId { get; init; }

    /// <summary>
    /// The player's last name.
    /// </summary>
    public required string LastName { get; init; }

    /// <summary>
    /// The player's first name.
    /// </summary>
    public required string FirstName { get; init; }

    /// <summary>
    /// The player's batting hand: <c>L</c>, <c>R</c>, <c>B</c> (switch), or <c>?</c> if unknown.
    /// </summary>
    public char BattingHand { get; init; } = '?';

    /// <summary>
    /// The player's throwing hand: <c>L</c>, <c>R</c>, or <c>?</c> if unknown.
    /// </summary>
    public char ThrowingHand { get; init; } = '?';

    /// <summary>
    /// The team ID this roster entry lists the player under, if the source roster file included
    /// it as a trailing field. Not all Retrosheet roster files include this.
    /// </summary>
    public string? TeamId { get; init; }

    /// <summary>
    /// The player's primary position, if the source roster file included it as a trailing field.
    /// Not all Retrosheet roster files include this.
    /// </summary>
    public string? Position { get; init; }
}
