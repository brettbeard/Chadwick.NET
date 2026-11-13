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
using Chadwick.Core.Simulation;

namespace Chadwick.NET.Tests.Simulation;

[TestClass]
public sealed class GameIteratorTests
{
    [TestMethod]
    public async Task Reset_SetsUpLineupsAndBattingTeamFromHtbf()
    {
        var game = await TestFixtures.LoadMiniGameAsync();
        var iterator = new GameIterator(game);

        Assert.AreEqual(0, iterator.State.BattingTeam); // htbf=false, so the visiting team bats first
        Assert.AreEqual("v1", iterator.State.Lineups[1, 0].PlayerId);
        Assert.AreEqual("h1", iterator.State.Lineups[1, 1].PlayerId);
        Assert.AreEqual("v1", iterator.State.Fielders[1, 0]); // position-1 starter is the pitcher
        Assert.AreEqual("h1", iterator.State.Fielders[1, 1]);
    }

    [TestMethod]
    public async Task Next_FlyOut_RecordsOneOutAndNoScore()
    {
        var game = await TestFixtures.LoadMiniGameAsync();
        var iterator = new GameIterator(game);

        iterator.Next(); // v1 flies out

        Assert.AreEqual(1, iterator.State.Outs);
        Assert.AreEqual(0, iterator.State.Score[0]);
        Assert.AreEqual(0, iterator.State.Score[1]);
    }

    [TestMethod]
    public async Task Next_ThreeOuts_ChangesSidesAndResetsOuts()
    {
        var game = await TestFixtures.LoadMiniGameAsync();
        var iterator = new GameIterator(game);

        iterator.Next(); // v1 out (1)
        iterator.Next(); // v2 out (2)
        iterator.Next(); // v3 out (3) - side retires

        Assert.AreEqual(0, iterator.State.Outs);
        Assert.AreEqual(1, iterator.State.BattingTeam); // now the home team's turn to bat
    }

    [TestMethod]
    public async Task Next_HomeRun_ScoresARunForTheBattingTeam()
    {
        var game = await TestFixtures.LoadMiniGameAsync();
        var iterator = new GameIterator(game);

        iterator.Next(); // v1 out
        iterator.Next(); // v2 out
        iterator.Next(); // v3 out (side retires)
        iterator.Next(); // h1 home run

        Assert.AreEqual(1, iterator.State.Score[1]);
        Assert.AreEqual(0, iterator.State.Outs);
    }

    /// <summary>
    /// A play that does not parse has an unknown outcome, so applying it would move some runners,
    /// silently lose others, and leave every total accumulated afterwards wrong with nothing in
    /// the output to show it. Advancing past one must fail loudly instead.
    /// </summary>
    [TestMethod]
    public async Task Next_UnparseablePlay_ThrowsRatherThanApplyingPartialData()
    {
        var game = await TestFixtures.LoadGameAsync(TestFixtures.MiniGameWithUnparseablePlayEventFileText);
        var iterator = new GameIterator(game);

        iterator.Next(); // v1 out
        iterator.Next(); // v2 out
        iterator.Next(); // v3 out (side retires)

        Assert.IsFalse(iterator.ParseOk);
        Assert.IsNull(iterator.EventData);

        var exception = Assert.ThrowsExactly<InvalidPlayTextException>(iterator.Next);
        Assert.AreEqual("TST200104150", exception.GameId);
        Assert.AreEqual(4, exception.EventNumber); // the fourth play of the game
        Assert.AreEqual(TestFixtures.UnparseablePlayText, exception.PlayText);

        // The failed play left no mark on the state: still 0-0 with the side just retired.
        Assert.AreEqual(0, iterator.State.Score[0]);
        Assert.AreEqual(0, iterator.State.Score[1]);
    }

    /// <summary>
    /// Runner fate is computed by replaying the rest of the half-inning on a copy of the
    /// iterator, so a later play that does not parse leaves the fate genuinely unknown - which
    /// must not be reported as the 0 ("put out") that an unknown fate would otherwise collapse
    /// to.
    /// </summary>
    [TestMethod]
    public async Task RunnerFate_UnparseableLaterPlay_ThrowsRatherThanReportingTheRunnerOut()
    {
        var game = await TestFixtures.LoadGameAsync(TestFixtures.MiniGameWithUnparseablePlayAfterRunnersEventFileText);
        var iterator = new GameIterator(game);

        iterator.Next(); // v1 out
        iterator.Next(); // v2 out
        iterator.Next(); // v3 out (side retires)
        iterator.Next(); // h1 walks

        // Positioned on h2's walk, with h1 on first and h3's unparseable play still ahead.
        Assert.IsTrue(iterator.State.BaseOccupied(1));
        Assert.IsTrue(iterator.ParseOk);

        var exception = Assert.ThrowsExactly<InvalidPlayTextException>(() => iterator.RunnerFate(1));
        Assert.AreEqual(TestFixtures.UnparseablePlayText, exception.PlayText);
    }

    [TestMethod]
    public async Task RunnerFate_UnparseableCurrentPlay_ThrowsRatherThanReportingTheRunnerOut()
    {
        var game = await TestFixtures.LoadGameAsync(TestFixtures.MiniGameWithUnparseablePlayEventFileText);
        var iterator = new GameIterator(game);

        iterator.Next(); // v1 out
        iterator.Next(); // v2 out
        iterator.Next(); // v3 out (side retires), leaving the unparseable play current

        var exception = Assert.ThrowsExactly<InvalidPlayTextException>(() => iterator.RunnerFate(1));
        Assert.AreEqual(TestFixtures.UnparseablePlayText, exception.PlayText);
    }

    [TestMethod]
    public async Task Next_PastTheLastEvent_Throws()
    {
        var game = await TestFixtures.LoadMiniGameAsync();
        var iterator = new GameIterator(game);

        while (iterator.CurrentEvent is not null)
        {
            iterator.Next();
        }

        Assert.ThrowsExactly<InvalidOperationException>(iterator.Next);
    }

    [TestMethod]
    public async Task Iterating_ToEndOfGame_ProducesExpectedFinalScore()
    {
        var game = await TestFixtures.LoadMiniGameAsync();
        var iterator = new GameIterator(game);

        while (iterator.CurrentEvent is not null)
        {
            iterator.Next();
        }

        Assert.AreEqual(0, iterator.State.Score[0]);
        Assert.AreEqual(1, iterator.State.Score[1]);
        Assert.AreEqual(3, iterator.State.Outs);
    }
}
