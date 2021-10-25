// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;
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

        public StackFrameTrivia? ScanRemainingTrivia()
        {
            if (Position == Text.Length)
            {
                return null;
            }

            var start = Position;
            Position = Text.Length;

            return CreateTrivia(StackFrameKind.TextTrivia, GetSubPatternToCurrentPos(start));
        }

        public StackFrameToken? ScanIdentifier(bool scanAtTrivia = false, bool scanWhitespace = false)
        {
            var originalPosition = Position;
            var atTrivia = scanAtTrivia ? ScanAtTrivia() : null;
            var whitespaceTrivia = scanWhitespace ? ScanWhiteSpace() : null;

            var startPosition = Position;
            var ch = CurrentChar;
            if (!UnicodeCharacterUtilities.IsIdentifierStartCharacter((char)ch.Value))
            {
                // If we scan only trivia but don't get an identifier, we want to make sure
                // to reset back to this original position to let the trivia be consumed
                // in some other fashion if necessary 
                Position = originalPosition;
                return null;
            }

            while (UnicodeCharacterUtilities.IsIdentifierPartCharacter((char)ch.Value))
            {
                Position++;
                ch = CurrentChar;
            }

            return CreateToken(StackFrameKind.IdentifierToken, CreateTrivia(atTrivia, whitespaceTrivia), GetSubPatternToCurrentPos(startPosition));
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
            => ScanIfMatch(kind, scanTrailingWhitespace: false, out token);

        /// <summary>
        /// Progress the position by one if the current character
        /// matches the kind.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the position was incremented
        /// </returns>
        internal bool ScanIfMatch(StackFrameKind kind, bool scanTrailingWhitespace, out StackFrameToken token)
        {
            if (GetKind(CurrentChar) == kind)
            {
                token = CurrentCharAsToken();
                Position++;

                if (scanTrailingWhitespace)
                {
                    token = token.With(trailingTrivia: CreateTrivia(ScanWhiteSpace()));
                }

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
                _ => IsBlank(ch)
                    ? StackFrameKind.WhitespaceTrivia
                    : IsNumber(ch)
                        ? StackFrameKind.NumberToken
                        : StackFrameKind.TextTrivia
            };

        private static bool IsNumber(VirtualChar ch)
            => ch.Value switch
            {
                >= '0' and <= '9' => true,
                _ => false
            };

        public StackFrameTrivia? ScanAtTrivia()
        {
            if (Position == Text.Length)
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
            if (Position == Text.Length)
            {
                return null;
            }

            // TODO: Handle multiple languages? Right now we're going to only parse english
            const string InString = " in ";

            if (IsStringAtPosition(InString))
            {
                var start = Position;
                Position += InString.Length;

                return CreateTrivia(StackFrameKind.InTrivia, GetSubPatternToCurrentPos(start));
            }

            return null;
        }

        /// <summary>
        /// Attempts to parse a path following https://docs.microsoft.com/en-us/windows/win32/fileio/naming-a-file#file-and-directory-names
        /// Uses <see cref="FileInfo"/> as a tool to determine if the path is correct for returning. 
        /// </summary>
        internal StackFrameToken? ScanPath()
        {
            if (Position == Text.Length)
            {
                return null;
            }

            var startPosition = Position;

            while (Position < Text.Length)
            {
                // Path needs to do a look ahead to determine if adding the next character
                // invalidates the path. Break if it does
                var str = GetSubPattern(startPosition, Position + 1).CreateString();

                var isValidPath = IOUtilities.PerformIO(() =>
                {
                    var fileInfo = new FileInfo(str);
                    return true;
                }, false);

                if (!isValidPath)
                {
                    break;
                }

                Position++;
            }

            if (startPosition == Position)
            {
                return null;
            }

            return CreateToken(StackFrameKind.PathToken, GetSubPatternToCurrentPos(startPosition));
        }

        internal StackFrameTrivia? ScanLineTrivia()
        {
            if (Position == Text.Length)
            {
                return null;
            }

            // TODO: Handle multiple languages? Right now we're going to only parse english
            const string LineString = "line ";
            if (IsStringAtPosition(LineString))
            {
                var start = Position;
                Position += LineString.Length;

                return CreateTrivia(StackFrameKind.LineTrivia, GetSubPatternToCurrentPos(start));
            }

            return null;
        }

        internal StackFrameToken? ScanNumbers()
        {
            if (Position == Text.Length)
            {
                return null;
            }

            var start = Position;
            while (IsNumber(CurrentChar))
            {
                Position++;
            }

            if (start == Position)
            {
                return null;
            }

            return CreateToken(StackFrameKind.NumberToken, GetSubPatternToCurrentPos(start));
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
            => CreateToken(kind, ImmutableArray<StackFrameTrivia>.Empty, virtualChars);

        public static StackFrameToken CreateToken(StackFrameKind kind, ImmutableArray<StackFrameTrivia> leadingTrivia, VirtualCharSequence virtualChars)
            => new(kind, leadingTrivia, virtualChars, ImmutableArray<StackFrameTrivia>.Empty, ImmutableArray<EmbeddedDiagnostic>.Empty, value: null!);

        public static StackFrameTrivia CreateTrivia(StackFrameKind kind, VirtualCharSequence virtualChars)
            => CreateTrivia(kind, virtualChars, ImmutableArray<EmbeddedDiagnostic>.Empty);

        public static StackFrameTrivia CreateTrivia(StackFrameKind kind, VirtualCharSequence virtualChars, ImmutableArray<EmbeddedDiagnostic> diagnostics)
            => new(kind, virtualChars, diagnostics);

        public static ImmutableArray<StackFrameTrivia> CreateTrivia(params StackFrameTrivia?[] triviaArray)
        {
            using var _ = ArrayBuilder<StackFrameTrivia>.GetInstance(out var builder);
            foreach (var trivia in triviaArray)
            {
                if (trivia.HasValue)
                {
                    builder.Add(trivia.Value);
                }
            }

            return builder.ToImmutable();
        }
    }
}
