// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Shared.Utilities
{
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

            var lastNonNoisyCharPosition = previousLine.GetLastNonWhitespacePosition().Value;
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
}
