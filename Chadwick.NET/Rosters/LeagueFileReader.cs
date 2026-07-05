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
/// Reads a Retrosheet team file (<c>TEAMyyyy</c>) into a <see cref="League"/> of empty team
/// rosters - one line per team, in the form <c>team_id,league,city,nickname</c>.
/// </summary>
public static class LeagueFileReader
{
    /// <summary>
    /// Reads all team entries from <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">The open team file stream.</param>
    /// <param name="year">The season year the team file applies to.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A league containing one roster per team, with no players populated yet.</returns>
    public static async Task<League> ReadAsync(Stream stream, int year, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var league = new League();
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            var fields = RetrosheetLineTokenizer.Tokenize(line);
            if (fields.Count < 4)
            {
                continue; // blank or malformed line; skip, matching cw_league_read's tolerance
            }

            league.AddRoster(new Roster
            {
                TeamId = fields[0],
                League = fields[1],
                City = fields[2],
                Nickname = fields[3],
                Year = year,
            });
        }

        return league;
    }
}
