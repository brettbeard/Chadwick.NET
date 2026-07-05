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
/// Wraps a <see cref="TextReader"/> to allow looking at the next line without consuming it.
/// Used when parsing a stream of games from a single event file, where recognizing that a new
/// game has started requires reading its first line - which must then be left for that game's
/// own reader to consume.
/// </summary>
public sealed class PeekableLineReader
{
    private readonly TextReader _reader;
    private string? _peekedLine;
    private bool _hasPeekedLine;

    /// <summary>
    /// Wraps <paramref name="reader"/> for peekable line-by-line reading.
    /// </summary>
    /// <param name="reader">The underlying reader. Ownership stays with the caller.</param>
    public PeekableLineReader(TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);
        _reader = reader;
    }

    /// <summary>
    /// Returns the next line without consuming it; a subsequent <see cref="ReadLineAsync"/> or
    /// <see cref="PeekLineAsync"/> call will return the same line.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The next line, or <see langword="null"/> at end of stream.</returns>
    public async Task<string?> PeekLineAsync(CancellationToken cancellationToken)
    {
        if (!_hasPeekedLine)
        {
            _peekedLine = await _reader.ReadLineAsync(cancellationToken);
            _hasPeekedLine = true;
        }

        return _peekedLine;
    }

    /// <summary>
    /// Consumes and returns the next line.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The next line, or <see langword="null"/> at end of stream.</returns>
    public async Task<string?> ReadLineAsync(CancellationToken cancellationToken)
    {
        if (_hasPeekedLine)
        {
            _hasPeekedLine = false;
            var line = _peekedLine;
            _peekedLine = null;
            return line;
        }

        return await _reader.ReadLineAsync(cancellationToken);
    }
}
