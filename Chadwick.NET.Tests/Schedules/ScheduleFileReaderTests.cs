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
using Chadwick.Core.Schedules;

namespace Chadwick.NET.Tests.Schedules;

[TestClass]
public sealed class ScheduleFileReaderTests
{
    private const string HeaderRow = "Date,Num,Day,Visitor,League,Game,Home,League,Game,Day/Night,Postponed,Makeup";

    private static async Task<IReadOnlyList<ScheduledGame>> ReadGamesAsync(params string[] dataRows)
    {
        var text = HeaderRow + "\n" + string.Join("\n", dataRows) + "\n";
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        return await ScheduleFileReader.ReadAllGamesAsync(stream, CancellationToken.None);
    }

    [TestMethod]
    public async Task ReadAllGames_SkipsTheHeaderRow()
    {
        var games = await ReadGamesAsync(
            "\"19680410\",\"0\",\"Wed\",\"CLE\",\"AL\",2,\"CHA\",\"AL\",2,\"D\",\"\",\"\"");

        Assert.HasCount(1, games);
    }

    [TestMethod]
    public async Task ReadAllGames_ParsesAnOrdinaryUnpostponedGame()
    {
        var games = await ReadGamesAsync(
            "\"19680410\",\"0\",\"Wed\",\"CLE\",\"AL\",2,\"CHA\",\"AL\",2,\"D\",\"\",\"\"");

        var game = games[0];
        Assert.AreEqual(new DateOnly(1968, 4, 10), game.Date);
        Assert.AreEqual(ScheduledGameSlot.SingleGame, game.Slot);
        Assert.AreEqual(DayOfWeek.Wednesday, game.DayOfWeek);
        Assert.AreEqual("CLE", game.VisitingTeamId);
        Assert.AreEqual("AL", game.VisitingTeamLeague);
        Assert.AreEqual(2, game.VisitingTeamGameNumber);
        Assert.AreEqual("CHA", game.HomeTeamId);
        Assert.AreEqual("AL", game.HomeTeamLeague);
        Assert.AreEqual(2, game.HomeTeamGameNumber);
        Assert.AreEqual(ScheduledGameTimeOfDay.Day, game.TimeOfDay);
        Assert.IsFalse(game.WasPostponed);
        Assert.IsEmpty(game.PostponementNotes);
        Assert.IsEmpty(game.MakeupDates);
        Assert.IsEmpty(game.MakeupFieldNotes);
    }

    [TestMethod]
    public async Task ReadAllGames_ParsesADoubleHeaderGame()
    {
        var games = await ReadGamesAsync(
            "\"19680409\",\"2\",\"Tue\",\"PIT\",\"NL\",2,\"HOU\",\"NL\",2,\"N\",\"\",\"\"");

        Assert.AreEqual(ScheduledGameSlot.DoubleHeaderGame2, games[0].Slot);
    }

    [TestMethod]
    public async Task ReadAllGames_ParsesASimplePostponementWithOneMakeupDate()
    {
        // Real 1968 game: postponed by Martin Luther King Jr.'s funeral, made up three days later.
        var games = await ReadGamesAsync(
            "\"19680408\",\"0\",\"Mon\",\"CHN\",\"NL\",1,\"CIN\",\"NL\",1,\"D\",\"Funeral of Martin Luther King Jr.\",\"19680411\"");

        var game = games[0];
        Assert.IsTrue(game.WasPostponed);
        CollectionAssert.AreEqual(new[] { "Funeral of Martin Luther King Jr." }, game.PostponementNotes.ToArray());
        CollectionAssert.AreEqual(new[] { new DateOnly(1968, 4, 11) }, game.MakeupDates.ToArray());
        Assert.IsEmpty(game.MakeupFieldNotes);
    }

    [TestMethod]
    public async Task ReadAllGames_SplitsMultiplePostponementNotes()
    {
        // Real 1968 game: postponed for two reasons at once (RFK's assassination and a site change).
        var games = await ReadGamesAsync(
            "\"19680609\",\"0\",\"Sun\",\"OAK\",\"AL\",58,\"BAL\",\"AL\",58,\"D\",\"Death of Robert Kennedy; Site change\",\"19680616\"");

        CollectionAssert.AreEqual(
            new[] { "Death of Robert Kennedy", "Site change" },
            games[0].PostponementNotes.ToArray());
    }

    [TestMethod]
    public async Task ReadAllGames_ParsesTwoMakeupDatesWhenTheMakeupWasItselfPostponed()
    {
        // Real 1968 game: the 6/09 makeup attempt was itself postponed and played on 8/27 instead.
        var games = await ReadGamesAsync(
            "\"19680411\",\"0\",\"Thu\",\"OAK\",\"AL\",2,\"BAL\",\"AL\",2,\"N\",\"Schedule change\",\"19680609; 19680827\"");

        CollectionAssert.AreEqual(
            new[] { new DateOnly(1968, 6, 9), new DateOnly(1968, 8, 27) },
            games[0].MakeupDates.ToArray());
    }

    [TestMethod]
    public async Task ReadAllGames_ParsesAPostponedGameThatWasNeverMadeUp()
    {
        // Real 1968 game: rained out late in the season and never rescheduled.
        var games = await ReadGamesAsync(
            "\"19680918\",\"0\",\"Wed\",\"WS2\",\"AL\",153,\"CLE\",\"AL\",156,\"N\",\"Rain; No makeup played\",\"\"");

        var game = games[0];
        CollectionAssert.AreEqual(new[] { "Rain", "No makeup played" }, game.PostponementNotes.ToArray());
        Assert.IsEmpty(game.MakeupDates);
        Assert.IsEmpty(game.MakeupFieldNotes);
    }

    [TestMethod]
    public async Task ReadAllGames_TreatsAFoldedTeamsReplacementCodeAsAMakeupFieldNote()
    {
        // Synthetic case modeled on the schedule format's documented folded-team scenario (e.g.
        // the Altoona Mountain Cities folding in 1884 and being replaced by Kansas City Unions);
        // no real season in this repository's sample data is old enough to exercise this.
        var games = await ReadGamesAsync(
            "\"18840607\",\"0\",\"Sat\",\"ALT\",\"UA\",30,\"KCU\",\"UA\",1,\"D\",\"ALT folded\",\"KCU\"");

        var game = games[0];
        CollectionAssert.AreEqual(new[] { "ALT folded" }, game.PostponementNotes.ToArray());
        Assert.IsEmpty(game.MakeupDates);
        CollectionAssert.AreEqual(new[] { "KCU" }, game.MakeupFieldNotes.ToArray());
    }

    [TestMethod]
    public async Task ReadAllGames_SkipsARowWithAnUnrecognizedDayOfWeekRatherThanThrowing()
    {
        var games = await ReadGamesAsync(
            "\"19680410\",\"0\",\"Zzz\",\"CLE\",\"AL\",2,\"CHA\",\"AL\",2,\"D\",\"\",\"\"",
            "\"19680411\",\"0\",\"Thu\",\"CAL\",\"AL\",3,\"CLE\",\"AL\",3,\"D\",\"\",\"\"");

        Assert.HasCount(1, games);
        Assert.AreEqual("CAL", games[0].VisitingTeamId);
    }

    [TestMethod]
    public async Task ReadAllGames_SkipsATooShortRow()
    {
        var games = await ReadGamesAsync(
            "\"19680410\",\"0\",\"Wed\"",
            "\"19680411\",\"0\",\"Thu\",\"CAL\",\"AL\",3,\"CLE\",\"AL\",3,\"D\",\"\",\"\"");

        Assert.HasCount(1, games);
    }
}
