// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Shared.Collections;

namespace Microsoft.CodeAnalysis.QuickInfo.Presentation;

internal static class QuickInfoContentBuilder
{
    private static readonly FrozenDictionary<Glyph, QuickInfoGlyphElement> s_glyphToElementMap = CreateGlyphToElementMap().ToFrozenDictionary();

    private static Dictionary<Glyph, QuickInfoGlyphElement> CreateGlyphToElementMap()
    {
        var glyphs = (Glyph[])Enum.GetValues(typeof(Glyph));
        var result = new Dictionary<Glyph, QuickInfoGlyphElement>(capacity: glyphs.Length);

        foreach (var glyph in glyphs)
        {
            result.Add(glyph, new QuickInfoGlyphElement(glyph));
        }

        return result;
    }

    public static async Task<QuickInfoContainerElement> BuildInteractiveContentAsync(
        QuickInfoItem quickInfoItem,
        QuickInfoContentBuilderContext? context,
        CancellationToken cancellationToken)
    {
        var (symbolGlyph, addWarningGlyph) = ComputeGlyphs(quickInfoItem);

        using var remainingSections = TemporaryArray<QuickInfoSection>.Empty;
        var (descriptionSection, documentationCommentsSection) = ComputeSections(quickInfoItem, ref remainingSections.AsRef());

        // We can't declare elements in a using statement because we need to assign to its indexer below.
        // TemporaryArray<T>'s non-copyable semantics restrict this.
        var elements = TemporaryArray<QuickInfoElement>.Empty;
        try
        {
            // Add a dummy item that we'll replace with the first line below.
            elements.Add(null!);

            // Build the first line of QuickInfo item, the images and the Description section should be on the first line with Wrapped style
            using var firstLineElements = TemporaryArray<QuickInfoElement>.Empty;

            if (symbolGlyph != Glyph.None)
            {
                firstLineElements.Add(s_glyphToElementMap[symbolGlyph]);
            }

            if (addWarningGlyph)
            {
                firstLineElements.Add(s_glyphToElementMap[Glyph.CompletionWarning]);
            }

            var navigationActionFactory = context?.NavigationActionFactory;

            if (descriptionSection is not null)
            {
                var isFirstElement = true;

                foreach (var element in descriptionSection.TaggedParts.ToInteractiveTextElements(navigationActionFactory))
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

            // Replace the dummy first element with the real first line elements.
            elements[0] = new QuickInfoContainerElement(QuickInfoContainerStyle.Wrapped, firstLineElements.ToImmutableAndClear());

            if (documentationCommentsSection is not null)
            {
                var isFirstElement = true;

                foreach (var element in documentationCommentsSection.TaggedParts.ToInteractiveTextElements(navigationActionFactory))
                {
                    if (isFirstElement)
                    {
                        isFirstElement = false;

                        // Stack the first paragraph of the documentation comments with the last line of the description
                        // to avoid vertical padding between the two.
                        var lastIndex = elements.Count - 1;
                        var lastElement = elements[lastIndex];

                        elements[lastIndex] = new QuickInfoContainerElement(
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

            // Add the remaining sections
            if (remainingSections.Count > 0)
            {
                foreach (var section in remainingSections)
                {
                    foreach (var element in section.TaggedParts.ToInteractiveTextElements(navigationActionFactory))
                    {
                        elements.Add(element);
                    }
                }
            }

            if (context is not null)
            {
                // build text for RelatedSpan
                if (quickInfoItem.RelatedSpans.Length > 0)
                {
                    using var textRuns = TemporaryArray<QuickInfoClassifiedTextRun>.Empty;
                    var document = context.Document;
                    var tabSize = context.LineFormattingOptions.TabSize;
                    var spanSeparatorNeededBefore = false;

                    var text = await document.GetValueTextAsync(cancellationToken).ConfigureAwait(false);

                    foreach (var relatedSpan in quickInfoItem.RelatedSpans)
                    {
                        // We don't present additive-spans (like static/reassigned-variable) any differently, so strip them
                        // out of the classifications we get back.
                        var classifiedSpans = await ClassifierHelper.GetClassifiedSpansAsync(
                            document, relatedSpan, context.ClassificationOptions, includeAdditiveSpans: false, cancellationToken).ConfigureAwait(false);

                        foreach (var span in IndentationHelper.GetSpansWithAlignedIndentation(text, classifiedSpans, tabSize))
                        {
                            if (spanSeparatorNeededBefore)
                            {
                                textRuns.Add(new QuickInfoClassifiedTextRun(
                                    ClassificationTypeNames.WhiteSpace,
                                    "\r\n",
                                    QuickInfoClassifiedTextStyle.UseClassificationFont));

                                spanSeparatorNeededBefore = false;
                            }

                            textRuns.Add(new QuickInfoClassifiedTextRun(
                                span.ClassificationType,
                                text.ToString(span.TextSpan),
                                QuickInfoClassifiedTextStyle.UseClassificationFont));
                        }

                        spanSeparatorNeededBefore = true;
                    }

                    if (textRuns.Count > 0)
                    {
                        elements.Add(new QuickInfoClassifiedTextElement(textRuns.ToImmutableAndClear()));
                    }
                }

                // Add on-the-fly documentation
                if (quickInfoItem.OnTheFlyDocsInfo is not null)
                {
                    elements.Add(new QuickInfoOnTheFlyDocsElement(context.Document, quickInfoItem.OnTheFlyDocsInfo));
                }
            }

            return new QuickInfoContainerElement(
                QuickInfoContainerStyle.Stacked | QuickInfoContainerStyle.VerticalPadding,
                elements.ToImmutableAndClear());
        }
        finally
        {
            elements.Dispose();
        }
    }

    private static (Glyph symbolGlyph, bool addWarningGlyph) ComputeGlyphs(QuickInfoItem quickInfoItem)
    {
        var symbolGlyph = Glyph.None;
        var addWarningGlyph = false;

        foreach (var glyph in quickInfoItem.Tags.GetGlyphs())
        {
            if (symbolGlyph != Glyph.None && addWarningGlyph)
            {
                break;
            }

            if (symbolGlyph == Glyph.None && glyph != Glyph.CompletionWarning)
            {
                symbolGlyph = glyph;
            }
            else if (!addWarningGlyph && glyph == Glyph.CompletionWarning)
            {
                addWarningGlyph = true;
            }
        }

        return (symbolGlyph, addWarningGlyph);
    }

    private static (QuickInfoSection? descriptionSection, QuickInfoSection? documentationCommentsSection) ComputeSections(
        QuickInfoItem quickInfoItem,
        ref TemporaryArray<QuickInfoSection> remainingSections)
    {
        QuickInfoSection? descriptionSection = null;
        QuickInfoSection? documentationCommentsSection = null;

        foreach (var section in quickInfoItem.Sections)
        {
            switch (section.Kind)
            {
                case QuickInfoSectionKinds.Description:
                    descriptionSection ??= section;
                    break;

                case QuickInfoSectionKinds.DocumentationComments:
                    documentationCommentsSection ??= section;
                    break;

                default:
                    remainingSections.Add(section);
                    break;
            }
        }

        return (descriptionSection, documentationCommentsSection);
    }
}
