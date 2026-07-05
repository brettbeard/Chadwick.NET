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
/// A free-text <c>com</c> record from a Retrosheet event file. Comments are attached to the
/// event that precedes them, or to the game itself if they appear before its first event.
/// </summary>
public sealed class Comment
{
    /// <summary>
    /// The comment's raw text, exactly as recorded in the event file.
    /// </summary>
    public required string Text { get; init; }
}
