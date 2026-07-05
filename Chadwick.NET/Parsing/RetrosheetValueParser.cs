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
/// Converts raw Retrosheet field text into typed values, translating the file format's
/// "not available" conventions into idiomatic C# nulls at the parse boundary.
/// </summary>
public static class RetrosheetValueParser
{
    /// <summary>
    /// Parses an integer field, returning <see langword="null"/> if the text is missing,
    /// unparseable, or the literal sentinel value <c>-1</c> that Retrosheet files use to mean
    /// "not available" (matching Chadwick's <c>cw_atoi</c>, which returns -1 for both cases).
    /// </summary>
    /// <param name="token">The raw field text, or <see langword="null"/>.</param>
    public static int? ParseNullableInt(string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        if (int.TryParse(token, out var value) && value != -1)
        {
            return value;
        }

        return null;
    }

    /// <summary>
    /// Parses a single-character code field (such as a batting or throwing hand), returning
    /// <c>'?'</c> - Retrosheet's own convention for "unknown" - if the text is missing or blank.
    /// </summary>
    /// <param name="token">The raw field text, or <see langword="null"/>.</param>
    public static char ParseHandCode(string? token)
    {
        if (string.IsNullOrEmpty(token) || token[0] == ' ')
        {
            return '?';
        }

        return token[0];
    }
}
