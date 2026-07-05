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

namespace Chadwick.Core.Parsing;

/// <summary>
/// Splits a single line of a Retrosheet DiamondWare file into its comma-separated fields,
/// honoring quoted fields the way Retrosheet's own tools do (a field wrapped in double quotes
/// may itself contain commas, and the quotes are stripped from the returned value).
/// </summary>
/// <remarks>
/// This replaces Chadwick's <c>cw_strtok</c>, which tracked its scan position in a static
/// variable shared across calls. Since callers here always tokenize one already-read line at a
/// time (rather than repeatedly re-entering a shared buffer), no equivalent shared state is
/// needed - each call is independent and safe to use concurrently across lines.
/// </remarks>
public static class RetrosheetLineTokenizer
{
    /// <summary>
    /// Tokenizes <paramref name="line"/> into its comma-separated fields.
    /// </summary>
    /// <param name="line">A single line of text, without its trailing newline.</param>
    /// <returns>The fields found in the line, in order. An empty line yields an empty list.</returns>
    public static IReadOnlyList<string> Tokenize(string line)
    {
        ArgumentNullException.ThrowIfNull(line);

        var tokens = new List<string>();
        var position = 0;

        while (position < line.Length)
        {
            while (position < line.Length && (line[position] == ' ' || line[position] == '\t'))
            {
                position++;
            }

            if (position >= line.Length)
            {
                break;
            }

            if (line[position] == '"')
            {
                position++;
                var start = position;
                while (position < line.Length && line[position] != '"')
                {
                    position++;
                }

                tokens.Add(line[start..position]);

                if (position < line.Length)
                {
                    position++; // skip the closing quote
                }

                if (position < line.Length && line[position] == ',')
                {
                    position++; // a comma immediately after a closing quote is consumed, not treated as a separate empty field
                }
            }
            else
            {
                var start = position;
                while (position < line.Length && line[position] != ',')
                {
                    position++;
                }

                tokens.Add(line[start..position]);

                if (position < line.Length)
                {
                    position++; // skip the comma
                }
            }
        }

        return tokens;
    }
}
