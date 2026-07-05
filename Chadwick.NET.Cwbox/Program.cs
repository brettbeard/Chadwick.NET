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

using Chadwick.Core.Boxscores;
using Chadwick.Core.FileSources;
using Chadwick.Core.Rosters;
using Chadwick.Core.Scorebook;
using Chadwick.NET.Cwbox;

if (args.Length < 3)
{
    Console.Error.WriteLine("Usage: cwbox <zip-file-or-directory> <year> <game-id>");
    Console.Error.WriteLine("Example: cwbox data/1968eve.zip 1968 BAL196804100");
    return 1;
}

var path = args[0];
if (!int.TryParse(args[1], out var year))
{
    Console.Error.WriteLine($"'{args[1]}' is not a valid year.");
    return 1;
}

var gameId = args[2];

using IRetrosheetFileSource fileSource = Directory.Exists(path) ? new DirectoryFileSource(path) : new ZipFileSource(path);

var league = await RosterCatalogLoader.LoadAsync(fileSource, year, CancellationToken.None);

if (gameId.Length < 3)
{
    Console.Error.WriteLine($"'{gameId}' is not a valid game ID.");
    return 1;
}

var homeTeamId = gameId[..3];
var homeTeamRoster = league.FindRoster(homeTeamId);
if (homeTeamRoster is null)
{
    Console.Error.WriteLine($"Unknown home team '{homeTeamId}' for game '{gameId}'.");
    return 1;
}

// Retrosheet event files are named for the home team, with an extension (.EVN/.EVA) matching
// that team's league.
var eventFileName = $"{year}{homeTeamId}.EV{homeTeamRoster.League}";
await using var eventFileStream = await fileSource.OpenFileAsync(eventFileName, CancellationToken.None);
var games = await ScorebookReader.ReadAllGamesAsync(eventFileStream, CancellationToken.None);

var game = games.FirstOrDefault(g => g.GameId == gameId);
if (game is null)
{
    Console.Error.WriteLine($"Game '{gameId}' was not found in '{eventFileName}'.");
    return 1;
}

var visitorRoster = league.FindRoster(game.GetInfo("visteam") ?? "");
var homeRoster = league.FindRoster(game.GetInfo("hometeam") ?? "");

var boxscore = BoxscoreBuilder.Create(game);
BoxscoreTextRenderer.Render(Console.Out, game, boxscore, visitorRoster, homeRoster);

return 0;
