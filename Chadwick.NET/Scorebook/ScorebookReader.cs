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

namespace Chadwick.Core.Scorebook;

/// <summary>
/// Reads every game from a Retrosheet event file (<c>.EVN</c>/<c>.EVA</c>), which is simply a
/// sequence of games back to back, each starting with its own <c>id</c> record.
/// </summary>
public static class ScorebookReader
{
    /// <summary>
    /// Reads all games from <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">The open event file stream.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The games found in the file, in file order.</returns>
    public static async Task<IReadOnlyList<Game>> ReadAllGamesAsync(Stream stream, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var games = new List<Game>();
        using var textReader = new StreamReader(stream);
        var lineReader = new PeekableLineReader(textReader);

        while (await SkipToNextGameAsync(lineReader, cancellationToken))
        {
            games.Add(await GameRecordReader.ReadNextAsync(lineReader, cancellationToken));
        }

        return games;
    }

    /// <summary>
    /// Advances past any lines that precede the next game's <c>id</c> record (such as leading
    /// comments), leaving that <c>id</c> line unconsumed for <see cref="GameRecordReader"/>.
    /// </summary>
    /// <returns><see langword="true"/> if another game was found; <see langword="false"/> at end of file.</returns>
    private static async Task<bool> SkipToNextGameAsync(PeekableLineReader lineReader, CancellationToken cancellationToken)
    {
        while (true)
        {
            var line = await lineReader.PeekLineAsync(cancellationToken);
            if (line is null)
            {
                return false;
            }

            var fields = RetrosheetLineTokenizer.Tokenize(line);
            if (fields.Count > 0 && fields[0] == "id")
            {
                return true;
            }

            await lineReader.ReadLineAsync(cancellationToken); // discard non-id lines preceding the first game
        }
    }
}
