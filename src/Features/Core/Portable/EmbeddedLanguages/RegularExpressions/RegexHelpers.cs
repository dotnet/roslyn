// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.EmbeddedLanguages.Common;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;

namespace Microsoft.CodeAnalysis.EmbeddedLanguages.RegularExpressions
{
    using RegexToken = EmbeddedSyntaxToken<RegexKind>;
    using RegexTrivia = EmbeddedSyntaxTrivia<RegexKind>;

    internal static class RegexHelpers
    {
        public static bool HasOption(RegexOptions options, RegexOptions val)
            => (options & val) != 0;

        public static RegexToken CreateToken(RegexKind kind, ImmutableArray<RegexTrivia> leadingTrivia, VirtualCharSequence virtualChars)
            => new(kind, leadingTrivia, virtualChars, ImmutableArray<RegexTrivia>.Empty, ImmutableArray<EmbeddedDiagnostic>.Empty, value: null);

        public static RegexToken CreateMissingToken(RegexKind kind)
            => CreateToken(kind, ImmutableArray<RegexTrivia>.Empty, VirtualCharSequence.Empty);

        public static RegexTrivia CreateTrivia(RegexKind kind, VirtualCharSequence virtualChars)
            => CreateTrivia(kind, virtualChars, ImmutableArray<EmbeddedDiagnostic>.Empty);

        public static RegexTrivia CreateTrivia(RegexKind kind, VirtualCharSequence virtualChars, ImmutableArray<EmbeddedDiagnostic> diagnostics)
            => new(kind, virtualChars, diagnostics);

        /// <summary>
        /// Maps an escaped character to the actual character it was escaping.  For something like
        /// 'a' this will map to actual '\a' char (the bell character).  However, for something like
        /// '(' this will just map to '(' as that's all that \( does in a regex.
        /// </summary>
        public static VirtualChar MapEscapeChar(VirtualChar ch)
            => ch.Value switch
            {
                'a' => VirtualChar.Create(new Rune('\u0007'), ch.Span),    // bell
                'b' => VirtualChar.Create(new Rune('\b'), ch.Span),        // backspace
                'e' => VirtualChar.Create(new Rune('\u001B'), ch.Span),    // escape
                'f' => VirtualChar.Create(new Rune('\f'), ch.Span),        // form feed
                'n' => VirtualChar.Create(new Rune('\n'), ch.Span),        // new line
                'r' => VirtualChar.Create(new Rune('\r'), ch.Span),        // carriage return
                't' => VirtualChar.Create(new Rune('\t'), ch.Span),        // tab
                'v' => VirtualChar.Create(new Rune('\u000B'), ch.Span),    // vertical tab
                _ => ch,
            };

        public static bool IsSelfEscape(this RegexSimpleEscapeNode node)
        {
            if (node.TypeToken.VirtualChars.Length > 0)
            {
                var ch = node.TypeToken.VirtualChars[0];
                return MapEscapeChar(ch) == ch;
            }

            return true;
        }
    }
}
