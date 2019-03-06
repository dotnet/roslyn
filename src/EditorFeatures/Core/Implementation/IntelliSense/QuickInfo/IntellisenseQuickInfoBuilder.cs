// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Roslyn.Utilities;

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
                foreach (var element in BuildClassifiedTextElements(descSection))
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
                foreach (var element in BuildClassifiedTextElements(documentationCommentSection))
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
                                      .SelectMany(BuildClassifiedTextElements));

            // build text for RelatedSpan
            if (quickInfoItem.RelatedSpans.Any())
            {
                var classifiedSpanList = new List<ClassifiedSpan>();
                foreach (var span in quickInfoItem.RelatedSpans)
                {
                    var classifiedSpans = await EditorClassifier.GetClassifiedSpansAsync(document, span, cancellationToken).ConfigureAwait(false);
                    classifiedSpanList.AddRange(classifiedSpans);
                }

                var tabSize = document.Project.Solution.Workspace.Options.GetOption(FormattingOptions.TabSize, document.Project.Language);
                var text = await document.GetTextAsync().ConfigureAwait(false);
                var spans = IndentationHelper.GetSpansWithAlignedIndentation(text, classifiedSpanList.ToImmutableArray(), tabSize);
                var textRuns = spans.Select(s => new ClassifiedTextRun(s.ClassificationType, snapshot.GetText(s.TextSpan.ToSpan())));

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

        private static IEnumerable<object> BuildClassifiedTextElements(QuickInfoSection section)
        {
            // This method produces a sequence of zero or more paragraphs
            var paragraphs = new List<object>();

            // Each paragraph is constructed from one or more lines
            var currentParagraph = new List<ClassifiedTextElement>();

            // Each line is constructed from one or more inline elements
            var currentRuns = new List<ClassifiedTextRun>();

            foreach (var part in section.TaggedParts)
            {
                if (part.Tag == TextTags.LineBreak)
                {
                    if (currentRuns.Count > 0)
                    {
                        // This line break means the end of a line within a paragraph.
                        currentParagraph.Add(new ClassifiedTextElement(currentRuns));
                        currentRuns.Clear();
                    }
                    else
                    {
                        // This line break means the end of a paragraph. Empty paragraphs are ignored, but could appear
                        // in the input to this method:
                        //
                        // * Empty <para> elements
                        // * Explicit line breaks at the start of a comment
                        // * Multiple line breaks between paragraphs
                        if (currentParagraph.Count > 0)
                        {
                            // The current paragraph is not empty, so add it to the result collection
                            paragraphs.Add(CreateParagraphFromLines(currentParagraph));
                            currentParagraph.Clear();
                        }
                        else
                        {
                            // The current paragraph is empty, so we simply ignore it.
                        }
                    }
                }
                else
                {
                    // This is tagged text getting added to the current line we are building.
                    currentRuns.Add(new ClassifiedTextRun(part.Tag.ToClassificationTypeName(), part.Text));
                }
            }

            if (currentRuns.Count > 0)
            {
                // Add the final line to the final paragraph.
                currentParagraph.Add(new ClassifiedTextElement(currentRuns));
            }

            if (currentParagraph.Count > 0)
            {
                // Add the final paragraph to the result.
                paragraphs.Add(CreateParagraphFromLines(currentParagraph));
            }

            return paragraphs;
        }

        private static object CreateParagraphFromLines(IReadOnlyList<ClassifiedTextElement> lines)
        {
            Contract.ThrowIfFalse(lines.Count > 0);

            if (lines.Count == 1)
            {
                // The paragraph contains only one line, so it doesn't need to be added to a container. Avoiding the
                // wrapping container here also avoids a wrapping element in the WPF elements used for rendering,
                // improving efficiency.
                return lines[0];
            }
            else
            {
                // The lines of a multi-line paragraph are stacked to produce the full paragraph.
                return new ContainerElement(ContainerElementStyle.Stacked, lines);
            }
        }
    }
}
