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
/// The kind of event a parsed play represents. Values 0-24 are set identical to the event codes
/// used by Retrosheet's own tools - preserve them exactly, since other Retrosheet-ecosystem
/// software and historical files depend on these specific numeric codes.
/// </summary>
/// <remarks>
/// Chadwick's C parser also defines pitch-by-pitch event codes (100+) for classifying individual
/// pitch characters in a plate appearance's pitch sequence. Those are a separate concern from
/// play-text parsing and are not yet ported.
/// </remarks>
public enum PlayEventType
{
    /// <summary>No event type has been assigned yet.</summary>
    Unknown = 0,

    /// <summary>No event occurred.</summary>
    None = 1,

    /// <summary>A batter is put out on a batted ball, not a strikeout.</summary>
    GenericOut = 2,

    /// <summary>The batter struck out.</summary>
    Strikeout = 3,

    /// <summary>A runner stole a base.</summary>
    StolenBase = 4,

    /// <summary>The defense allowed a stolen base through defensive indifference.</summary>
    Indifference = 5,

    /// <summary>A runner was caught stealing.</summary>
    CaughtStealing = 6,

    /// <summary>A pickoff attempt resulted in an error.</summary>
    PickoffError = 7,

    /// <summary>A runner was picked off.</summary>
    Pickoff = 8,

    /// <summary>The pitcher threw a wild pitch.</summary>
    WildPitch = 9,

    /// <summary>The catcher allowed a passed ball.</summary>
    PassedBall = 10,

    /// <summary>The pitcher balked.</summary>
    Balk = 11,

    /// <summary>A runner advanced for a reason not otherwise covered (e.g. defensive indifference on a non-steal, or a runner advancing on a play elsewhere).</summary>
    OtherAdvance = 12,

    /// <summary>A fielder made an error attempting to field a foul ball.</summary>
    FoulError = 13,

    /// <summary>The batter walked.</summary>
    Walk = 14,

    /// <summary>The batter was intentionally walked.</summary>
    IntentionalWalk = 15,

    /// <summary>The batter was hit by a pitch.</summary>
    HitByPitch = 16,

    /// <summary>The batter reached on interference (typically catcher's interference).</summary>
    Interference = 17,

    /// <summary>The batter reached base on a fielding error.</summary>
    Error = 18,

    /// <summary>The batter reached on a fielder's choice.</summary>
    FieldersChoice = 19,

    /// <summary>The batter hit a single.</summary>
    Single = 20,

    /// <summary>The batter hit a double.</summary>
    Double = 21,

    /// <summary>The batter hit a triple.</summary>
    Triple = 22,

    /// <summary>The batter hit a home run.</summary>
    HomeRun = 23,

    /// <summary>The play's text is missing or could not be determined.</summary>
    MissingPlay = 24,
}
