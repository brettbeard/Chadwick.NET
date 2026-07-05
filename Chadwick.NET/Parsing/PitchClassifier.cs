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
/// Classifies the individual pitch characters that make up a <c>play</c> record's pitch
/// sequence (e.g. <c>CBFBX</c>).
/// </summary>
public static class PitchClassifier
{
    /// <summary>Whether <paramref name="pitch"/> represents a pitch actually thrown (as opposed to a no-pitch marker).</summary>
    public static bool IsPitchThrown(char pitch) => IsBallThrown(pitch) || IsStrikeThrown(pitch);

    /// <summary>Whether <paramref name="pitch"/> is any kind of ball.</summary>
    public static bool IsBallThrown(char pitch) => pitch is 'B' or 'H' or 'I' or 'P';

    /// <summary>Whether <paramref name="pitch"/> is a called ball.</summary>
    public static bool IsBallCalled(char pitch) => pitch == 'B';

    /// <summary>Whether <paramref name="pitch"/> is an intentional ball.</summary>
    public static bool IsBallIntentional(char pitch) => pitch == 'I';

    /// <summary>Whether <paramref name="pitch"/> is a pitchout.</summary>
    public static bool IsBallPitchout(char pitch) => pitch == 'P';

    /// <summary>Whether <paramref name="pitch"/> hit the batter.</summary>
    public static bool IsBallHitBatter(char pitch) => pitch == 'H';

    /// <summary>Whether <paramref name="pitch"/> is some other kind of ball.</summary>
    public static bool IsBallOther(char pitch) => pitch == 'V';

    /// <summary>Whether <paramref name="pitch"/> is any kind of strike.</summary>
    public static bool IsStrikeThrown(char pitch) =>
        pitch is 'C' or 'F' or 'K' or 'L' or 'M' or 'O' or 'Q' or 'R' or 'S' or 'T' or 'X' or 'Y';

    /// <summary>Whether <paramref name="pitch"/> is a called strike.</summary>
    public static bool IsStrikeCalled(char pitch) => pitch == 'C';

    /// <summary>Whether <paramref name="pitch"/> is a swinging strike.</summary>
    public static bool IsStrikeSwinging(char pitch) => pitch is 'S' or 'M' or 'Q';

    /// <summary>Whether <paramref name="pitch"/> is a foul ball.</summary>
    public static bool IsStrikeFoul(char pitch) => pitch is 'F' or 'L' or 'O' or 'T' or 'R';

    /// <summary>Whether <paramref name="pitch"/> was put in play.</summary>
    public static bool IsStrikeInPlay(char pitch) => pitch is 'X' or 'Y';

    /// <summary>Whether <paramref name="pitch"/> is some other kind of strike.</summary>
    public static bool IsStrikeOther(char pitch) => pitch is 'A' or 'K';

    /// <summary>Counts the pitches in <paramref name="pitches"/> matching <paramref name="criterion"/>.</summary>
    public static int CountPitches(string pitches, Func<char, bool> criterion)
    {
        var count = 0;
        foreach (var pitch in pitches)
        {
            if (criterion(pitch))
            {
                count++;
            }
        }

        return count;
    }
}
