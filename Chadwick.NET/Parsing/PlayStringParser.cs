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

using System.Text;
using Chadwick.Core.Model;

namespace Chadwick.Core.Parsing;

/// <summary>
/// Parses a Retrosheet play-text string (e.g. <c>S7.2-H;1-3</c> or <c>64(1)3/GDP</c>) into a
/// structured <see cref="ParsedPlay"/>.
/// </summary>
/// <remarks>
/// This is a close, function-by-function port of Chadwick's recursive-descent parser in
/// <c>parse.c</c>, deliberately kept structurally close to the original rather than restructured
/// into more idiomatic C# - the play-text grammar has many special cases and historical quirks
/// that are easy to silently break by "cleaning up" the logic, so fidelity to the reference
/// implementation is prioritized over elegance here.
/// </remarks>
public static class PlayStringParser
{
    /// <summary>
    /// Attempts to parse <paramref name="playText"/> into a structured play.
    /// </summary>
    /// <param name="playText">The play-text portion of a <c>play</c> record (e.g. <c>S7.2-H;1-3</c>).</param>
    /// <param name="parsedPlay">The parsed play, if parsing succeeded.</param>
    /// <returns><see langword="true"/> if the text was valid play-text; otherwise <see langword="false"/>.</returns>
    public static bool TryParse(string playText, out ParsedPlay parsedPlay)
    {
        ArgumentNullException.ThrowIfNull(playText);

        parsedPlay = new ParsedPlay();
        var state = new ParserState(playText);

        ParsePrimaryEvent(state);

        if (state.Token.Length == 0)
        {
            parsedPlay.EventType = PlayEventType.GenericOut;
            if (!ParseGenericOut(state, parsedPlay, true))
            {
                return false;
            }
        }
        else
        {
            var matched = false;
            foreach (var entry in PrimaryTable)
            {
                if (entry.Token == state.Token)
                {
                    parsedPlay.EventType = entry.EventCode;
                    if (!entry.ParseFunc(state, parsedPlay, true))
                    {
                        return false;
                    }

                    matched = true;
                    break;
                }
            }

            if (!matched)
            {
                return false;
            }
        }

        if (state.Sym == '.')
        {
            if (!ParseAdvancement(state, parsedPlay))
            {
                return false;
            }
        }

        if (state.Sym == '+' || state.Sym == '-' || state.Sym == '#')
        {
            state.NextSym();
        }

        if (state.Sym != '\0')
        {
            return false;
        }

        SanityCheck(parsedPlay);
        return true;
    }

    /// <summary>
    /// Tracks the parser's position and last-read token as it scans through the play-text
    /// string. Mirrors Chadwick's <c>CWParserState</c>; kept private since it's purely an
    /// implementation detail of the parsing algorithm.
    /// </summary>
    private sealed class ParserState
    {
        public char Sym;
        public readonly string InputString;
        public int InputPos;
        public string Token = "";

        public ParserState(string input)
        {
            var chars = input.ToUpperInvariant().ToCharArray();
            var current = new string(chars);

            // Preprocessing to turn SBH and CSH strings into SB4 and CS4.
            var sbhIndex = current.IndexOf("SBH", StringComparison.Ordinal);
            if (sbhIndex >= 0)
            {
                chars[sbhIndex + 2] = '4';
                current = new string(chars);
            }

            var cshIndex = current.IndexOf("CSH", StringComparison.Ordinal);
            if (cshIndex >= 0 && !current.Contains("FCSH", StringComparison.Ordinal))
            {
                chars[cshIndex + 2] = '4';
            }

            InputString = new string(chars);
            Sym = InputString.Length > 0 ? InputString[0] : '\0';
            InputPos = 1;
        }

        /// <summary>Advances to the next character, silently skipping the human-annotation characters '#' and '!'.</summary>
        public char NextSym()
        {
            if (InputPos > InputString.Length)
            {
                Sym = '\0';
                return Sym;
            }

            do
            {
                Sym = InputPos < InputString.Length ? InputString[InputPos] : '\0';
                InputPos++;
            } while (Sym == '#' || Sym == '!');

            return Sym;
        }

        /// <summary>Returns the next character without advancing the parser.</summary>
        public char Peek() => InputPos >= InputString.Length ? ' ' : InputString[InputPos];
    }

    private static bool IsFielder(char c) => (c >= '1' && c <= '9') || c == '?';

    /// <summary>
    /// Reports a parse failure. Named distinctly from "error" since that word already means
    /// something else in baseball, matching Chadwick's own naming choice.
    /// </summary>
    private static bool ParseInvalid(ParserState state) => false;

    private static void ParsePrimaryEvent(ParserState state)
    {
        var token = new StringBuilder();
        while (state.Sym is >= 'A' and <= 'Z')
        {
            token.Append(state.Sym);
            state.NextSym();
        }

        state.Token = token.ToString();
    }

    private static void ParseHitFielder(ParserState state, ParsedPlay play)
    {
        if (char.IsDigit(state.Sym))
        {
            play.FieldedBy = state.Sym - '0';
        }

        while (IsFielder(state.Sym) || state.Sym == '0')
        {
            state.NextSym();
        }
    }

