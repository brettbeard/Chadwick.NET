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
using Chadwick.Core.Parsing;

namespace Chadwick.Core.Simulation;

/// <summary>
/// Walks a <see cref="Game"/>'s events one at a time, maintaining a <see cref="GameState"/> that
/// reflects the score, lineups, and baserunners as of the current event.
/// </summary>
/// <remarks>
/// A close port of Chadwick's <c>CWGameIterator</c> in <c>gameiter.c</c>. Where the C version
/// walks a linked list of events via <c>next</c> pointers, this uses a plain index into
/// <see cref="Model.Game.Events"/> (a <c>List&lt;T&gt;</c>-backed collection per this port's
/// domain-model design) - functionally identical, just without needing pointer-chasing.
/// </remarks>
public sealed class GameIterator
{
    private int _eventIndex;

    /// <summary>The game being iterated.</summary>
    public Game Game { get; }

    /// <summary>The event at the current position, or <see langword="null"/> if iteration has passed the last event.</summary>
    public GameEvent? CurrentEvent => _eventIndex < Game.Events.Count ? Game.Events[_eventIndex] : null;

    /// <summary>
    /// The parsed play data for <see cref="CurrentEvent"/>, or <see langword="null"/> if there is
    /// no current event, or the current event is an <c>"NP"</c> ("no play") placeholder.
    /// </summary>
    /// <remarks>
    /// When advancing past an <c>"NP"</c> event, this deliberately retains the previous real
    /// play's data rather than clearing it, matching the original C behavior where the
    /// underlying struct is simply left unwritten for that turn.
    /// </remarks>
    public ParsedPlay? EventData { get; private set; }

    /// <summary>Whether <see cref="CurrentEvent"/>'s play text parsed successfully.</summary>
    public bool ParseOk { get; private set; }

    /// <summary>The game state as of the current event.</summary>
    public GameState State { get; private set; } = new();

    /// <summary>Creates an iterator positioned at the first event of <paramref name="game"/>.</summary>
    public GameIterator(Game game)
    {
        ArgumentNullException.ThrowIfNull(game);
        Game = game;
        Reset();
    }

    /// <summary>Creates an independent copy of this iterator, safe to advance without affecting the original - used internally to look ahead when computing runner fates.</summary>
    public GameIterator Clone()
    {
        var clone = new GameIterator(Game)
        {
            _eventIndex = _eventIndex,
            ParseOk = ParseOk,
            EventData = EventData?.Clone(),
            State = State.Clone(),
        };
        return clone;
    }

    /// <summary>Resets the iterator to the beginning of the game.</summary>
    public void Reset()
    {
        _eventIndex = 0;
        State = new GameState();

        var date = Game.GetInfo("date");
        if (date is { Length: >= 10 })
        {
            // Reformats "yyyy/mm/dd" to "yyyymmdd" by dropping the two slash separators.
            State.Date = $"{date[0]}{date[1]}{date[2]}{date[3]}{date[5]}{date[6]}{date[8]}{date[9]}";
        }

        SetupLineups();

        State.BattingTeam = Game.GetInfo("htbf") == "true" ? 1 : 0;

        var currentEvent = CurrentEvent;
        if (currentEvent is null)
        {
            return;
        }

        if (currentEvent.PlayText != "NP")
        {
            State.BatterHand = currentEvent.BatterHandOverride;
            State.PitcherHand = currentEvent.PitcherHandOverride;
            ParseOk = PlayStringParser.TryParse(currentEvent.PlayText, out var parsed);
            EventData = parsed;
        }
        else
        {
            // There are some very rare instances of an NP as the first play in a game.
            ParseOk = true;
            EventData = null;
        }
    }

