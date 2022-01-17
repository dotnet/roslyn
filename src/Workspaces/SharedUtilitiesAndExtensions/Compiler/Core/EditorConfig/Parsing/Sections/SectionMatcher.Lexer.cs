// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace Microsoft.CodeAnalysis.EditorConfig.Parsing
{
    internal readonly partial struct SectionMatcher
    {
        private struct Lexer
        {
            private readonly string _headerText;
            public int Position { get; set; }

            public Lexer(string headerText)
            {
                _headerText = headerText;
                Position = 0;
            }

            public bool IsDone => Position >= _headerText.Length;

            public TokenKind Lex()
            {
                var tokenKind = GetTokenKindAtPosition(_headerText, Position);
                switch (tokenKind)
                {
                    case TokenKind.StarStar:
                        Position += 2;
                        break;
                    case TokenKind.SimpleCharacter:
                        if (_headerText[Position] == '\\')
                        {
                            // Backslash escapes the next character
                            Position++;
                        }

                        // Don't increment position, since caller needs to fetch the character
                        break;
                    case TokenKind.Question:
                    case TokenKind.OpenCurly:
                    case TokenKind.Comma:
                    case TokenKind.OpenBracket:
                    case TokenKind.CloseCurly:
                    case TokenKind.Star:
                        Position++;
                        break;
                    case TokenKind.BadToken:
                    default:
                        break;
                }

                return tokenKind;
            }

            public bool TryPeekNext(out TokenKind kind)
            {
                var position = Position;
                position++;
                if (position < _headerText.Length)
                {
                    kind = GetTokenKindAtPosition(_headerText, position);
                    return true;
                }

                kind = default;
                return false;
            }

            public bool TryPeekPrevious(out TokenKind kind)
            {
                var position = Position;
                position--;
                if (position >= 0)
                {
                    kind = GetTokenKindAtPosition(_headerText, position);
                    return true;
                }

                kind = default;
                return false;
            }

            private static TokenKind GetTokenKindAtPosition(string headerText, int position)
            {
                switch (headerText[position])
                {
                    case '*':
                        {
                            position++;
                            if (position < headerText.Length &&
                                headerText[position] == '*')
                            {
                                return TokenKind.StarStar;
                            }
                            else
                            {
                                return TokenKind.Star;
                            }
                        }

                    case '?':
                        return TokenKind.Question;

                    case '{':
                        return TokenKind.OpenCurly;

                    case ',':
                        return TokenKind.Comma;

                    case '}':
                        return TokenKind.CloseCurly;

                    case '[':
                        return TokenKind.OpenBracket;

                    case '\\':
                        position++;
                        if (position >= headerText.Length)
                        {
                            return TokenKind.BadToken;
                        }

                        return TokenKind.SimpleCharacter;
                    default:
                        return TokenKind.SimpleCharacter;
                }
            }

            public char CurrentCharacter => _headerText[Position];

            public char EatCurrentCharacter() => _headerText[Position++];

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

            public char this[int position] => _headerText[position];

            public string? TryLexNumber()
            {
                var start = true;
                var sb = new StringBuilder();

                while (!IsDone)
                {
                    var currentChar = CurrentCharacter;
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
            OpenBracket,
        }
    }
}
