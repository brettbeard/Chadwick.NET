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
using Chadwick.Core.Model;
using Chadwick.Core.Scorebook;

namespace Chadwick.NET.Tests;

/// <summary>
/// A small, fully self-contained synthetic game used by iterator and boxscore tests: full
/// 9-player lineups for both teams, a top-1st (three outs, no runs) and a bottom-1st (a leadoff
/// home run, then three outs), giving a simple, hand-verifiable final score of 0-1.
/// </summary>
internal static class TestFixtures
{
    public const string MiniGameEventFileText =
        "id,TST200104150\n" +
        "version,2\n" +
        "info,visteam,VIS\n" +
        "info,hometeam,HOM\n" +
        "info,date,2001/04/15\n" +
        "info,number,0\n" +
        "info,htbf,false\n" +
        "start,v1,\"Vis One\",0,1,1\n" +
        "start,v2,\"Vis Two\",0,2,2\n" +
        "start,v3,\"Vis Three\",0,3,3\n" +
        "start,v4,\"Vis Four\",0,4,4\n" +
        "start,v5,\"Vis Five\",0,5,5\n" +
        "start,v6,\"Vis Six\",0,6,6\n" +
        "start,v7,\"Vis Seven\",0,7,7\n" +
        "start,v8,\"Vis Eight\",0,8,8\n" +
        "start,v9,\"Vis Nine\",0,9,9\n" +
        "start,h1,\"Home One\",1,1,1\n" +
        "start,h2,\"Home Two\",1,2,2\n" +
        "start,h3,\"Home Three\",1,3,3\n" +
        "start,h4,\"Home Four\",1,4,4\n" +
        "start,h5,\"Home Five\",1,5,5\n" +
        "start,h6,\"Home Six\",1,6,6\n" +
        "start,h7,\"Home Seven\",1,7,7\n" +
        "start,h8,\"Home Eight\",1,8,8\n" +
        "start,h9,\"Home Nine\",1,9,9\n" +
        "play,1,0,v1,??,,8\n" +
        "play,1,0,v2,??,,43\n" +
        "play,1,0,v3,??,,K\n" +
        "play,1,1,h1,??,,HR\n" +
        "play,1,1,h2,??,,8\n" +
        "play,1,1,h3,??,,63\n" +
        "play,1,1,h4,??,,K\n";

    public static async Task<Game> LoadMiniGameAsync()
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(MiniGameEventFileText));
        var games = await ScorebookReader.ReadAllGamesAsync(stream, CancellationToken.None);
        return games[0];
    }
}
