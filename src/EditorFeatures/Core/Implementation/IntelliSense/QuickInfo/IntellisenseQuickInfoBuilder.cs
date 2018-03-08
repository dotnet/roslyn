using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.QuickInfo;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;

using CodeAnalysisQuickInfoItem = Microsoft.CodeAnalysis.QuickInfo.QuickInfoItem;
using IntellisenseQuickInfoItem = Microsoft.VisualStudio.Language.Intellisense.QuickInfoItem;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo
{
    internal static class IntellisenseQuickInfoBuilder
    {
        internal static IntellisenseQuickInfoItem BuildItem(SnapshotPoint triggerPoint, CodeAnalysisQuickInfoItem quickInfoItem)
        {
            var line = triggerPoint.GetContainingLine();            
            var lineSpan = triggerPoint.Snapshot.CreateTrackingSpan(
                line.Extent,
                SpanTrackingMode.EdgeInclusive);

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

            var descSection = quickInfoItem.Sections.Where(s => s.Kind == QuickInfoSectionKinds.Description);
            if (descSection.Count() > 0)
            {
                firstLineElements.Add(BuildClassifiedTextElement(descSection.First()));
            }

            var elements = new List<object>
            {
                new ContainerElement(ContainerElementStyle.Wrapped, firstLineElements)
            };

            // Add the remaining sections as Stacked style
            elements.AddRange(
                quickInfoItem.Sections.Where(s => s.Kind != QuickInfoSectionKinds.Description)
                                      .Select(section => BuildClassifiedTextElement(section)));

            var content = new ContainerElement(
                                ContainerElementStyle.Stacked,
                                elements);

            return new IntellisenseQuickInfoItem(lineSpan, content);
        }

        private static ClassifiedTextElement BuildClassifiedTextElement(QuickInfoSection section)
        {
            return new ClassifiedTextElement(section.TaggedParts.Select(
                    part => new ClassifiedTextRun(part.Tag.ToClassificationTypeName(), part.Text)));
        }

    }
}
