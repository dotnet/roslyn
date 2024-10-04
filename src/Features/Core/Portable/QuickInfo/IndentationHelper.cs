// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.QuickInfo;

// Reproduces logic in IProjectionBufferFactoryServiceExtensions (editor layer) 
// Used for tests currently, but probably needed for other non-vs-editor API consumers.
internal static class IndentationHelper
{
    /// <summary>
    /// Recomputes span segments so that all text lines appear to have the same reduction in indentation.
    /// This operation is typically used to align text for display when the initial span does not include all of the first line's indentation.
    /// This operation will potentially split spans that cover multiple lines into separate spans.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="classifiedSpans">The initial set of spans to align.</param>
    /// <param name="tabSize">The number of spaces to </param>
    /// <returns></returns>
    public static ImmutableArray<ClassifiedSpan> GetSpansWithAlignedIndentation(
        SourceText text,
        ImmutableArray<ClassifiedSpan> classifiedSpans,
        int tabSize)
    {
        if (classifiedSpans.IsDefaultOrEmpty)
        {
            return [];
        }

        // We need to figure out the shortest indentation level of the exposed lines.  We'll
        // then remove that indentation from all lines.
        var indentationColumn = DetermineIndentationColumn(text, classifiedSpans, tabSize);
        using var adjustedClassifiedSpans = TemporaryArray<ClassifiedSpan>.Empty;

        var lines = text.Lines;

        foreach (var classifiedSpan in classifiedSpans)
        {
            var spanClassificationType = classifiedSpan.ClassificationType;
            var span = classifiedSpan.TextSpan;

            var startLineNumber = lines.GetLineFromPosition(span.Start).LineNumber;
            var endLineNumber = lines.GetLineFromPosition(span.End).LineNumber;

            for (var lineNumber = startLineNumber; lineNumber <= endLineNumber; lineNumber++)
            {
                var line = lines[lineNumber];
                var lineOffsetOfColumn = line.GetLineOffsetFromColumn(indentationColumn, tabSize);

                var deletion = TextSpan.FromBounds(line.Start, line.Start + lineOffsetOfColumn);

                if (deletion.Start > span.Start)
                {
                    var spanBeforeDeletion = TextSpan.FromBounds(span.Start, Math.Min(span.End, deletion.Start));
                    if (spanBeforeDeletion.Length > 0)
                    {
                        adjustedClassifiedSpans.Add(new(spanClassificationType, spanBeforeDeletion));
                    }
                }

                if (deletion.End > span.Start)
                {
                    span = TextSpan.FromBounds(Math.Min(deletion.End, span.End), span.End);
                }
            }

            if (span.Length > 0)
            {
                adjustedClassifiedSpans.Add(new(spanClassificationType, span));
            }
        }

        return adjustedClassifiedSpans.ToImmutableAndClear();
    }

    private static int DetermineIndentationColumn(
        SourceText text,
        ImmutableArray<ClassifiedSpan> spans,
        int tabSize)
    {
        var lines = text.Lines;
        int? indentationColumn = null;

        foreach (var span in spans)
        {
            var startLineNumber = lines.GetLineFromPosition(span.TextSpan.Start).LineNumber;
            var endLineNumber = lines.GetLineFromPosition(span.TextSpan.End).LineNumber;

            // If the span starts after the first non-whitespace of the first line, we'll
            // exclude that line to avoid throwing off the calculation. Otherwise, the
            // incorrect indentation will be returned for lambda cases like so:
            //
            // void M()
            // {
            //     Func<int> f = () =>
            //         {
            //             return 1;
            //         };
            // }
            //
            // Without throwing out the first line in the example above, the indentation column
            // used will be 4, rather than 8.
            var startLineFirstNonWhitespace = lines[startLineNumber].GetFirstNonWhitespacePosition();
            if (startLineFirstNonWhitespace is int value && value < span.TextSpan.Start)
            {
                startLineNumber++;
            }

            for (var lineNumber = startLineNumber; lineNumber <= endLineNumber; lineNumber++)
            {
                var line = lines[lineNumber];
                if (line.IsEmptyOrWhitespace())
                {
                    continue;
                }

                indentationColumn = indentationColumn is int currentValue
                    ? Math.Min(currentValue, line.GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(tabSize))
                    : line.GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(tabSize);
            }
        }

        return indentationColumn ?? 0;
    }
}
