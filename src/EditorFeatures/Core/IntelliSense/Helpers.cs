// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.QuickInfo.Presentation;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.Text.Adornments;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense;

internal static class Helpers
{
    internal static IReadOnlyCollection<object> BuildInteractiveTextElements(
        ImmutableArray<TaggedText> taggedTexts,
        QuickInfoContentBuilderContext? context)
    {
        var index = 0;
        return BuildInteractiveTextElements(taggedTexts, ref index, context);
    }

    private static IReadOnlyCollection<object> BuildInteractiveTextElements(
        ImmutableArray<TaggedText> taggedTexts,
        ref int index,
        QuickInfoContentBuilderContext? context)
    {
        // This method produces a sequence of zero or more paragraphs
        var paragraphs = new List<object>();

        // Each paragraph is constructed from one or more lines
        var currentParagraph = new List<object>();

        // Each line is constructed from one or more inline elements
        var currentRuns = new List<ClassifiedTextRun>();

        while (index < taggedTexts.Length)
        {
            var part = taggedTexts[index];

            // These tags can be ignored - they are for markdown formatting only.
            if (part.Tag is TextTags.CodeBlockStart or TextTags.CodeBlockEnd)
            {
                index++;
                continue;
            }

            if (part.Tag == TextTags.ContainerStart)
            {
                if (currentRuns.Count > 0)
                {
                    // This line break means the end of a line within a paragraph.
                    currentParagraph.Add(new ClassifiedTextElement(currentRuns));
                    currentRuns.Clear();
                }

                index++;
                var nestedElements = BuildInteractiveTextElements(taggedTexts, ref index, context);
                if (nestedElements.Count <= 1)
                {
                    currentParagraph.Add(new ContainerElement(
                        ContainerElementStyle.Wrapped,
                        new ClassifiedTextElement(new ClassifiedTextRun(ClassificationTypeNames.Text, part.Text)),
                        new ContainerElement(ContainerElementStyle.Stacked, nestedElements)));
                }
                else
                {
                    currentParagraph.Add(new ContainerElement(
                        ContainerElementStyle.Wrapped,
                        new ClassifiedTextElement(new ClassifiedTextRun(ClassificationTypeNames.Text, part.Text)),
                        new ContainerElement(
                            ContainerElementStyle.Stacked,
                            nestedElements.First(),
                            new ContainerElement(
                                ContainerElementStyle.Stacked | ContainerElementStyle.VerticalPadding,
                                nestedElements.Skip(1)))));
                }

                index++;
                continue;
            }
            else if (part.Tag == TextTags.ContainerEnd)
            {
                // Return the current result and let the caller continue
                break;
            }

            if (part.Tag is TextTags.ContainerStart
                or TextTags.ContainerEnd)
            {
                index++;
                continue;
            }

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
                var style = GetClassifiedTextRunStyle(part.Style);

                // If the tagged text has navigation target AND a NavigationActionFactory
                // is available, we'll create the classified run with a navigation action.
                if (part.NavigationTarget is not null &&
                    context?.NavigationActionFactory is { } factory)
                {
                    currentRuns.Add(new ClassifiedTextRun(
                        part.Tag.ToClassificationTypeName(),
                        part.Text,
                        factory.CreateNavigationAction(part.NavigationTarget),
                        tooltip: part.NavigationHint,
                        style));
                }
                else
                {
                    currentRuns.Add(new ClassifiedTextRun(part.Tag.ToClassificationTypeName(), part.Text, style));
                }
            }

            index++;
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

    private static ClassifiedTextRunStyle GetClassifiedTextRunStyle(TaggedTextStyle style)
    {
        var result = ClassifiedTextRunStyle.Plain;
        if ((style & TaggedTextStyle.Emphasis) == TaggedTextStyle.Emphasis)
        {
            result |= ClassifiedTextRunStyle.Italic;
        }

        if ((style & TaggedTextStyle.Strong) == TaggedTextStyle.Strong)
        {
            result |= ClassifiedTextRunStyle.Bold;
        }

        if ((style & TaggedTextStyle.Underline) == TaggedTextStyle.Underline)
        {
            result |= ClassifiedTextRunStyle.Underline;
        }

        if ((style & TaggedTextStyle.Code) == TaggedTextStyle.Code)
        {
            result |= ClassifiedTextRunStyle.UseClassificationFont;
        }

        return result;
    }

    internal static object CreateParagraphFromLines(IReadOnlyList<object> lines)
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
