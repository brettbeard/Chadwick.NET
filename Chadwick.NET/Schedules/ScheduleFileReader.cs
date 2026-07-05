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

using System.Globalization;
using Chadwick.Core.Parsing;

namespace Chadwick.Core.Schedules;

/// <summary>
/// Reads a Retrosheet original-schedule file - one row per originally planned game, in the form
/// <c>date,game_number,day,visitor,league,visitor_game_number,home,league,home_game_number,
/// time_of_day,postponement_notes,makeup_date</c> - into a sequence of <see cref="ScheduledGame"/>
/// records.
/// </summary>
/// <remarks>
/// Schedule files are just another named file inside a Retrosheet <c>.zip</c> archive or
/// extracted directory, so this reader takes an already-open <see cref="Stream"/> and leaves
/// locating the right file to the caller via
/// <see cref="Chadwick.Core.FileSources.IRetrosheetFileSource"/> - the same division of
/// responsibility <see cref="Chadwick.Core.Scorebook.ScorebookReader"/> uses for event files.
/// No schedule-specific file-naming convention is assumed here: most seasons' archives
/// contain one file named <c>{year}schedule.csv</c>, but 2020's archive contains two
/// differently-named files (the pre-pandemic and shortened schedules), so guessing a single
/// fixed name would be wrong for at least that season.
/// </remarks>
public static class ScheduleFileReader
{
    private const int ExpectedFieldCount = 12;
    private const string DateFormat = "yyyyMMdd";

    /// <summary>
    /// Reads every scheduled game from <paramref name="stream"/>.
    /// </summary>
    /// <param name="stream">The open schedule file stream.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The scheduled games found in the file, in file order.</returns>
    public static async Task<IReadOnlyList<ScheduledGame>> ReadAllGamesAsync(Stream stream, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var scheduledGames = new List<ScheduledGame>();
        using var reader = new StreamReader(stream);
        var hasCheckedForHeaderRow = false;

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            if (line.Length == 0)
            {
                continue;
            }

            var fields = RetrosheetLineTokenizer.Tokenize(line);
            if (fields.Count < ExpectedFieldCount)
            {
                continue; // blank or malformed line; skip, matching the rest of this library's tolerance
            }

            if (!hasCheckedForHeaderRow)
            {
                hasCheckedForHeaderRow = true;
                if (!DateOnly.TryParseExact(fields[0], DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
                {
                    continue; // the file's column-name header row, not a game
                }
            }

            try
            {
                scheduledGames.Add(ParseScheduledGame(fields));
            }
            catch (FormatException)
            {
                // A single corrupted row shouldn't prevent reading the rest of the season.
            }
        }

        return scheduledGames;
    }

    private static ScheduledGame ParseScheduledGame(IReadOnlyList<string> fields)
    {
        var makeupDates = new List<DateOnly>();
        var makeupFieldNotes = new List<string>();
        ParseMakeupField(fields[11], makeupDates, makeupFieldNotes);

        return new ScheduledGame
        {
            Date = DateOnly.ParseExact(fields[0], DateFormat, CultureInfo.InvariantCulture),
            Slot = ParseSlot(fields[1]),
            DayOfWeek = ParseDayOfWeek(fields[2]),
            VisitingTeamId = fields[3],
            VisitingTeamLeague = fields[4],
            VisitingTeamGameNumber = int.Parse(fields[5], CultureInfo.InvariantCulture),
            HomeTeamId = fields[6],
            HomeTeamLeague = fields[7],
            HomeTeamGameNumber = int.Parse(fields[8], CultureInfo.InvariantCulture),
            TimeOfDay = ParseTimeOfDay(fields[9]),
            PostponementNotes = SplitNotes(fields[10]),
            MakeupDates = makeupDates,
            MakeupFieldNotes = makeupFieldNotes,
        };
    }

    private static ScheduledGameSlot ParseSlot(string field)
    {
        return field switch
        {
            "0" => ScheduledGameSlot.SingleGame,
            "1" => ScheduledGameSlot.DoubleHeaderGame1,
            "2" => ScheduledGameSlot.DoubleHeaderGame2,
            _ => throw new FormatException($"Unrecognized game-number field '{field}'."),
        };
    }

    private static DayOfWeek ParseDayOfWeek(string field)
    {
        return field switch
        {
            "Sun" => DayOfWeek.Sunday,
            "Mon" => DayOfWeek.Monday,
            "Tue" => DayOfWeek.Tuesday,
            "Wed" => DayOfWeek.Wednesday,
            "Thu" => DayOfWeek.Thursday,
            "Fri" => DayOfWeek.Friday,
            "Sat" => DayOfWeek.Saturday,
            _ => throw new FormatException($"Unrecognized day-of-week field '{field}'."),
        };
    }

    private static ScheduledGameTimeOfDay ParseTimeOfDay(string field)
    {
        return field switch
        {
            "D" => ScheduledGameTimeOfDay.Day,
            "N" => ScheduledGameTimeOfDay.Night,
            "A" => ScheduledGameTimeOfDay.Afternoon,
            "E" => ScheduledGameTimeOfDay.Evening,
            _ => throw new FormatException($"Unrecognized time-of-day field '{field}'."),
        };
    }

    private static IReadOnlyList<string> SplitNotes(string field)
    {
        if (field.Length == 0)
        {
            return [];
        }

        var notes = new List<string>();
        foreach (var note in field.Split(';'))
        {
            var trimmedNote = note.Trim();
            if (trimmedNote.Length > 0)
            {
                notes.Add(trimmedNote);
            }
        }

        return notes;
    }

    /// <summary>
    /// Splits the makeup field into its date and non-date entries. Each semicolon-separated
    /// token is classified individually: a token that parses as a date is a makeup date (a
    /// second one means the first makeup attempt was itself postponed); a token that doesn't
    /// parse as a date is treated as a note instead (this is how a folded team's replacement
    /// code shows up in this field, per the schedule format's documentation).
    /// </summary>
    private static void ParseMakeupField(string field, List<DateOnly> makeupDates, List<string> makeupFieldNotes)
    {
        if (field.Length == 0)
        {
            return;
        }

        foreach (var token in field.Split(';'))
        {
            var trimmedToken = token.Trim();
            if (trimmedToken.Length == 0)
            {
                continue;
            }

            if (DateOnly.TryParseExact(trimmedToken, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var makeupDate))
            {
                makeupDates.Add(makeupDate);
            }
            else
            {
                makeupFieldNotes.Add(trimmedToken);
            }
        }
    }
}
