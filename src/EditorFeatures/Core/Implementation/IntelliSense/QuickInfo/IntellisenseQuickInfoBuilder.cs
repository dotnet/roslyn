// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;

using CodeAnalysisQuickInfoItem = Microsoft.CodeAnalysis.QuickInfo.QuickInfoItem;
using IntellisenseQuickInfoItem = Microsoft.VisualStudio.Language.Intellisense.QuickInfoItem;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
{
    internal static class IntellisenseQuickInfoBuilder
    {
        internal static async Task<IntellisenseQuickInfoItem> BuildItem(ITrackingSpan trackingSpan,
            CodeAnalysisQuickInfoItem quickInfoItem,
            ITextSnapshot snapshot,
            Document document,
            CancellationToken cancellationToken)
        {
            // Build the first line of QuickInfo item, the images and the Description section should be on the first line with Wrapped style
            var glyphs = quickInfoItem.Tags.GetGlyphs();
            var symbolGlyph = glyphs.FirstOrDefault(g => g != Glyph.CompletionWarning);
            var warningGlyph = glyphs.FirstOrDefault(g => g == Glyph.CompletionWarning);
            var firstLineElements = new List<object>();
            if (symbolGlyph != Glyph.None)
            {
                firstLineElements.Add(new ImageElement(symbolGlyph.GetImageId()));
            }

            if (warningGlyph != Glyph.None)
            {
                firstLineElements.Add(new ImageElement(warningGlyph.GetImageId()));
            }

            var descSection = quickInfoItem.Sections.FirstOrDefault(s => s.Kind == QuickInfoSectionKinds.Description);
            if (descSection != null)
            {
                firstLineElements.Add(BuildClassifiedTextElement(descSection));
            }

            var elements = new List<object>
            {
                new ContainerElement(ContainerElementStyle.Wrapped, firstLineElements)
            };

            // Add the remaining sections as Stacked style
            elements.AddRange(
                quickInfoItem.Sections.Where(s => s.Kind != QuickInfoSectionKinds.Description)
                                      .Select(BuildClassifiedTextElement));

            // build text for RelatedSpan
            if (quickInfoItem.RelatedSpans.Any())
            {
                elements.Add(await BuildTextElementForRelatedSpans(quickInfoItem, snapshot, document, cancellationToken).ConfigureAwait(false));
            }

            var content = new ContainerElement(
                                ContainerElementStyle.Stacked,
                                elements);

            return new IntellisenseQuickInfoItem(trackingSpan, content);
        }

        private static async Task<ClassifiedTextElement> BuildTextElementForRelatedSpans(CodeAnalysisQuickInfoItem quickInfoItem, ITextSnapshot snapshot, Document document, CancellationToken cancellationToken)
        {
            var classificationService = document.Project.LanguageServices.GetService<IClassificationService>();

            var classifiedSpans = new List<ClassifiedSpan>();
            var semanticSpans = new List<ClassifiedSpan>();

            foreach (var span in quickInfoItem.RelatedSpans)
            {
                await classificationService.AddSyntacticClassificationsAsync(document, span, classifiedSpans, cancellationToken).ConfigureAwait(false);
                await classificationService.AddSemanticClassificationsAsync(document, span, semanticSpans, cancellationToken).ConfigureAwait(false);
            }

            // replace the spans from SyntacticClassifications with spans from SemanticClassifications
            classifiedSpans.Sort((a, b) => a.TextSpan.Start.CompareTo(b.TextSpan.Start));
            semanticSpans.Sort((a, b) => a.TextSpan.Start.CompareTo(b.TextSpan.Start));

            var i = 0;
            foreach (var sSpan in semanticSpans)
            {
                for (; i < classifiedSpans.Count; i++)
                {
                    if (classifiedSpans[i].TextSpan.Start == sSpan.TextSpan.Start)
                    {
                        classifiedSpans.RemoveAt(i);
                        classifiedSpans.Insert(i, sSpan);
                        i++;
                        break;
                    }
                }
            }

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
                    var spanText = spanGap.GetSpanText(snapshot);

                    // special handling for new line, remove all the whitespace at the begining of the new line.
                    if (spanText.StartsWith("\r\n"))
                    {
                        spanText = "\r\n";
                    }

                    runs.Add(new ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, spanText));
                }

                lastSpanEnd = span.TextSpan.End;
                runs.Add(new ClassifiedTextRun(span.ClassificationType, span.TextSpan.ToSpan().GetSpanText(snapshot)));
            }

            return new ClassifiedTextElement(runs);
        }

        private static ClassifiedTextElement BuildClassifiedTextElement(QuickInfoSection section)
        {
            return new ClassifiedTextElement(section.TaggedParts.Select(
                    part => new ClassifiedTextRun(part.Tag.ToClassificationTypeName(), part.Text)));
        }

        private static string GetSpanText(this Span span, ITextSnapshot snapshot)
        {
            var tSpan = snapshot.Version.CreateTrackingSpan(span, SpanTrackingMode.EdgeExclusive);
            return tSpan.GetText(snapshot);
        }

    }
}
