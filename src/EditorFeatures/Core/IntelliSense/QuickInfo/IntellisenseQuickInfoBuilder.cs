// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.QuickInfo.Presentation;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using CodeAnalysisQuickInfoItem = Microsoft.CodeAnalysis.QuickInfo.QuickInfoItem;
using IntellisenseQuickInfoItem = Microsoft.VisualStudio.Language.Intellisense.QuickInfoItem;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;

internal static class IntellisenseQuickInfoBuilder
{
    private static async Task<ContainerElement> BuildInteractiveContentAsync(
        CodeAnalysisQuickInfoItem quickInfoItem,
        QuickInfoContentBuilderContext? context,
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

        var elements = new List<object>();
        var descSection = quickInfoItem.Sections.FirstOrDefault(s => s.Kind == QuickInfoSectionKinds.Description);
        if (descSection != null)
        {
            var isFirstElement = true;
            foreach (var element in Helpers.BuildInteractiveTextElements(descSection.TaggedParts, context))
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

        elements.Insert(0, new ContainerElement(ContainerElementStyle.Wrapped, firstLineElements));

        var documentationCommentSection = quickInfoItem.Sections.FirstOrDefault(s => s.Kind == QuickInfoSectionKinds.DocumentationComments);
        if (documentationCommentSection != null)
        {
            var isFirstElement = true;
            foreach (var element in Helpers.BuildInteractiveTextElements(documentationCommentSection.TaggedParts, context))
            {
                if (isFirstElement)
                {
                    isFirstElement = false;

                    // Stack the first paragraph of the documentation comments with the last line of the description
                    // to avoid vertical padding between the two.
                    var lastElement = elements[^1];
                    elements[^1] = new ContainerElement(
                        ContainerElementStyle.Stacked,
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
                                  .SelectMany(s => Helpers.BuildInteractiveTextElements(s.TaggedParts, context)));

        // build text for RelatedSpan
        if (quickInfoItem.RelatedSpans.Any() && context != null)
        {
            var document = context.Document;
            var textRuns = new List<ClassifiedTextRun>();
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
                var textRunsOfSpan = spans.Select(s => new ClassifiedTextRun(s.ClassificationType, text.GetSubText(s.TextSpan).ToString(), ClassifiedTextRunStyle.UseClassificationFont)).ToList();
                if (textRunsOfSpan.Count > 0)
                {
                    if (spanSeparatorNeededBefore)
                    {
                        textRuns.Add(new ClassifiedTextRun(ClassificationTypeNames.WhiteSpace, "\r\n", ClassifiedTextRunStyle.UseClassificationFont));
                    }

                    textRuns.AddRange(textRunsOfSpan);
                    spanSeparatorNeededBefore = true;
                }
            }

            if (textRuns.Any())
            {
                elements.Add(new ClassifiedTextElement(textRuns));
            }
        }

        if (context is not null && quickInfoItem.OnTheFlyDocsInfo is not null)
            elements.Add(new QuickInfoOnTheFlyDocsElement(context.Document, quickInfoItem.OnTheFlyDocsInfo));

        return new ContainerElement(
                            ContainerElementStyle.Stacked | ContainerElementStyle.VerticalPadding,
                            elements);
    }

    internal static async Task<IntellisenseQuickInfoItem> BuildItemAsync(
        ITrackingSpan trackingSpan,
        CodeAnalysisQuickInfoItem quickInfoItem,
        Document document,
        ClassificationOptions classificationOptions,
        LineFormattingOptions lineFormattingOptions,
        INavigationActionFactory navigationActionFactory,
        CancellationToken cancellationToken)
    {
        var context = new QuickInfoContentBuilderContext(document, classificationOptions, lineFormattingOptions, navigationActionFactory);
        var content = await BuildInteractiveContentAsync(quickInfoItem, context, cancellationToken).ConfigureAwait(false);
        return new IntellisenseQuickInfoItem(trackingSpan, content);
    }

    /// <summary>
    /// Builds the classified hover content without navigation actions and requiring
    /// an instance of <see cref="IStreamingFindUsagesPresenter"/>
    /// TODO - This can be removed once LSP supports colorization in markupcontent
    /// https://devdiv.visualstudio.com/DevDiv/_workitems/edit/918138
    /// </summary>
    internal static Task<ContainerElement> BuildContentWithoutNavigationActionsAsync(
        CodeAnalysisQuickInfoItem quickInfoItem,
        QuickInfoContentBuilderContext? context,
        CancellationToken cancellationToken)
    {
        return BuildInteractiveContentAsync(quickInfoItem, context, cancellationToken);
    }
}
