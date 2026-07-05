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
public sealed class RetrosheetValueParserTests
{
    [TestMethod]
    public void ParseNullableInt_ParsesOrdinaryValue()
    {
        Assert.AreEqual(42, RetrosheetValueParser.ParseNullableInt("42"));
    }

    [TestMethod]
    public void ParseNullableInt_TreatsLiteralNegativeOneAsNull()
    {
        Assert.IsNull(RetrosheetValueParser.ParseNullableInt("-1"));
    }

    [TestMethod]
    public void ParseNullableInt_TreatsUnparseableTextAsNull()
    {
        Assert.IsNull(RetrosheetValueParser.ParseNullableInt("not-a-number"));
    }

    [TestMethod]
    public void ParseNullableInt_TreatsNullOrEmptyAsNull()
    {
        Assert.IsNull(RetrosheetValueParser.ParseNullableInt(null));
        Assert.IsNull(RetrosheetValueParser.ParseNullableInt(""));
    }

    [TestMethod]
    public void ParseHandCode_ReturnsFirstCharacter()
    {
        Assert.AreEqual('R', RetrosheetValueParser.ParseHandCode("R"));
        Assert.AreEqual('B', RetrosheetValueParser.ParseHandCode("Both"));
    }

    [TestMethod]
    public void ParseHandCode_ReturnsQuestionMarkForBlankOrMissing()
    {
        Assert.AreEqual('?', RetrosheetValueParser.ParseHandCode(null));
        Assert.AreEqual('?', RetrosheetValueParser.ParseHandCode(""));
        Assert.AreEqual('?', RetrosheetValueParser.ParseHandCode(" "));
    }
}
