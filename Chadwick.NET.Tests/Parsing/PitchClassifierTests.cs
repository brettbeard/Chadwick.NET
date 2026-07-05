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

namespace Chadwick.NET.Tests.Parsing;

[TestClass]
public sealed class PitchClassifierTests
{
    [TestMethod]
    public void IsBallThrown_RecognizesBallCodes()
    {
        Assert.IsTrue(PitchClassifier.IsBallThrown('B'));
        Assert.IsTrue(PitchClassifier.IsBallThrown('I'));
        Assert.IsTrue(PitchClassifier.IsBallThrown('P'));
        Assert.IsTrue(PitchClassifier.IsBallThrown('H'));
        Assert.IsFalse(PitchClassifier.IsBallThrown('C'));
    }

    [TestMethod]
    public void IsStrikeThrown_RecognizesStrikeCodes()
    {
        Assert.IsTrue(PitchClassifier.IsStrikeThrown('C'));
        Assert.IsTrue(PitchClassifier.IsStrikeThrown('S'));
        Assert.IsTrue(PitchClassifier.IsStrikeThrown('X'));
        Assert.IsFalse(PitchClassifier.IsStrikeThrown('B'));
    }

    [TestMethod]
    public void IsPitchThrown_IsTrueForEitherBallOrStrike()
    {
        Assert.IsTrue(PitchClassifier.IsPitchThrown('B'));
        Assert.IsTrue(PitchClassifier.IsPitchThrown('C'));
    }

    [TestMethod]
    public void CountPitches_CountsMatchingCharactersOnly()
    {
        var count = PitchClassifier.CountPitches("CBFBX", PitchClassifier.IsBallThrown);

        Assert.AreEqual(2, count);
    }
}
