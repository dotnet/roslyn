// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.ConvertToRawString
{
    internal static class ConvertToRawStringHelpers
    {
        public static bool CanBeSingleLine(VirtualCharSequence characters)
        {
            // Single line raw strings cannot start/end with quote.
            if (characters.First().Rune.Value == '"' ||
                characters.Last().Rune.Value == '"')
            {
                return false;
            }

            // a single line raw string cannot contain a newline.
            if (characters.Any(static ch => IsNewLine(ch)))
                return false;

            return true;
        }

        public static bool IsNewLine(VirtualChar ch)
            => ch.Rune.Utf16SequenceLength == 1 && SyntaxFacts.IsNewLine((char)ch.Value);

        public static bool AllEscapesAreQuotes(VirtualCharSequence sequence)
            => AllEscapesAre(sequence, static ch => ch.Value == '"');

        private static bool AllEscapesAre(VirtualCharSequence sequence, Func<VirtualChar, bool> predicate)
        {
            var hasEscape = false;

            foreach (var ch in sequence)
            {
                if (ch.Span.Length > 1)
                {
                    hasEscape = true;
                    if (!predicate(ch))
                        return false;
                }
            }

            return hasEscape;
        }

        public static bool IsInDirective(SyntaxNode? node)
        {
            while (node != null)
            {
                if (node is DirectiveTriviaSyntax)
                    return true;

                node = node.GetParent(ascendOutOfTrivia: true);
            }

            return false;
        }

        public static bool CanConvert(VirtualChar ch)
        {
            // Don't bother with unpaired surrogates.  This is just a legacy language corner case that we don't care to
            // even try having support for.
            if (ch.SurrogateChar != 0)
                return false;

            // Can't ever encode a null value directly in a c# file as our lexer/parser/text apis will stop righ there.
            if (ch.Rune.Value == 0)
                return false;

            // Check if we have an escaped character in the original string.
            if (ch.Span.Length > 1)
            {
                // An escaped newline is fine to convert (to a multi-line raw string).
                if (IsNewLine(ch))
                    return true;

                // Control/formatting unicode escapes should stay as escapes.  The user code will just be enormously
                // difficult to read/reason about if we convert those to the actual corresponding non-escaped chars.
                var category = Rune.GetUnicodeCategory(ch.Rune);
                if (category is UnicodeCategory.Format or UnicodeCategory.Control)
                    return false;
            }

            return true;
        }

        public static int GetLongestQuoteSequence(VirtualCharSequence characters)
        {
            var longestQuoteSequence = 0;
            for (int i = 0, n = characters.Length; i < n;)
            {
                var j = i;
                while (j < n && characters[j] == '"')
                    j++;

                longestQuoteSequence = Math.Max(longestQuoteSequence, j - i);
                i = j + 1;
            }

            return longestQuoteSequence;
        }
    }
}
