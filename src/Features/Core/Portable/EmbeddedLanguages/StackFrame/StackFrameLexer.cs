// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
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

        public VirtualCharSequence GetSubSequenceToCurrentPos(int start)
            => GetSubSequence(start, Position);

        public VirtualCharSequence GetSubSequence(int start, int end)
            => Text.GetSubSequence(TextSpan.FromBounds(start, end));

        public StackFrameTrivia? TryScanRemainingTrivia()
        {
            if (Position == Text.Length)
            {
                return null;
            }

            var start = Position;
            Position = Text.Length;

            return CreateTrivia(StackFrameKind.SkippedTextTrivia, GetSubSequenceToCurrentPos(start));
        }

        public StackFrameToken? TryScanIdentifier()
            => TryScanIdentifier(scanAtTrivia: false, scanWhitespace: false);

        public StackFrameToken? TryScanIdentifier(bool scanAtTrivia, bool scanWhitespace)
        {
            var originalPosition = Position;
            var atTrivia = scanAtTrivia ? TryScanAtTrivia() : null;
            var leadingWhitespace = scanWhitespace ? TryScanWhiteSpace() : null;

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

            var trailingWhitespace = scanWhitespace ? TryScanWhiteSpace() : null;

            return CreateToken(
                StackFrameKind.IdentifierToken,
                leadingTrivia: CreateTrivia(atTrivia, leadingWhitespace),
                GetSubSequenceToCurrentPos(startPosition),
                trailingTrivia: CreateTrivia(trailingWhitespace));
        }

        public StackFrameToken CurrentCharAsToken()
        {
            if (Position == Text.Length)
            {
                return CreateToken(StackFrameKind.EndOfLine, VirtualCharSequence.Empty);
            }

            var ch = Text[Position];
            return CreateToken(GetKind(ch), Text.GetSubSequence(new TextSpan(Position, 1)));
        }

        /// <summary>
        /// Progress the position by one if the current character
        /// matches the kind.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the position was incremented
        /// </returns>
        public bool ScanCurrentCharAsTokenIfMatch(StackFrameKind kind, out StackFrameToken token)
            => ScanCurrentCharAsTokenIfMatch(kind, scanTrailingWhitespace: false, out token);

        /// <summary>
        /// Progress the position by one if the current character
        /// matches the kind.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if the position was incremented
        /// </returns>
        public bool ScanCurrentCharAsTokenIfMatch(StackFrameKind kind, bool scanTrailingWhitespace, out StackFrameToken token)
        {
            if (GetKind(CurrentChar) == kind)
            {
                token = CurrentCharAsToken();
                Position++;

                if (scanTrailingWhitespace)
                {
                    token = token.With(trailingTrivia: CreateTrivia(TryScanWhiteSpace()));
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
        public bool ScanCurrentCharAsTokenIfMatch(Func<StackFrameKind, bool> matchFn, out StackFrameToken token)
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

        public StackFrameTrivia? TryScanAtTrivia()
            // TODO: Handle multiple languages? Right now we're going to only parse english
            => TryScanStringTrivia("at ", StackFrameKind.AtTrivia);
        public StackFrameTrivia? TryScanInTrivia()
            // TODO: Handle multiple languages? Right now we're going to only parse english
            => TryScanStringTrivia(" in ", StackFrameKind.InTrivia);

        public StackFrameTrivia? TryScanLineTrivia()
            // TODO: Handle multiple languages? Right now we're going to only parse english
            => TryScanStringTrivia("line ", StackFrameKind.LineTrivia);

        /// <summary>
        /// Attempts to parse <see cref="StackFrameKind.InTrivia"/> and a path following https://docs.microsoft.com/en-us/windows/win32/fileio/naming-a-file#file-and-directory-names
        /// Uses <see cref="FileInfo"/> as a tool to determine if the path is correct for returning. 
        /// </summary>
        public StackFrameToken? TryScanPath()
        {
            var inTrivia = TryScanInTrivia();
            if (!inTrivia.HasValue)
            {
                return null;
            }

            var startPosition = Position;

            while (Position < Text.Length)
            {
                // Path needs to do a look ahead to determine if adding the next character
                // invalidates the path. Break if it does.
                //
                // This helps to keep the complex rules for what FileInfo does to validate path by calling to it directly.
                // We can't simply check all invalid characters for a path because location in the path is important, and we're not 
                // in the business of validating if something is correctly a file. It's cheap enough to let the FileInfo constructor do that. The downside 
                // is that we are constructing a new object with every pass. If this becomes problematic we can revisit and fine a more 
                // optimized pattern to handle all of the edge cases.
                //
                // Example: C:\my\path \:other
                //                      ^-- the ":" breaks the path, but we can't simply break on all ":" (which is included in the invalid characters for Path)
                //

                var str = GetSubSequence(startPosition, Position + 1).CreateString();

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
                return CreateToken(StackFrameKind.None, ImmutableArray.Create(inTrivia.Value), VirtualCharSequence.Empty);
            }

            return CreateToken(StackFrameKind.PathToken, ImmutableArray.Create(inTrivia.Value), GetSubSequenceToCurrentPos(startPosition));
        }

        /// <summary>
        /// Returns a number token with the <see cref="StackFrameKind.LineTrivia"/> and remainging <see cref="StackFrameKind.SkippedTextTrivia"/>
        /// attached to it. If no numbers are found, returns a <see cref="StackFrameKind.None"/> token with the trivia that was scanned.
        /// </summary>
        /// <returns></returns>
        public StackFrameToken? TryScanLineNumber()
        {
            var lineTrivia = TryScanLineTrivia();
            if (!lineTrivia.HasValue)
            {
                return null;
            }

            var numberToken = TryScanNumbers();
            if (!numberToken.HasValue)
            {
                return CreateToken(StackFrameKind.None, ImmutableArray.Create(lineTrivia.Value), VirtualCharSequence.Empty);
            }

            var remainingTrivia = TryScanRemainingTrivia();

            return numberToken.Value.With(
                leadingTrivia: ImmutableArray.Create(lineTrivia.Value),
                trailingTrivia: remainingTrivia.HasValue ? ImmutableArray.Create(remainingTrivia.Value) : ImmutableArray<StackFrameTrivia>.Empty);
        }

        public StackFrameToken? TryScanNumbers()
        {
            var start = Position;
            while (IsNumber(CurrentChar))
            {
                Position++;
            }

            if (start == Position)
            {
                return null;
            }

            return CreateToken(StackFrameKind.NumberToken, GetSubSequenceToCurrentPos(start));
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

        public static StackFrameToken CreateToken(StackFrameKind kind, ImmutableArray<StackFrameTrivia> leadingTrivia, VirtualCharSequence virtualChars, ImmutableArray<StackFrameTrivia> trailingTrivia)
            => new(kind, leadingTrivia, virtualChars, trailingTrivia, ImmutableArray<EmbeddedDiagnostic>.Empty, value: null!);

        public static StackFrameTrivia CreateTrivia(StackFrameKind kind, VirtualCharSequence virtualChars)
            => CreateTrivia(kind, virtualChars, ImmutableArray<EmbeddedDiagnostic>.Empty);

        public static StackFrameTrivia CreateTrivia(StackFrameKind kind, VirtualCharSequence virtualChars, ImmutableArray<EmbeddedDiagnostic> diagnostics)
        {
            // Empty trivia is not supported in StackFrames
            Debug.Assert(virtualChars.Length > 0);
            return new(kind, virtualChars, diagnostics);
        }

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

        private bool IsStringAtPosition(string val)
           => IsAtStartOfText(Position, val);

        private bool IsAtStartOfText(int position, string val)
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

        private StackFrameTrivia? TryScanStringTrivia(string valueToLookFor, StackFrameKind triviaKind)
        {
            if (IsStringAtPosition(valueToLookFor))
            {
                var start = Position;
                Position += valueToLookFor.Length;

                return CreateTrivia(triviaKind, GetSubSequenceToCurrentPos(start));
            }

            return null;
        }

        private StackFrameTrivia? TryScanWhiteSpace()
        {
            var startPosition = Position;

            while (IsBlank(CurrentChar))
            {
                Position++;
            }

            if (Position == startPosition)
            {
                return null;
            }

            return CreateTrivia(StackFrameKind.WhitespaceTrivia, GetSubSequenceToCurrentPos(startPosition));
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
                        : StackFrameKind.SkippedTextTrivia
            };

        private static bool IsNumber(VirtualChar ch)
            => ch.Value is >= '0' and <= '9';
    }
}
