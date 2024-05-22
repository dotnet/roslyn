// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public sealed partial class AnalyzerConfig
    {
        private static readonly ConcurrentDictionary<string, Regex> s_regexMap = [];

        internal readonly struct SectionNameMatcher
        {
            private readonly ImmutableArray<(int minValue, int maxValue)> _numberRangePairs;
            // internal for testing
            internal Regex Regex { get; }

            internal SectionNameMatcher(
                Regex regex,
                ImmutableArray<(int minValue, int maxValue)> numberRangePairs)
            {
                Debug.Assert(regex.GetGroupNumbers().Length - 1 == numberRangePairs.Length);
                Regex = regex;
                _numberRangePairs = numberRangePairs;
            }

            public bool IsMatch(string s)
            {
                if (_numberRangePairs.IsEmpty)
                {
                    return Regex.IsMatch(s);
                }

                var match = Regex.Match(s);
                if (!match.Success)
                {
                    return false;
                }
                Debug.Assert(match.Groups.Count - 1 == _numberRangePairs.Length);
                for (int i = 0; i < _numberRangePairs.Length; i++)
                {
                    var (minValue, maxValue) = _numberRangePairs[i];
                    // Index 0 is the whole regex
                    if (!int.TryParse(match.Groups[i + 1].Value, out int matchedNum) ||
                        matchedNum < minValue ||
                        matchedNum > maxValue)
                    {
                        return false;
                    }
                }
                return true;
            }
        }

        /// <summary>
        /// Takes a <see cref="Section.Name"/> and creates a matcher that
        /// matches the given language. Returns null if the section name is
        /// invalid.
        /// </summary>
        internal static SectionNameMatcher? TryCreateSectionNameMatcher(string sectionName)
        {
            // An editorconfig section name is a language for recognizing file paths
            // defined by the following grammar:
            //
            // <path> ::= <path-list>
            // <path-list> ::= <path-item> | <path-item> <path-list>
            // <path-item> ::= "*"  | "**" | "?" | <char> | <choice> | <range>
            // <char> ::= any unicode character
            // <choice> ::= "{" <choice-list> "}"
            // <choice-list> ::= <path-list> | <path-list> "," <choice-list>
            // <range> ::= "{" <integer> ".." <integer> "}"
            // <integer> ::= "-" <digit-list> | <digit-list>
            // <digit-list> ::= <digit> | <digit> <digit-list>
            // <digit> ::= 0-9

            var sb = new StringBuilder();
            sb.Append('^');

            // EditorConfig matching depends on the whether or not there are
            // directory separators and where they are located in the section
            // name. Specifically, the editorconfig core parser says:
            // https://github.com/editorconfig/editorconfig-core-c/blob/5d3996811e962a717a7d7fdd0a941192382241a7/src/lib/editorconfig.c#L231
            //
            //     Pattern would be:
            //     /dir/of/editorconfig/file[double_star]/[section] if section does not contain '/',
            //     /dir/of/editorconfig/file[section] if section starts with a '/', or
            //     /dir/of/editorconfig/file/[section] if section contains '/' but does not start with '/'.

            if (!sectionName.Contains("/"))
            {
                sb.Append(".*/");
            }
            else if (sectionName[0] != '/')
            {
                sb.Append('/');
            }

            var lexer = new SectionNameLexer(sectionName);
            var numberRangePairs = ArrayBuilder<(int minValue, int maxValue)>.GetInstance();
            if (!TryCompilePathList(ref lexer, sb, parsingChoice: false, numberRangePairs))
            {
                numberRangePairs.Free();
                return null;
            }

            sb.Append('$');

            var pattern = sb.ToString();
            var regex = s_regexMap.GetOrAdd(pattern, static pattern => new(pattern, RegexOptions.Compiled));

            return new SectionNameMatcher(regex, numberRangePairs.ToImmutableAndFree());
        }

        internal static string UnescapeSectionName(string sectionName)
        {
            var sb = new StringBuilder();
            SectionNameLexer lexer = new SectionNameLexer(sectionName);
            while (!lexer.IsDone)
            {
                var tokenKind = lexer.Lex();
                if (tokenKind == TokenKind.SimpleCharacter)
                {
                    sb.Append(lexer.EatCurrentCharacter());
                }
                else
                {
                    // We only call this on strings that were already passed through IsAbsoluteEditorConfigPath, so
                    // we shouldn't have any other token kinds here.
                    throw ExceptionUtilities.UnexpectedValue(tokenKind);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Test if a section name is an absolute path with no special chars
        /// </summary>
        internal static bool IsAbsoluteEditorConfigPath(string sectionName)
        {
            // NOTE: editorconfig paths must use '/' as a directory separator character on all OS.

            // on all unix systems this is thus a simple test: does the path start with '/'
            // and contain no special chars?

            // on windows, a path can be either drive rooted or not (e.g. start with 'c:' or just '')
            // in addition to being absolute or relative.
            // for example c:myfile.cs is a relative path, but rooted on drive c:
            // /myfile2.cs is an absolute path but rooted to the current drive.

            // in addition there are UNC paths and volume guids (see https://docs.microsoft.com/en-us/dotnet/standard/io/file-path-formats)
            // but these start with \\ (and thus '/' in editor config terminology)

            // in this implementation we choose to ignore the drive root for the purposes of
            // determining validity. On windows c:/file.cs and /file.cs are both assumed to be
            // valid absolute paths, even though the second one is technically relative to
            // the current drive of the compiler working directory. 

            // Note that this check has no impact on config correctness. Files on windows
            // will still be compared using their full path (including drive root) so it's
            // not possible to target the wrong file. It's just possible that the user won't
            // receive a warning that this section is ignored on windows in this edge case.

            SectionNameLexer nameLexer = new SectionNameLexer(sectionName);
            bool sawStartChar = false;
            int logicalIndex = 0;
            while (!nameLexer.IsDone)
            {
                if (nameLexer.Lex() != TokenKind.SimpleCharacter)
                {
                    return false;
                }
                var simpleChar = nameLexer.EatCurrentCharacter();

                // check the path starts with '/'
                if (logicalIndex == 0)
                {
                    if (simpleChar == '/')
                    {
                        sawStartChar = true;
                    }
                    else if (Path.DirectorySeparatorChar == '/')
                    {
                        return false;
                    }
                }
                // on windows we get a second chance to find the start char
                else if (!sawStartChar && Path.DirectorySeparatorChar == '\\')
                {
                    if (logicalIndex == 1 && simpleChar != ':')
                    {
                        return false;
                    }
                    else if (logicalIndex == 2)
                    {
                        if (simpleChar != '/')
                        {
                            return false;
                        }
                        else
                        {
                            sawStartChar = true;
                        }
                    }
                }
                logicalIndex++;
            }
            return sawStartChar;
        }

        /// <summary>
        /// <![CDATA[
        /// <path-list> ::= <path-item> | <path-item> <path-list>
        /// <path-item> ::= "*"  | "**" | "?" | <char> | <choice> | <range>
        /// <char> ::= any unicode character
        /// <choice> ::= "{" <choice-list> "}"
        /// <choice-list> ::= <path-list> | <path-list> "," <choice-list>
        /// ]]>
        /// </summary>
        private static bool TryCompilePathList(
            ref SectionNameLexer lexer,
            StringBuilder sb,
            bool parsingChoice,
            ArrayBuilder<(int minValue, int maxValue)> numberRangePairs)
        {
            while (!lexer.IsDone)
            {
                var tokenKind = lexer.Lex();
                switch (tokenKind)
                {
                    case TokenKind.BadToken:
                        // Parsing failure
                        return false;
                    case TokenKind.SimpleCharacter:
                        // Matches just this character
                        sb.Append(Regex.Escape(lexer.EatCurrentCharacter().ToString()));
                        break;
                    case TokenKind.Question:
                        // '?' matches any single character
                        sb.Append('.');
                        break;
                    case TokenKind.Star:
                        // Matches any string of characters except directory separator
                        // Directory separator is defined in editorconfig spec as '/'
                        sb.Append("[^/]*");
                        break;
                    case TokenKind.StarStar:
                        // Matches any string of characters
                        sb.Append(".*");
                        break;
                    case TokenKind.OpenCurly:
                        // Back up token stream. The following helpers all expect a '{'
                        lexer.Position--;
                        // This is ambiguous between {num..num} and {item1,item2}
                        // We need to look ahead to disambiguate. Looking for {num..num}
                        // is easier because it can't be recursive.
                        (string numStart, string numEnd)? rangeOpt = TryParseNumberRange(ref lexer);
                        if (rangeOpt is null)
                        {
                            // Not a number range. Try a choice expression
                            if (!TryCompileChoice(ref lexer, sb, numberRangePairs))
                            {
                                return false;
                            }
                            // Keep looping. There may be more after the '}'.
                            break;
                        }
                        else
                        {
                            (string numStart, string numEnd) = rangeOpt.GetValueOrDefault();
                            if (int.TryParse(numStart, out var intStart) && int.TryParse(numEnd, out var intEnd))
                            {
                                var pair = intStart < intEnd ? (intStart, intEnd) : (intEnd, intStart);
                                numberRangePairs.Add(pair);
                                // Group allowing any digit sequence. The validity will be checked outside of the regex
                                sb.Append("(-?[0-9]+)");
                                // Keep looping
                                break;
                            }
                            return false;
                        }
                    case TokenKind.CloseCurly:
                        // Either the end of a choice, or a failed parse
                        return parsingChoice;
                    case TokenKind.Comma:
                        // The end of a choice section, or a failed parse
                        return parsingChoice;
                    case TokenKind.OpenBracket:
                        sb.Append('[');
                        if (!TryCompileCharacterClass(ref lexer, sb))
                        {
                            return false;
                        }
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(tokenKind);
                }
            }
            // If we're parsing a choice we should not exit without a closing '}'
            return !parsingChoice;
        }

        /// <summary>
        /// Compile a globbing character class of the form [...]. Returns true if
        /// the character class was successfully compiled. False if there was a syntax
        /// error. The starting character is expected to be directly after the '['.
        /// </summary>
        private static bool TryCompileCharacterClass(ref SectionNameLexer lexer, StringBuilder sb)
        {
            // [...] should match any of the characters in the brackets, with special
            // behavior for four characters: '!' immediately after the opening bracket
            // implies the negation of the character class, '-' implies matching
            // between the locale-dependent range of the previous and next characters,
            // '\' escapes the following character, and ']' ends the range
            if (!lexer.IsDone && lexer.CurrentCharacter == '!')
            {
                sb.Append('^');
                lexer.Position++;
            }
            while (!lexer.IsDone)
            {
                var currentChar = lexer.EatCurrentCharacter();
                switch (currentChar)
                {
                    case '-':
                        // '-' means the same thing in regex as it does in the glob, so
                        // put it in verbatim
                        sb.Append(currentChar);
                        break;

                    case '\\':
                        // Escape the next char
                        if (lexer.IsDone)
                        {
                            return false;
                        }
                        sb.Append('\\');
                        sb.Append(lexer.EatCurrentCharacter());
                        break;

                    case ']':
                        sb.Append(currentChar);
                        return true;

                    default:
                        sb.Append(Regex.Escape(currentChar.ToString()));
                        break;
                }
            }
            // Stream ended without a closing bracket
            return false;
        }

        /// <summary>
        /// Parses choice defined by the following grammar:
        /// <![CDATA[
        /// <choice> ::= "{" <choice-list> "}"
        /// <choice-list> ::= <path-list> | <path-list> "," <choice-list>
        /// ]]>
        /// </summary>
        private static bool TryCompileChoice(
            ref SectionNameLexer lexer,
            StringBuilder sb,
            ArrayBuilder<(int, int)> numberRangePairs)
        {
            if (lexer.Lex() != TokenKind.OpenCurly)
            {
                return false;
            }

            // Start a non-capturing group for the choice
            sb.Append("(?:");

            // We start immediately after a '{'
            // Try to compile the nested <path-list>
            while (TryCompilePathList(ref lexer, sb, parsingChoice: true, numberRangePairs))
            {
                // If we've successfully compiled a <path-list> the last token should
                // have been a ',' or a '}'
                char lastChar = lexer[lexer.Position - 1];
                if (lastChar == ',')
                {
                    // Another option
                    sb.Append('|');
                }
                else if (lastChar == '}')
                {
                    // Close out the capture group
                    sb.Append(')');
                    return true;
                }
                else
                {
                    throw ExceptionUtilities.UnexpectedValue(lastChar);
                }
            }

            // Propagate failure
            return false;
        }

        /// <summary>
        /// Parses range defined by the following grammar.
        /// <![CDATA[
        /// <range> ::= "{" <integer> ".." <integer> "}"
        /// <integer> ::= "-" <digit-list> | <digit-list>
        /// <digit-list> ::= <digit> | <digit> <digit-list>
        /// <digit> ::= 0-9
        /// ]]>
        /// </summary>
        private static (string numStart, string numEnd)? TryParseNumberRange(ref SectionNameLexer lexer)
        {
            var saved = lexer.Position;
            if (lexer.Lex() != TokenKind.OpenCurly)
            {
                lexer.Position = saved;
                return null;
            }

            var numStart = lexer.TryLexNumber();
            if (numStart is null)
            {
                // Not a number
                lexer.Position = saved;
                return null;
            }

            // The next two characters must be ".."
            if (!lexer.TryEatCurrentCharacter(out char c) || c != '.' ||
                !lexer.TryEatCurrentCharacter(out c) || c != '.')
            {
                lexer.Position = saved;
                return null;
            }

            // Now another number
            var numEnd = lexer.TryLexNumber();
            if (numEnd is null || lexer.IsDone || lexer.Lex() != TokenKind.CloseCurly)
            {
                // Not a number or no '}'
                lexer.Position = saved;
                return null;
            }

            return (numStart, numEnd);
        }

        private struct SectionNameLexer
        {
            private readonly string _sectionName;

            public int Position { get; set; }

            public SectionNameLexer(string sectionName)
            {
                _sectionName = sectionName;
                Position = 0;
            }

            public bool IsDone => Position >= _sectionName.Length;

            public TokenKind Lex()
            {
                int lexemeStart = Position;
                switch (_sectionName[Position])
                {
                    case '*':
                        {
                            int nextPos = Position + 1;
                            if (nextPos < _sectionName.Length &&
                                _sectionName[nextPos] == '*')
                            {
                                Position += 2;
                                return TokenKind.StarStar;
                            }
                            else
                            {
                                Position++;
                                return TokenKind.Star;
                            }
                        }

                    case '?':
                        Position++;
                        return TokenKind.Question;

                    case '{':
                        Position++;
                        return TokenKind.OpenCurly;

                    case ',':
                        Position++;
                        return TokenKind.Comma;

                    case '}':
                        Position++;
                        return TokenKind.CloseCurly;

                    case '[':
                        Position++;
                        return TokenKind.OpenBracket;

                    case '\\':
                        {
                            // Backslash escapes the next character
                            Position++;
                            if (IsDone)
                            {
                                return TokenKind.BadToken;
                            }

                            return TokenKind.SimpleCharacter;
                        }

                    default:
                        // Don't increment position, since caller needs to fetch the character
                        return TokenKind.SimpleCharacter;
                }
            }

            public char CurrentCharacter => _sectionName[Position];

            /// <summary>
            /// Call after getting <see cref="TokenKind.SimpleCharacter" /> from <see cref="Lex()" />
            /// </summary>
            public char EatCurrentCharacter() => _sectionName[Position++];

            /// <summary>
            /// Returns false if there are no more characters in the lex stream.
            /// Otherwise, produces the next character in the stream and returns true.
            /// </summary>
            public bool TryEatCurrentCharacter(out char nextChar)
            {
                if (IsDone)
                {
                    nextChar = default;
                    return false;
                }
                else
                {
                    nextChar = EatCurrentCharacter();
                    return true;
                }
            }

            public char this[int position] => _sectionName[position];

            /// <summary>
            /// Returns the string representation of a decimal integer, or null if
            /// the current lexeme is not an integer.
            /// </summary>
            public string? TryLexNumber()
            {
                bool start = true;
                var sb = new StringBuilder();

                while (!IsDone)
                {
                    char currentChar = CurrentCharacter;
                    if (start && currentChar == '-')
                    {
                        Position++;
                        sb.Append('-');
                    }
                    else if (char.IsDigit(currentChar))
                    {
                        Position++;
                        sb.Append(currentChar);
                    }
                    else
                    {
                        break;
                    }
                    start = false;
                }

                var str = sb.ToString();
                return str.Length == 0 || str == "-"
                    ? null
                    : str;
            }
        }

        private enum TokenKind
        {
            BadToken,
            SimpleCharacter,
            Star,
            StarStar,
            Question,
            OpenCurly,
            CloseCurly,
            Comma,
            DoubleDot,
            OpenBracket,
        }
    }
}
