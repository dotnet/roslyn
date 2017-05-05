// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;
using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    internal sealed partial class MemberSignatureParser
    {
        private enum TokenKind
        {
            OpenParen = '(',
            CloseParen = ')',
            OpenBracket = '[',
            CloseBracket = ']',
            Dot = '.',
            Comma = ',',
            Asterisk = '*',
            QuestionMark = '?',
            LessThan = '<',
            GreaterThan = '>',

            Start = char.MaxValue + 1,
            End,
            Identifier,
            Keyword,
        }

        [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
        private struct Token
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
                return (Text == null) ?
                    Kind.ToString() :
                    $"{Kind}: \"{Text}\"";
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

                var c = _text[_offset++];
                if (UnicodeCharacterUtilities.IsIdentifierStartCharacter(c))
                {
                    return ScanIdentifierAfterStartCharacter(verbatim: false);
                }
                else if (c == '@' && _offset < length && UnicodeCharacterUtilities.IsIdentifierStartCharacter(_text[_offset]))
                {
                    _offset++;
                    return ScanIdentifierAfterStartCharacter(verbatim: true);
                }

                return new Token((TokenKind)c);
            }

            private Token ScanIdentifierAfterStartCharacter(bool verbatim)
            {
                // Assert the offset is immediately following the start character.
                Debug.Assert(_offset > 0);
                Debug.Assert(UnicodeCharacterUtilities.IsIdentifierStartCharacter(_text[_offset - 1]));
                Debug.Assert(_offset == 1 || !UnicodeCharacterUtilities.IsIdentifierPartCharacter(_text[_offset - 2]));

                int length = _text.Length;
                int start = _offset - 1;
                while ((_offset < length) && UnicodeCharacterUtilities.IsIdentifierPartCharacter(_text[_offset]))
                {
                    _offset++;
                }
                var text = _text.Substring(start, _offset - start);
                var keywordKind = verbatim ?
                    SyntaxKind.None :
                    SyntaxFacts.GetKeywordKind(text);
                if (keywordKind == SyntaxKind.None)
                {
                    return new Token(TokenKind.Identifier, text);
                }
                return new Token(TokenKind.Keyword, text, keywordKind);
            }
        }
    }
}
