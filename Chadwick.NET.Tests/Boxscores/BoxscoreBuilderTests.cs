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

namespace Chadwick.NET.Tests.Boxscores;

[TestClass]
public sealed class BoxscoreBuilderTests
{
    [TestMethod]
    public async Task Create_FinalScoreMatchesExpectedResult()
    {
        var game = await TestFixtures.LoadMiniGameAsync();

        var boxscore = BoxscoreBuilder.Create(game);

        Assert.AreEqual(0, boxscore.Score[0]);
        Assert.AreEqual(1, boxscore.Score[1]);
        Assert.AreEqual(0, boxscore.Hits[0]);
        Assert.AreEqual(1, boxscore.Hits[1]);
    }

    [TestMethod]
    public async Task Create_CreditsTheHomeRunToTheBatterAndPitcher()
    {
        var game = await TestFixtures.LoadMiniGameAsync();

        var boxscore = BoxscoreBuilder.Create(game);

        var h1 = boxscore.FindPlayer("h1", battersOnly: true);
        Assert.IsNotNull(h1);
        Assert.AreEqual(1, h1.Batting.Ab);
        Assert.AreEqual(1, h1.Batting.R);
        Assert.AreEqual(1, h1.Batting.H);
        Assert.AreEqual(1, h1.Batting.Hr);
        Assert.AreEqual(1, h1.Batting.Bi);

        var v1Pitcher = boxscore.FindPitcher("v1");
        Assert.IsNotNull(v1Pitcher);
        Assert.AreEqual(1, v1Pitcher.Pitching.H);
        Assert.AreEqual(1, v1Pitcher.Pitching.R);
        Assert.AreEqual(1, v1Pitcher.Pitching.Er);
        Assert.AreEqual(4, v1Pitcher.Pitching.Bf);
        Assert.AreEqual(3, v1Pitcher.Pitching.Outs);
    }

    [TestMethod]
    public async Task Create_HomePitcherAllowsNothing()
    {
        var game = await TestFixtures.LoadMiniGameAsync();

        var boxscore = BoxscoreBuilder.Create(game);

        var h1Pitcher = boxscore.FindPitcher("h1");
        Assert.IsNotNull(h1Pitcher);
        Assert.AreEqual(0, h1Pitcher.Pitching.H);
        Assert.AreEqual(0, h1Pitcher.Pitching.R);
        Assert.AreEqual(3, h1Pitcher.Pitching.Outs);
        Assert.AreEqual(3, h1Pitcher.Pitching.Bf);
    }

    [TestMethod]
    public async Task Create_RecordsHomeRunInTheHomeRunList()
    {
        var game = await TestFixtures.LoadMiniGameAsync();

        var boxscore = BoxscoreBuilder.Create(game);

        Assert.HasCount(1, boxscore.HomeRuns);
        Assert.AreEqual("h1", boxscore.HomeRuns[0].Players[0]);
        Assert.AreEqual("v1", boxscore.HomeRuns[0].Players[1]);
    }

    [TestMethod]
    public async Task Create_NoRunnersLeftOnBase()
    {
        var game = await TestFixtures.LoadMiniGameAsync();

        var boxscore = BoxscoreBuilder.Create(game);

        Assert.AreEqual(0, boxscore.LeftOnBase[0]);
        Assert.AreEqual(0, boxscore.LeftOnBase[1]);
    }

    /// <summary>
    /// The mini game's top of the first with pitch data, covering the three ways a pitch string
    /// relates to the records before it, and a mid-plate-appearance pitching change:
    /// <list type="bullet">
    /// <item>v1: one record whose period marks a play with no record of its own (the shape of
    /// the Retrosheet spec's <c>C1FBB.BFB</c> example). All seven pitches are new.</item>
    /// <item>v2: two pitches to h1, a pitching change, then two more to h10. The second record
    /// repeats the first's pitches, so only <c>FX</c> is new and goes to h10.</item>
    /// <item>v3: a no-play record, then the strikeout record repeating it. Four pitches, not
    /// seven.</item>
    /// </list>
    /// </summary>
    private static readonly string PitchCountGameEventFileText = TestFixtures.MiniGameEventFileText
        .Replace("play,1,0,v1,??,,8\n", "play,1,0,v1,32,C1FBB.BFX,8\n")
        .Replace("play,1,0,v2,??,,43\n",
            "play,1,0,v2,11,CB,NP\n" +
            "sub,h10,\"Home Ten\",1,1,1\n" +
            "play,1,0,v2,11,CB.FX,43\n")
        .Replace("play,1,0,v3,??,,K\n",
            "play,1,0,v3,12,CFB,NP\n" +
            "play,1,0,v3,12,CFB.S,K\n")
        .Replace("play,1,1,h1,??,,HR\n", "play,1,1,h10,??,,HR\n");

    [TestMethod]
    public async Task Create_CountsEveryPitchOfAPlateAppearanceExactlyOnce()
    {
        var game = await TestFixtures.LoadGameAsync(PitchCountGameEventFileText);

        var boxscore = BoxscoreBuilder.Create(game);

        // Batters: v1 saw seven pitches (the C original's last-period rule counts three), v2 four,
        // v3 four.
        Assert.AreEqual(7, boxscore.FindPlayer("v1", battersOnly: true)!.Batting.Pitches);
        Assert.AreEqual(4, boxscore.FindPlayer("v2", battersOnly: true)!.Batting.Pitches);
        Assert.AreEqual(4, boxscore.FindPlayer("v3", battersOnly: true)!.Batting.Pitches);
        Assert.AreEqual(3, boxscore.FindPlayer("v3", battersOnly: true)!.Batting.Strikes);

        // Pitchers: h1 threw v1's seven and v2's first two; h10 threw v2's last two and v3's four.
        Assert.AreEqual(9, boxscore.FindPitcher("h1")!.Pitching.Pitches);
        Assert.AreEqual(6, boxscore.FindPitcher("h10")!.Pitching.Pitches);
        Assert.AreEqual(15, boxscore.FindPitcher("h1")!.Pitching.Pitches + boxscore.FindPitcher("h10")!.Pitching.Pitches);
    }

    [TestMethod]
    public async Task Create_ThrowsForAGameWithNoEvents()
    {
        var game = await TestFixtures.LoadMiniGameAsync();
        var emptyGame = new Chadwick.Core.Model.Game { GameId = game.GameId };

        Assert.ThrowsExactly<NotSupportedException>(() => BoxscoreBuilder.Create(emptyGame));
    }
}
