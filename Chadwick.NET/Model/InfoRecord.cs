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
/// A single <c>info</c> record from a Retrosheet event file: a (label, value) pair describing
/// game metadata such as the teams, date, or umpires.
/// </summary>
public sealed class InfoRecord
{
    /// <summary>
    /// The info field's name (e.g. <c>visteam</c>, <c>date</c>, <c>temp</c>).
    /// </summary>
    public required string Label { get; init; }

    /// <summary>
    /// The info field's value. Empty if the record supplied no value.
    /// </summary>
    public required string Data { get; init; }
}
