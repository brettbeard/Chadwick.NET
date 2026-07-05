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

namespace Chadwick.Core.Schedules;

/// <summary>
/// Which game of the day a scheduled game is: a standalone single game, or one game of a
/// doubleheader (including separate-admission doubleheaders, which Retrosheet's schedule format
/// does not distinguish from traditional ones).
/// </summary>
public enum ScheduledGameSlot
{
    /// <summary>A standalone game, not part of a doubleheader.</summary>
    SingleGame = 0,

    /// <summary>The first game of a doubleheader.</summary>
    DoubleHeaderGame1 = 1,

    /// <summary>The second game of a doubleheader.</summary>
    DoubleHeaderGame2 = 2,
}
