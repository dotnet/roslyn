// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal sealed partial class EditorConfig
    {
        /// <summary>
        /// Takes a <see cref="Section.Name"/> and compiles the file glob
        /// inside to a regex which recognizes the given language. Returns
        /// null if the section name is invalid.
        /// </summary>
        public static string TryCompileSectionNameToRegEx(string sectionName)
        {
            // An editorconfig section name is a language for recognizing file paths
            // defined by the following grammar:
            //
            // <path> ::= <path-list>
            // <path-list> ::= <path-item> | <path-item> <path-list>
            // <path-item> ::= "*"  | "**" | "?" | <char> | <choice> | <range>
            // <char> ::= any unicode character
            //
            // PROTOTYPE(editorconfig): Below remains unimplemented
            //
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
            while (!lexer.IsDone)
            {
                var tokenKind = lexer.Lex();
                switch (tokenKind)
                {
                    case TokenKind.BadToken:
                        // Parsing failure
                        return null;
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
                    case TokenKind.LiteralStar:
                        // Match a literal '*'
                        sb.Append("\\*");
                        break;
                    case TokenKind.LiteralQuestion:
                        sb.Append("\\?");
                        break;
                    case TokenKind.Backslash:
                        // Literal backslash
                        sb.Append("\\\\");
                        break;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(tokenKind);
                }
            }
            sb.Append('$');

            return sb.ToString();
        }

        private ref struct SectionNameLexer
        {
            private readonly string _sectionName;

            private int _position;

            public SectionNameLexer(string sectionName)
            {
                _sectionName = sectionName;
                _position = 0;
            }

            public bool IsDone => _position >= _sectionName.Length;

            public TokenKind Lex()
            {
                int lexemeStart = _position;
                switch (_sectionName[_position])
                {
                    case '*':
                    {
                        int nextPos = _position + 1;
                        if (nextPos < _sectionName.Length &&
                            _sectionName[nextPos] == '*')
                        {
                            _position += 2;
                            return TokenKind.StarStar;
                        }
                        else
                        {
                            _position++;
                            return TokenKind.Star;
                        }
                    }

                    case '?':
                        _position++;
                        return TokenKind.Question;

                    case '\\':
                    {
                        // Backslash escapes the next character
                        _position++;
                        if (_position >= _sectionName.Length)
                        {
                            return TokenKind.BadToken;
                        }

                        // Check for all of the possible escapes
                        switch (_sectionName[_position++])
                        {
                            case '\\':
                                // "\\" -> "\"
                                return TokenKind.Backslash;

                            case '*':
                                // "*" -> "\*"
                                return TokenKind.LiteralStar;

                            case '?':
                                // "?" -> "\?"
                                return TokenKind.LiteralQuestion;

                            default:
                                return TokenKind.BadToken;
                        }
                    }

                    default:
                        // Don't increment position, since caller needs to fetch the character
                        return TokenKind.SimpleCharacter;
                }
            }

            /// <summary>
            /// Call after getting <see cref="TokenKind.SimpleCharacter" /> from <see cref="Lex()" />
            /// </summary>
            public char EatCurrentCharacter() => _sectionName[_position++];
        }

        private enum TokenKind
        {
            BadToken,
            SimpleCharacter,
            Star,
            StarStar,
            Question,
            Backslash,
            LiteralStar,
            LiteralQuestion
        }
    }
}
