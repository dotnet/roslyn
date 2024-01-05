// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.ConvertToRawString;

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
        if (characters.Any(static ch => IsCSharpNewLine(ch)))
            return false;

        return true;
    }

    public static bool IsCSharpNewLine(VirtualChar ch)
        => ch.Rune.Utf16SequenceLength == 1 && SyntaxFacts.IsNewLine((char)ch.Value);

    public static bool IsCSharpWhitespace(VirtualChar ch)
        => ch.Rune.Utf16SequenceLength == 1 && SyntaxFacts.IsWhitespace((char)ch.Value);

    public static bool IsCarriageReturnNewLine(VirtualCharSequence characters, int index)
    {
        return index + 1 < characters.Length &&
            characters[index].Rune is { Utf16SequenceLength: 1, Value: '\r' } &&
            characters[index + 1].Rune is { Utf16SequenceLength: 1, Value: '\n' };
    }

    public static bool AllEscapesAreQuotes(VirtualCharSequence sequence)
        => AllEscapesAre(sequence, static ch => ch.Value == '"');

    public static bool AllEscapesAre(VirtualCharSequence sequence, Func<VirtualChar, bool> predicate)
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

    public static bool CanConvert(VirtualCharSequence characters)
        => !characters.IsDefault && characters.All(static ch => CanConvert(ch));

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
            if (IsCSharpNewLine(ch))
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
        => GetLongestCharacterSequence(characters, '"');

    public static int GetLongestBraceSequence(VirtualCharSequence characters)
        => Math.Max(GetLongestCharacterSequence(characters, '{'), GetLongestCharacterSequence(characters, '}'));

    private static int GetLongestCharacterSequence(VirtualCharSequence characters, char c)
    {
        var longestSequence = 0;
        for (int i = 0, n = characters.Length; i < n;)
        {
            var j = i;
            while (j < n && characters[j] == c)
                j++;

            longestSequence = Math.Max(longestSequence, j - i);
            i = j + 1;
        }

        return longestSequence;
    }
}
