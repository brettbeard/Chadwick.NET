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
using Chadwick.Core.Rosters;

namespace Chadwick.NET.Tests.Rosters;

[TestClass]
public sealed class RosterParsingTests
{
    [TestMethod]
    public async Task LeagueFileReader_ReadsOneRosterPerTeamLine()
    {
        const string teamFileText =
            "BAL,A,Baltimore,Orioles\n" +
            "ATL,N,Atlanta,Braves\n";

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(teamFileText));
        var league = await LeagueFileReader.ReadAsync(stream, 1968, CancellationToken.None);

        Assert.HasCount(2, league.Rosters);
        var baltimore = league.FindRoster("BAL");
        Assert.IsNotNull(baltimore);
        Assert.AreEqual("A", baltimore.League);
        Assert.AreEqual("Baltimore", baltimore.City);
        Assert.AreEqual("Orioles", baltimore.Nickname);
        Assert.AreEqual(1968, baltimore.Year);
        Assert.IsEmpty(baltimore.Players);
    }

    [TestMethod]
    public async Task LeagueFileReader_SkipsMalformedLines()
    {
        const string teamFileText = "BAL,A,Baltimore,Orioles\ntoo,short\n";

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(teamFileText));
        var league = await LeagueFileReader.ReadAsync(stream, 1968, CancellationToken.None);

        Assert.HasCount(1, league.Rosters);
    }

    [TestMethod]
    public async Task RosterFileReader_PopulatesPlayersWithHandCodes()
    {
        const string rosterFileText =
            "blaip101,Blair,Paul,R,R,BAL,OF\n" +
            "beenf101,Beene,Fred,B,R,BAL,P\n";

        var roster = new Roster { TeamId = "BAL", Year = 1968, League = "A", City = "Baltimore", Nickname = "Orioles" };

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(rosterFileText));
        await RosterFileReader.PopulatePlayersAsync(roster, stream, CancellationToken.None);

        Assert.HasCount(2, roster.Players);
        var blair = roster.FindPlayer("blaip101");
        Assert.IsNotNull(blair);
        Assert.AreEqual("Blair", blair.LastName);
        Assert.AreEqual("Paul", blair.FirstName);
        Assert.AreEqual('R', blair.BattingHand);
        Assert.AreEqual('R', blair.ThrowingHand);
        Assert.AreEqual('R', roster.GetBattingHand("blaip101"));
    }

    [TestMethod]
    public void Roster_GetBattingHand_ReturnsQuestionMarkForUnknownPlayer()
    {
        var roster = new Roster { TeamId = "BAL", Year = 1968, League = "A", City = "Baltimore", Nickname = "Orioles" };

        Assert.AreEqual('?', roster.GetBattingHand("nobody999"));
    }
}
