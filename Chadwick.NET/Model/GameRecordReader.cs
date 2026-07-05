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

using Chadwick.Core.Parsing;

namespace Chadwick.Core.Model;

/// <summary>
/// Reads one game's records from a Retrosheet event file, starting at its <c>id</c> line and
/// continuing until the next game's <c>id</c> line (which is left unconsumed) or end of file.
/// </summary>
public static class GameRecordReader
{
    /// <summary>
    /// Reads the next game from <paramref name="lineReader"/>.
    /// </summary>
    /// <param name="lineReader">
    /// A reader positioned so that its next line is a game's <c>id</c> record.
    /// </param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The parsed game.</returns>
    /// <exception cref="InvalidDataException">
    /// The next line is not a well-formed <c>id</c> record.
    /// </exception>
    public static async Task<Game> ReadNextAsync(PeekableLineReader lineReader, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(lineReader);

        var idLine = await lineReader.ReadLineAsync(cancellationToken)
            ?? throw new InvalidDataException("Expected an 'id' record, but the file ended.");
        var idFields = RetrosheetLineTokenizer.Tokenize(idLine);
        if (idFields.Count < 2 || idFields[0] != "id")
        {
            throw new InvalidDataException($"Expected an 'id' record, but found: {idLine}");
        }

        var game = new Game { GameId = idFields[1] };
        var pendingAdjustments = new PendingAdjustments();

        while (true)
        {
            var nextLine = await lineReader.PeekLineAsync(cancellationToken);
            if (nextLine is null)
            {
                break;
            }

            var fields = RetrosheetLineTokenizer.Tokenize(nextLine);
            if (fields.Count == 0 || fields[0] == "id")
            {
                break; // next game starts here; leave it for the caller
            }

            await lineReader.ReadLineAsync(cancellationToken); // consume the line now that we know it belongs to this game
            ApplyRecord(game, fields, pendingAdjustments);
        }

        return game;
    }

    private static void ApplyRecord(Game game, IReadOnlyList<string> fields, PendingAdjustments pendingAdjustments)
    {
        switch (fields[0])
        {
            case "version":
                if (fields.Count > 1)
                {
                    game.Version = fields[1];
                }
                break;

            case "info":
                if (fields.Count > 1)
                {
                    game.AddInfo(fields[1], fields.Count > 2 ? fields[2] : "");
                }
                break;

            case "start":
                if (fields.Count > 5)
                {
                    game.AddStarter(ParseAppearance(fields));
                }
                break;

            case "play":
                if (fields.Count > 6)
                {
                    game.AddEvent(BuildEvent(fields, pendingAdjustments));
                }
                break;

            case "sub":
                if (fields.Count > 5 && game.Events.Count > 0)
                {
                    game.Events[^1].AddSubstitution(ParseAppearance(fields));
                }
                break;

            case "com":
                if (fields.Count > 1)
                {
                    var comment = new Comment { Text = fields[1] };
                    if (game.Events.Count > 0)
                    {
                        game.Events[^1].AddComment(comment);
                    }
                    else
                    {
                        game.AddLeadingComment(comment);
                    }
                }
                break;

            case "data":
                game.AddDataRecord(new DataRecord { Values = fields.Skip(1).ToList() });
                break;

            case "stat":
                game.AddStatRecord(new DataRecord { Values = fields.Skip(1).ToList() });
                break;

            case "event":
                game.AddEventDetailRecord(new DataRecord { Values = fields.Skip(1).ToList() });
                break;

            case "line":
                game.AddLineScoreRecord(new DataRecord { Values = fields.Skip(1).ToList() });
                break;

            case "badj":
                if (fields.Count > 2)
                {
                    pendingAdjustments.BatterHandBatterId = fields[1];
                    pendingAdjustments.BatterHand = RetrosheetValueParser.ParseHandCode(fields[2]);
                }
                break;

            case "padj":
                if (fields.Count > 2)
                {
                    pendingAdjustments.PitcherHandPitcherId = fields[1];
                    pendingAdjustments.PitcherHand = RetrosheetValueParser.ParseHandCode(fields[2]);
                }
                break;

            case "ladj":
                if (fields.Count > 2)
                {
                    pendingAdjustments.LineupAdjustmentAlignment = RetrosheetValueParser.ParseNullableInt(fields[1]);
                    pendingAdjustments.LineupAdjustmentSlot = RetrosheetValueParser.ParseNullableInt(fields[2]);
                }
                break;

            case "radj":
            case "cw:itb": // legacy Chadwick extension with the same semantics as radj
                if (fields.Count > 2)
                {
                    pendingAdjustments.AutomaticRunnerId = fields[1];
                    pendingAdjustments.AutomaticRunnerBase = RetrosheetValueParser.ParseNullableInt(fields[2]);
                }
                break;

            case "presadj":
                if (fields.Count > 2)
                {
                    var base_ = RetrosheetValueParser.ParseNullableInt(fields[2]);
                    switch (base_)
                    {
                        case 1: pendingAdjustments.PresentPitcherAtFirstBaseId = fields[1]; break;
                        case 2: pendingAdjustments.PresentPitcherAtSecondBaseId = fields[1]; break;
                        case 3: pendingAdjustments.PresentPitcherAtThirdBaseId = fields[1]; break;
                    }
                }
                break;

            default:
                break; // unrecognized record types are skipped, matching the original tool's tolerance
        }
    }

