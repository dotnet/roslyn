// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.VisualBasic.ExpressionEvaluator
{
    internal sealed partial class MemberSignatureParser
    {
        private enum TokenKind
        {
            OpenParen = '(',
            CloseParen = ')',
            Dot = '.',
            Comma = ',',
            QuestionMark = '?',

            Start = char.MaxValue + 1,
            End,
            Identifier,
            Keyword,
        }

        [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
        private readonly struct Token
        {
            internal readonly TokenKind Kind;
            internal readonly string Text;
            internal readonly SyntaxKind KeywordKind;

            internal Token(TokenKind kind, string text = null, SyntaxKind keywordKind = SyntaxKind.None)
            {
                Kind = kind;
                Text = text;
                KeywordKind = keywordKind;
            }

            private string GetDebuggerDisplay()
            {
                return (Text == null)
                    ? Kind.ToString()
                    : $"{Kind}: \"{Text}\"";
            }
        }

        private sealed class Scanner
        {
            private readonly string _text;
            private int _offset;
            private Token _currentToken;

            internal Scanner(string text)
            {
                _text = text;
                _offset = 0;
                _currentToken = default(Token);
            }

            internal Token CurrentToken
            {
                get
                {
                    if (_currentToken.Kind == TokenKind.Start)
                    {
                        throw new InvalidOperationException();
                    }
                    return _currentToken;
                }
            }

            internal void MoveNext()
            {
                _currentToken = Scan();
            }

            private Token Scan()
            {
                int length = _text.Length;
                while (_offset < length && char.IsWhiteSpace(_text[_offset]))
                {
                    _offset++;
                }

                if (_offset == length)
                {
                    return new Token(TokenKind.End);
                }

                int n = ScanIdentifier();
                if (n > 0)
                {
                    var text = _text.Substring(_offset, n);
                    _offset += n;
                    if (Keywords.Contains(text))
                    {
                        var keywordKind = SyntaxKind.None;
                        KeywordKinds.TryGetValue(text, out keywordKind);
                        return new Token(TokenKind.Keyword, text, keywordKind);
                    }
                    return new Token(TokenKind.Identifier, text);
                }

                var c = _text[_offset++];
                if (c == '[')
                {
                    n = ScanIdentifier();
                    if (n > 0 && _offset + n < length && _text[_offset + n] == ']')
                    {
                        // A verbatim identifier. Treat the '[' and ']' as part
                        // of the token, but not part of the text.
                        var text = _text.Substring(_offset, n);
                        _offset += n + 1;
                        return new Token(TokenKind.Identifier, text);
                    }
                }

                return new Token((TokenKind)c);
            }

            // Returns the number of characters in the
            // identifier starting at the current offset.
            private int ScanIdentifier()
            {
                int length = _text.Length - _offset;
                if (length > 0 && UnicodeCharacterUtilities.IsIdentifierStartCharacter(_text[_offset]))
                {
                    int n = 1;
                    while (n < length && UnicodeCharacterUtilities.IsIdentifierPartCharacter(_text[_offset + n]))
                    {
                        n++;
                    }
                    return n;
                }
                return 0;
            }
        }
    }
}
