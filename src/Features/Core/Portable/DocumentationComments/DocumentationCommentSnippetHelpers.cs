// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.DocumentationComments;

internal static class DocumentationCommentSnippetHelpers
{
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
}
