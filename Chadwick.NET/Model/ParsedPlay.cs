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
/// The structured result of parsing a <see cref="GameEvent.PlayText"/> string (e.g.
/// <c>S7.2-H;1-3</c>) - what happened on the play, how each runner advanced, and the fielding
/// credits involved.
/// </summary>
/// <remarks>
/// The four "base" indices used throughout this type (<see cref="Advance"/>, <see cref="Play"/>,
/// etc.) refer to the batter (index 0) and the runners on first, second, and third base (indices
/// 1-3) at the time of the play.
/// </remarks>
public sealed class ParsedPlay
{
    /// <summary>The kind of event this play represents.</summary>
    public PlayEventType EventType { get; set; } = PlayEventType.Unknown;

    /// <summary>
    /// Where each runner ended up: 0 = out, 1-3 = advanced to that base, 4 = scored (earned), 5 =
    /// scored unearned, 6 = scored team-unearned but individually earned.
    /// </summary>
    public int[] Advance { get; } = new int[4];

    /// <summary>
    /// Whether each runner's advance was credited with an RBI: 0 = no, 1 = yes, 2 = yes, and
    /// explicitly confirmed by an <c>(RBI)</c> annotation in the play text.
    /// </summary>
    public int[] RbiFlag { get; } = new int[4];

    /// <summary>Whether each base was the site of a fielder's choice.</summary>
    public bool[] FcFlag { get; } = new bool[4];

    /// <summary>Whether the fielder handling each base's play muffed it, allowing the runner to be safe.</summary>
    public bool[] MuffFlag { get; } = new bool[4];

    /// <summary>The raw fielding-credit text recorded for each base (e.g. <c>643</c> for a putout at that base).</summary>
    public string[] Play { get; } = ["", "", "", ""];

    /// <summary>Whether the play was a sacrifice hit (bunt).</summary>
    public bool ShFlag { get; set; }

    /// <summary>Whether the play was a sacrifice fly.</summary>
    public bool SfFlag { get; set; }

    /// <summary>Whether the play was part of a double play.</summary>
    public bool DpFlag { get; set; }

    /// <summary>Whether the play was a ground-ball double play specifically.</summary>
    public bool GdpFlag { get; set; }

    /// <summary>Whether the play was part of a triple play.</summary>
    public bool TpFlag { get; set; }

    /// <summary>Whether a wild pitch occurred as part of this play.</summary>
    public bool WpFlag { get; set; }

    /// <summary>Whether a passed ball occurred as part of this play.</summary>
    public bool PbFlag { get; set; }

    /// <summary>Whether the batted ball was foul.</summary>
    public bool FoulFlag { get; set; }

    /// <summary>Whether the batter was bunting.</summary>
    public bool BuntFlag { get; set; }

    /// <summary>Whether an out on this play was a force out.</summary>
    public bool ForceFlag { get; set; }

    /// <summary>Whether the runner starting at each base stole it (indices 1-3 used; index 0 unused).</summary>
    public bool[] SbFlag { get; } = new bool[4];

    /// <summary>Whether the runner starting at each base was caught stealing it (indices 1-3 used; index 0 unused).</summary>
    public bool[] CsFlag { get; } = new bool[4];

    /// <summary>Whether the runner starting at each base was picked off (indices 1-3 used; index 0 unused).</summary>
    public bool[] PoFlag { get; } = new bool[4];

    /// <summary>The position number of the fielder given primary credit for fielding the ball, or 0 if not applicable.</summary>
    public int FieldedBy { get; set; }

    /// <summary>The fielders (by position number) credited with putouts on this play, in the order credited.</summary>
    public List<int> Putouts { get; } = [];

    /// <summary>The fielders (by position number) credited with assists on this play, in the order credited.</summary>
    public List<int> Assists { get; } = [];

    /// <summary>The errors charged on this play, in the order they occurred.</summary>
    public List<FieldingError> Errors { get; } = [];

    /// <summary>
    /// The fielders (by position number) who touched the ball on this play, in order - intended
    /// for identifying the players involved in a double or triple play.
    /// </summary>
    public List<int> Touches { get; } = [];