    /// <summary>
    /// Reads a fielding credit (e.g. <c>643</c> or <c>E4</c>), assigning putout/assist/error
    /// credit as it goes.
    /// </summary>
    /// <param name="state">The parser state.</param>
    /// <param name="play">The play being built.</param>
    /// <param name="prev">
    /// The previous fielder in a chain like <c>64(1)3</c>, where the second call needs to also
    /// credit fielder '4' as having touched the ball before '3', or ' ' if there is none.
    /// </param>
    /// <returns><see langword="true"/> if the runner/batter was safe due to a muffed throw; otherwise <see langword="false"/>.</returns>
    private static bool ParseFieldingCredit(ParserState state, ParsedPlay play, char prev)
    {
        var token = new StringBuilder();
        var lastChar = state.Sym;
        var assists = new List<int>();

        if (state.Sym == 'E')
        {
            state.NextSym();
            if (!IsFielder(state.Sym))
            {
                return ParseInvalid(state);
            }

            if (char.IsDigit(state.Sym) && play.EventType != PlayEventType.Interference)
            {
                // Special case: C.B-1(E2) shouldn't generate a second error.
                play.Errors.Add(new FieldingError(state.Sym - '0', 'F'));
            }

            token.Append('E').Append(state.Sym);
            state.Token = token.ToString();
            state.NextSym();
            return true;
        }

        if (prev != ' ' && prev != state.Sym)
        {
            assists.Add(prev - '0');
            token.Append(prev);
        }

        token.Append(state.Sym);

        while (true)
        {
            state.NextSym();

            if ((state.Sym >= '1' && state.Sym <= '9') || state.Sym == '?')
            {
                if (char.IsDigit(lastChar))
                {
                    assists.Add(lastChar - '0');
                    AddUniqueTouch(play, lastChar - '0');
                }

                if (state.Sym != '?')
                {
                    token.Append(state.Sym);
                }

                lastChar = state.Sym;
            }
            else if (state.Sym == 'E')
            {
                if (char.IsDigit(lastChar))
                {
                    assists.Add(lastChar - '0');
                }

                token.Append('E');
                state.NextSym();
                if (!char.IsDigit(state.Sym))
                {
                    return ParseInvalid(state);
                }

                play.Errors.Add(new FieldingError(state.Sym - '0', 'D'));
                token.Append(state.Sym);
                state.Token = token.ToString();
                state.NextSym();

                AddUniqueAssists(play, assists);
                return true;
            }
            else
            {
                if (char.IsDigit(lastChar))
                {
                    play.Putouts.Add(lastChar - '0');
                    AddUniqueTouch(play, lastChar - '0');
                }

                state.Token = token.ToString();

                AddUniqueAssists(play, assists);
                return false;
            }
        }
    }

    private static void AddUniqueTouch(ParsedPlay play, int fielder)
    {
        if (play.Touches.Count == 0 || play.Touches[^1] != fielder)
        {
            play.Touches.Add(fielder);
        }
    }

    private static void AddUniqueAssists(ParsedPlay play, List<int> assists)
    {
        for (var i = 0; i < assists.Count; i++)
        {
            var isDuplicate = false;
            for (var j = 0; j < i; j++)
            {
                if (assists[j] == assists[i])
                {
                    isDuplicate = true;
                    break;
                }
            }

            if (!isDuplicate)
            {
                play.Assists.Add(assists[i]);
            }
        }
    }

    private static void ParseFlag(ParserState state)
    {
        var token = new StringBuilder();
        while (true)
        {
            state.NextSym();
            if (state.Sym != '/' && state.Sym != '.' && state.Sym != '(' && state.Sym != ')' &&
                state.Sym != '#' && state.Sym != '!' && state.Sym != '+' && state.Sym != '-' &&
                state.Sym != '\0')
            {
                token.Append(state.Sym);
            }
            else
            {
                break;
            }
        }

        state.Token = token.ToString();
    }

    private static bool ParseAdvanceModifier(ParserState state, ParsedPlay play, bool safe, int baseFrom, int baseTo)
    {
        var isError = false;

        if (IsFielder(state.Sym) || state.Sym == 'E')
        {
            if (state.Sym == 'E')
            {
                isError = true;
            }

            if (ParseFieldingCredit(state, play, ' '))
            {
                isError = true;
                if (!safe)
                {
                    safe = true;
                    play.MuffFlag[baseFrom] = true;
                    if (play.Advance[baseFrom] < 5)
                    {
                        // Guards against things like 3XH(UR)(5E2); the (UR) already implies
                        // the runner is safe, so the advancement is already set.
                        play.Advance[baseFrom] = baseTo;
                    }

                    if (baseFrom == 0 && play.EventType == PlayEventType.Strikeout)
                    {
                        RemoveImpliedStrikeoutPutout(play);
                    }

                    for (var i = baseFrom; i >= 0; i--)
                    {
                        play.RbiFlag[i] = -1;
                    }
                }
            }
            else if (baseFrom == 0 && play.EventType == PlayEventType.Strikeout)
            {
                RemoveImpliedStrikeoutPutout(play);
            }

            var tokenFirstChar = state.Token.Length > 0 ? state.Token[0] : '\0';
            if (tokenFirstChar != 'E')
            {
                play.Play[baseFrom] = state.Token;
            }
            else
            {
                for (var i = baseFrom; i >= 0; i--)
                {
                    play.RbiFlag[i] = -1;
                }
            }

            if (state.Sym == '/')
            {
                ParseFlag(state);
                if (state.Token is "TH" or "TH1" or "TH2" or "TH3" or "THH")
                {
                    if (isError && play.Errors.Count > 0)
                    {
                        // /TH occasionally appears at the end of an out credit; without this
                        // check, an earlier error in the string would be misflagged as throwing.
                        play.Errors[^1] = play.Errors[^1] with { ErrorType = 'T' };
                    }
                }
                else if (state.Token is "INT" or "BINT" or "OBS" or "G" or "U" or "AP" or "BR" or "FO")
                {
                    // interference/obstruction/appeal-play/relay/force-out modifiers accepted silently
                }
                else
                {
                    return ParseInvalid(state);
                }
            }

            if (state.Sym == '(')
            {
                state.NextSym();
                if (!ParseAdvanceModifier(state, play, safe, baseFrom, baseTo))
                {
                    return false;
                }
            }

            // Tolerates weird constructs like '2XH(9S)', seen in 1989 files.
            while (state.Sym != ')' && state.Sym != '\0')
            {
                state.NextSym();
            }
        }
        else
        {
            ParsePrimaryEvent(state);

            if (state.Token is "NR" or "NORBI")
            {
                play.RbiFlag[baseFrom] = 0;
            }
            else if (state.Token == "RBI" && play.Advance[baseFrom] >= 4)
            {
                // rbi_flag == 2 means (RBI) is actually present, overriding rare ambiguity.
                play.RbiFlag[baseFrom] = 2;
            }
            else if (state.Token == "UR")
            {
                play.Advance[baseFrom] = 5;
            }
            else if (state.Token == "TUR")
            {
                play.Advance[baseFrom] = 6;
            }
            else if (state.Token == "WP")
            {
                play.WpFlag = true;
                play.RbiFlag[baseFrom] = 0;
            }
            else if (state.Token == "PB")
            {
                play.PbFlag = true;
                play.RbiFlag[baseFrom] = 0;
            }
            else if (state.Token == "TH")
            {
                if (state.Sym is >= '1' and <= '3')
                {
                    state.NextSym();
                }
            }
            else if (state.Token is "THH" or "INT")
            {
                // silently accept interference flag
            }
            else
            {
                return ParseInvalid(state);
            }
        }

        if (state.Sym == ')')
        {
            state.NextSym();
            return true;
        }

        return ParseInvalid(state);
    }

