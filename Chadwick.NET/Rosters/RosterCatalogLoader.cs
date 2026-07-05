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

using Chadwick.Core.FileSources;

namespace Chadwick.Core.Rosters;

/// <summary>
/// Loads a full season's <see cref="League"/> from an <see cref="IRetrosheetFileSource"/>,
/// reading the team file first and then each team's roster file - mirroring Chadwick's own
/// driver order. A team missing its <c>.ROS</c> file is tolerated; its roster is simply left
/// with no players.
/// </summary>
public static class RosterCatalogLoader
{
    /// <summary>
    /// Loads the league for <paramref name="year"/> from <paramref name="fileSource"/>.
    /// </summary>
    /// <param name="fileSource">The source to read the team and roster files from.</param>
    /// <param name="year">The season year to load.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public static async Task<League> LoadAsync(IRetrosheetFileSource fileSource, int year, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(fileSource);

        var teamFileName = RetrosheetFileNaming.TeamFileName(year);
        League league;
        await using (var teamFileStream = await fileSource.OpenFileAsync(teamFileName, cancellationToken))
        {
            league = await LeagueFileReader.ReadAsync(teamFileStream, year, cancellationToken);
        }

        var availableFileNames = await fileSource.GetFileNamesAsync(cancellationToken);

        foreach (var roster in league.Rosters)
        {
            var rosterFileName = RetrosheetFileNaming.RosterFileName(roster.TeamId, year);
            var matchedFileName = availableFileNames.FirstOrDefault(name => string.Equals(name, rosterFileName, StringComparison.OrdinalIgnoreCase));
            if (matchedFileName is null)
            {
                continue; // missing .ROS file is tolerated, matching the original C driver
            }

            await using var rosterFileStream = await fileSource.OpenFileAsync(matchedFileName, cancellationToken);
            await RosterFileReader.PopulatePlayersAsync(roster, rosterFileStream, cancellationToken);
        }

        return league;
    }
}
