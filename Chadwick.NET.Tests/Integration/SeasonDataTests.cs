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
using Chadwick.Core.Parsing;
using Chadwick.Core.Rosters;
using Chadwick.Core.Scorebook;
using Chadwick.Core.Simulation;

namespace Chadwick.NET.Tests.Integration;

/// <summary>
/// End-to-end regression tests against the real Retrosheet season sample data checked into
/// <c>data/</c> (e.g. <c>1968eve.zip</c>). These are skipped (rather than failed) if a given
/// season's data file cannot be found, so the suite still runs cleanly in environments that
/// don't have the repository's sample data checked out.
/// </summary>
[TestClass]
public sealed class SeasonDataTests
{
    private static string? FindDataDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "data");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static string? FindSeasonZipPath(int year)
    {
        var dataDirectory = FindDataDirectory();
        if (dataDirectory is null)
        {
            return null;
        }

        var candidate = Path.Combine(dataDirectory, $"{year}eve.zip");
        return File.Exists(candidate) ? candidate : null;
    }

    /// <summary>
    /// The total number of games expected for each season, independently verified by counting
    /// <c>id,</c> records across all of that season's event files (each game appears exactly
    /// once, in its home team's file) - not derived from this project's own parsing code.
    /// </summary>
    private static readonly Dictionary<int, int> ExpectedGameCountsBySeason = new()
    {
        [1967] = 1617,
        [1968] = 1610,
    };

    /// <summary>
    /// Yields every season year this test class knows about, regardless of whether that
    /// season's data file is actually present. MSTest's <c>[DynamicData]</c> fails the test
    /// outright if the source yields zero rows (rather than skipping), so this must never come
    /// back empty - each test method checks for the file itself and calls
    /// <see cref="Assert.Inconclusive(string)"/> if it's missing.
    /// </summary>
    public static IEnumerable<object[]> AllConfiguredSeasons()
    {
        foreach (var year in ExpectedGameCountsBySeason.Keys)
        {
            yield return [year];
        }
    }

    [TestMethod]
    [DynamicData(nameof(AllConfiguredSeasons))]
    public async Task Season_EveryPlayParsesSuccessfully(int year)
    {
        var zipPath = FindSeasonZipPath(year);
        if (zipPath is null)
        {
            Assert.Inconclusive($"Sample data 'data/{year}eve.zip' was not found; skipping season regression test.");
            return;
        }

        using IRetrosheetFileSource fileSource = new ZipFileSource(zipPath);
        var fileNames = await fileSource.GetFileNamesAsync(CancellationToken.None);
        var eventFileNames = fileNames.Where(name => name.EndsWith(".EVN") || name.EndsWith(".EVA")).ToList();
        Assert.IsNotEmpty(eventFileNames);

        var totalPlays = 0;
        foreach (var eventFileName in eventFileNames)
        {
            await using var stream = await fileSource.OpenFileAsync(eventFileName, CancellationToken.None);
            var games = await ScorebookReader.ReadAllGamesAsync(stream, CancellationToken.None);

            foreach (var game in games)
            {
                foreach (var gameEvent in game.Events)
                {
                    if (gameEvent.PlayText == "NP")
                    {
                        continue;
                    }

                    totalPlays++;
                    Assert.IsTrue(PlayStringParser.TryParse(gameEvent.PlayText, out _), $"Failed to parse '{gameEvent.PlayText}' in {game.GameId}.");
                }
            }
        }

        Assert.IsGreaterThan(50_000, totalPlays);
    }

    [TestMethod]
    [DynamicData(nameof(AllConfiguredSeasons))]
    public async Task Season_EveryGameIteratesAndBuildsABoxscoreWithoutError(int year)
    {
        var zipPath = FindSeasonZipPath(year);
        if (zipPath is null)
        {
            Assert.Inconclusive($"Sample data 'data/{year}eve.zip' was not found; skipping season regression test.");
            return;
        }

        using IRetrosheetFileSource fileSource = new ZipFileSource(zipPath);
        var fileNames = await fileSource.GetFileNamesAsync(CancellationToken.None);
        var eventFileNames = fileNames.Where(name => name.EndsWith(".EVN") || name.EndsWith(".EVA")).ToList();

        var totalGames = 0;
        foreach (var eventFileName in eventFileNames)
        {
            await using var stream = await fileSource.OpenFileAsync(eventFileName, CancellationToken.None);
            var games = await ScorebookReader.ReadAllGamesAsync(stream, CancellationToken.None);

            foreach (var game in games)
            {
                var iterator = new GameIterator(game);
                while (iterator.CurrentEvent is not null)
                {
                    iterator.Next();
                }

                BoxscoreBuilder.Create(game);
                totalGames++;
            }
        }

        Assert.AreEqual(ExpectedGameCountsBySeason[year], totalGames);
    }

    [TestMethod]
    public async Task KnownGame_BaltimoreOpeningDay1968_ProducesTheHistoricalFinalScore()
    {
        var zipPath = FindSeasonZipPath(1968);
        if (zipPath is null)
        {
            Assert.Inconclusive("Sample data 'data/1968eve.zip' was not found; skipping season regression test.");
            return;
        }

        using IRetrosheetFileSource fileSource = new ZipFileSource(zipPath);
        var league = await RosterCatalogLoader.LoadAsync(fileSource, 1968, CancellationToken.None);

        await using var stream = await fileSource.OpenFileAsync("1968BAL.EVA", CancellationToken.None);
        var games = await ScorebookReader.ReadAllGamesAsync(stream, CancellationToken.None);
        var game = games.Single(g => g.GameId == "BAL196804100");

        var boxscore = BoxscoreBuilder.Create(game);

        // Oakland 1, Baltimore 3 - opening day, April 10, 1968, per Retrosheet.
        Assert.AreEqual(1, boxscore.Score[0]);
        Assert.AreEqual(3, boxscore.Score[1]);
        Assert.AreEqual("OAK", game.GetInfo("visteam"));
        Assert.AreEqual("BAL", game.GetInfo("hometeam"));

        var winningPitcher = league.FindRoster("BAL")?.FindPlayer(game.GetInfo("wp") ?? "");
        Assert.IsNotNull(winningPitcher);
        Assert.AreEqual("Phoebus", winningPitcher.LastName);
    }

    [TestMethod]
    public async Task KnownGame_Baltimore1967OpeningSeries_ParsesWithConsistentLineScore()
    {
        var zipPath = FindSeasonZipPath(1967);
        if (zipPath is null)
        {
            Assert.Inconclusive("Sample data 'data/1967eve.zip' was not found; skipping season regression test.");
            return;
        }

        using IRetrosheetFileSource fileSource = new ZipFileSource(zipPath);
        await using var stream = await fileSource.OpenFileAsync("1967BAL.EVA", CancellationToken.None);
        var games = await ScorebookReader.ReadAllGamesAsync(stream, CancellationToken.None);
        var game = games.Single(g => g.GameId == "BAL196704110");

        Assert.AreEqual("MIN", game.GetInfo("visteam"));
        Assert.AreEqual("BAL", game.GetInfo("hometeam"));
        Assert.AreEqual("1967/04/11", game.GetInfo("date"));

        var boxscore = BoxscoreBuilder.Create(game);

        // Cross-check that the boxscore's own linescore totals reconcile with its final score -
        // an internal consistency check independent of any external "known result."
        var summedAwayRuns = 0;
        var summedHomeRuns = 0;
        for (var inning = 1; inning < 50; inning++)
        {
            summedAwayRuns += boxscore.GetLineScore(inning, 0) ?? 0;
            summedHomeRuns += boxscore.GetLineScore(inning, 1) ?? 0;
        }

        Assert.AreEqual(boxscore.Score[0], summedAwayRuns);
        Assert.AreEqual(boxscore.Score[1], summedHomeRuns);
    }
}