    private static void RemoveImpliedStrikeoutPutout(ParsedPlay play)
    {
        // Special case: for K.BX1(2E3) and similar, remove the implied putout for the catcher
        // (always listed first in the putout/touch list for a bare strikeout).
        if (play.Putouts.Count > 0)
        {
            play.Putouts.RemoveAt(0);
        }

        if (play.Touches.Count > 0)
        {
            play.Touches.RemoveAt(0);
        }
    }

    private static readonly string[] Locations =
    [
        "1", "13", "15", "1S", "2", "2F", "23", "23F", "25", "25F",
        "3SF", "3F", "3DF", "3S", "3", "3D", "34S", "34", "34D",
        "4S", "4", "4D", "4MS", "4M", "4MD",
        "6MS", "6M", "6MD", "6S", "6", "6D",
        "56S", "56", "56D", "5S", "5", "5D", "5SF", "5F", "5DF",
        "7LSF", "7LS", "7S", "78S", "8S", "89S", "9S", "9LS", "9LSF",
        "7LF", "7L", "7", "78", "8", "89", "9", "9L", "9LF",
        "7LDF", "7LD", "7D", "78D", "8D", "89D", "9D", "9LD", "9LDF",
        "78XD", "8XD", "89XD",
        // The following locations are nonstandard or archaic, but appear in existing Retrosheet data.
        "13S", "15S", "2LF", "2RF", "2L", "2R", "3L", "46", "5L",
        "7LDW", "7DW", "78XDW", "8XDW", "89XDW", "9DW", "9LDW",
        "7LMF", "7LM", "7M", "78M", "8LM", "8M", "8RM", "89M", "9M", "9LM", "9LMF",
        "8LS", "8RS", "8LD", "8RD", "8LXD", "8RXD", "8LXDW", "8RXDW",
    ];

    private static bool IsLocation(string text) => Array.IndexOf(Locations, text) >= 0;

    private static void ParseFlags(ParserState state, ParsedPlay play)
    {
        do
        {
            var flag = new StringBuilder("/");

            do
            {
                state.NextSym();
                if (state.Sym != '/' && state.Sym != '.' && state.Sym != '#' && state.Sym != '!' &&
                    state.Sym != '+' && state.Sym != '-' && state.Sym != '\0')
                {
                    flag.Append(state.Sym);
                }
            } while (state.Sym != '/' && state.Sym != '.' && state.Sym != '#' && state.Sym != '!' &&
                     state.Sym != '+' && state.Sym != '-' && state.Sym != '\0');

            ApplyFlag(play, flag.ToString());
        } while (state.Sym != '.' && state.Sym != '\0');
    }

    private static void ApplyFlag(ParsedPlay play, string flag)
    {
        if (flag is "/SH" or "/SAC")
        {
            play.ShFlag = true;
            play.BuntFlag = true;
        }
        else if (flag == "/SF")
        {
            play.SfFlag = true;
            // Unless marked otherwise, a /SF is considered a fly ball. Special case: plays like
            // E4/SF award a sac fly when an infielder drops a fly, overriding the default.
            if (play.BattedBallType == ' ' || (play.EventType == PlayEventType.Error && play.BattedBallType == 'G'))
            {
                play.BattedBallType = 'F';
            }
        }
        else if (flag == "/DP")
        {
            play.DpFlag = true;
        }
        else if (flag == "/GDP")
        {
            play.DpFlag = true;
            play.GdpFlag = true;
            play.BattedBallType = 'G';
        }
        else if (flag == "/LDP")
        {
            play.DpFlag = true;
            play.BattedBallType = 'L';
        }
        else if (flag == "/FDP")
        {
            play.DpFlag = true;
            play.BattedBallType = 'F';
        }
        else if (flag == "/BGDP")
        {
            play.BuntFlag = true;
            play.DpFlag = true;
            play.GdpFlag = true;
            play.BattedBallType = 'G';
        }
        else if (flag == "/BPDP")
        {
            play.BuntFlag = true;
            play.DpFlag = true;
            play.BattedBallType = 'P';
        }
        else if (flag == "/BFDP")
        {
            // Grammatically this would be bunt-fly double play, but it is interpreted as
            // bunt-foul double play.
            play.BuntFlag = true;
            play.DpFlag = true;
            play.BattedBallType = 'P';
            play.FoulFlag = true;
        }
        else if (flag == "/TP")
        {
            play.TpFlag = true;
        }
        else if (flag == "/GTP")
        {
            play.TpFlag = true;
            play.BattedBallType = 'G';
        }
        else if (flag == "/LTP")
        {
            play.TpFlag = true;
            play.BattedBallType = 'L';
        }
        else if (flag == "/FL")
        {
            play.FoulFlag = true;
        }
        else if (flag == "/FO")
        {
            play.ForceFlag = true;
            if (play.BattedBallType == ' ')
            {
                play.BattedBallType = 'G';
            }
        }
        else if ((flag is "/TH" or "/TH1" or "/TH2" or "/TH3" or "/THH") &&
                 (play.EventType == PlayEventType.Error || play.EventType == PlayEventType.PickoffError))
        {
            if (play.Errors.Count > 0)
            {
                play.Errors[0] = play.Errors[0] with { ErrorType = 'T' };
            }
        }
        else if (flag == "/B")
        {
            play.BuntFlag = true;
        }
        else if (flag == "/BG")
        {
            play.BuntFlag = true;
            play.BattedBallType = 'G';
        }
        else if (flag == "/BP")
        {
            play.BuntFlag = true;
            play.BattedBallType = 'P';
        }
        else if (flag == "/BF")
        {
            play.BuntFlag = true;
            play.BattedBallType = 'F';
        }
        else if (flag == "/BL")
        {
            play.BuntFlag = true;
            play.BattedBallType = 'L';
        }
        else if (flag == "/F")
        {
            play.BattedBallType = 'F';
        }
        else if (flag == "/G")
        {
            play.BattedBallType = 'G';
        }
        else if (flag == "/L")
        {
            play.BattedBallType = 'L';
        }
        else if (flag is "/P" or "/IF")
        {
            // Infield fly is assumed to be a popup.
            play.BattedBallType = 'P';
        }
        else if (flag.Length >= 3)
        {
            var traj = flag[1] == 'B' ? flag[2] : flag[1];
            if (traj is 'G' or 'F' or 'P' or 'L')
            {
                var loc = flag[1] == 'B' ? flag[3..] : flag[2..];
                if (IsLocation(loc))
                {
                    play.BattedBallType = traj;
                    play.HitLocation = loc;
                    if (loc.EndsWith('F'))
                    {
                        play.FoulFlag = true;
                    }

                    if (flag[1] == 'B')
                    {
                        play.BuntFlag = true;
                    }
                }
            }
            else
            {
                var loc = flag[1] == 'B' ? flag[2..] : flag[1..];
                if (IsLocation(loc))
                {
                    play.HitLocation = loc;
                    if (loc.EndsWith('F'))
                    {
                        play.FoulFlag = true;
                    }

                    if (flag[1] == 'B')
                    {
                        play.BuntFlag = true;
                    }
                }
            }
        }
        else
        {
            var loc = flag.Length > 1 ? flag[1..] : "";
            if (IsLocation(loc))
            {
                play.HitLocation = loc;
            }
        }
    }

