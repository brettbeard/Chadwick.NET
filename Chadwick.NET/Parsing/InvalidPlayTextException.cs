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
/// Thrown when a <c>play</c> record's play text could not be parsed and so cannot be applied to
/// game state.
/// </summary>
/// <remarks>
/// <para>
/// Play-text parsing populates its result incrementally and can fail part-way through, so a play
/// that does not parse is not a play that is merely missing a detail or two - it is a play whose
/// runners, outs, and credits are unknown. Applying one to game state would quietly corrupt
/// every total accumulated from that point on, so
/// <see cref="Simulation.GameIterator.Next"/> throws this instead.
/// </para>
/// <para>
/// Chadwick's own <c>cwbox</c> handles the same situation at the game level: <c>cw_game_lint</c>
/// reports the parse failure and <c>cwbox</c> skips the whole game rather than printing a
/// boxscore built on top of it. Callers that want to make that decision before iterating - and
/// so never see this exception - can run <see cref="Validation.GameValidator.Validate"/> first.
/// </para>
/// </remarks>
public sealed class InvalidPlayTextException : Exception
{
    /// <summary>Creates an exception for a play that failed to parse.</summary>
    /// <param name="gameId">The ID of the game the play belongs to, if known.</param>
    /// <param name="eventNumber">
    /// The play's 1-based position among the game's plays (<c>"NP"</c> placeholders excluded), or
    /// 0 if not known.
    /// </param>
    /// <param name="playText">The play text that failed to parse.</param>
    public InvalidPlayTextException(string? gameId, int eventNumber, string playText)
        : base(BuildMessage(gameId, eventNumber, playText))
    {
        GameId = gameId;
        EventNumber = eventNumber;
        PlayText = playText;
    }

    /// <summary>
    /// Creates an exception for a play that failed to parse, where the game and the play's
    /// position in it are not known - as when play text is parsed on its own.
    /// </summary>
    /// <param name="playText">The play text that failed to parse.</param>
    public InvalidPlayTextException(string playText)
        : this(null, 0, playText)
    {
    }

    /// <summary>The ID of the game the play belongs to, or <see langword="null"/> if not known.</summary>
    public string? GameId { get; }

    /// <summary>
    /// The play's 1-based position among the game's plays (<c>"NP"</c> placeholders excluded), or
    /// 0 if not known. This matches the event numbering Chadwick's own <c>cw_game_lint</c>
    /// reports, which counts plays rather than <c>play</c> records.
    /// </summary>
    public int EventNumber { get; }

    /// <summary>The play text that failed to parse.</summary>
    public string PlayText { get; }

    private static string BuildMessage(string? gameId, int eventNumber, string playText)
    {
        var where = (gameId, eventNumber) switch
        {
            (null, <= 0) => "",
            (null, _) => $" at event {eventNumber}",
            (_, <= 0) => $" in game {gameId}",
            _ => $" in game {gameId} at event {eventNumber}",
        };

        return $"Invalid play string \"{playText}\"{where}; it cannot be applied to game state.";
    }
}
