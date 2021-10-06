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
        public int Position { get; private set; }

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
            if (!UnicodeCharacterUtilities.IsIdentifierStartCharacter((char)ch.Value))
            {
                return null;
            }

            while (UnicodeCharacterUtilities.IsIdentifierPartCharacter((char)ch.Value))
            {
                Position++;
                ch = CurrentChar;
            }

            var identifierSpan = new TextSpan(startPosition, Position - startPosition);
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
                var kind = GetKind(CurrentChar);
                if (!IsArrayBracket(kind))
                {
                    break;
                }

                builder.Add(CurrentCharAsToken());
                Position++;
            }

            Debug.Assert(builder.Count >= 2);

            return builder.ToImmutable();

            static bool IsArrayBracket(StackFrameKind kind)
                => kind is StackFrameKind.OpenBracketToken or StackFrameKind.CloseBracketToken;
        }

        internal StackFrameTrivia? ScanWhiteSpace()
        {
            if (Position == Text.Length)
            {
                return null;
            }

            var startPosition = Position;

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

        public bool IsStringAtPosition(string val)
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

        /// <summary>
        /// Progress the position by one if the current character
        /// matches the kind.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the position was incremented
        /// </returns>
        internal bool ScanIfMatch(StackFrameKind kind, out StackFrameToken token)
        {
            if (GetKind(CurrentChar) == kind)
            {
                token = CurrentCharAsToken();
                Position++;
                return true;
            }

            token = default;
            return false;
        }

        /// <summary>
        /// Progress the position by one if the current character
        /// matches the kind.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the position was incremented
        /// </returns>
        internal bool ScanIfMatch(Func<StackFrameKind, bool> matchFn, out StackFrameToken token)
        {
            if (matchFn(GetKind(CurrentChar)))
            {
                token = CurrentCharAsToken();
                Position++;
                return true;
            }

            token = default;
            return false;
        }

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
                >= '0' and <= '9' => true,
                _ => false
            };

        public StackFrameTrivia? ScanAtTrivia()
        {
            if (Position >= Text.Length)
            {
                return null;
            }

            // TODO: Handle multiple languages? Right now we're going to only parse english
            const string AtString = "at ";

            if (IsStringAtPosition(AtString))
            {
                var start = Position;
                Position += AtString.Length;

                return CreateTrivia(StackFrameKind.AtTrivia, GetSubPatternToCurrentPos(start));
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

            if (IsStringAtPosition(InString))
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

        public static StackFrameToken CreateToken(StackFrameKind kind, VirtualCharSequence virtualChars)
            => new(kind, ImmutableArray<StackFrameTrivia>.Empty, virtualChars, ImmutableArray<StackFrameTrivia>.Empty, ImmutableArray<EmbeddedDiagnostic>.Empty, value: null!);

        public static StackFrameToken CreateToken(StackFrameKind kind, ImmutableArray<StackFrameTrivia> leadingTrivia, VirtualCharSequence virtualChars)
            => new(kind, leadingTrivia, virtualChars, ImmutableArray<StackFrameTrivia>.Empty, ImmutableArray<EmbeddedDiagnostic>.Empty, value: null!);

        public static StackFrameTrivia CreateTrivia(StackFrameKind kind, VirtualCharSequence virtualChars)
            => CreateTrivia(kind, virtualChars, ImmutableArray<EmbeddedDiagnostic>.Empty);

        public static StackFrameTrivia CreateTrivia(StackFrameKind kind, VirtualCharSequence virtualChars, ImmutableArray<EmbeddedDiagnostic> diagnostics)
            => new(kind, virtualChars, diagnostics);
    }
}
