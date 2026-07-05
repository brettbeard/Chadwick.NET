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
using Chadwick.Core.Schedules;

namespace Chadwick.NET.Tests.Integration;

/// <summary>
/// End-to-end regression tests for schedule-file parsing against the real 1968 season schedule
/// checked into <c>data/schedules/1968SKED.zip</c>. Skipped (rather than failed) if that file
/// cannot be found.
/// </summary>
[TestClass]
public sealed class ScheduleDataTests
{
    private static string? FindScheduleZipPath()
    {
        return TestDataLocator.FindDataFile(Path.Combine("schedules", "1968SKED.zip"));
    }

    [TestMethod]
    public async Task Schedule1968_ParsesTheFullSeasonFromTheZipArchive()
    {
        var zipPath = FindScheduleZipPath();
        if (zipPath is null)
        {
            Assert.Inconclusive("Sample data 'data/schedules/1968SKED.zip' was not found; skipping schedule regression test.");
            return;
        }

        using IRetrosheetFileSource fileSource = new ZipFileSource(zipPath);
        var fileNames = await fileSource.GetFileNamesAsync(CancellationToken.None);
        var scheduleFileName = fileNames.Single(name => name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase));

        await using var stream = await fileSource.OpenFileAsync(scheduleFileName, CancellationToken.None);
        var scheduledGames = await ScheduleFileReader.ReadAllGamesAsync(stream, CancellationToken.None);

        // 1968's schedule file has 1621 rows including its header, so 1620 scheduled games.
        Assert.HasCount(1620, scheduledGames);
    }

    [TestMethod]
    public async Task Schedule1968_ZipAndManuallyExtractedFileProduceIdenticalResults()
    {
        var zipPath = FindScheduleZipPath();
        if (zipPath is null)
        {
            Assert.Inconclusive("Sample data 'data/schedules/1968SKED.zip' was not found; skipping schedule regression test.");
            return;
        }

        List<ScheduledGame> gamesFromZip;
        using (IRetrosheetFileSource zipSource = new ZipFileSource(zipPath))
        {
            var fileNames = await zipSource.GetFileNamesAsync(CancellationToken.None);
            var scheduleFileName = fileNames.Single(name => name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase));
            await using var zipStream = await zipSource.OpenFileAsync(scheduleFileName, CancellationToken.None);
            gamesFromZip = (await ScheduleFileReader.ReadAllGamesAsync(zipStream, CancellationToken.None)).ToList();
        }

        var extractedDirectory = Path.Combine(Path.GetTempPath(), $"chadwicknet-schedule-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(extractedDirectory);
        try
        {
            System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractedDirectory);

            List<ScheduledGame> gamesFromDirectory;
            using (IRetrosheetFileSource directorySource = new DirectoryFileSource(extractedDirectory))
            {
                var fileNames = await directorySource.GetFileNamesAsync(CancellationToken.None);
                var scheduleFileName = fileNames.Single(name => name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase));
                await using var directoryStream = await directorySource.OpenFileAsync(scheduleFileName, CancellationToken.None);
                gamesFromDirectory = (await ScheduleFileReader.ReadAllGamesAsync(directoryStream, CancellationToken.None)).ToList();
            }

            Assert.HasCount(gamesFromZip.Count, gamesFromDirectory);
            for (var i = 0; i < gamesFromZip.Count; i++)
            {
                Assert.AreEqual(gamesFromZip[i].Date, gamesFromDirectory[i].Date);
                Assert.AreEqual(gamesFromZip[i].VisitingTeamId, gamesFromDirectory[i].VisitingTeamId);
                Assert.AreEqual(gamesFromZip[i].HomeTeamId, gamesFromDirectory[i].HomeTeamId);
                CollectionAssert.AreEqual(gamesFromZip[i].PostponementNotes.ToArray(), gamesFromDirectory[i].PostponementNotes.ToArray());
                CollectionAssert.AreEqual(gamesFromZip[i].MakeupDates.ToArray(), gamesFromDirectory[i].MakeupDates.ToArray());
            }
        }
        finally
        {
            Directory.Delete(extractedDirectory, recursive: true);
        }
    }

    [TestMethod]
    public async Task Schedule1968_ContainsTheKnownMartinLutherKingFuneralPostponement()
    {
        var zipPath = FindScheduleZipPath();
        if (zipPath is null)
        {
            Assert.Inconclusive("Sample data 'data/schedules/1968SKED.zip' was not found; skipping schedule regression test.");
            return;
        }

        using IRetrosheetFileSource fileSource = new ZipFileSource(zipPath);
        var fileNames = await fileSource.GetFileNamesAsync(CancellationToken.None);
        var scheduleFileName = fileNames.Single(name => name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase));

        await using var stream = await fileSource.OpenFileAsync(scheduleFileName, CancellationToken.None);
        var scheduledGames = await ScheduleFileReader.ReadAllGamesAsync(stream, CancellationToken.None);

        var game = scheduledGames.Single(g =>
            g.Date == new DateOnly(1968, 4, 8) && g.VisitingTeamId == "CHN" && g.HomeTeamId == "CIN");

        Assert.IsTrue(game.WasPostponed);
        CollectionAssert.AreEqual(new[] { "Funeral of Martin Luther King Jr." }, game.PostponementNotes.ToArray());
        CollectionAssert.AreEqual(new[] { new DateOnly(1968, 4, 11) }, game.MakeupDates.ToArray());

        var doublyPostponedGame = scheduledGames.Single(g =>
            g.Date == new DateOnly(1968, 4, 11) && g.VisitingTeamId == "OAK" && g.HomeTeamId == "BAL");

        CollectionAssert.AreEqual(
            new[] { new DateOnly(1968, 6, 9), new DateOnly(1968, 8, 27) },
            doublyPostponedGame.MakeupDates.ToArray());
    }
}
