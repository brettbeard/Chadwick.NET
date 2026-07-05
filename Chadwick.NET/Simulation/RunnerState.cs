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

namespace Chadwick.Core.Simulation;

/// <summary>
/// The runner occupying one base, and who is responsible for that runner being there - used to
/// correctly charge earned runs to the pitcher (and catcher, for catcher ERA) who put the
/// runner on base, even after substitutions or a fielder's-choice responsibility handoff.
/// </summary>
public sealed class RunnerState
{
    /// <summary>The runner's Retrosheet player ID, or <see langword="null"/> if the base is unoccupied.</summary>
    public string? RunnerId { get; set; }

    /// <summary>The pitcher responsible for this runner being on base.</summary>
    public string? PitcherId { get; set; }

    /// <summary>The catcher responsible for this runner being on base (for catcher ERA).</summary>
    public string? CatcherId { get; set; }

    /// <summary>The index (in the game's event list) of the play that put this runner on base.</summary>
    public int SourceEventIndex { get; set; }

    /// <summary>Whether this runner was placed by an automatic-runner (extra-innings tiebreaker) rule rather than reaching base through play.</summary>
    public bool IsAuto { get; set; }
}
