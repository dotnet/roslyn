// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Shared.Collections;

namespace Microsoft.CodeAnalysis.QuickInfo.Presentation;

internal static class QuickInfoContentBuilder
{
    public static async Task<QuickInfoContainerElement> BuildInteractiveContentAsync(
        QuickInfoItem quickInfoItem,
        QuickInfoContentBuilderContext? context,
        CancellationToken cancellationToken)
    {
        // Build the first line of QuickInfo item, the images and the Description section should be on the first line with Wrapped style
        var glyphs = quickInfoItem.Tags.GetGlyphs();
        var symbolGlyph = glyphs.FirstOrDefault(g => g != Glyph.CompletionWarning);
        var warningGlyph = glyphs.FirstOrDefault(g => g == Glyph.CompletionWarning);
        using var firstLineElements = TemporaryArray<QuickInfoElement>.Empty;
        if (symbolGlyph != Glyph.None)
        {
            firstLineElements.Add(new QuickInfoGlyphElement(symbolGlyph));
        }

        if (warningGlyph != Glyph.None)
        {
            firstLineElements.Add(new QuickInfoGlyphElement(warningGlyph));
        }

        var elements = new List<QuickInfoElement>();
        var descSection = quickInfoItem.Sections.FirstOrDefault(s => s.Kind == QuickInfoSectionKinds.Description);
        if (descSection != null)
        {
            var isFirstElement = true;
            foreach (var element in descSection.TaggedParts.ToInteractiveTextElements(context?.NavigationActionFactory))
            {
                if (isFirstElement)
                {
                    isFirstElement = false;
                    firstLineElements.Add(element);
                }
                else
                {
                    // If the description section contains multiple paragraphs, the second and additional paragraphs
                    // are not wrapped in firstLineElements (they are normal paragraphs).
                    elements.Add(element);
                }
            }
        }

        elements.Insert(0, new QuickInfoContainerElement(QuickInfoContainerStyle.Wrapped, firstLineElements.ToImmutableAndClear()));

        var documentationCommentSection = quickInfoItem.Sections.FirstOrDefault(s => s.Kind == QuickInfoSectionKinds.DocumentationComments);
        if (documentationCommentSection != null)
        {
            var isFirstElement = true;
            foreach (var element in documentationCommentSection.TaggedParts.ToInteractiveTextElements(context?.NavigationActionFactory))
            {
                if (isFirstElement)
                {
                    isFirstElement = false;

                    // Stack the first paragraph of the documentation comments with the last line of the description
                    // to avoid vertical padding between the two.
                    var lastElement = elements[^1];
                    elements[^1] = new QuickInfoContainerElement(
                        QuickInfoContainerStyle.Stacked,
                        lastElement,
                        element);
                }
                else
                {
                    elements.Add(element);
                }
            }
        }

        // Add the remaining sections as Stacked style
        elements.AddRange(
            quickInfoItem.Sections.Where(s => s.Kind is not QuickInfoSectionKinds.Description and not QuickInfoSectionKinds.DocumentationComments)
                                  .SelectMany(s => s.TaggedParts.ToInteractiveTextElements(context?.NavigationActionFactory)));

        // build text for RelatedSpan
        if (quickInfoItem.RelatedSpans.Any() && context != null)
        {
            var document = context.Document;
            using var textRuns = TemporaryArray<QuickInfoClassifiedTextRun>.Empty;
            var spanSeparatorNeededBefore = false;
            foreach (var span in quickInfoItem.RelatedSpans)
            {
                // We don't present additive-spans (like static/reassigned-variable) any differently, so strip them
                // out of the classifications we get back.
                var classifiedSpans = await ClassifierHelper.GetClassifiedSpansAsync(
                    document, span, context.ClassificationOptions, includeAdditiveSpans: false, cancellationToken).ConfigureAwait(false);

                var tabSize = context.LineFormattingOptions.TabSize;

                var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

                var spans = IndentationHelper.GetSpansWithAlignedIndentation(text, classifiedSpans, tabSize);

                var textRunsOfSpan = spans.SelectAsArray(
                    static (span, text) => new QuickInfoClassifiedTextRun(span.ClassificationType, text.ToString(span.TextSpan), QuickInfoClassifiedTextStyle.UseClassificationFont),
                    arg: text);

                if (textRunsOfSpan.Length > 0)
                {
                    if (spanSeparatorNeededBefore)
                    {
                        textRuns.Add(new QuickInfoClassifiedTextRun(ClassificationTypeNames.WhiteSpace, "\r\n", QuickInfoClassifiedTextStyle.UseClassificationFont));
                    }

                    textRuns.AddRange(textRunsOfSpan);
                    spanSeparatorNeededBefore = true;
                }
            }

            if (textRuns.Count > 0)
            {
                elements.Add(new QuickInfoClassifiedTextElement(textRuns.ToImmutableAndClear()));
            }
        }

        if (context is not null && quickInfoItem.OnTheFlyDocsInfo is not null)
            elements.Add(new QuickInfoOnTheFlyDocsElement(context.Document, quickInfoItem.OnTheFlyDocsInfo));

        return new QuickInfoContainerElement(
            QuickInfoContainerStyle.Stacked | QuickInfoContainerStyle.VerticalPadding,
            [.. elements]);
    }
}
