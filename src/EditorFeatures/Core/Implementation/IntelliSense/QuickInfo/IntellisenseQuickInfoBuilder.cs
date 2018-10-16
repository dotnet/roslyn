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
                                ContainerElementStyle.Stacked,
                                elements);

            return new IntellisenseQuickInfoItem(trackingSpan, content);
        }

        private static ClassifiedTextElement BuildClassifiedTextElement(QuickInfoSection section)
        {
            return new ClassifiedTextElement(section.TaggedParts.Select(
                    part => new ClassifiedTextRun(part.Tag.ToClassificationTypeName(), part.Text)));
        }
    }
}
