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

using Chadwick.Core.Model;
using Chadwick.Core.Simulation;

namespace Chadwick.Core.Validation;

/// <summary>
/// Checks a <see cref="Game"/> for internal consistency - plays that do not parse, and plays
/// whose outcome contradicts the state of the game they occur in.
/// </summary>
/// <remarks>
/// <para>
/// A port of Chadwick's <c>cw_game_lint</c> in <c>lint.c</c>. Where the C version reports each
/// problem to <c>stderr</c> and returns a single pass/fail flag, this returns the problems
/// themselves, since library code must stay UI-agnostic and a caller generally wants to say
/// which game it is skipping and why.
/// </para>
/// <para>
/// This is the screening step Chadwick's <c>cwbox</c> runs before building a boxscore: a game
/// that fails it is skipped entirely rather than reported from play-by-play that is known to be
/// wrong. Running it is also how a caller avoids
/// <see cref="Parsing.InvalidPlayTextException"/> from <see cref="GameIterator.Next"/>.
/// </para>
/// </remarks>
public static class GameValidator
{
    /// <summary>
    /// Checks <paramref name="game"/> and returns every problem found, or an empty list if the
    /// game is internally consistent.
    /// </summary>
    /// <remarks>
    /// Checking stops at the first play found to have a problem, as the C original's loop does.
    /// This is deliberate rather than incidental: the checks after it compare a play against the
    /// game state built up from the plays before it, and that state is only as trustworthy as
    /// the last play applied to it - so once one play is known to be wrong, further findings say
    /// more about the accumulated state than about the plays themselves. Problems in the starter
    /// records are all reported together, and stop the play-by-play checks from running at all.
    /// </remarks>
    /// <param name="game">The game to check.</param>
    public static IReadOnlyList<GameProblem> Validate(Game game)
    {
        ArgumentNullException.ThrowIfNull(game);

        var problems = new List<GameProblem>();
        ValidateStarters(game, problems);
        if (problems.Count > 0)
        {
            return problems;
        }

        var iterator = new GameIterator(game);
        while (iterator.CurrentEvent is not null)
        {
            if (iterator.CurrentEvent.PlayText != "NP")
            {
                ValidateCurrentPlay(iterator, problems);
                if (problems.Count > 0)
                {
                    break;
                }
            }

            iterator.Next();
        }

        return problems;
    }

    private static void ValidateStarters(Game game, List<GameProblem> problems)
    {
        foreach (var starter in game.Starters)
        {
            if (starter.Slot is < 0 or > 9)
            {
                problems.Add(new GameProblem(
                    GameProblemKind.InvalidStarterSlot,
                    $"Invalid slot {starter.Slot} for player '{starter.PlayerId}'.",
                    PlayerId: starter.PlayerId));
            }

            if (starter.Team is < 0 or > 1)
            {
                problems.Add(new GameProblem(
                    GameProblemKind.InvalidStarterTeam,
                    $"Invalid team {starter.Team} for player '{starter.PlayerId}'.",
                    PlayerId: starter.PlayerId));
            }

            if (starter.Position is < 1 or > 10)
            {
                problems.Add(new GameProblem(
                    GameProblemKind.InvalidStarterPosition,
                    $"Invalid position {starter.Position} for player '{starter.PlayerId}'.",
                    PlayerId: starter.PlayerId));
            }
        }
    }

    private static void ValidateCurrentPlay(GameIterator iterator, List<GameProblem> problems)
    {
        var state = iterator.State;
        var gameEvent = iterator.CurrentEvent!;
        var eventNumber = state.EventCount + 1;
        var batter = gameEvent.Batter;

        if (iterator.EventData is not { } play)
        {
            // Unlike the C original, which goes on to check a partially-filled event struct, there
            // is nothing here to check further: a failed parse yields no play at all.
            problems.Add(new GameProblem(
                GameProblemKind.InvalidPlayText,
                $"Invalid play string \"{gameEvent.PlayText}\" ({batter} batting).",
                eventNumber,
                gameEvent.PlayText));
            return;
        }

        if (play.DpFlag && play.OutsOnPlay < 2)
        {
            problems.Add(new GameProblem(
                GameProblemKind.DoublePlayWithoutTwoOuts,
                $"Fewer than two outs on play marked DP (event \"{gameEvent.PlayText}\", {batter} batting).",
                eventNumber,
                gameEvent.PlayText));
        }

        if (play.TpFlag && play.OutsOnPlay < 3)
        {
            problems.Add(new GameProblem(
                GameProblemKind.TriplePlayWithoutThreeOuts,
                $"Fewer than three outs on play marked TP (event \"{gameEvent.PlayText}\", {batter} batting).",
                eventNumber,
                gameEvent.PlayText));
        }

        for (var source = 1; source <= 3; source++)
        {
            if (!state.BaseOccupied(source) && (play.Advance[source] != 0 || play.RunnerPutOut(source)))
            {
                problems.Add(new GameProblem(
                    GameProblemKind.AdvanceFromEmptyBase,
                    $"Advancement from empty base {source} (event \"{gameEvent.PlayText}\", {batter} batting).",
                    eventNumber,
                    gameEvent.PlayText));
            }
        }

        for (var destination = 1; destination <= 3; destination++)
        {
            if (!state.BaseOccupied(destination))
            {
                continue;
            }

            for (var source = 0; source < destination; source++)
            {
                var sourceAdvance = play.Advance[source];
                var destinationAdvance = play.Advance[destination];

                if (sourceAdvance >= destinationAdvance &&
                    !play.RunnerPutOut(destination) &&
                    destinationAdvance < 4 &&
                    state.Outs + play.OutsOnPlay < 3)
                {
                    problems.Add(new GameProblem(
                        GameProblemKind.RunnerOvertaken,
                        $"Runner on {destination} overtaken by runner on {source} (event \"{gameEvent.PlayText}\", {batter} batting).",
                        eventNumber,
                        gameEvent.PlayText));
                }
            }
        }
    }
}
