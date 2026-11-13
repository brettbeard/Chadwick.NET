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

namespace Chadwick.Core.Validation;

/// <summary>
/// The kinds of internal-consistency problem <see cref="GameValidator"/> reports.
/// </summary>
public enum GameProblemKind
{
    /// <summary>A starter's batting-order slot is outside the valid range 0-9.</summary>
    InvalidStarterSlot,

    /// <summary>A starter's team is neither 0 (visitors) nor 1 (home).</summary>
    InvalidStarterTeam,

    /// <summary>A starter's fielding position is outside the valid range 1-10.</summary>
    InvalidStarterPosition,

    /// <summary>A play's text could not be parsed, so its outcome is unknown.</summary>
    InvalidPlayText,

    /// <summary>A play is marked as a double play but records fewer than two outs.</summary>
    DoublePlayWithoutTwoOuts,

    /// <summary>A play is marked as a triple play but records fewer than three outs.</summary>
    TriplePlayWithoutThreeOuts,

    /// <summary>A play advances, or puts out, a runner on a base that no runner occupies.</summary>
    AdvanceFromEmptyBase,

    /// <summary>A play has a trailing runner end up on or past the base a runner ahead of them ends on.</summary>
    RunnerOvertaken,
}
