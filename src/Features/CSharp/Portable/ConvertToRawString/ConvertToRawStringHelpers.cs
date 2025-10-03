// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.EmbeddedLanguages.VirtualChars;
using Microsoft.CodeAnalysis.ExtractInterface;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.ConvertToRawString;

internal static class ConvertToRawStringHelpers
{
    public static bool CanBeSingleLine(VirtualCharSequence characters)
    {
        // Single line raw strings cannot start/end with quote.
        if (characters[0] == '"' || characters[^1] == '"')
            return false;

        // a single line raw string cannot contain a newline.
        if (characters.Any(static ch => IsCSharpNewLine(ch)))
            return false;

        return true;
    }

    public static bool IsCSharpNewLine(VirtualChar ch)
        => SyntaxFacts.IsNewLine(ch);

    public static bool IsCSharpWhitespace(VirtualChar ch)
        => SyntaxFacts.IsWhitespace(ch);

    public static bool IsCarriageReturnNewLine(VirtualCharSequence characters, int index)
    {
        return index + 1 < characters.Length &&
            characters[index] == '\r' &&
            characters[index + 1] == '\n';
    }

    public static bool AllEscapesAreQuotes(VirtualCharSequence sequence)
        => AllEscapesAre(sequence, static ch => ch == '"');

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

    /// <summary>
    /// Returns if this sequence of characters can be converted to a raw string.  If it can, also returns if it
    /// contained an explicitly escaped newline (like <c>\r\n</c>) within it.  It it can't convert, then the value of
    /// <paramref name="containsEscapedEndOfLineCharacter"/> is undefined.
    /// </summary>
    public static bool CanConvert(VirtualCharSequence characters, out bool containsEscapedEndOfLineCharacter)
    {
        containsEscapedEndOfLineCharacter = false;
        if (characters.IsDefault)
            return false;

        for (var i = 0; i < characters.Length; i++)
        {
            var ch = characters[i];

            // Look for *explicit* usages of sequences like \r or \n.  These are multi character representations of
            // newlines.  If we see these, we only want to fix these up in a fix-all if the original string contained
            // those as well.
            //
            // Also, Check if we have an escaped character in the original string. An escaped newline is fine to convert
            // (to a multi-line raw string). Whereas Control/formatting unicode escapes should stay as escapes.  The
            // user code will just be enormously difficult to read/reason about if we convert those to the actual
            // corresponding non-escaped chars.
            if (ch.Span.Length > 1)
            {
                if (SyntaxFacts.IsNewLine(ch))
                {
                    containsEscapedEndOfLineCharacter = true;
                }
                else if (IsFormatOrControl(char.GetUnicodeCategory(ch)))
                {
                    return false;
                }
            }

            // Can't ever encode a null value directly in a c# file as our lexer/parser/text apis will stop right there.
            if (ch.Value == 0)
                return false;

            // A paired surrogate is fine to convert.
            if (i + 1 < characters.Length &&
                char.IsHighSurrogate(ch) &&
                char.IsLowSurrogate(characters[i + 1]) &&
                !IsFormatOrControl(Rune.GetUnicodeCategory(new Rune(ch, characters[i + 1]))))
            {
                // Increase by one more to account for the low surrogate we just looked at.
                i++;
            }
            else if (char.IsSurrogate(ch))
            {
                // Unpaired surrogates aren't things we want to convert from an escape to a random character.
                return false;
            }
        }

        return true;
    }

    private static bool IsFormatOrControl(UnicodeCategory category)
        => category is UnicodeCategory.Format or UnicodeCategory.Control;

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
