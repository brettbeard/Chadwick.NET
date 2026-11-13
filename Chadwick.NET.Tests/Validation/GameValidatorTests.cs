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
using Chadwick.Core.Parsing;
using Chadwick.Core.Validation;

namespace Chadwick.NET.Tests.Validation;

[TestClass]
public sealed class GameValidatorTests
{
    [TestMethod]
    public async Task Validate_ConsistentGame_ReportsNoProblems()
    {
        var game = await TestFixtures.LoadMiniGameAsync();

        var problems = GameValidator.Validate(game);

        Assert.IsEmpty(problems);
    }

    [TestMethod]
    public async Task Validate_UnparseablePlay_ReportsItWithTheOffendingText()
    {
        var game = await TestFixtures.LoadGameAsync(TestFixtures.MiniGameWithUnparseablePlayEventFileText);

        var problems = GameValidator.Validate(game);

        Assert.HasCount(1, problems);
        Assert.AreEqual(GameProblemKind.InvalidPlayText, problems[0].Kind);
        Assert.AreEqual(4, problems[0].EventNumber);
        Assert.AreEqual("S7.2-H;1-3X", problems[0].PlayText);
        StringAssert.Contains(problems[0].Description, "S7.2-H;1-3X");
        StringAssert.Contains(problems[0].Description, "h1"); // the batter
    }

    /// <summary>
    /// Validating must not itself trip over the bad play it is looking for - it has to be usable
    /// as the up-front screen that lets a caller skip a game instead of catching
    /// <see cref="InvalidPlayTextException"/> part-way through replaying it.
    /// </summary>
    [TestMethod]
    public async Task Validate_UnparseablePlay_DoesNotThrow()
    {
        var game = await TestFixtures.LoadGameAsync(TestFixtures.MiniGameWithUnparseablePlayEventFileText);

        var problems = GameValidator.Validate(game);

        Assert.IsNotEmpty(problems);
        Assert.ThrowsExactly<InvalidPlayTextException>(() => BoxscoreBuilder.Create(game));
    }

    [TestMethod]
    public async Task Validate_AdvanceFromAnEmptyBase_ReportsIt()
    {
        // Nobody is on second when the leadoff batter walks, so "2-3" cannot be happening.
        var game = await TestFixtures.LoadGameAsync(
            TestFixtures.MiniGameEventFileText.Replace("play,1,0,v1,??,,8", "play,1,0,v1,??,,W.2-3"));

        var problems = GameValidator.Validate(game);

        Assert.IsNotEmpty(problems);
        Assert.AreEqual(GameProblemKind.AdvanceFromEmptyBase, problems[0].Kind);
        Assert.AreEqual(1, problems[0].EventNumber);
    }

    [TestMethod]
    public async Task Validate_DoublePlayWithTooFewOuts_ReportsIt()
    {
        // A fly out to left marked as a double play, with only the one out actually recorded.
        var game = await TestFixtures.LoadGameAsync(
            TestFixtures.MiniGameEventFileText.Replace("play,1,0,v1,??,,8", "play,1,0,v1,??,,7/DP"));

        var problems = GameValidator.Validate(game);

        Assert.IsNotEmpty(problems);
        Assert.AreEqual(GameProblemKind.DoublePlayWithoutTwoOuts, problems[0].Kind);
        Assert.AreEqual(1, problems[0].EventNumber);
    }

    [TestMethod]
    public async Task Validate_InvalidStarterPosition_ReportsItAndSkipsThePlayByPlayChecks()
    {
        var game = await TestFixtures.LoadGameAsync(
            TestFixtures.MiniGameWithUnparseablePlayEventFileText.Replace(
                "start,v9,\"Vis Nine\",0,9,9", "start,v9,\"Vis Nine\",0,9,11"));

        var problems = GameValidator.Validate(game);

        Assert.HasCount(1, problems);
        Assert.AreEqual(GameProblemKind.InvalidStarterPosition, problems[0].Kind);
        Assert.AreEqual("v9", problems[0].PlayerId);
        Assert.AreEqual(0, problems[0].EventNumber);
    }
}
