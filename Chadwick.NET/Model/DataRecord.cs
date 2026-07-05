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

namespace Chadwick.Core.Model;

/// <summary>
/// A generic multi-field record from a Retrosheet event file - used for <c>data</c> (earned run
/// data), <c>stat</c>, <c>line</c> (linescores), and <c>event</c> (event detail) records, all of
/// which carry a variable-length list of string fields rather than a fixed schema.
/// </summary>
public sealed class DataRecord
{
    /// <summary>
    /// The record's fields, in file order, excluding the leading record-type keyword itself
    /// (e.g. <c>data</c> or <c>line</c>).
    /// </summary>
    public required IReadOnlyList<string> Values { get; init; }
}
