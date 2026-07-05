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
/// Provides access to the files that make up a Retrosheet season archive - team files, roster
/// files, and event files - without the rest of the library needing to know whether they come
/// from a <c>.zip</c> archive or a plain directory on disk.
/// </summary>
public interface IRetrosheetFileSource : IDisposable
{
    /// <summary>
    /// Lists the names of all files available from this source.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<IReadOnlyList<string>> GetFileNamesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Opens a readable stream for the file named <paramref name="fileName"/>.
    /// </summary>
    /// <param name="fileName">The exact file name, as returned by <see cref="GetFileNamesAsync"/>.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="FileNotFoundException">No file with that name exists in this source.</exception>
    Task<Stream> OpenFileAsync(string fileName, CancellationToken cancellationToken);
}