    private static bool ParseBalk(ParserState state, ParsedPlay play, bool flags)
    {
        while (flags && state.Sym == '/')
        {
            ParseFlag(state);
            // /OBS (obstruction) is silently accepted; no other flags make sense on a balk.
        }

        return true;
    }

    private static bool ParseStolenBase(ParserState state, ParsedPlay play, bool flags)
    {
        if (state.Sym == '2')
        {
            play.SbFlag[1] = true;
            play.Advance[1] = 2;
            state.NextSym();
        }
        else if (state.Sym == '3')
        {
            play.SbFlag[2] = true;
            play.Advance[2] = 3;
            state.NextSym();
        }
        else if (state.Sym == '4')
        {
            // SBH is converted to SB4 during preprocessing.
            play.SbFlag[3] = true;
            play.Advance[3] = 4;
            state.NextSym();

            if (state.Sym == '(')
            {
                // Special case: accept archaic SBH(UR) or SBH(TUR).
                play.Advance[3] = 5;
                state.NextSym();
                if (state.Sym == 'T')
                {
                    play.Advance[3] = 6;
                    state.NextSym();
                }

                if (state.Sym != 'U')
                {
                    return ParseInvalid(state);
                }

                state.NextSym();
                if (state.Sym != 'R')
                {
                    return ParseInvalid(state);
                }

                state.NextSym();
                if (state.Sym != ')')
                {
                    return ParseInvalid(state);
                }

                state.NextSym();
            }
        }
        else
        {
            return ParseInvalid(state);
        }

        if (state.Sym == ';')
        {
            state.NextSym();
            ParsePrimaryEvent(state);

            if (state.Token == "SB")
            {
                ParseStolenBase(state, play, false);
            }
            else if (state.Token == "CS")
            {
                // Chadwick extension: under modern rules a SB and CS can't share a play, but
                // early history has instances of both, so we permit it.
                ParseCaughtStealing(state, play, false);
            }
            else
            {
                return ParseInvalid(state);
            }
        }

        while (flags && state.Sym == '/')
        {
            // Flags accepted silently; most common is /INT, occasionally /R or /U (relay).
            ParseFlag(state);
        }

        return true;
    }

    private static bool ParseCaughtStealing(ParserState state, ParsedPlay play, bool flags)
    {
        if (state.Sym < '2' || state.Sym > '4')
        {
            return ParseInvalid(state);
        }

        // CSH is converted to CS4 during preprocessing.
        var runner = state.Sym - '1';
        play.CsFlag[runner] = true;

        while (state.NextSym() == '(')
        {
            state.NextSym();
            if (IsFielder(state.Sym))
            {
                if (ParseFieldingCredit(state, play, ' '))
                {
                    play.Advance[runner] = runner + 1;
                    play.MuffFlag[runner] = true;
                    play.Play[runner] = state.Token;

                    if (state.Sym == '/')
                    {
                        ParseFlag(state);
                        if (state.Token is "TH" or "TH1" or "TH2" or "TH3" or "THH")
                        {
                            if (play.Errors.Count > 0)
                            {
                                play.Errors[^1] = play.Errors[^1] with { ErrorType = 'T' };
                            }
                        }
                        else if (state.Token == "INT")
                        {
                            // accept interference flag silently
                        }
                        else
                        {
                            return ParseInvalid(state);
                        }
                    }
                }
                else
                {
                    play.Play[runner] = state.Token;
                }
            }
            else if (state.Sym == 'E')
            {
                ParseFieldingCredit(state, play, ' ');
                play.Advance[runner] = runner + 1;
                play.MuffFlag[runner] = true;
                play.Play[runner] = state.Token;

                if (state.Sym == '/')
                {
                    ParseFlag(state);
                    if (state.Token is "TH" or "TH1" or "TH2" or "TH3" or "THH")
                    {
                        if (play.Errors.Count > 0)
                        {
                            play.Errors[^1] = play.Errors[^1] with { ErrorType = 'T' };
                        }
                    }
                    else if (state.Token == "INT")
                    {
                        // accept interference flag silently
                    }
                    else
                    {
                        return ParseInvalid(state);
                    }
                }
            }
            else if (char.IsLetter(state.Sym))
            {
                ParsePrimaryEvent(state);

                if (state.Token == "UR" && play.Advance[runner] == 4)
                {
                    play.Advance[runner] = 5;
                }
                else if (state.Token == "TUR" && play.Advance[runner] == 4)
                {
                    play.Advance[runner] = 6;
                }
                else
                {
                    return ParseInvalid(state);
                }

                if (state.Sym != ')')
                {
                    return ParseInvalid(state);
                }
            }
        }

        if (state.Sym == ';')
        {
            // Two caught stealings can happen, though they're rare.
            state.NextSym();
            ParsePrimaryEvent(state);

            if (state.Token == "CS")
            {
                ParseCaughtStealing(state, play, false);
            }
            else if (state.Token == "SB")
            {
                // Chadwick extension; see ParseStolenBase's matching comment.
                ParseStolenBase(state, play, false);
            }
            else
            {
                return ParseInvalid(state);
            }
        }

        while (flags && state.Sym == '/')
        {
            ParseFlag(state);
            if (state.Token == "DP")
            {
                play.DpFlag = true;
            }
            // other flags accepted silently
        }

        // This loop only ever examines and updates index 0, mirroring an apparent quirk in the
        // original C (`cw_parse_caught_stealing`) where the loop counter is unused inside the
        // body; preserved exactly rather than "fixed", per the port's fidelity requirement.
        if (play.Errors.Count > 0)
        {
            for (var i = 0; i < play.Errors.Count; i++)
            {
                if (play.Errors[0].ErrorType == 'F')
                {
                    play.Errors[0] = play.Errors[0] with { ErrorType = 'D' };
                }
            }
        }

        return true;
    }

