// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.LanguageServices;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis;
internal class SnippetUtilities
{
    public static bool TryGetWordOnLeft(int position, SourceText currentText, ISyntaxFactsService syntaxFactsService, [NotNullWhen(true)] out TextSpan? wordSpan)
    {
        var endPosition = position;
        var startPosition = endPosition;

        // Find the snippet shortcut
        while (startPosition > 0)
        {
            var c = currentText[startPosition - 1];
            if (!syntaxFactsService.IsIdentifierPartCharacter(c) && c != '#' && c != '~')
            {
                break;
            }

            startPosition--;
        }

        if (startPosition == endPosition)
        {
            wordSpan = null;
            return false;
        }

        wordSpan = TextSpan.FromBounds(startPosition, endPosition);
        return true;
    }
}