    /// <summary>The type of batted ball: <c>G</c> ground ball, <c>F</c> fly ball, <c>L</c> line drive, <c>P</c> pop-up, or a space if not recorded.</summary>
    public char BattedBallType { get; set; } = ' ';

    /// <summary>The hit location code (e.g. <c>78</c>), or empty if not recorded.</summary>
    public string HitLocation { get; set; } = "";

    /// <summary>
    /// Whether this event type is one that is charged to the batter as a plate appearance
    /// outcome (as opposed to, e.g., a stolen base or wild pitch that happens independent of the
    /// batter's own plate appearance).
    /// </summary>
    public bool IsBatter =>
        EventType == PlayEventType.GenericOut ||
        EventType == PlayEventType.Strikeout ||
        (EventType >= PlayEventType.Walk && EventType <= PlayEventType.HomeRun);

    /// <summary>Whether this event counts as an official at-bat.</summary>
    public bool IsOfficialAtBat
    {
        get
        {
            if (!IsBatter)
            {
                return false;
            }

            if (ShFlag || SfFlag ||
                EventType == PlayEventType.Walk ||
                EventType == PlayEventType.IntentionalWalk ||
                EventType == PlayEventType.HitByPitch ||
                EventType == PlayEventType.Interference)
            {
                return false;
            }

            return true;
        }
    }

    /// <summary>Whether the runner who started at <paramref name="baseIndex"/> was put out on this play.</summary>
    /// <param name="baseIndex">0 for the batter, 1-3 for the runners on first through third base.</param>
    public bool RunnerPutOut(int baseIndex) => Play[baseIndex].Length > 0 && !Play[baseIndex].Contains('E');

    /// <summary>The number of outs recorded on this play.</summary>
    public int OutsOnPlay
    {
        get
        {
            var outs = 0;
            for (var baseIndex = 0; baseIndex < 4; baseIndex++)
            {
                if (RunnerPutOut(baseIndex))
                {
                    outs++;
                }
            }

            return outs;
        }
    }

    /// <summary>The number of runs scored on this play.</summary>
    public int RunsOnPlay
    {
        get
        {
            var runs = 0;
            for (var baseIndex = 0; baseIndex < 4; baseIndex++)
            {
                if (Advance[baseIndex] >= 4)
                {
                    runs++;
                }
            }

            return runs;
        }
    }

    /// <summary>The number of RBIs credited on this play.</summary>
    public int RbiOnPlay
    {
        get
        {
            var rbis = 0;
            for (var baseIndex = 0; baseIndex < 4; baseIndex++)
            {
                if (RbiFlag[baseIndex] > 0)
                {
                    rbis++;
                }
            }

            return rbis;
        }
    }

    /// <summary>Creates an independent copy of this play, safe to mutate without affecting the original.</summary>
    public ParsedPlay Clone()
    {
        var clone = new ParsedPlay
        {
            EventType = EventType,
            ShFlag = ShFlag,
            SfFlag = SfFlag,
            DpFlag = DpFlag,
            GdpFlag = GdpFlag,
            TpFlag = TpFlag,
            WpFlag = WpFlag,
            PbFlag = PbFlag,
            FoulFlag = FoulFlag,
            BuntFlag = BuntFlag,
            ForceFlag = ForceFlag,
            FieldedBy = FieldedBy,
            BattedBallType = BattedBallType,
            HitLocation = HitLocation,
        };

        Array.Copy(Advance, clone.Advance, Advance.Length);
        Array.Copy(RbiFlag, clone.RbiFlag, RbiFlag.Length);
        Array.Copy(FcFlag, clone.FcFlag, FcFlag.Length);
        Array.Copy(MuffFlag, clone.MuffFlag, MuffFlag.Length);
        Array.Copy(Play, clone.Play, Play.Length);
        Array.Copy(SbFlag, clone.SbFlag, SbFlag.Length);
        Array.Copy(CsFlag, clone.CsFlag, CsFlag.Length);
        Array.Copy(PoFlag, clone.PoFlag, PoFlag.Length);

        clone.Putouts.AddRange(Putouts);
        clone.Assists.AddRange(Assists);
        clone.Errors.AddRange(Errors);
        clone.Touches.AddRange(Touches);

        return clone;
    }
}