    private static bool ParseSafeOnError(ParserState state, ParsedPlay play, bool flags)
    {
        play.Advance[0] = 1;

        // Chadwick extension: E0 accepted for "reached on error, unknown fielder" is not
        // possible here since '0' isn't in range; only digits 1-9 are valid fielders for E.
        if (state.Sym < '0' || state.Sym > '9')
        {
            return ParseInvalid(state);
        }

        play.Errors.Add(new FieldingError(state.Sym - '0', 'F'));
        play.FieldedBy = state.Sym - '0';
        play.BattedBallType = state.Sym <= '6' ? 'G' : 'F';
        state.NextSym();

        // Special case: writing En? for a really bad play.
        if (state.Sym == '?')
        {
            state.NextSym();
        }

        if (flags && state.Sym == '/')
        {
            ParseFlags(state, play);
        }

        return true;
    }

    private static bool ParseFieldersChoice(ParserState state, ParsedPlay play, bool flags)
    {
        play.Advance[0] = 1;
        play.BattedBallType = 'G';

        if (state.Sym >= '1' && state.Sym <= '9')
        {
            play.FieldedBy = state.Sym - '0';
            state.NextSym();
        }
        else if (state.Sym == '?')
        {
            state.NextSym();
        }

        if (flags && state.Sym == '/')
        {
            ParseFlags(state, play);
        }

        return true;
    }

    private static bool ParseFoulError(ParserState state, ParsedPlay play, bool flags)
    {
        if (state.Sym >= '1' && state.Sym <= '9')
        {
            play.Errors.Add(new FieldingError(state.Sym - '0', 'F'));
            play.FieldedBy = state.Sym - '0';
            state.NextSym();
        }
        else
        {
            return ParseInvalid(state);
        }

        if (flags && state.Sym == '/')
        {
            // Most likely a trajectory code.
            ParseFlags(state, play);
        }

        return true;
    }

    /// <summary>Parses the parenthesized force-play base notation. Returns the base (0 for batter), or -1 on error.</summary>
    private static int ParseOutBase(ParserState state)
    {
        state.NextSym();
        if (state.Sym != '1' && state.Sym != '2' && state.Sym != '3' && state.Sym != 'B')
        {
            ParseInvalid(state);
            return -1;
        }

        var baseNumber = state.Sym == 'B' ? 0 : state.Sym - '0';

        state.NextSym();
        if (state.Sym != ')')
        {
            ParseInvalid(state);
            return -1;
        }

        state.NextSym();
        return baseNumber;
    }

    private static bool ParseGenericOut(ParserState state, ParsedPlay play, bool flags)
    {
        // Tracks the fielder who made the previous putout, to generate correct credit on plays
        // like 54(1)3/GDP.
        var lastFielder = ' ';
        var forcePlay = -1;

        if (state.Sym != '?' && (state.Sym != '9' || state.Peek() != '9'))
        {
            // Since June 2020, DWS-modified BEVENT gives fielded_by = 0 (not 9) for outs
            // starting with "99".
            play.FieldedBy = state.Sym - '0';
        }

        play.Advance[0] = 1;

        while (IsFielder(state.Sym))
        {
            var safe = ParseFieldingCredit(state, play, lastFielder);

            if (state.Sym == '(')
            {
                var basePut = ParseOutBase(state);
                if (basePut < 0)
                {
                    return false;
                }

                if (forcePlay == -1)
                {
                    forcePlay = basePut > 0 ? 1 : 0;
                }

                play.Advance[basePut] = safe ? basePut + 1 : 0;
                if (safe)
                {
                    play.MuffFlag[basePut] = true;
                }

                play.FcFlag[basePut] = true;
                if (play.BattedBallType == ' ')
                {
                    if (state.Token.Length > 1 || basePut > 0)
                    {
                        // More than one fielder implies a ground ball unless overridden by a
                        // flag later; getting the first out on a non-batter implies a bounce.
                        play.BattedBallType = 'G';
                    }
                    else if (state.Token.Length == 1 && basePut == 0)
                    {
                        play.BattedBallType = 'F';
                    }
                }

                play.Play[basePut] = state.Token;
                lastFielder = state.Token.Length > 0 ? state.Token[^1] : ' ';
            }
            else
            {
                if (state.Token.Length > 1 || lastFielder != ' ')
                {
                    // More than one fielder implies a ground ball unless overridden by a flag.
                    play.BattedBallType = 'G';
                }
                else
                {
                    play.BattedBallType = 'F';
                }

                play.Play[0] = state.Token;
                play.Advance[0] = safe ? 1 : 0;
                if (safe)
                {
                    play.MuffFlag[0] = true;
                }

                break;
            }
        }

        if (state.Sym == '+' || state.Sym == '-')
        {
            // Ignore hard/soft-hit ball modifiers.
            state.NextSym();
        }

        if (flags && state.Sym == '/')
        {
            ParseFlags(state, play);
        }

        // For 10.18(g) tracking: when the force notation is used but the first play puts out
        // the batter, we assume the ball was caught in the air, so runner responsibility should
        // not be handed off. The exception is reverse-force GDPs.
        if (forcePlay == 0 && !state.InputString.Contains("/GDP", StringComparison.Ordinal))
        {
            for (var i = 1; i <= 3; i++)
            {
                play.FcFlag[i] = false;
            }
        }

        return true;
    }

    private static bool ParseHitByPitch(ParserState state, ParsedPlay play, bool flags)
    {
        play.Advance[0] = 1;
        while (flags && state.Sym == '/')
        {
            ParseFlag(state);
            // /REV (review) is silently accepted; no other flag is currently supported.
        }

        return true;
    }

