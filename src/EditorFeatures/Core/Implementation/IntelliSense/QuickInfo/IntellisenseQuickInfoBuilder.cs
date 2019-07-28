// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Formatting;
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
        internal static async Task<IntellisenseQuickInfoItem> BuildItemAsync(ITrackingSpan trackingSpan,
            CodeAnalysisQuickInfoItem quickInfoItem,
            ITextSnapshot snapshot,
            Document document,
            Lazy<IStreamingFindUsagesPresenter> streamingPresenter,
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
                foreach (var element in Helpers.BuildInteractiveTextElements(descSection.TaggedParts, document, streamingPresenter))
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
                foreach (var element in Helpers.BuildInteractiveTextElements(documentationCommentSection.TaggedParts, document, streamingPresenter))
                {
                    if (isFirstElement)
                    {
                        isFirstElement = false;

                        // Stack the first paragraph of the documentation comments with the last line of the description
                        // to avoid vertical padding between the two.
                        var lastElement = elements[elements.Count - 1];
                        elements[elements.Count - 1] = new ContainerElement(
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
                quickInfoItem.Sections.Where(s => s.Kind != QuickInfoSectionKinds.Description && s.Kind != QuickInfoSectionKinds.DocumentationComments)
                                      .SelectMany(s => Helpers.BuildInteractiveTextElements(s.TaggedParts, document, streamingPresenter)));

            // build text for RelatedSpan
            if (quickInfoItem.RelatedSpans.Any())
            {
                var classifiedSpanList = new List<ClassifiedSpan>();
                foreach (var span in quickInfoItem.RelatedSpans)
                {
                    var classifiedSpans = await ClassifierHelper.GetClassifiedSpansAsync(document, span, cancellationToken).ConfigureAwait(false);
                    classifiedSpanList.AddRange(classifiedSpans);
                }

                var tabSize = document.Project.Solution.Workspace.Options.GetOption(FormattingOptions.TabSize, document.Project.Language);
                var text = await document.GetTextAsync().ConfigureAwait(false);
                var spans = IndentationHelper.GetSpansWithAlignedIndentation(text, classifiedSpanList.ToImmutableArray(), tabSize);
                var textRuns = spans.Select(s => new ClassifiedTextRun(s.ClassificationType, snapshot.GetText(s.TextSpan.ToSpan()), ClassifiedTextRunStyle.UseClassificationFont));

                if (textRuns.Any())
                {
                    elements.Add(new ClassifiedTextElement(textRuns));
                }
            }

            var content = new ContainerElement(
                                ContainerElementStyle.Stacked | ContainerElementStyle.VerticalPadding,
                                elements);

            return new IntellisenseQuickInfoItem(trackingSpan, content);
        }
    }
}
