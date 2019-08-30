// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.GoToDefinition;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.VisualStudio.Text.Adornments;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense
{
    internal static class Helpers
    {
        internal static IReadOnlyCollection<object> BuildInteractiveTextElements(ImmutableArray<TaggedText> taggedTexts, Document document, Lazy<IStreamingFindUsagesPresenter> streamingPresenter)
        {
            var index = 0;
            return BuildInteractiveTextElements(taggedTexts, ref index, document, streamingPresenter);
        }

        private static IReadOnlyCollection<object> BuildInteractiveTextElements(ImmutableArray<TaggedText> taggedTexts, ref int index, Document document, Lazy<IStreamingFindUsagesPresenter> streamingPresenter)
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
                if (part.Tag == TextTags.ContainerStart)
                {
                    if (currentRuns.Count > 0)
                    {
                        // This line break means the end of a line within a paragraph.
                        currentParagraph.Add(new ClassifiedTextElement(currentRuns));
                        currentRuns.Clear();
                    }

                    index++;
                    var nestedElements = BuildInteractiveTextElements(taggedTexts, ref index, document, streamingPresenter);
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

                if (part.Tag == TextTags.ContainerStart
                    || part.Tag == TextTags.ContainerEnd)
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
                    if (part.NavigationTarget is object)
                    {
                        var target = part.NavigationTarget;
                        var tooltip = part.NavigationHint;
                        currentRuns.Add(new ClassifiedTextRun(part.Tag.ToClassificationTypeName(), part.Text, () => NavigateToQuickInfoTarget(target, document, streamingPresenter.Value), tooltip, style));
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

        private static void NavigateToQuickInfoTarget(string navigationTarget, Document document, IStreamingFindUsagesPresenter streamingPresenter)
        {
            var navigateToLinkService = document.Project.Solution.Workspace.Services.GetRequiredService<INavigateToLinkService>();
            if (Uri.TryCreate(navigationTarget, UriKind.Absolute, out var absoluteUri))
            {
                navigateToLinkService.TryNavigateToLinkAsync(absoluteUri, CancellationToken.None);
                return;
            }

            SymbolKeyResolution resolvedSymbolKey;
            try
            {
                resolvedSymbolKey = SymbolKey.ResolveString(navigationTarget, document.Project.GetCompilationAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None), cancellationToken: CancellationToken.None);
            }
            catch
            {
                // Ignore symbol resolution failures. It likely is just a badly formed URI.
                return;
            }

            if (resolvedSymbolKey.GetAnySymbol() is { } symbol)
            {
                GoToDefinitionHelpers.TryGoToDefinition(symbol, document.Project, streamingPresenter, CancellationToken.None);
                return;
            }
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
}
