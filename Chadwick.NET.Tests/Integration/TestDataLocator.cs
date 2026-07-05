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

namespace Chadwick.NET.Tests.Integration;

/// <summary>
/// Locates the repository's <c>data/</c> directory of real Retrosheet sample files, by walking
/// upward from the test binary's directory. Shared by the integration test classes that run
/// against real season data.
/// </summary>
internal static class TestDataLocator
{
    /// <summary>Finds the repository's <c>data/</c> directory, or <see langword="null"/> if none is found.</summary>
    public static string? FindDataDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "data");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return null;
    }

    /// <summary>
    /// Finds a file at <paramref name="relativePath"/> under the repository's <c>data/</c>
    /// directory (e.g. <c>"play-by-play/1968eve.zip"</c>), or <see langword="null"/> if the
    /// <c>data/</c> directory or the file itself cannot be found.
    /// </summary>
    public static string? FindDataFile(string relativePath)
    {
        var dataDirectory = FindDataDirectory();
        if (dataDirectory is null)
        {
            return null;
        }

        var candidate = Path.Combine(dataDirectory, relativePath);
        return File.Exists(candidate) ? candidate : null;
    }
}
