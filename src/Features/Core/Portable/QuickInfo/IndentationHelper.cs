// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.QuickInfo
{
    // Reproduces logic in IProjectionBufferFactoryServiceExtensions (editor layer) 
    // Used for tests currently, but probably needed for other non-vs-editor API consumers.
    internal static class IndentationHelper
    {
        /// <summary>
        /// Recomputes span segments so that all text lines appear to have the same reduction in indentation.
        /// This operation is typically used to align text for display when the initial span does not include all of the first line's identation.
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
            if (!classifiedSpans.IsDefault && classifiedSpans.Length > 0)
            {
                // We need to figure out the shortest indentation level of the exposed lines.  We'll
                // then remove that indentation from all lines.
                var indentationColumn = DetermineIndentationColumn(text, classifiedSpans, tabSize);

                string spanClassificationType = null;
                var adjustedClassifiedSpans = new List<ClassifiedSpan>();

                for (var i = 0; i < classifiedSpans.Length; i++)
                {
                    var classifiedSpan = classifiedSpans[i];
                    spanClassificationType = classifiedSpan.ClassificationType;
                    var span = classifiedSpan.TextSpan;

                    var startLineNumber = text.Lines.GetLineFromPosition(span.Start).LineNumber;
                    var endLineNumber = text.Lines.GetLineFromPosition(span.End).LineNumber;

                    for (var lineNumber = startLineNumber; lineNumber <= endLineNumber; lineNumber++)
                    {
                        var line = text.Lines[lineNumber];
                        var lineOffsetOfColumn = line.GetLineOffsetFromColumn(indentationColumn, tabSize);

                        var deletion = TextSpan.FromBounds(line.Start, line.Start + lineOffsetOfColumn);

                        if (deletion.Start > span.Start)
                        {
                            var spanBeforeDeletion = TextSpan.FromBounds(span.Start, Math.Min(span.End, deletion.Start));
                            if (spanBeforeDeletion.Length > 0)
                            {
                                adjustedClassifiedSpans.Add(new ClassifiedSpan(spanClassificationType, spanBeforeDeletion));
                            }
                        }

                        if (deletion.End > span.Start)
                        {
                            span = TextSpan.FromBounds(Math.Min(deletion.End, span.End), span.End);
                        }
                    }

                    if (span.Length > 0)
                    {
                        adjustedClassifiedSpans.Add(new ClassifiedSpan(spanClassificationType, span));
                    }
                }

                return adjustedClassifiedSpans.ToImmutableArray();
            }
            else
            {
                return ImmutableArray<ClassifiedSpan>.Empty;
            }
        }

        private static int DetermineIndentationColumn(
            SourceText text,
            ImmutableArray<ClassifiedSpan> spans,
            int tabSize)
        {
            int? indentationColumn = null;
            foreach (var span in spans)
            {
                var startLineNumber = text.Lines.GetLineFromPosition(span.TextSpan.Start).LineNumber;
                var endLineNumber = text.Lines.GetLineFromPosition(span.TextSpan.End).LineNumber;

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
                var startLineFirstNonWhitespace = text.Lines[startLineNumber].GetFirstNonWhitespacePosition();
                if (startLineFirstNonWhitespace.HasValue && startLineFirstNonWhitespace.Value < span.TextSpan.Start)
                {
                    startLineNumber++;
                }

                for (var lineNumber = startLineNumber; lineNumber <= endLineNumber; lineNumber++)
                {
                    var line = text.Lines[lineNumber];
                    if (line.IsEmptyOrWhitespace())
                    {
                        continue;
                    }

                    indentationColumn = indentationColumn.HasValue
                        ? Math.Min(indentationColumn.Value, line.GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(tabSize))
                        : line.GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(tabSize);
                }
            }

            return indentationColumn ?? 0;
        }
    }
}
