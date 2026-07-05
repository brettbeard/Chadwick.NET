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

using Chadwick.Core.Parsing;

namespace Chadwick.Core.Rosters;

/// <summary>
/// Reads a Retrosheet roster file (<c>{team}{yyyy}.ROS</c>) and populates a <see cref="Roster"/>
/// with the players it lists - one line per player, in the form
/// <c>player_id,last_name,first_name,bats,throws</c> (some files add a team ID and position as
/// trailing fields, which are captured if present).
/// </summary>
public static class RosterFileReader
{
    /// <summary>
    /// Reads all player entries from <paramref name="stream"/> and adds them to
    /// <paramref name="roster"/>.
    /// </summary>
    /// <param name="roster">The roster to populate.</param>
    /// <param name="stream">The open roster file stream.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public static async Task PopulatePlayersAsync(Roster roster, Stream stream, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(roster);
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            var fields = RetrosheetLineTokenizer.Tokenize(line);
            if (fields.Count < 5)
            {
                continue; // blank or malformed line; skip, matching cw_roster_read's tolerance
            }

            roster.AddPlayer(new Player
            {
                PlayerId = fields[0],
                LastName = fields[1],
                FirstName = fields[2],
                BattingHand = RetrosheetValueParser.ParseHandCode(fields[3]),
                ThrowingHand = RetrosheetValueParser.ParseHandCode(fields[4]),
                TeamId = fields.Count > 5 ? fields[5] : null,
                Position = fields.Count > 6 ? fields[6] : null,
            });
        }
    }
}
