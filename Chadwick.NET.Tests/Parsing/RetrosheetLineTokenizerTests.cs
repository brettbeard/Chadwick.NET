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
public sealed class RetrosheetLineTokenizerTests
{
    [TestMethod]
    public void Tokenize_SplitsPlainCommaSeparatedFields()
    {
        var tokens = RetrosheetLineTokenizer.Tokenize("adamm101,Adamson,Mike,R,R");

        CollectionAssert.AreEqual(new[] { "adamm101", "Adamson", "Mike", "R", "R" }, tokens.ToArray());
    }

    [TestMethod]
    public void Tokenize_StripsQuotesFromQuotedField()
    {
        var tokens = RetrosheetLineTokenizer.Tokenize("info,site,\"Some, Place\"");

        CollectionAssert.AreEqual(new[] { "info", "site", "Some, Place" }, tokens.ToArray());
    }

    [TestMethod]
    public void Tokenize_TreatsCommaImmediatelyAfterClosingQuoteAsFieldSeparator()
    {
        var tokens = RetrosheetLineTokenizer.Tokenize("start,\"Player, Jr.\",1,2,3");

        CollectionAssert.AreEqual(new[] { "start", "Player, Jr.", "1", "2", "3" }, tokens.ToArray());
    }

    [TestMethod]
    public void Tokenize_ProducesEmptyFieldBetweenConsecutiveCommas()
    {
        var tokens = RetrosheetLineTokenizer.Tokenize("a,,b");

        CollectionAssert.AreEqual(new[] { "a", "", "b" }, tokens.ToArray());
    }

    [TestMethod]
    public void Tokenize_DoesNotProduceTrailingEmptyFieldAfterFinalComma()
    {
        var tokens = RetrosheetLineTokenizer.Tokenize("a,b,");

        CollectionAssert.AreEqual(new[] { "a", "b" }, tokens.ToArray());
    }

    [TestMethod]
    public void Tokenize_EmptyLineProducesNoTokens()
    {
        var tokens = RetrosheetLineTokenizer.Tokenize("");

        Assert.IsEmpty(tokens);
    }
}
