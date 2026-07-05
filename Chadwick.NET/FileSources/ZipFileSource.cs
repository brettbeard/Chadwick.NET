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

using System.IO.Compression;

namespace Chadwick.Core.FileSources;

/// <summary>
/// An <see cref="IRetrosheetFileSource"/> backed by a <c>.zip</c> archive containing a
/// Retrosheet season's team, roster, and event files - read directly without extracting to disk.
/// </summary>
public sealed class ZipFileSource : IRetrosheetFileSource
{
    private readonly ZipArchive _archive;

    /// <summary>
    /// Opens the <c>.zip</c> archive at <paramref name="zipFilePath"/> for reading.
    /// </summary>
    /// <param name="zipFilePath">Path to the season archive.</param>
    public ZipFileSource(string zipFilePath)
    {
        ArgumentNullException.ThrowIfNull(zipFilePath);
        _archive = ZipFile.OpenRead(zipFilePath);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetFileNamesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<string> fileNames = _archive.Entries
            .Where(entry => entry.Name.Length > 0) // skip directory entries
            .Select(entry => entry.Name)
            .ToList();

        return Task.FromResult(fileNames);
    }

    /// <inheritdoc />
    public async Task<Stream> OpenFileAsync(string fileName, CancellationToken cancellationToken)
    {
        var entry = _archive.Entries.FirstOrDefault(e => string.Equals(e.Name, fileName, StringComparison.OrdinalIgnoreCase))
            ?? throw new FileNotFoundException($"Entry '{fileName}' was not found in the archive.", fileName);

        // Zip entry streams don't support seeking, and only one can be open at a time per
        // archive in some implementations, so buffer the entry into memory and hand back a
        // fully independent, seekable stream.
        var buffer = new MemoryStream();
        await using (var entryStream = entry.Open())
        {
            await entryStream.CopyToAsync(buffer, cancellationToken);
        }

        buffer.Position = 0;
        return buffer;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _archive.Dispose();
    }
}
