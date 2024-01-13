// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Syntax.InternalSyntax
{
    internal partial class Lexer
    {
        // Maximum size of tokens/trivia that we cache and use in quick scanner.
        // From what I see in our own codebase, tokens longer then 40-50 chars are 
        // not very common. 
        // So it seems reasonable to limit the sizes to some round number like 42.
        internal const int MaxCachedTokenSize = 42;

        private enum QuickScanState : byte
        {
            Initial,
            FollowingWhite,
            FollowingCR,
            Ident,
            Number,
            Punctuation,
            Dot,
            CompoundPunctStart,
            DoneAfterNext,
            // we are relying on Bad state immediately following Done 
            // to be able to detect exiting conditions in one "state >= Done" test.
            // And we are also relying on this to be the last item in the enum.
            Done,
            Bad = Done + 1
        }

        private enum CharFlags : byte
        {
            White,      // simple whitespace (space/tab)
            CR,         // carriage return
            LF,         // line feed
            Letter,     // letter
            Digit,      // digit 0-9
            Punct,      // some simple punctuation (parens, braces, comma, equals, question)
            Dot,        // dot is different from other punctuation when followed by a digit (Ex: .9 )
            CompoundPunctStart, // may be a part of compound punctuation. will be used only if followed by (not white) && (not punct)
            Slash,      // /
            Complex,    // complex - causes scanning to abort
            EndOfFile,  // legal type character (except !, which is contextually dictionary lookup
        }

        // PERF: Use byte instead of QuickScanState so the compiler can use array literal initialization.
        //       The most natural type choice, Enum arrays, are not blittable due to a CLR limitation.
        private static readonly byte[,] s_stateTransitions = new byte[,]
        {
            // Initial
            {
                (byte)QuickScanState.Initial,             // White
                (byte)QuickScanState.Initial,             // CR
                (byte)QuickScanState.Initial,             // LF
                (byte)QuickScanState.Ident,               // Letter
                (byte)QuickScanState.Number,              // Digit
                (byte)QuickScanState.Punctuation,         // Punct
                (byte)QuickScanState.Dot,                 // Dot
                (byte)QuickScanState.CompoundPunctStart,  // Compound
                (byte)QuickScanState.Bad,                 // Slash
                (byte)QuickScanState.Bad,                 // Complex
                (byte)QuickScanState.Bad,                 // EndOfFile
            },

            // Following White
            {
                (byte)QuickScanState.FollowingWhite,      // White
                (byte)QuickScanState.FollowingCR,         // CR
                (byte)QuickScanState.DoneAfterNext,       // LF
                (byte)QuickScanState.Done,                // Letter
                (byte)QuickScanState.Done,                // Digit
                (byte)QuickScanState.Done,                // Punct
                (byte)QuickScanState.Done,                // Dot
                (byte)QuickScanState.Done,                // Compound
                (byte)QuickScanState.Bad,                 // Slash
                (byte)QuickScanState.Bad,                 // Complex
                (byte)QuickScanState.Done,                // EndOfFile
            },

            // Following CR
            {
                (byte)QuickScanState.Done,                // White
                (byte)QuickScanState.Done,                // CR
                (byte)QuickScanState.DoneAfterNext,       // LF
                (byte)QuickScanState.Done,                // Letter
                (byte)QuickScanState.Done,                // Digit
                (byte)QuickScanState.Done,                // Punct
                (byte)QuickScanState.Done,                // Dot
                (byte)QuickScanState.Done,                // Compound
                (byte)QuickScanState.Done,                // Slash
                (byte)QuickScanState.Done,                // Complex
                (byte)QuickScanState.Done,                // EndOfFile
            },

            // Identifier
            {
                (byte)QuickScanState.FollowingWhite,      // White
                (byte)QuickScanState.FollowingCR,         // CR
                (byte)QuickScanState.DoneAfterNext,       // LF
                (byte)QuickScanState.Ident,               // Letter
                (byte)QuickScanState.Ident,               // Digit
                (byte)QuickScanState.Done,                // Punct
                (byte)QuickScanState.Done,                // Dot
                (byte)QuickScanState.Done,                // Compound
                (byte)QuickScanState.Bad,                 // Slash
                (byte)QuickScanState.Bad,                 // Complex
                (byte)QuickScanState.Done,                // EndOfFile
            },

            // Number
            {
                (byte)QuickScanState.FollowingWhite,      // White
                (byte)QuickScanState.FollowingCR,         // CR
                (byte)QuickScanState.DoneAfterNext,       // LF
                (byte)QuickScanState.Bad,                 // Letter (might be 'e' or 'x' or suffix)
                (byte)QuickScanState.Number,              // Digit
                (byte)QuickScanState.Done,                // Punct
                (byte)QuickScanState.Bad,                 // Dot (Number is followed by a dot - too complex for us to handle here).
                (byte)QuickScanState.Done,                // Compound
                (byte)QuickScanState.Bad,                 // Slash
                (byte)QuickScanState.Bad,                 // Complex
                (byte)QuickScanState.Done,                // EndOfFile
            },

            // Punctuation
            {
                (byte)QuickScanState.FollowingWhite,      // White
                (byte)QuickScanState.FollowingCR,         // CR
                (byte)QuickScanState.DoneAfterNext,       // LF
                (byte)QuickScanState.Done,                // Letter
                (byte)QuickScanState.Done,                // Digit
                (byte)QuickScanState.Done,                // Punct
                (byte)QuickScanState.Done,                // Dot
                (byte)QuickScanState.Done,                // Compound
                (byte)QuickScanState.Bad,                 // Slash
                (byte)QuickScanState.Bad,                 // Complex
                (byte)QuickScanState.Done,                // EndOfFile
            },

            // Dot
            {
                (byte)QuickScanState.FollowingWhite,      // White
                (byte)QuickScanState.FollowingCR,         // CR
                (byte)QuickScanState.DoneAfterNext,       // LF
                (byte)QuickScanState.Done,                // Letter
                (byte)QuickScanState.Number,              // Digit
                (byte)QuickScanState.Done,                // Punct
                (byte)QuickScanState.Bad,                 // Dot (DotDot range token, exit so that we handle it in subsequent scanning code)
                (byte)QuickScanState.Done,                // Compound
                (byte)QuickScanState.Bad,                 // Slash
                (byte)QuickScanState.Bad,                 // Complex
                (byte)QuickScanState.Done,                // EndOfFile
            },

            // Compound Punctuation
            {
                (byte)QuickScanState.FollowingWhite,      // White
                (byte)QuickScanState.FollowingCR,         // CR
                (byte)QuickScanState.DoneAfterNext,       // LF
                (byte)QuickScanState.Done,                // Letter
                (byte)QuickScanState.Done,                // Digit
                (byte)QuickScanState.Bad,                 // Punct
                (byte)QuickScanState.Done,                // Dot
                (byte)QuickScanState.Bad,                 // Compound
                (byte)QuickScanState.Bad,                 // Slash
                (byte)QuickScanState.Bad,                 // Complex
                (byte)QuickScanState.Done,                // EndOfFile
            },

            // Done after next
            {
                (byte)QuickScanState.Done,                // White
                (byte)QuickScanState.Done,                // CR
                (byte)QuickScanState.Done,                // LF
                (byte)QuickScanState.Done,                // Letter
                (byte)QuickScanState.Done,                // Digit
                (byte)QuickScanState.Done,                // Punct
                (byte)QuickScanState.Done,                // Dot
                (byte)QuickScanState.Done,                // Compound
                (byte)QuickScanState.Done,                // Slash
                (byte)QuickScanState.Done,                // Complex
                (byte)QuickScanState.Done,                // EndOfFile
            },
        };

        private SyntaxToken? QuickScanSyntaxToken()
        {
            this.Start();
            var state = QuickScanState.Initial;
            int i = TextWindow.Offset;
            int n = TextWindow.CharacterWindowCount;
            n = Math.Min(n, i + MaxCachedTokenSize);

            int hashCode = Hash.FnvOffsetBias;

            //localize frequently accessed fields
            var charWindow = TextWindow.CharacterWindow;
            var charPropLength = CharProperties.Length;

            for (; i < n; i++)
            {
                char c = charWindow[i];
                int uc = unchecked((int)c);

                var flags = uc < charPropLength ? (CharFlags)CharProperties[uc] : CharFlags.Complex;

                state = (QuickScanState)s_stateTransitions[(int)state, (int)flags];
                // NOTE: that Bad > Done and it is the only state like that
                // as a result, we will exit the loop on either Bad or Done.
                // the assert below will validate that these are the only states on which we exit
                // Also note that we must exit on Done or Bad
                // since the state machine does not have transitions for these states 
                // and will promptly fail if we do not exit.
                if (state >= QuickScanState.Done)
                {
                    goto exitWhile;
                }

                hashCode = unchecked((hashCode ^ uc) * Hash.FnvPrime);
            }

            state = QuickScanState.Bad; // ran out of characters in window
exitWhile:

            TextWindow.AdvanceChar(i - TextWindow.Offset);
            Debug.Assert(state == QuickScanState.Bad || state == QuickScanState.Done, "can only exit with Bad or Done");

            if (state == QuickScanState.Done)
            {
                // this is a good token!
                var token = _cache.LookupToken(
                    TextWindow.CharacterWindow,
                    TextWindow.LexemeRelativeStart,
                    i - TextWindow.LexemeRelativeStart,
                    hashCode,
                    _createQuickTokenFunction);
                return token;
            }
            else
            {
                TextWindow.Reset(TextWindow.LexemeStartPosition);
                return null;
            }
        }

        private readonly Func<SyntaxToken> _createQuickTokenFunction;

        private SyntaxToken CreateQuickToken()
        {
#if DEBUG
            var quickWidth = TextWindow.Width;
#endif
            TextWindow.Reset(TextWindow.LexemeStartPosition);
            var token = this.LexSyntaxToken();
#if DEBUG
            Debug.Assert(quickWidth == token.FullWidth);
#endif
            return token;
        }

        // The following table classifies the first 0x180 Unicode characters. 
        // # is marked complex as it may start directives.
        // PERF: Use byte instead of CharFlags so the compiler can use array literal initialization.
        //       The most natural type choice, Enum arrays, are not blittable due to a CLR limitation.
        private static ReadOnlySpan<byte> CharProperties => new[]
        {
            // 0 .. 31
            (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex,
            (byte)CharFlags.Complex,
            (byte)CharFlags.White,   // TAB
            (byte)CharFlags.LF,      // LF
            (byte)CharFlags.White,   // VT
            (byte)CharFlags.White,   // FF
            (byte)CharFlags.CR,      // CR
            (byte)CharFlags.Complex,
            (byte)CharFlags.Complex,
            (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex,
            (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex,

            // 32 .. 63
            (byte)CharFlags.White,    // SPC
            (byte)CharFlags.CompoundPunctStart,    // !
            (byte)CharFlags.Complex,  // "
            (byte)CharFlags.Complex,  // #
            (byte)CharFlags.Complex,  // $
            (byte)CharFlags.CompoundPunctStart, // %
            (byte)CharFlags.CompoundPunctStart, // &
            (byte)CharFlags.Complex,  // '
            (byte)CharFlags.Punct,    // (
            (byte)CharFlags.Punct,    // )
            (byte)CharFlags.CompoundPunctStart, // *
            (byte)CharFlags.CompoundPunctStart, // +
            (byte)CharFlags.Punct,    // ,
            (byte)CharFlags.CompoundPunctStart, // -
            (byte)CharFlags.Dot,      // .
            (byte)CharFlags.Slash,    // /
            (byte)CharFlags.Digit,    // 0
            (byte)CharFlags.Digit,    // 1
            (byte)CharFlags.Digit,    // 2
            (byte)CharFlags.Digit,    // 3
            (byte)CharFlags.Digit,    // 4
            (byte)CharFlags.Digit,    // 5
            (byte)CharFlags.Digit,    // 6
            (byte)CharFlags.Digit,    // 7
            (byte)CharFlags.Digit,    // 8
            (byte)CharFlags.Digit,    // 9
            (byte)CharFlags.CompoundPunctStart,  // :
            (byte)CharFlags.Punct,    // ;
            (byte)CharFlags.CompoundPunctStart,  // <
            (byte)CharFlags.CompoundPunctStart,  // =
            (byte)CharFlags.CompoundPunctStart,  // >
            (byte)CharFlags.CompoundPunctStart,  // ?

            // 64 .. 95
            (byte)CharFlags.Complex,  // @
            (byte)CharFlags.Letter,   // A
            (byte)CharFlags.Letter,   // B
            (byte)CharFlags.Letter,   // C
            (byte)CharFlags.Letter,   // D
            (byte)CharFlags.Letter,   // E
            (byte)CharFlags.Letter,   // F
            (byte)CharFlags.Letter,   // G
            (byte)CharFlags.Letter,   // H
            (byte)CharFlags.Letter,   // I
            (byte)CharFlags.Letter,   // J
            (byte)CharFlags.Letter,   // K
            (byte)CharFlags.Letter,   // L
            (byte)CharFlags.Letter,   // M
            (byte)CharFlags.Letter,   // N
            (byte)CharFlags.Letter,   // O
            (byte)CharFlags.Letter,   // P
            (byte)CharFlags.Letter,   // Q
            (byte)CharFlags.Letter,   // R
            (byte)CharFlags.Letter,   // S
            (byte)CharFlags.Letter,   // T
            (byte)CharFlags.Letter,   // U
            (byte)CharFlags.Letter,   // V
            (byte)CharFlags.Letter,   // W
            (byte)CharFlags.Letter,   // X
            (byte)CharFlags.Letter,   // Y
            (byte)CharFlags.Letter,   // Z
            (byte)CharFlags.Punct,    // [
            (byte)CharFlags.Complex,  // \
            (byte)CharFlags.Punct,    // ]
            (byte)CharFlags.CompoundPunctStart,    // ^
            (byte)CharFlags.Letter,   // _

            // 96 .. 127
            (byte)CharFlags.Complex,  // `
            (byte)CharFlags.Letter,   // a
            (byte)CharFlags.Letter,   // b
            (byte)CharFlags.Letter,   // c
            (byte)CharFlags.Letter,   // d
            (byte)CharFlags.Letter,   // e
            (byte)CharFlags.Letter,   // f
            (byte)CharFlags.Letter,   // g
            (byte)CharFlags.Letter,   // h
            (byte)CharFlags.Letter,   // i
            (byte)CharFlags.Letter,   // j
            (byte)CharFlags.Letter,   // k
            (byte)CharFlags.Letter,   // l
            (byte)CharFlags.Letter,   // m
            (byte)CharFlags.Letter,   // n
            (byte)CharFlags.Letter,   // o
            (byte)CharFlags.Letter,   // p
            (byte)CharFlags.Letter,   // q
            (byte)CharFlags.Letter,   // r
            (byte)CharFlags.Letter,   // s
            (byte)CharFlags.Letter,   // t
            (byte)CharFlags.Letter,   // u
            (byte)CharFlags.Letter,   // v
            (byte)CharFlags.Letter,   // w
            (byte)CharFlags.Letter,   // x
            (byte)CharFlags.Letter,   // y
            (byte)CharFlags.Letter,   // z
            (byte)CharFlags.Punct,    // {
            (byte)CharFlags.CompoundPunctStart,  // |
            (byte)CharFlags.Punct,    // }
            (byte)CharFlags.CompoundPunctStart,    // ~
            (byte)CharFlags.Complex,

            // 128 .. 159
            (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex,
            (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex,
            (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex,
            (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex,

            // 160 .. 191
            (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex,
            (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Letter, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex,
            (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Letter, (byte)CharFlags.Complex, (byte)CharFlags.Complex,
            (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Letter, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex, (byte)CharFlags.Complex,

            // 192 .. 
            (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter,
            (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter,
            (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Complex,
            (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter,

            (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter,
            (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter,
            (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Complex,
            (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter,

            (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter,
            (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter,
            (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter,
            (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter,

            (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter,
            (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter,
            (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter,
            (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter,

            (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter,
            (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter,
            (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter,
            (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter,

            (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter,
            (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter,
            (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter,
            (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter, (byte)CharFlags.Letter
        };
    }
}