    private static Appearance ParseAppearance(IReadOnlyList<string> fields)
    {
        return new Appearance
        {
            PlayerId = fields[1],
            Name = fields[2],
            Team = RetrosheetValueParser.ParseNullableInt(fields[3]) ?? 0,
            Slot = RetrosheetValueParser.ParseNullableInt(fields[4]) ?? 0,
            Position = RetrosheetValueParser.ParseNullableInt(fields[5]) ?? 0,
        };
    }

    private static GameEvent BuildEvent(IReadOnlyList<string> fields, PendingAdjustments pendingAdjustments)
    {
        var batter = fields[3];
        var gameEvent = new GameEvent
        {
            Inning = RetrosheetValueParser.ParseNullableInt(fields[1]) ?? 0,
            BattingTeam = RetrosheetValueParser.ParseNullableInt(fields[2]) ?? 0,
            Batter = batter,
            Count = fields[4],
            Pitches = fields[5],
            PlayText = fields[6],
        };

        // A batter-hand adjustment only takes effect while it keeps matching the current
        // batter; once a different batter is up, it is discarded rather than applied. This
        // mirrors cw_game_read's behavior in game.c exactly (including that a match leaves the
        // pending adjustment in place for any further plays by the same batter).
        if (pendingAdjustments.BatterHand.HasValue && pendingAdjustments.BatterHandBatterId == batter)
        {
            gameEvent.BatterHandOverride = pendingAdjustments.BatterHand;
        }
        else
        {
            pendingAdjustments.BatterHand = null;
            pendingAdjustments.BatterHandBatterId = null;
        }

        if (pendingAdjustments.PitcherHand.HasValue)
        {
            gameEvent.PitcherHandOverride = pendingAdjustments.PitcherHand;
            gameEvent.PitcherHandOverridePlayerId = pendingAdjustments.PitcherHandPitcherId;
            pendingAdjustments.PitcherHand = null;
            pendingAdjustments.PitcherHandPitcherId = null;
        }

        if (pendingAdjustments.LineupAdjustmentSlot is int and not 0)
        {
            gameEvent.LineupAdjustmentAlignment = pendingAdjustments.LineupAdjustmentAlignment;
            gameEvent.LineupAdjustmentSlot = pendingAdjustments.LineupAdjustmentSlot;
            pendingAdjustments.LineupAdjustmentAlignment = null;
            pendingAdjustments.LineupAdjustmentSlot = null;
        }

        if (pendingAdjustments.AutomaticRunnerBase.HasValue)
        {
            gameEvent.AutomaticRunnerId = pendingAdjustments.AutomaticRunnerId;
            gameEvent.AutomaticRunnerBase = pendingAdjustments.AutomaticRunnerBase;
            pendingAdjustments.AutomaticRunnerId = null;
            pendingAdjustments.AutomaticRunnerBase = null;
        }

        if (pendingAdjustments.PresentPitcherAtFirstBaseId is not null)
        {
            gameEvent.PresentPitcherAtFirstBaseId = pendingAdjustments.PresentPitcherAtFirstBaseId;
            pendingAdjustments.PresentPitcherAtFirstBaseId = null;
        }

        if (pendingAdjustments.PresentPitcherAtSecondBaseId is not null)
        {
            gameEvent.PresentPitcherAtSecondBaseId = pendingAdjustments.PresentPitcherAtSecondBaseId;
            pendingAdjustments.PresentPitcherAtSecondBaseId = null;
        }

        if (pendingAdjustments.PresentPitcherAtThirdBaseId is not null)
        {
            gameEvent.PresentPitcherAtThirdBaseId = pendingAdjustments.PresentPitcherAtThirdBaseId;
            pendingAdjustments.PresentPitcherAtThirdBaseId = null;
        }

        return gameEvent;
    }

    /// <summary>
    /// Tracks adjustment records (<c>badj</c>/<c>padj</c>/<c>ladj</c>/<c>radj</c>/<c>presadj</c>)
    /// seen since the last <c>play</c> record, so they can be attached to the next one - mirroring
    /// the local state <c>cw_game_read</c> keeps across lines in game.c.
    /// </summary>
    private sealed class PendingAdjustments
    {
        public string? BatterHandBatterId;
        public char? BatterHand;
        public string? PitcherHandPitcherId;
        public char? PitcherHand;
        public int? LineupAdjustmentAlignment;
        public int? LineupAdjustmentSlot;
        public string? AutomaticRunnerId;
        public int? AutomaticRunnerBase;
        public string? PresentPitcherAtFirstBaseId;
        public string? PresentPitcherAtSecondBaseId;
        public string? PresentPitcherAtThirdBaseId;
    }
}
