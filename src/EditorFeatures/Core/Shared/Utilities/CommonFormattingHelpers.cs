// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities;

internal static class CommonFormattingHelpers
{
    public static TextSpan GetFormattingSpan(SyntaxNode root, TextSpan span)
        => CodeAnalysis.Shared.Utilities.CommonFormattingHelpers.GetFormattingSpan(root, span);

    public static TextSpan GetFormattingSpan(ITextSnapshot snapshot, SnapshotSpan selectedSpan)
    {
        var currentLine = snapshot.GetLineFromPosition(selectedSpan.Start);
        var endPosition = selectedSpan.IsEmpty ? currentLine.End : selectedSpan.End;
        var previousLine = GetNonEmptyPreviousLine(snapshot, currentLine);

        // first line on screen
        if (currentLine == previousLine)
        {
            return TextSpan.FromBounds(currentLine.Start, endPosition);
        }

        var lastNonNoisyCharPosition = previousLine.GetLastNonWhitespacePosition().GetValueOrDefault();
        return TextSpan.FromBounds(lastNonNoisyCharPosition, endPosition);
    }

    public static ITextSnapshotLine GetNonEmptyPreviousLine(ITextSnapshot snapshot, ITextSnapshotLine currentLine)
    {
        do
        {
            var previousLine = snapshot.GetLineFromLineNumber(Math.Max(currentLine.LineNumber - 1, 0));

            // first line in the file
            if (previousLine.LineNumber == currentLine.LineNumber)
            {
                return currentLine;
            }

            if (previousLine.IsEmptyOrWhitespace())
            {
                // keep goes up until it find non empty previous line
                currentLine = previousLine;
                continue;
            }

            return previousLine;
        }
        while (true);
    }
}
