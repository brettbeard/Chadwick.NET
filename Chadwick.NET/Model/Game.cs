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
/// One game's full record as read from a Retrosheet event file: its metadata, starting lineups,
/// play-by-play events, and any auxiliary data (earned runs, linescores, box-score detail).
/// </summary>
public sealed class Game
{
    private readonly List<InfoRecord> _infoRecords = new();
    private readonly List<Appearance> _starters = new();
    private readonly List<GameEvent> _events = new();
    private readonly List<DataRecord> _dataRecords = new();
    private readonly List<DataRecord> _statRecords = new();
    private readonly List<DataRecord> _lineScoreRecords = new();
    private readonly List<DataRecord> _eventDetailRecords = new();
    private readonly List<Comment> _leadingComments = new();

    /// <summary>
    /// The game's unique Retrosheet ID (e.g. <c>BAL196804100</c>).
    /// </summary>
    public required string GameId { get; init; }

    /// <summary>
    /// The event file format version, from the <c>version</c> record, or <see langword="null"/>
    /// if none was present.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// The game's <c>info</c> metadata records, in file order.
    /// </summary>
    public IReadOnlyList<InfoRecord> InfoRecords => _infoRecords;

    /// <summary>
    /// The starting lineups for both teams, in file order.
    /// </summary>
    public IReadOnlyList<Appearance> Starters => _starters;

    /// <summary>
    /// The play-by-play events, in chronological order.
    /// </summary>
    public IReadOnlyList<GameEvent> Events => _events;

    /// <summary>
    /// The <c>data</c> records (currently used only for pitchers' earned runs), in file order.
    /// </summary>
    public IReadOnlyList<DataRecord> DataRecords => _dataRecords;

    /// <summary>
    /// The <c>stat</c> records, in file order.
    /// </summary>
    public IReadOnlyList<DataRecord> StatRecords => _statRecords;

    /// <summary>
    /// The <c>line</c> records (linescores by inning), present only in box-score event files, in
    /// file order.
    /// </summary>
    public IReadOnlyList<DataRecord> LineScoreRecords => _lineScoreRecords;

    /// <summary>
    /// The <c>event</c> records (event detail), present only in box-score event files, in file
    /// order.
    /// </summary>
    public IReadOnlyList<DataRecord> EventDetailRecords => _eventDetailRecords;

    /// <summary>
    /// Comments that appeared before this game's first play event.
    /// </summary>
    public IReadOnlyList<Comment> LeadingComments => _leadingComments;

    /// <summary>
    /// Finds this game's most recently recorded value for the info field named
    /// <paramref name="label"/>. When a label appears more than once (which happens with, e.g.,
    /// duplicated edit-date records), the last one recorded wins, matching Retrosheet's own
    /// convention.
    /// </summary>
    /// <param name="label">The info field's name (e.g. <c>visteam</c>).</param>
    /// <returns>The field's value, or <see langword="null"/> if the label is not present.</returns>
    public string? GetInfo(string label)
    {
        for (var index = _infoRecords.Count - 1; index >= 0; index--)
        {
            if (_infoRecords[index].Label == label)
            {
                return _infoRecords[index].Data;
            }
        }

        return null;
    }

    /// <summary>
    /// Finds the starting-lineup appearance for <paramref name="team"/> in batting-order
    /// <paramref name="slot"/>.
    /// </summary>
    /// <param name="team">0 for the visiting team, 1 for the home team.</param>
    /// <param name="slot">The batting-order slot (0-9; slot 0 is a non-batting starting pitcher when the DH is in effect).</param>
    public Appearance? FindStarter(int team, int slot)
    {
        foreach (var starter in _starters)
        {
            if (starter.Team == team && starter.Slot == slot)
            {
                return starter;
            }
        }

        return null;
    }

    /// <summary>
    /// Adds an info record to the game.
    /// </summary>
    /// <param name="label">The info field's name.</param>
    /// <param name="data">The info field's value.</param>
    public void AddInfo(string label, string data)
    {
        _infoRecords.Add(new InfoRecord { Label = label, Data = data });
    }

    /// <summary>
    /// Adds a starting lineup entry to the game.
    /// </summary>
    /// <param name="starter">The starter to add.</param>
    public void AddStarter(Appearance starter)
    {
        ArgumentNullException.ThrowIfNull(starter);
        _starters.Add(starter);
    }

    /// <summary>
    /// Adds a play event to the game.
    /// </summary>
    /// <param name="gameEvent">The event to add.</param>
    public void AddEvent(GameEvent gameEvent)
    {
        ArgumentNullException.ThrowIfNull(gameEvent);
        _events.Add(gameEvent);
    }

    /// <summary>
    /// Adds a <c>data</c> record to the game.
    /// </summary>
    /// <param name="record">The record to add.</param>
    public void AddDataRecord(DataRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        _dataRecords.Add(record);
    }

    /// <summary>
    /// Adds a <c>stat</c> record to the game.
    /// </summary>
    /// <param name="record">The record to add.</param>
    public void AddStatRecord(DataRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        _statRecords.Add(record);
    }

    /// <summary>
    /// Adds a <c>line</c> record to the game.
    /// </summary>
    /// <param name="record">The record to add.</param>
    public void AddLineScoreRecord(DataRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        _lineScoreRecords.Add(record);
    }

    /// <summary>
    /// Adds an <c>event</c> detail record to the game.
    /// </summary>
    /// <param name="record">The record to add.</param>
    public void AddEventDetailRecord(DataRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        _eventDetailRecords.Add(record);
    }

    /// <summary>
    /// Adds a comment that appeared before this game's first play event.
    /// </summary>
    /// <param name="comment">The comment to add.</param>
    public void AddLeadingComment(Comment comment)
    {
        ArgumentNullException.ThrowIfNull(comment);
        _leadingComments.Add(comment);
    }
}
