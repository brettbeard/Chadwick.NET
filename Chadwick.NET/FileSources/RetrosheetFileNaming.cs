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
/// Computes the conventional file names Retrosheet uses for a season's team and roster files,
/// so callers can ask an <see cref="IRetrosheetFileSource"/> for them by name.
/// </summary>
public static class RetrosheetFileNaming
{
    /// <summary>
    /// The team file for a season, conventionally named <c>TEAMyyyy</c> (e.g. <c>TEAM1968</c>).
    /// </summary>
    /// <param name="year">The four-digit season year.</param>
    public static string TeamFileName(int year) => $"TEAM{year}";

    /// <summary>
    /// The roster file for one team in a season, conventionally named <c>{team}{yyyy}.ROS</c>
    /// (e.g. <c>BAL1968.ROS</c>).
    /// </summary>
    /// <param name="teamId">The team's Retrosheet ID (e.g. <c>BAL</c>).</param>
    /// <param name="year">The four-digit season year.</param>
    public static string RosterFileName(string teamId, int year) => $"{teamId}{year}.ROS";
}
