// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.StackFrame
{
    using StackFrameToken = EmbeddedSyntaxToken<StackFrameKind>;
    using StackFrameTrivia = EmbeddedSyntaxTrivia<StackFrameKind>;

    internal struct StackFrameLexer
    {
        public readonly VirtualCharSequence Text;
        public int Position;

        public StackFrameLexer(string text)
            : this(VirtualCharSequence.Create(0, text))
        {
        }

        public StackFrameLexer(VirtualCharSequence text) : this()
            => Text = text;

        public VirtualChar CurrentChar => Position < Text.Length ? Text[Position] : default;

        public VirtualCharSequence GetSubPatternToCurrentPos(int start)
            => GetSubPattern(start, Position);

        public VirtualCharSequence GetSubPattern(int start, int end)
            => Text.GetSubSequence(TextSpan.FromBounds(start, end));

        public StackFrameToken ScanNextToken(bool allowTrivia)
        {
            var trivia = ScanLeadingTrivia(allowTrivia);
            if (Position == Text.Length)
            {
                return CreateToken(StackFrameKind.EndOfLine, trivia, VirtualCharSequence.Empty);
            }

            var ch = CurrentChar;
            Position++;

            return CreateToken(GetKind(ch), trivia, Text.GetSubSequence(new TextSpan(Position - 1, 1)));
        }

        /// <summary>
        /// Scans until EndOfLine is found, treating all text as trivia
        /// </summary>
        public StackFrameTrivia? ScanTrailingTrivia()
        {
            if (Position == Text.Length)
            {
                return null;
            }

            var length = Text.Length - Position;
            return CreateTrivia(StackFrameKind.TrailingTrivia, Text.GetSubSequence(new TextSpan(Position - 1, length)));
        }

        public StackFrameToken? ScanIdentifier()
        {
            if (Position == Text.Length)
            {
                return null;
            }

            var startPosition = Position;

            var ch = CurrentChar;
            Position++;

            if (!UnicodeCharacterUtilities.IsIdentifierStartCharacter((char)ch.Value))
            {
                return null;
            }

            while (UnicodeCharacterUtilities.IsIdentifierPartCharacter((char)ch.Value))
            {
                ch = CurrentChar;
                Position++;
            }

            var identifierEnd = Position - 1;
            var identifierSpan = new TextSpan(startPosition, identifierEnd - startPosition);
            var identifier = CreateToken(StackFrameKind.IdentifierToken, Text.GetSubSequence(identifierSpan));
            return identifier;
        }

        public StackFrameToken ScanTypeArity()
        {
            if (Position == Text.Length)
            {
                return default;
            }

            var startPosition = Position;
            var ch = CurrentChar;
            Position++;

            while (IsNumber(ch))
            {
                ch = CurrentChar;
                Position++;
            }

            var aritySpan = new TextSpan(startPosition, (Position - 1) - startPosition);
            var arityToken = CreateToken(StackFrameKind.TextToken, Text.GetSubSequence(aritySpan));
            return arityToken;
        }

        internal ImmutableArray<StackFrameToken> ScanArrayBrackets()
        {
            if (Position == Text.Length)
            {
                return default;
            }

            using var _ = ArrayBuilder<StackFrameToken>.GetInstance(out var builder);

            while (Position < Text.Length)
            {
                // Use PreviousCharAsToken here because we expect the 
                // start of the brackets to be the previous token that started
                // the need to parse for an array
                var token = PreviousCharAsToken();
                if (!IsArrayBracket(token))
                {
                    break;
                }

                Position++;
                builder.Add(token);
            }

            Debug.Assert(builder.Count >= 2);

            return builder.ToImmutable();

            static bool IsArrayBracket(StackFrameToken token)
                => token.Kind is StackFrameKind.OpenBracketToken or StackFrameKind.CloseBracketToken;
        }

        public StackFrameToken PreviousCharAsToken()
        {
            var previousChar = Text[Position - 1];
            return CreateToken(GetKind(previousChar), Text.GetSubSequence(new TextSpan(Position - 1, 1)));
        }

        internal StackFrameTrivia? ScanWhiteSpace(bool includePrevious)
        {
            if (Position == Text.Length)
            {
                return null;
            }

            var startPosition = includePrevious ? Position - 1 : Position;

            while (IsBlank(CurrentChar))
            {
                Position++;
            }

            if (Position == startPosition)
            {
                return null;
            }

            var whitespaceSpan = new TextSpan(startPosition, Position - startPosition);
            return CreateTrivia(StackFrameKind.WhitespaceTrivia, Text.GetSubSequence(whitespaceSpan));
        }

        public StackFrameToken CurrentCharAsToken()
        {
            if (Position == Text.Length)
            {
                return CreateToken(StackFrameKind.EndOfLine, VirtualCharSequence.Empty);
            }

            var previousChar = Text[Position];
            return CreateToken(GetKind(previousChar), Text.GetSubSequence(new TextSpan(Position, 1)));
        }

        public bool IsAt(string val)
            => TextAt(Position, val);

        private bool TextAt(int position, string val)
        {
            for (var i = 0; i < val.Length; i++)
            {
                if (position + i >= Text.Length ||
                    Text[position + i] != val[i])
                {
                    return false;
                }
            }

            return true;
        }

#nullable disable
        public static StackFrameToken CreateToken(StackFrameKind kind, VirtualCharSequence virtualChars)
            => new(kind, ImmutableArray<StackFrameTrivia>.Empty, virtualChars, ImmutableArray<StackFrameTrivia>.Empty, ImmutableArray<EmbeddedDiagnostic>.Empty, value: null);

        public static StackFrameToken CreateToken(StackFrameKind kind, ImmutableArray<StackFrameTrivia> leadingTrivia, VirtualCharSequence virtualChars)
            => new(kind, leadingTrivia, virtualChars, ImmutableArray<StackFrameTrivia>.Empty, ImmutableArray<EmbeddedDiagnostic>.Empty, value: null);

        public static StackFrameTrivia CreateTrivia(StackFrameKind kind, VirtualCharSequence virtualChars)
            => CreateTrivia(kind, virtualChars, ImmutableArray<EmbeddedDiagnostic>.Empty);

        public static StackFrameTrivia CreateTrivia(StackFrameKind kind, VirtualCharSequence virtualChars, ImmutableArray<EmbeddedDiagnostic> diagnostics)
            => new(kind, virtualChars, diagnostics);
#nullable enable

        private static StackFrameKind GetKind(VirtualChar ch)
            => ch.Value switch
            {
                '\n' => StackFrameKind.EndOfLine,
                '&' => StackFrameKind.AmpersandToken,
                '[' => StackFrameKind.OpenBracketToken,
                ']' => StackFrameKind.CloseBracketToken,
                '(' => StackFrameKind.OpenParenToken,
                ')' => StackFrameKind.CloseParenToken,
                '.' => StackFrameKind.DotToken,
                '+' => StackFrameKind.PlusToken,
                ',' => StackFrameKind.CommaToken,
                ':' => StackFrameKind.ColonToken,
                '=' => StackFrameKind.EqualsToken,
                '>' => StackFrameKind.GreaterThanToken,
                '<' => StackFrameKind.LessThanToken,
                '-' => StackFrameKind.MinusToken,
                '\'' => StackFrameKind.SingleQuoteToken,
                '`' => StackFrameKind.GraveAccentToken,
                '\\' => StackFrameKind.BackslashToken,
                '/' => StackFrameKind.ForwardSlashToken,
                _ => IsBlank(ch) ? StackFrameKind.WhitespaceToken : StackFrameKind.TextToken,
            };

        private static bool IsNumber(VirtualChar ch)
            => ch.Value switch
            {
                '0' or '1' or '2' or '3' or '4' or '5' or '6' or '7' or '8' or '9'
                  => true,
                _ => false
            };

        private ImmutableArray<StackFrameTrivia> ScanLeadingTrivia(bool allowTrivia)
        {
            if (!allowTrivia)
            {
                return ImmutableArray<StackFrameTrivia>.Empty;
            }

            using var _ = ArrayBuilder<StackFrameTrivia>.GetInstance(out var result);

            while (Position < Text.Length)
            {
                var atTrivia = ScanAtTrivia();
                if (atTrivia != null)
                {
                    result.Add(atTrivia.Value);
                    continue;
                }

                var whitespace = ScanWhitespace();
                if (whitespace != null)
                {
                    result.Add(whitespace.Value);
                    continue;
                }

                var inTrivia = ScanInTrivia();
                if (inTrivia != null)
                {
                    result.Add(inTrivia.Value);
                    continue;
                }

                break;
            }

            return result.ToImmutable();
        }

        public StackFrameTrivia? ScanAtTrivia()
        {
            if (Position >= Text.Length)
            {
                return null;
            }

            // TODO: Handle multiple languages? Right now we're going to only parse english
            const string AtString = "at ";

            if (IsAt(AtString))
            {
                var start = Position;
                Position += AtString.Length;

                return CreateTrivia(StackFrameKind.AtTrivia, GetSubPatternToCurrentPos(start));
            }

            return null;
        }

        private StackFrameTrivia? ScanWhitespace()
        {
            var start = Position;
            while (Position < Text.Length && IsBlank(Text[Position]))
            {
                Position++;
            }

            if (Position > start)
            {
                return CreateTrivia(StackFrameKind.WhitespaceTrivia, GetSubPatternToCurrentPos(start));
            }

            return null;
        }

        public StackFrameTrivia? ScanInTrivia()
        {
            if (Position >= Text.Length)
            {
                return null;
            }

            // TODO: Handle multiple languages? Right now we're going to only parse english
            const string InString = " in ";

            if (IsAt(InString))
            {
                var start = Position;
                Position += InString.Length;

                return CreateTrivia(StackFrameKind.AtTrivia, GetSubPatternToCurrentPos(start));
            }

            return null;
        }

        public static bool IsBlank(VirtualChar ch)
        {
            // List taken from the native regex parser.
            switch (ch.Value)
            {
                case '\u0009':
                case '\u000A':
                case '\u000C':
                case '\u000D':
                case ' ':
                    return true;
                default:
                    return false;
            }
        }
    }
}
