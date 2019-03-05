// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
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
            => new RegexToken(kind, leadingTrivia, virtualChars, ImmutableArray<RegexTrivia>.Empty, ImmutableArray<EmbeddedDiagnostic>.Empty, value: null);

        public static RegexToken CreateMissingToken(RegexKind kind)
            => CreateToken(kind, ImmutableArray<RegexTrivia>.Empty, VirtualCharSequence.Empty);

        public static RegexTrivia CreateTrivia(RegexKind kind, VirtualCharSequence virtualChars)
            => CreateTrivia(kind, virtualChars, ImmutableArray<EmbeddedDiagnostic>.Empty);

        public static RegexTrivia CreateTrivia(RegexKind kind, VirtualCharSequence virtualChars, ImmutableArray<EmbeddedDiagnostic> diagnostics)
            => new RegexTrivia(kind, virtualChars, diagnostics);

        /// <summary>
        /// Maps an escaped character to the actual character it was escaping.  For something like
        /// 'a' this will map to actual '\a' char (the bell character).  However, for something like
        /// '(' this will just map to '(' as that's all that \( does in a regex.
        /// </summary>
        public static char MapEscapeChar(char ch)
        {
            switch (ch)
            {
                default:
                    return ch;

                case 'a': return '\u0007';  // bell
                case 'b': return '\b';      // backspace
                case 'e': return '\u001B';  // escape
                case 'f': return '\f';      // form feed
                case 'n': return '\n';      // new line
                case 'r': return '\r';      // carriage return
                case 't': return '\t';      // tab
                case 'v': return '\u000B';  // vertical tab
            }
        }

        public static bool IsSelfEscape(this RegexSimpleEscapeNode node)
        {
            if (node.TypeToken.VirtualChars.Length > 0)
            {
                var ch = node.TypeToken.VirtualChars[0].Char;
                return MapEscapeChar(ch) == ch;
            }

            return true;
        }
    }
}
