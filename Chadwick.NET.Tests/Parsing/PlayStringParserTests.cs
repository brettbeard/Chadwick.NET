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
        Assert.IsNotNull(play);
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
        Assert.IsNotNull(play);
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
        Assert.IsNotNull(play);
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
        Assert.IsNotNull(play);
        Assert.AreEqual(PlayEventType.Strikeout, play.EventType);
        Assert.AreEqual(1, play.OutsOnPlay);
        CollectionAssert.AreEqual(new[] { 2 }, play.Putouts);
    }

    [TestMethod]
    public void TryParse_Walk_AdvancesBatterToFirst()
    {
        var ok = PlayStringParser.TryParse("W", out var play);

        Assert.IsTrue(ok);
        Assert.IsNotNull(play);
        Assert.AreEqual(PlayEventType.Walk, play.EventType);
        Assert.AreEqual(1, play.Advance[0]);
    }

    [TestMethod]
    public void TryParse_ReachedOnError_ChargesFielderAndAdvancesRunner()
    {
        var ok = PlayStringParser.TryParse("E6/G6.1-2", out var play);

        Assert.IsTrue(ok);
        Assert.IsNotNull(play);
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
        Assert.IsNotNull(play);
        Assert.AreEqual(PlayEventType.StolenBase, play.EventType);
        Assert.IsTrue(play.SbFlag[1]);
        Assert.AreEqual(2, play.Advance[1]);
    }

    [TestMethod]
    public void TryParse_CaughtStealing_CreditsPutoutAndAssist()
    {
        var ok = PlayStringParser.TryParse("CS2(26)", out var play);

        Assert.IsTrue(ok);
        Assert.IsNotNull(play);
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
        Assert.IsNotNull(play);
        Assert.AreEqual(PlayEventType.FieldersChoice, play.EventType);
        Assert.AreEqual(1, play.Advance[0]); // batter safe at first on the fielder's choice
        Assert.AreEqual(2, play.Advance[1]); // runner from first to second
        Assert.AreEqual(0, play.Advance[3]); // runner from third out at home
        Assert.AreEqual(1, play.OutsOnPlay);
    }

    /// <summary>
    /// Retrosheet has credited a stolen base on a balk once, in 2025, which makes a steal
    /// grammatically legal in the advancement part of any play. Chadwick's own parser accepts it
    /// on that basis, so this port has to as well.
    /// </summary>
    [TestMethod]
    public void TryParse_StolenBaseAnnotationOnAnAdvance_CreditsTheStealAndKeepsTheAdvances()
    {
        // KCA202506140: a balk on which the runner from second was credited with stealing third.
        var ok = PlayStringParser.TryParse("BK.2-3(SB3);1-2", out var play);

        Assert.IsTrue(ok);
        Assert.IsNotNull(play);
        Assert.AreEqual(PlayEventType.Balk, play.EventType);
        Assert.IsTrue(play.SbFlag[2]);
        Assert.AreEqual(3, play.Advance[2]);
        Assert.AreEqual(2, play.Advance[1]); // the advance after the annotated one still parses
        Assert.IsFalse(play.SbFlag[1]);
    }

    [TestMethod]
    [DataRow("W.2-3(SB3)", DisplayName = "walk")]
    [DataRow("S7.2-3(SB3)", DisplayName = "single")]
    [DataRow("E6/G6.2-3(SB3)", DisplayName = "error")]
    [DataRow("BK.2-3(SB)", DisplayName = "balk, with the base left unstated")]
    public void TryParse_StolenBaseAnnotation_IsAcceptedWhateverTheEvent(string playText)
    {
        var ok = PlayStringParser.TryParse(playText, out var play);

        Assert.IsTrue(ok);
        Assert.IsNotNull(play);
        Assert.IsTrue(play.SbFlag[2]);
        Assert.AreEqual(3, play.Advance[2]);
    }

    [TestMethod]
    [DataRow("BK.3-H(SBH)", DisplayName = "SBH")]
    [DataRow("BK.3-H(SB4)", DisplayName = "SB4, the form SBH is rewritten to before parsing")]
    public void TryParse_StolenBaseAnnotationOfHome_CreditsTheRunnerFromThird(string playText)
    {
        var ok = PlayStringParser.TryParse(playText, out var play);

        Assert.IsTrue(ok);
        Assert.IsNotNull(play);
        Assert.IsTrue(play.SbFlag[3]);
        Assert.AreEqual(4, play.Advance[3]);
    }

    /// <summary>
    /// Accepting the annotation must not amount to accepting anything that starts with
    /// <c>SB</c>: only a base that can actually be stolen, as Chadwick's parser has it.
    /// </summary>
    [TestMethod]
    [DataRow("BK.2-3(SB5)", DisplayName = "a base that cannot be stolen")]
    [DataRow("BK.2-3(SBX)", DisplayName = "not a base at all")]
    public void TryParse_StolenBaseAnnotationWithABogusBase_ReturnsFalse(string playText)
    {
        var ok = PlayStringParser.TryParse(playText, out var play);

        Assert.IsFalse(ok);
        Assert.IsNull(play);
    }

    /// <summary>
    /// A catcher-interference play whose fielding modifier ends in a trailing hyphen parses
    /// once advances follow. ParseInterference keys its modifier loop on '/' alone, where every
    /// other event type tolerates the '-'/'+'/'#'/'!' terminators ParseFlag can stop on, so a
    /// trailing hyphen used to strand the parser before the advances.
    /// </summary>
    [TestMethod]
    public void TryParse_InterferenceWithTrailingHyphenModifierAndAdvances_ParsesTheAdvances()
    {
        // BOS202509030: catcher interference, the error and obstruction modifiers, a batted-ball
        // modifier ending in a trailing hyphen, then three advances.
        var ok = PlayStringParser.TryParse("C/E2/OBS/G2-.3-H(RBI);2-3;B-1", out var play);

        Assert.IsTrue(ok);
        Assert.IsNotNull(play);
        Assert.AreEqual(PlayEventType.Interference, play.EventType);
        Assert.AreEqual(1, play.Advance[0]); // batter awarded first on the interference
        Assert.AreEqual(3, play.Advance[2]); // runner on second to third
        Assert.AreEqual(4, play.Advance[3]); // runner on third scores
    }

    [TestMethod]
    [DataRow("C/G2-.3-H", DisplayName = "bare ground-ball modifier with the trailing hyphen")]
    [DataRow("C/E2/G34-.3-H", DisplayName = "error modifier and a two-fielder location")]
    public void TryParse_InterferenceWithTrailingHyphenModifier_IsAccepted(string playText)
    {
        var ok = PlayStringParser.TryParse(playText, out var play);

        Assert.IsTrue(ok);
        Assert.IsNotNull(play);
        Assert.AreEqual(PlayEventType.Interference, play.EventType);
        Assert.AreEqual(4, play.Advance[3]); // the advance after the hyphenated modifier parses
    }

    /// <summary>
    /// The control cases from the report: the same play parsed with no advances, with no trailing
    /// hyphen, and on a different event type, each of which worked before the fix. They are kept
    /// so the fix is pinned to the combination rather than masking a regression in one of them.
    /// </summary>
    [TestMethod]
    [DataRow("C/E2/OBS/G2-", DisplayName = "same modifiers, no advances")]
    [DataRow("C/E2/G2.3-H", DisplayName = "same shape, no trailing hyphen")]
    [DataRow("S7/G2-.3-H", DisplayName = "same modifier and advances, different event")]
    [DataRow("S3/G34S-.1-2", DisplayName = "a trailing-hyphen modifier that always parsed")]
    public void TryParse_TrailingHyphenModifierControlCases_StillParse(string playText)
    {
        var ok = PlayStringParser.TryParse(playText, out var play);

        Assert.IsTrue(ok);
        Assert.IsNotNull(play);
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

    /// <summary>
    /// Parsing fills its result as it goes and can fail part-way through - after the event type
    /// is set and some runners have been advanced - so a failed parse must hand back nothing at
    /// all rather than a play that looks usable but is missing whatever came after the point of
    /// failure.
    /// </summary>
    [TestMethod]
    [DataRow("ZZZ", DisplayName = "unrecognized primary event")]
    [DataRow("FC1/G1.3XH(2)1-2", DisplayName = "missing advance separator, after advances parsed")]
    [DataRow("S7.2-H;1-3X", DisplayName = "hit, after the event type and both advances are set")]
    [DataRow("W.2-3;1-2Q", DisplayName = "walk, after one advance is set")]
    public void TryParse_Failure_YieldsNoPlayAtAll(string playText)
    {
        var ok = PlayStringParser.TryParse(playText, out var play);

        Assert.IsFalse(ok);
        Assert.IsNull(play);
    }
}
