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
/// A single <c>play</c> record: one plate appearance's outcome, along with any substitutions or
/// comments that followed it in the event file before the next play.
/// </summary>
/// <remarks>
/// The adjustment fields (<see cref="BatterHandOverride"/>, <see cref="PitcherHandOverride"/>,
/// etc.) come from separate <c>badj</c>/<c>padj</c>/<c>ladj</c>/<c>radj</c>/<c>presadj</c>
/// records that precede a <c>play</c> record in the file and apply to it. They are <see
/// langword="null"/> when no such adjustment applied to this play.
/// </remarks>
public sealed class GameEvent
{
    private readonly List<Appearance> _substitutions = new();
    private readonly List<Comment> _comments = new();

    /// <summary>
    /// The inning number this play occurred in (1-based).
    /// </summary>
    public int Inning { get; init; }

    /// <summary>
    /// The batting team: 0 for the visiting team, 1 for the home team.
    /// </summary>
    public int BattingTeam { get; init; }

    /// <summary>
    /// The batter's Retrosheet player ID.
    /// </summary>
    public required string Batter { get; init; }

    /// <summary>
    /// The ball-strike count when this play was recorded (e.g. <c>12</c>), or <c>??</c> if
    /// unknown.
    /// </summary>
    public required string Count { get; init; }

    /// <summary>
    /// The pitch sequence leading to this play (e.g. <c>CBX</c>), or empty if not recorded.
    /// </summary>
    public required string Pitches { get; init; }

    /// <summary>
    /// The Retrosheet play-text describing the outcome (e.g. <c>S7.2-H;1-3</c>). Parsing this
    /// into a structured event is the responsibility of the play-string parser, not this model.
    /// </summary>
    public required string PlayText { get; init; }

    /// <summary>
    /// A <c>badj</c> override of the batter's normal batting hand for this play, if one applied.
    /// </summary>
    public char? BatterHandOverride { get; set; }

    /// <summary>
    /// A <c>padj</c> override of the responsible pitcher's throwing hand for this play, if one
    /// applied.
    /// </summary>
    public char? PitcherHandOverride { get; set; }

    /// <summary>
    /// The pitcher whose hand is overridden by <see cref="PitcherHandOverride"/>, if any.
    /// </summary>
    public string? PitcherHandOverridePlayerId { get; set; }

    /// <summary>
    /// A <c>ladj</c> lineup-alignment adjustment (which team's lineup is affected), if one
    /// applied to this play.
    /// </summary>
    public int? LineupAdjustmentAlignment { get; set; }

    /// <summary>
    /// A <c>ladj</c> batting-order slot adjustment, if one applied to this play.
    /// </summary>
    public int? LineupAdjustmentSlot { get; set; }

    /// <summary>
    /// The runner ID placed on base by a <c>radj</c> automatic-runner record, if one applied to
    /// this play (used for extra-inning automatic-runner rules).
    /// </summary>
    public string? AutomaticRunnerId { get; set; }

    /// <summary>
    /// The base (1, 2, or 3) the automatic runner from <see cref="AutomaticRunnerId"/> was
    /// placed on, if applicable.
    /// </summary>
    public int? AutomaticRunnerBase { get; set; }

    /// <summary>
    /// A <c>presadj</c> override of the pitcher of record for first base, if one applied to this
    /// play.
    /// </summary>
    public string? PresentPitcherAtFirstBaseId { get; set; }

    /// <summary>
    /// A <c>presadj</c> override of the pitcher of record for second base, if one applied to
    /// this play.
    /// </summary>
    public string? PresentPitcherAtSecondBaseId { get; set; }

    /// <summary>
    /// A <c>presadj</c> override of the pitcher of record for third base, if one applied to this
    /// play.
    /// </summary>
    public string? PresentPitcherAtThirdBaseId { get; set; }

    /// <summary>
    /// The substitutions (<c>sub</c> records) that occurred immediately after this play, in file
    /// order.
    /// </summary>
    public IReadOnlyList<Appearance> Substitutions => _substitutions;

    /// <summary>
    /// The comments (<c>com</c> records) that occurred immediately after this play, in file
    /// order.
    /// </summary>
    public IReadOnlyList<Comment> Comments => _comments;

    /// <summary>
    /// Adds a substitution that occurred after this play.
    /// </summary>
    /// <param name="substitution">The substitution to add.</param>
    public void AddSubstitution(Appearance substitution)
    {
        ArgumentNullException.ThrowIfNull(substitution);
        _substitutions.Add(substitution);
    }

    /// <summary>
    /// Adds a comment that occurred after this play.
    /// </summary>
    /// <param name="comment">The comment to add.</param>
    public void AddComment(Comment comment)
    {
        ArgumentNullException.ThrowIfNull(comment);
        _comments.Add(comment);
    }
}
