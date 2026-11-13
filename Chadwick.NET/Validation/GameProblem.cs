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
/// One internal-consistency problem found in a game by <see cref="GameValidator"/>.
/// </summary>
/// <param name="Kind">What kind of problem this is.</param>
/// <param name="Description">A human-readable description, suitable for logging or display.</param>
/// <param name="EventNumber">
/// The 1-based position of the offending play among the game's plays (<c>"NP"</c> placeholders
/// excluded), or 0 for a problem that is not tied to a particular play.
/// </param>
/// <param name="PlayText">The offending play's text, or <see langword="null"/> if not applicable.</param>
/// <param name="PlayerId">The player involved, or <see langword="null"/> if not applicable.</param>
public sealed record GameProblem(
    GameProblemKind Kind,
    string Description,
    int EventNumber = 0,
    string? PlayText = null,
    string? PlayerId = null);
