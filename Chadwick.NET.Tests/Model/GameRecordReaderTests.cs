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

using System.Text;
using Chadwick.Core.Scorebook;

namespace Chadwick.NET.Tests.Model;

[TestClass]
public sealed class GameRecordReaderTests
{
    private const string SingleGameEventFile =
        "id,TST200104150\n" +
        "version,2\n" +
        "info,visteam,VIS\n" +
        "info,hometeam,HOM\n" +
        "info,date,2001/04/15\n" +
        "start,pid01,\"Player One\",0,1,7\n" +
        "start,pid11,\"Home One\",1,1,8\n" +
        "play,1,0,pid01,??,,S7\n" +
        "sub,pid02,\"Player Two\",0,1,11\n" +
        "com,\"pinch hitter announced\"\n" +
        "data,er,pid09,0\n";

    private static async Task<Chadwick.Core.Model.Game> ReadSingleGameAsync(string text)
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        var games = await ScorebookReader.ReadAllGamesAsync(stream, CancellationToken.None);
        Assert.HasCount(1, games);
        return games[0];
    }

    [TestMethod]
    public async Task ReadAllGames_ParsesBasicGameMetadata()
    {
        var game = await ReadSingleGameAsync(SingleGameEventFile);

        Assert.AreEqual("TST200104150", game.GameId);
        Assert.AreEqual("2", game.Version);
        Assert.AreEqual("VIS", game.GetInfo("visteam"));
        Assert.AreEqual("HOM", game.GetInfo("hometeam"));
    }

    [TestMethod]
    public async Task ReadAllGames_ParsesStartersAndEvents()
    {
        var game = await ReadSingleGameAsync(SingleGameEventFile);

        Assert.HasCount(2, game.Starters);
        Assert.AreEqual("pid01", game.Starters[0].PlayerId);
        Assert.AreEqual(7, game.Starters[0].Position);

        Assert.HasCount(1, game.Events);
        Assert.AreEqual("pid01", game.Events[0].Batter);
        Assert.AreEqual("S7", game.Events[0].PlayText);
    }

    [TestMethod]
    public async Task ReadAllGames_AttachesSubstitutionsAndCommentsToPrecedingEvent()
    {
        var game = await ReadSingleGameAsync(SingleGameEventFile);

        Assert.HasCount(1, game.Events[0].Substitutions);
        Assert.AreEqual("pid02", game.Events[0].Substitutions[0].PlayerId);

        Assert.HasCount(1, game.Events[0].Comments);
        Assert.AreEqual("pinch hitter announced", game.Events[0].Comments[0].Text);
    }

    [TestMethod]
    public async Task ReadAllGames_ParsesDataRecords()
    {
        var game = await ReadSingleGameAsync(SingleGameEventFile);

        Assert.HasCount(1, game.DataRecords);
        CollectionAssert.AreEqual(new[] { "er", "pid09", "0" }, game.DataRecords[0].Values.ToArray());
    }

    [TestMethod]
    public async Task ReadAllGames_ReadsMultipleGamesFromOneFile()
    {
        var text = SingleGameEventFile + "id,TST200104160\nversion,2\ninfo,visteam,VIS\n";

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        var games = await ScorebookReader.ReadAllGamesAsync(stream, CancellationToken.None);

        Assert.HasCount(2, games);
        Assert.AreEqual("TST200104150", games[0].GameId);
        Assert.AreEqual("TST200104160", games[1].GameId);
    }

    [TestMethod]
    public async Task ReadAllGames_SkipsLeadingCommentsBeforeFirstGame()
    {
        var text = "com,\"leading comment before any game\"\n" + SingleGameEventFile;

        var game = await ReadSingleGameAsync(text);

        Assert.AreEqual("TST200104150", game.GameId);
    }
}