    private static bool ParseInterference(ParserState state, ParsedPlay play, bool flags)
    {
        play.Advance[0] = 1;

        while (state.Sym == '/')
        {
            ParseFlag(state);

            if (state.Token.Length > 0 && state.Token[0] == 'E' && play.Errors.Count > 0)
            {
                return ParseInvalid(state);
            }

            if (state.Token == "E2")
            {
                play.Errors.Add(new FieldingError(2, 'F'));
            }
            else if (state.Token == "E1")
            {
                play.Errors.Add(new FieldingError(1, 'F'));
            }
            else if (state.Token == "4E1")
            {
                play.Errors.Add(new FieldingError(1, 'F'));
                play.Assists.Add(4);
            }
            else if (state.Token == "E3")
            {
                play.Errors.Add(new FieldingError(3, 'F'));
            }
            else if (state.Token == "E4")
            {
                play.Errors.Add(new FieldingError(4, 'F'));
            }
            else if (state.Token == "E6")
            {
                play.Errors.Add(new FieldingError(6, 'F'));
            }
            else if (state.Token == "INT")
            {
                // silently accept redundant /INT flag
            }
            else if (state.Token == "G")
            {
                // Remember: this type of interference can also occur on a batted ball.
                play.BattedBallType = 'G';
            }
            else if (state.Token.Length >= 2)
            {
                var traj = state.Token[0] == 'B' ? state.Token[1] : state.Token[0];
                if (traj is 'G' or 'F' or 'P' or 'L')
                {
                    var loc = state.Token[0] == 'B' ? state.Token[2..] : state.Token[1..];
                    if (IsLocation(loc))
                    {
                        play.BattedBallType = traj;
                        play.HitLocation = loc;
                        if (loc.EndsWith('F'))
                        {
                            play.FoulFlag = true;
                        }

                        if (state.Token[0] == 'B')
                        {
                            play.BuntFlag = true;
                        }
                    }
                }
                else
                {
                    var loc = state.Token[0] == 'B' ? state.Token[1..] : state.Token;
                    if (IsLocation(loc))
                    {
                        play.HitLocation = loc;
                        if (loc.EndsWith('F'))
                        {
                            play.FoulFlag = true;
                        }

                        if (state.Token[0] == 'B')
                        {
                            play.BuntFlag = true;
                        }
                    }
                }
            }
            else if (IsLocation(state.Token))
            {
                play.HitLocation = state.Token;
            }
        }

        if (play.Errors.Count == 0)
        {
            play.Errors.Add(new FieldingError(2, 'F'));
        }

        play.Errors[0] = play.Errors[0] with { ErrorType = 'F' };

        return true;
    }

    private static bool ParseIndifference(ParserState state, ParsedPlay play, bool flags) => true;

    private static bool ParseOtherAdvance(ParserState state, ParsedPlay play, bool flags)
    {
        while (flags && state.Sym == '/')
        {
            ParseFlag(state);

            if (state.Token == "DP")
            {
                play.DpFlag = true;
            }
            else if (state.Token is "BINT" or "INT" or "AP" or "MREV" or "UREV" or "NDP" or "OBS")
            {
                // no action required on these flags
            }
            else if (state.Token == "TP")
            {
                play.TpFlag = true;
            }
            else if (state.Token.Length > 0 && state.Token[0] == 'R')
            {
                // accept "relay" notation flags silently
            }
            else
            {
                return ParseInvalid(state);
            }
        }

        return true;
    }

    private static bool ParsePassedBall(ParserState state, ParsedPlay play, bool flags)
    {
        play.PbFlag = true;

        while (flags && state.Sym == '/')
        {
            ParseFlag(state);
            if (state.Token == "DP")
            {
                play.DpFlag = true;
            }
        }

        return true;
    }

    private static bool ParsePickoffStolenBase(ParserState state, ParsedPlay play, bool flags)
        => ParseStolenBase(state, play, flags);

    private static bool ParsePickoffCaughtStealing(ParserState state, ParsedPlay play, bool flags)
    {
        play.PoFlag[state.Sym - '1'] = true;
        return ParseCaughtStealing(state, play, flags);
    }

    private static bool ParsePickoff(ParserState state, ParsedPlay play, bool flags)
    {
        if (state.Sym < '1' || state.Sym > '3')
        {
            return ParseInvalid(state);
        }

        var runner = state.Sym - '0';
        play.PoFlag[runner] = true;

        if (state.NextSym() != '(')
        {
            return ParseInvalid(state);
        }

        state.NextSym();
        if (IsFielder(state.Sym))
        {
            ParseFieldingCredit(state, play, ' ');
            play.Play[runner] = state.Token;
        }
        else if (state.Sym == 'E')
        {
            ParseFieldingCredit(state, play, ' ');
            play.Play[runner] = state.Token;

            if (state.Sym == '/')
            {
                ParseFlag(state);
                if (state.Token is "TH" or "TH1" or "TH2" or "TH3" or "THH")
                {
                    if (play.Errors.Count > 0)
                    {
                        play.Errors[^1] = play.Errors[^1] with { ErrorType = 'T' };
                    }
                }
                else
                {
                    return ParseInvalid(state);
                }
            }
            else if (play.Errors.Count > 0 &&
                      (play.Errors[^1].FielderPosition == 1 || play.Errors[^1].FielderPosition == 2) &&
                      play.Errors[^1].ErrorType == 'F')
            {
                // By convention, errors on the pitcher or catcher are assumed throwing errors;
                // others are assumed muffs unless explicitly marked otherwise.
                play.Errors[^1] = play.Errors[^1] with { ErrorType = 'T' };
            }
            else if (play.Errors.Count > 0)
            {
                play.Errors[^1] = play.Errors[^1] with { ErrorType = 'D' };
            }
        }
        else
        {
            return ParseInvalid(state);
        }

        if (state.Sym == ')')
        {
            state.NextSym();
        }
        else
        {
            return ParseInvalid(state);
        }

        if (flags && state.Sym == '/')
        {
            // Most likely flag is /DP.
            ParseFlags(state, play);
        }

        return true;
    }

    private static bool ParseBaseHit(ParserState state, ParsedPlay play, bool flags)
    {
        if (IsFielder(state.Sym) || state.Sym == '0')
        {
            ParseHitFielder(state, play);
        }

        if (flags && state.Sym == '/')
        {
            ParseFlags(state, play);
        }

        return true;
    }

    private static bool ParseGroundRuleDouble(ParserState state, ParsedPlay play, bool flags)
    {
        while (state.Sym >= '1' && state.Sym <= '9')
        {
            // Some newer event files list fielders after DGR, which shouldn't be possible, but
            // bevent gives no fielded-by credit for this case, so we simply skip over them.
            state.NextSym();
        }

        if (flags && state.Sym == '/')
        {
            ParseFlags(state, play);
        }

        return true;
    }

