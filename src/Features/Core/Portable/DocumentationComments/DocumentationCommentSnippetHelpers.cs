// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.DocumentationComments;

internal static class DocumentationCommentSnippetHelpers
{
    // Match each ampersand together with an optional complete named or numeric entity. This lets EscapePastedText
    // preserve valid entities such as &amp; and &#x41;, while escaping bare, malformed, or invalid entities.
    private static readonly Regex s_xmlEntityRegex = new(
        @"&(?<entity>(?:amp|apos|gt|lt|quot|#(?<decimal>[0-9]+)|#[xX](?<hex>[0-9A-Fa-f]+));)?",
        RegexOptions.CultureInvariant);

    public static bool WillBeAtEndOfDocCommentTriviaOnBlankLine(SourceText text, int currentPosition, char documentationCommentCharacter)
    {
        // We need to check if we currently have "//" and typing "/" will make "///"
        var commentStart = currentPosition - 2;
        if (commentStart < 0)
            return false;

        if (text[commentStart + 0] != documentationCommentCharacter ||
            text[commentStart + 1] != documentationCommentCharacter)
        {
            return false;
        }

        // Check that everything before those two characters on the line is whitespace
        var line = text.Lines.GetLineFromPosition(commentStart);
        for (var i = line.Start; i < commentStart; i++)
        {
            if (!char.IsWhiteSpace(text[i]))
                return false;
        }

        return true;
    }

    public static string EscapePastedText(string text)
        => s_xmlEntityRegex.Replace(text, static match =>
            IsValidXmlEntity(match) ? match.Value : "&amp;" + match.Groups["entity"].Value);

    private static bool IsValidXmlEntity(Match match)
    {
        var entity = match.Groups["entity"];
        if (!entity.Success)
            return false;

        var decimalDigits = match.Groups["decimal"];
        var hexadecimalDigits = match.Groups["hex"];
        if (!decimalDigits.Success && !hexadecimalDigits.Success)
            return true;

        var digits = (decimalDigits.Success ? decimalDigits.Value : hexadecimalDigits.Value).TrimStart('0');
        return digits.Length > 0 &&
            int.TryParse(
                digits,
                decimalDigits.Success ? NumberStyles.None : NumberStyles.AllowHexSpecifier,
                CultureInfo.InvariantCulture,
                out var value) &&
            IsValidXmlCharacter(value);
    }

    private static bool IsValidXmlCharacter(int value)
        => value is 0x9 or 0xA or 0xD or
            >= 0x20 and <= 0xD7FF or
            >= 0xE000 and <= 0xFFFD or
            >= 0x10000 and <= 0x10FFFF;
}
