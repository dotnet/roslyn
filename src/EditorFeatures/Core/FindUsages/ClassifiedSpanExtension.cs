// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;

namespace Microsoft.CodeAnalysis.Editor.FindUsages
{
    internal static class ClassifiedSpanExtension
    {
        public static ClassifiedTextElement BuildClassifiedTextElementForClassifiedSpans(this List<ClassifiedSpan> classifiedSpans, ITextSnapshot snapshot, ITrackingSpan trackingSpan, CancellationToken cancellationToken)
        {
            Contract.Assert(classifiedSpans != null && classifiedSpans.Any());

            // Get column index of current span
            snapshot.GetLineAndColumn(trackingSpan.GetStartPoint(snapshot).Position, out _, out var closeBraceColumnIndex);

            // sort the spans by the Start to guarantee the order
            classifiedSpans.Sort((s1, s2) => s1.TextSpan.Start.CompareTo(s2.TextSpan.Start));

            // Convert spans to textruns
            var runs = new List<ClassifiedTextRun>();
            var lastSpanEnd = -1;
            foreach (var span in classifiedSpans)
            {
                // Add whitespace
                if (lastSpanEnd > 0 && span.TextSpan.Start > lastSpanEnd)
                {
                    // find the gap between last span and current span, read the gap text from document
                    var spanGap = new Span(lastSpanEnd, span.TextSpan.Start - lastSpanEnd);
                    var spanText = snapshot.GetText(spanGap);

                    if (spanText.ContainsLineBreak())
                    {
                        // only need to adjust the leading whitespace for the last line
                        // all other lines do not affect the indentation.
                        var lastLineText = spanText.GetLastLineText();

                        // if the close brace is deeper indented than this line, we use the same indentation for this line
                        var lengthOfWhitespaceToRemove = Math.Min(lastLineText.Length, closeBraceColumnIndex);
                        var removeStartIndex = spanText.Length - lengthOfWhitespaceToRemove;
                        if (removeStartIndex < spanText.Length)
                        {
                            spanText = spanText.Remove(removeStartIndex);
                        }
                    }

                    runs.Add(new ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, spanText));
                }

                lastSpanEnd = span.TextSpan.End;
                runs.Add(new ClassifiedTextRun(span.ClassificationType, snapshot.GetText(span.TextSpan.ToSpan())));
            }

            return new ClassifiedTextElement(runs);
        }
    }
}
