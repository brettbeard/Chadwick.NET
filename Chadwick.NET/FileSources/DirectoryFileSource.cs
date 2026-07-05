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

namespace Chadwick.Core.FileSources;

/// <summary>
/// An <see cref="IRetrosheetFileSource"/> backed by a plain directory of already-extracted
/// Retrosheet files.
/// </summary>
public sealed class DirectoryFileSource : IRetrosheetFileSource
{
    private readonly string _directoryPath;

    /// <summary>
    /// Creates a file source rooted at <paramref name="directoryPath"/>.
    /// </summary>
    /// <param name="directoryPath">The directory containing the Retrosheet files.</param>
    /// <exception cref="DirectoryNotFoundException">The directory does not exist.</exception>
    public DirectoryFileSource(string directoryPath)
    {
        ArgumentNullException.ThrowIfNull(directoryPath);
        if (!Directory.Exists(directoryPath))
        {
            throw new DirectoryNotFoundException($"Directory '{directoryPath}' was not found.");
        }

        _directoryPath = directoryPath;
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<string>> GetFileNamesAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<string> fileNames = Directory.EnumerateFiles(_directoryPath)
            .Select(Path.GetFileName)
            .Where(name => name is not null)
            .Select(name => name!)
            .ToList();

        return Task.FromResult(fileNames);
    }

    /// <inheritdoc />
    public Task<Stream> OpenFileAsync(string fileName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var fullPath = Path.Combine(_directoryPath, fileName);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"File '{fileName}' was not found in '{_directoryPath}'.", fileName);
        }

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
        return Task.FromResult(stream);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // No unmanaged resources are held between calls; each stream is disposed by its caller.
    }
}
