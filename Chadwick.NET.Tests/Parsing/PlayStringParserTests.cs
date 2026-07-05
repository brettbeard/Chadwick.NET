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

using Chadwick.Core.Model;
using Chadwick.Core.Parsing;

namespace Chadwick.NET.Tests.Parsing;

[TestClass]
public sealed class PlayStringParserTests
{
    [TestMethod]
    public void TryParse_Single_AdvancesRunnersAndCreditsRbi()
    {
        var ok = PlayStringParser.TryParse("S7.2-H;1-3", out var play);

        Assert.IsTrue(ok);
        Assert.AreEqual(PlayEventType.Single, play.EventType);
        Assert.AreEqual(1, play.Advance[0]); // batter to first
        Assert.AreEqual(3, play.Advance[1]); // runner on first to third
        Assert.AreEqual(4, play.Advance[2]); // runner on second scores (earned)
        Assert.AreEqual(1, play.RbiFlag[2]);
        Assert.AreEqual(1, play.RunsOnPlay);
        Assert.AreEqual(1, play.RbiOnPlay);
        Assert.AreEqual(0, play.OutsOnPlay);
    }

    [TestMethod]
    public void TryParse_GroundedIntoDoublePlay_RecordsTwoOutsAndCredits()
    {
        var ok = PlayStringParser.TryParse("64(1)3/GDP", out var play);

        Assert.IsTrue(ok);
        Assert.AreEqual(PlayEventType.GenericOut, play.EventType);
        Assert.IsTrue(play.DpFlag);
        Assert.IsTrue(play.GdpFlag);
        Assert.AreEqual('G', play.BattedBallType);
        Assert.AreEqual(2, play.OutsOnPlay);
        CollectionAssert.AreEqual(new[] { 4, 3 }, play.Putouts);
        CollectionAssert.AreEqual(new[] { 6, 4 }, play.Assists);
    }

    [TestMethod]
    public void TryParse_HomeRun_ScoresBatterAndSetsHitLocation()
    {
        var ok = PlayStringParser.TryParse("HR/78/F", out var play);

        Assert.IsTrue(ok);
        Assert.AreEqual(PlayEventType.HomeRun, play.EventType);
        Assert.AreEqual(4, play.Advance[0]);
        Assert.AreEqual(1, play.RbiFlag[0]);
        Assert.AreEqual('F', play.BattedBallType);
        Assert.AreEqual("78", play.HitLocation);
    }

    [TestMethod]
    public void TryParse_BareStrikeout_CreditsCatcherPutout()
    {
        var ok = PlayStringParser.TryParse("K", out var play);

        Assert.IsTrue(ok);
        Assert.AreEqual(PlayEventType.Strikeout, play.EventType);
        Assert.AreEqual(1, play.OutsOnPlay);
        CollectionAssert.AreEqual(new[] { 2 }, play.Putouts);
    }

    [TestMethod]
    public void TryParse_Walk_AdvancesBatterToFirst()
    {
        var ok = PlayStringParser.TryParse("W", out var play);

        Assert.IsTrue(ok);
        Assert.AreEqual(PlayEventType.Walk, play.EventType);
        Assert.AreEqual(1, play.Advance[0]);
    }

    [TestMethod]
    public void TryParse_ReachedOnError_ChargesFielderAndAdvancesRunner()
    {
        var ok = PlayStringParser.TryParse("E6/G6.1-2", out var play);

        Assert.IsTrue(ok);
        Assert.AreEqual(PlayEventType.Error, play.EventType);
        Assert.AreEqual(1, play.Advance[0]);
        Assert.AreEqual(2, play.Advance[1]);
        Assert.AreEqual('G', play.BattedBallType);
        Assert.HasCount(1, play.Errors);
        Assert.AreEqual(6, play.Errors[0].FielderPosition);
        Assert.AreEqual('F', play.Errors[0].ErrorType);
    }

    [TestMethod]
    public void TryParse_StolenBase_SetsSbFlagAndAdvance()
    {
        var ok = PlayStringParser.TryParse("SB2", out var play);

        Assert.IsTrue(ok);
        Assert.AreEqual(PlayEventType.StolenBase, play.EventType);
        Assert.IsTrue(play.SbFlag[1]);
        Assert.AreEqual(2, play.Advance[1]);
    }

    [TestMethod]
    public void TryParse_CaughtStealing_CreditsPutoutAndAssist()
    {
        var ok = PlayStringParser.TryParse("CS2(26)", out var play);

        Assert.IsTrue(ok);
        Assert.AreEqual(PlayEventType.CaughtStealing, play.EventType);
        Assert.IsTrue(play.CsFlag[1]);
        Assert.AreEqual(1, play.OutsOnPlay);
        CollectionAssert.AreEqual(new[] { 6 }, play.Putouts);
        CollectionAssert.AreEqual(new[] { 2 }, play.Assists);
    }

    [TestMethod]
    public void TryParse_FieldersChoiceWithExplicitAdvances_ResolvesEachRunner()
    {
        var ok = PlayStringParser.TryParse("FC1/G1.3XH(2);1-2", out var play);

        Assert.IsTrue(ok);
        Assert.AreEqual(PlayEventType.FieldersChoice, play.EventType);
        Assert.AreEqual(1, play.Advance[0]); // batter safe at first on the fielder's choice
        Assert.AreEqual(2, play.Advance[1]); // runner from first to second
        Assert.AreEqual(0, play.Advance[3]); // runner from third out at home
        Assert.AreEqual(1, play.OutsOnPlay);
    }

    [TestMethod]
    public void TryParse_UnrecognizedToken_ReturnsFalse()
    {
        var ok = PlayStringParser.TryParse("ZZZ", out _);

        Assert.IsFalse(ok);
    }

    [TestMethod]
    public void TryParse_TrailingGarbageAfterAdvancement_ReturnsFalse()
    {
        // Missing the ';' separator between two advance clauses is invalid syntax.
        var ok = PlayStringParser.TryParse("FC1/G1.3XH(2)1-2", out _);

        Assert.IsFalse(ok);
    }
}