    private static bool ParseStrikeout(ParserState state, ParsedPlay play, bool flags)
    {
        if (state.Sym >= '1' && state.Sym <= '9')
        {
            var safe = ParseFieldingCredit(state, play, ' ');
            play.Advance[0] = safe ? 1 : 0;
            play.MuffFlag[0] = safe;
            play.Play[0] = state.Token;
        }
        else
        {
            // A bare strikeout implies the catcher (position 2) makes the putout.
            play.Play[0] = "2";
            play.Putouts.Add(2);
            play.Touches.Add(2);
        }

        if (state.Sym == '+')
        {
            state.NextSym();
            ParsePrimaryEvent(state);

            if (state.Token == "WP")
            {
                play.WpFlag = true;
            }
            else if (state.Token == "PB")
            {
                play.PbFlag = true;
            }
            else if (state.Token == "PO")
            {
                ParsePickoff(state, play, false);
            }
            else if (state.Token == "POCS")
            {
                ParsePickoffCaughtStealing(state, play, false);
            }
            else if (state.Token == "POSB")
            {
                ParsePickoffStolenBase(state, play, false);
            }
            else if (state.Token == "SB")
            {
                ParseStolenBase(state, play, false);
            }
            else if (state.Token == "CS")
            {
                ParseCaughtStealing(state, play, false);
            }
            else if (state.Token == "DI")
            {
                ParseIndifference(state, play, false);
            }
            else if (state.Token is "OA" or "OBA")
            {
                // no action required for K+OA
            }
            else if (state.Token == "E")
            {
                if (state.Sym < '1' || state.Sym > '9')
                {
                    return ParseInvalid(state);
                }

                play.Errors.Add(new FieldingError(state.Sym - '0', 'F'));
                state.NextSym();
            }
            else
            {
                return ParseInvalid(state);
            }
        }

        while (flags && state.Sym == '/')
        {
            ParseFlag(state);

            if ((state.Token is "TH" or "TH1" or "TH2" or "TH3" or "THH") && play.Errors.Count > 0)
            {
                play.Errors[0] = play.Errors[0] with { ErrorType = 'T' };
            }
            else if (state.Token == "DP")
            {
                play.DpFlag = true;
            }
            else if (state.Token == "TP")
            {
                play.TpFlag = true;
            }
            else if (state.Token is "B" or "BF" or "BG" or "BP")
            {
                play.BuntFlag = true;
            }
            else if (state.Token == "F")
            {
                // Chadwick 0.6.2+ no longer treats this as a bunt fly (a prior BEVENT quirk); no action.
            }
            else if (state.Token == "FL")
            {
                play.FoulFlag = true;
            }
            else if (state.Token == "L")
            {
                // Accepted; Chadwick 0.6.2+ no longer treats this as a line drive.
            }
            else
            {
                // Other flags exist in real files due to scoring inconsistencies; accepted silently.
            }
        }

        return true;
    }

    private static bool ParseWalk(ParserState state, ParsedPlay play, bool flags)
    {
        play.Advance[0] = 1;

        if (state.Sym == '+')
        {
            state.NextSym();
            ParsePrimaryEvent(state);

            if (state.Token == "WP")
            {
                play.WpFlag = true;
            }
            else if (state.Token == "PB")
            {
                play.PbFlag = true;
            }
            else if (state.Token == "PO")
            {
                if (!ParsePickoff(state, play, false))
                {
                    return false;
                }
            }
            else if (state.Token == "POSB")
            {
                if (!ParsePickoffStolenBase(state, play, false))
                {
                    return false;
                }
            }
            else if (state.Token == "POCS")
            {
                if (!ParsePickoffCaughtStealing(state, play, false))
                {
                    return false;
                }
            }
            else if (state.Token == "SB")
            {
                if (!ParseStolenBase(state, play, false))
                {
                    return false;
                }
            }
            else if (state.Token == "CS")
            {
                if (!ParseCaughtStealing(state, play, false))
                {
                    return false;
                }
            }
            else if (state.Token == "DI")
            {
                if (!ParseIndifference(state, play, false))
                {
                    return false;
                }
            }
            else if (state.Token == "OA")
            {
                // no action required for W+OA
            }
            else if (state.Token == "E")
            {
                if (state.Sym < '1' || state.Sym > '9')
                {
                    return ParseInvalid(state);
                }

                play.Errors.Add(new FieldingError(state.Sym - '0', 'F'));
                state.NextSym();
            }
        }

        while (flags && state.Sym == '/')
        {
            ParseFlag(state);

            if ((state.Token is "TH" or "TH1" or "TH2" or "TH3" or "THH") && play.Errors.Count > 0)
            {
                play.Errors[0] = play.Errors[0] with { ErrorType = 'T' };
            }
            else if (state.Token == "DP")
            {
                play.DpFlag = true;
            }
            else if (state.Token == "BOOT")
            {
                // silently accept batting-out-of-order flag
            }
            else if (state.Token is "MREV" or "UREV")
            {
                // silently accept review flags
            }
            else if (state.Token.Length > 0 && state.Token[0] == 'R')
            {
                // accept relay flags silently (e.g. runner picked off third after a walk)
            }
            else if (state.Token == "UINT")
            {
                // accept umpire interference flag
            }
            else if (state.Token == "COUR")
            {
                // accept courtesy runner flag
            }
            else
            {
                return ParseInvalid(state);
            }
        }

        return true;
    }

    private static bool ParseWildPitch(ParserState state, ParsedPlay play, bool flags)
    {
        play.WpFlag = true;

        while (flags && state.Sym == '/')
        {
            ParseFlag(state);
            if (state.Token == "DP")
            {
                play.DpFlag = true;
            }
        }

        return true;
    }