    /// <summary>Advances the iterator by one event, updating <see cref="State"/> with the outcome of the event just left behind.</summary>
    public void Next()
    {
        var currentEvent = CurrentEvent ?? throw new InvalidOperationException("Cannot advance past the end of the game.");

        if (currentEvent.PlayText != "NP")
        {
            State.Update(currentEvent.Batter, EventData!);
        }
        else
        {
            State.BatterHand = currentEvent.BatterHandOverride;
            State.PitcherHand = currentEvent.PitcherHandOverride;
        }

        ProcessComments(currentEvent);
        ProcessSubstitutions(currentEvent);

        // Move on to the next event and parse it. A few CWEventData-equivalent fields are
        // context-dependent and can't be inferred from the event text alone; the rest of this
        // method handles those cases.
        _eventIndex++;
        var nextEvent = CurrentEvent;

        if (nextEvent is not null && (State.Inning != nextEvent.Inning || State.BattingTeam != nextEvent.BattingTeam))
        {
            // We change sides whenever the event file says the inning or batting team changes,
            // rather than only after three outs - trusting the file's own inning/team fields.
            State.ChangeSides(nextEvent.Inning, nextEvent.BattingTeam);
        }

        if (nextEvent?.LineupAdjustmentSlot is int and not 0)
        {
            State.NextBatter[State.BattingTeam] = nextEvent.LineupAdjustmentSlot.Value;
        }

        if (nextEvent?.AutomaticRunnerBase is int and not 0)
        {
            State.PlaceRunner(nextEvent.AutomaticRunnerBase.Value, nextEvent.AutomaticRunnerId!);
        }

        if (nextEvent is not null)
        {
            if (nextEvent.PresentPitcherAtFirstBaseId is not null)
            {
                State.Runners[1].PitcherId = nextEvent.PresentPitcherAtFirstBaseId;
            }

            if (nextEvent.PresentPitcherAtSecondBaseId is not null)
            {
                State.Runners[2].PitcherId = nextEvent.PresentPitcherAtSecondBaseId;
            }

            if (nextEvent.PresentPitcherAtThirdBaseId is not null)
            {
                State.Runners[3].PitcherId = nextEvent.PresentPitcherAtThirdBaseId;
            }
        }

        if (nextEvent is null)
        {
            EventData = null;
            return;
        }

        if (nextEvent.PlayText == "NP")
        {
            // EventData is deliberately left as-is here; see the property's remarks.
            return;
        }

        State.BatterHand = nextEvent.BatterHandOverride;
        State.PitcherHand = nextEvent.PitcherHandOverride;
        ParseOk = PlayStringParser.TryParse(nextEvent.PlayText, out var parsed);
        EventData = parsed;

        for (var baseIndex = 1; baseIndex <= 3; baseIndex++)
        {
            if (EventData.Advance[baseIndex] == 0 &&
                State.BaseOccupied(baseIndex) &&
                !EventData.RunnerPutOut(baseIndex))
            {
                EventData.Advance[baseIndex] = baseIndex;
            }
        }

        if ((EventData.EventType == PlayEventType.Error && State.Outs == 2 && EventData.RbiFlag[3] == 1) ||
            ((EventData.EventType == PlayEventType.Walk || EventData.EventType == PlayEventType.IntentionalWalk) &&
             (!State.BaseOccupied(2) || !State.BaseOccupied(1))))
        {
            EventData.RbiFlag[3] = 0;
        }

        for (var baseIndex = 0; baseIndex <= 3; baseIndex++)
        {
            if (EventData.RbiFlag[baseIndex] == 2)
            {
                EventData.RbiFlag[baseIndex] = 1;
            }
        }
    }

    /// <summary>Computes the eventual "fate" of the runner on <paramref name="baseIndex"/>: 0 if put out, 1-3 the base eventually reached, or 4+ if the runner scores.</summary>
    public int RunnerFate(int baseIndex)
    {
        if (EventData is null)
        {
            return 0;
        }

        if (EventData.Advance[baseIndex] == 0 || EventData.Advance[baseIndex] >= 4)
        {
            return EventData.Advance[baseIndex];
        }

        var fateBase = EventData.Advance[baseIndex];
        var simulation = Clone();
        simulation.Next();

        while (simulation.CurrentEvent is not null &&
               simulation.State.Inning == State.Inning &&
               simulation.State.BattingTeam == State.BattingTeam)
        {
            if (simulation.CurrentEvent.PlayText != "NP")
            {
                fateBase = simulation.EventData!.Advance[fateBase];
                if (fateBase < 1 || fateBase > 3)
                {
                    break;
                }
            }

            simulation.Next();
        }

        return fateBase;
    }

    private void SetupLineups()
    {
        foreach (var starter in Game.Starters)
        {
            State.Lineups[starter.Slot, starter.Team].PlayerId = starter.PlayerId;
            State.Lineups[starter.Slot, starter.Team].Name = starter.Name;
            State.Lineups[starter.Slot, starter.Team].Position = starter.Position;

            if (starter.Position <= 9)
            {
                State.Fielders[starter.Position, starter.Team] = starter.PlayerId;
            }
            else if (starter.Position == 10)
            {
                State.DhSlot[starter.Team] = starter.Slot;
            }
        }
    }

    private void ProcessComments(GameEvent gameEvent)
    {
        foreach (var comment in gameEvent.Comments)
        {
            if (comment.Text.StartsWith("suspended,", StringComparison.Ordinal))
            {
                var parts = comment.Text.Split(',');
                if (parts.Length > 1)
                {
                    State.Date = parts[1];
                }
            }
        }
    }

    private void ProcessSubstitutions(GameEvent gameEvent)
    {
        foreach (var substitution in gameEvent.Substitutions)
        {
            State.Substitute(gameEvent.Batter, gameEvent.Count, substitution.PlayerId, substitution.Name, substitution.Team, substitution.Slot, substitution.Position);
        }
    }
}
