﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public sealed partial class AnalyzerConfig
    {
        public readonly struct SectionNameMatcher
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
        /// matches the the given language. Returns null if the section name is
        /// invalid.
        /// </summary>
        public static SectionNameMatcher? TryCreateSectionNameMatcher(string sectionName)
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
            return new SectionNameMatcher(
                new Regex(sb.ToString(), RegexOptions.Compiled),
                numberRangePairs.ToImmutableAndFree());
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
                    case TokenKind.LiteralStar:
                        // Match a literal '*'
                        sb.Append("\\*");
                        break;
                    case TokenKind.LiteralQuestion:
                        sb.Append("\\?");
                        break;
                    case TokenKind.LiteralOpenBrace:
                        sb.Append("\\{");
                        break;
                    case TokenKind.LiteralCloseBrace:
                        sb.Append("\\}");
                        break;
                    case TokenKind.LiteralComma:
                        sb.Append(",");
                        break;
                    case TokenKind.Backslash:
                        // Literal backslash
                        sb.Append("\\\\");
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(tokenKind);
                }
            }
            // If we're parsing a choice we should not exit without a closing '}'
            return !parsingChoice;
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
                // If we've succesfully compiled a <path-list> the last token should
                // have been a ',' or a '}'
                char lastChar = lexer[lexer.Position - 1];
                if (lastChar == ',')
                {
                    // Another option
                    sb.Append("|");
                }
                else if (lastChar == '}')
                {
                    // Close out the capture group
                    sb.Append(")");
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

                    case '\\':
                    {
                        // Backslash escapes the next character
                        Position++;
                        if (Position >= _sectionName.Length)
                        {
                            return TokenKind.BadToken;
                        }

                        // Check for all of the possible escapes
                        switch (_sectionName[Position++])
                        {
                            case '\\':
                                // "\\" -> "\"
                                return TokenKind.Backslash;

                            case '*':
                                // "\*" -> "\*"
                                return TokenKind.LiteralStar;

                            case '?':
                                // "\?" -> "\?"
                                return TokenKind.LiteralQuestion;

                            case '{':
                                // "\{" -> "{"
                                return TokenKind.LiteralOpenBrace;

                            case ',':
                                // "\," -> ","
                                return TokenKind.LiteralComma;

                            case '}':
                                // "\}" -> "}"
                                return TokenKind.LiteralCloseBrace;

                            default:
                                return TokenKind.BadToken;
                        }
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
            public string TryLexNumber()
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
            Backslash,
            LiteralStar,
            LiteralQuestion,
            LiteralOpenBrace,
            LiteralCloseBrace,
            LiteralComma
        }
    }
}