    private static bool ParseRunnerAdvance(ParserState state, ParsedPlay play)
    {
        if ((state.Sym < '1' || state.Sym > '3') && state.Sym != 'B')
        {
            return ParseInvalid(state);
        }

        var baseFrom = state.Sym == 'B' ? 0 : state.Sym - '0';

        state.NextSym();
        if (state.Sym != '-' && state.Sym != 'X')
        {
            return ParseInvalid(state);
        }

        var safe = state.Sym == '-';

        state.NextSym();
        if ((state.Sym < '1' || state.Sym > '3') && state.Sym != 'H')
        {
            return ParseInvalid(state);
        }

        var baseTo = state.Sym == 'H' ? 4 : state.Sym - '0';

        if (safe)
        {
            // Handles plays like CSH(1E2)(UR).3-H, where advancement is already implied and
            // marked unearned.
            if (baseTo < 4 || play.Advance[baseFrom] < 4)
            {
                play.Advance[baseFrom] = baseTo;
            }

            if (baseTo == 4 && play.IsBatter && !play.GdpFlag &&
                (play.EventType != PlayEventType.Error || baseFrom == 3) &&
                play.EventType != PlayEventType.Strikeout &&
                play.RbiFlag[baseFrom] != -1)
            {
                play.RbiFlag[baseFrom] = 1;
            }
        }
        else
        {
            play.Advance[baseFrom] = 0;
            if (play.EventType == PlayEventType.FieldersChoice)
            {
                play.FcFlag[baseFrom] = true;
            }
        }

        state.NextSym();
        while (state.Sym == '(')
        {
            state.NextSym();
            if (!ParseAdvanceModifier(state, play, safe, baseFrom, baseTo))
            {
                return false;
            }
        }

        return true;
    }

    private static bool ParseAdvancement(ParserState state, ParsedPlay play)
    {
        do
        {
            state.NextSym();
            if (!ParseRunnerAdvance(state, play))
            {
                return false;
            }
        } while (state.Sym == ';');

        return state.Sym == '\0';
    }

    private static void SanityCheck(ParsedPlay play)
    {
        if (play.EventType == PlayEventType.Single && play.Advance[0] == 0 && play.Play[0] == "")
        {
            play.Advance[0] = 1;
        }
        else if (play.EventType == PlayEventType.Double && play.Advance[0] == 0 && play.Play[0] == "")
        {
            play.Advance[0] = 2;
        }

        if (play.EventType == PlayEventType.Triple && play.Advance[0] == 0 && play.Play[0] == "")
        {
            play.Advance[0] = 3;
        }

        if (play.EventType == PlayEventType.HomeRun && play.Advance[0] == 0 && play.Play[0] == "")
        {
            play.Advance[0] = 4;
            play.RbiFlag[0] = 1;
        }

        if (play.EventType == PlayEventType.Strikeout && play.Play[0] == "2" && play.Advance[0] > 0)
        {
            play.Play[0] = "";
            if (play.Putouts.Count > 0)
            {
                play.Putouts.RemoveAt(0);
            }
        }

        if (play.EventType == PlayEventType.Walk)
        {
            play.RbiFlag[0] = 0;
            play.RbiFlag[1] = 0;
            play.RbiFlag[2] = 0;
        }

        if (play.EventType == PlayEventType.FoulError)
        {
            play.FoulFlag = true;
            if (play.BattedBallType == ' ')
            {
                play.BattedBallType = play.Errors.Count > 0 && play.Errors[0].FielderPosition >= 7 ? 'F' : 'P';
            }
        }
        else if (!play.IsBatter)
        {
            for (var i = 0; i < play.Errors.Count; i++)
            {
                if (play.Errors[i].ErrorType == 'F')
                {
                    play.Errors[i] = play.Errors[i] with { ErrorType = 'D' };
                }
            }
        }

        for (var baseIndex = 0; baseIndex <= 3; baseIndex++)
        {
            if (play.RbiFlag[baseIndex] == -1)
            {
                play.RbiFlag[baseIndex] = 0;
            }

            if (play.Play[baseIndex] != "" && !play.Play[baseIndex].Contains('E'))
            {
                // Patches up instances like BXH(832)(E8).
                play.Advance[baseIndex] = 0;
            }

            if (play.Play[baseIndex].Contains("99"))
            {
                // If fielding credits on any play are listed as unknown ("99"), no fielding
                // credits at all should be awarded.
                play.Putouts.Clear();
                play.Assists.Clear();
            }
        }

        // Default batted-ball types based upon defensive fielding credit, when not already set.
        if (play.EventType == PlayEventType.GenericOut)
        {
            if (play.Play[0].Length == 1 && !play.DpFlag && !play.TpFlag && !play.BuntFlag && play.BattedBallType == ' ')
            {
                play.BattedBallType = 'F';
            }
            else if (play.Play[0].Length >= 1 && play.BattedBallType == ' ')
            {
                play.BattedBallType = 'G';
            }
        }

        if (play.EventType == PlayEventType.Single && play.BuntFlag && play.BattedBallType == ' ')
        {
            play.BattedBallType = 'G';
        }

        if (play.ShFlag)
        {
            play.BattedBallType = 'G';
        }
    }

    private static readonly (PlayEventType EventCode, string Token, Func<ParserState, ParsedPlay, bool, bool> ParseFunc)[] PrimaryTable =
    [
        (PlayEventType.Balk, "BK", ParseBalk),
        (PlayEventType.Interference, "C", ParseInterference),
        (PlayEventType.CaughtStealing, "CS", ParseCaughtStealing),
        (PlayEventType.Double, "D", ParseBaseHit),
        (PlayEventType.Double, "DGR", ParseGroundRuleDouble),
        (PlayEventType.Indifference, "DI", ParseIndifference),
        (PlayEventType.Error, "E", ParseSafeOnError),
        (PlayEventType.FieldersChoice, "FC", ParseFieldersChoice),
        (PlayEventType.FoulError, "FLE", ParseFoulError),
        (PlayEventType.HomeRun, "H", ParseBaseHit),
        (PlayEventType.HitByPitch, "HP", ParseHitByPitch),
        (PlayEventType.HomeRun, "HR", ParseBaseHit),
        (PlayEventType.IntentionalWalk, "I", ParseWalk),
        (PlayEventType.IntentionalWalk, "IW", ParseWalk),
        (PlayEventType.Strikeout, "K", ParseStrikeout),
        (PlayEventType.OtherAdvance, "OA", ParseOtherAdvance),
        (PlayEventType.PassedBall, "PB", ParsePassedBall),
        (PlayEventType.Pickoff, "PO", ParsePickoff),
        (PlayEventType.Pickoff, "POCS", ParsePickoffCaughtStealing),
        (PlayEventType.StolenBase, "POSB", ParsePickoffStolenBase),
        (PlayEventType.Single, "S", ParseBaseHit),
        (PlayEventType.StolenBase, "SB", ParseStolenBase),
        (PlayEventType.Triple, "T", ParseBaseHit),
        (PlayEventType.Walk, "W", ParseWalk),
        (PlayEventType.WildPitch, "WP", ParseWildPitch),
    ];
}
