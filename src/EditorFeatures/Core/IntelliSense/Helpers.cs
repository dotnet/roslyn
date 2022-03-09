// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.QuickInfo;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.GoToDefinition;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense
{
    internal static class Helpers
    {
        internal static IReadOnlyCollection<object> BuildInteractiveTextElements(
            ImmutableArray<TaggedText> taggedTexts,
            IntellisenseQuickInfoBuilderContext? context)
        {
            var index = 0;
            return BuildInteractiveTextElements(taggedTexts, ref index, context);
        }

        private static IReadOnlyCollection<object> BuildInteractiveTextElements(
            ImmutableArray<TaggedText> taggedTexts,
            ref int index,
            IntellisenseQuickInfoBuilderContext? context)
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
                    if (part.NavigationTarget is not null &&
                        context?.ThreadingContext is { } threadingContext &&
                        context?.OperationExecutor is { } operationExecutor &&
                        context?.AsynchronousOperationListener is { } asyncListener &&
                        context?.StreamingPresenter is { } streamingPresenter)
                    {
                        var document = context.Document;
                        if (Uri.TryCreate(part.NavigationTarget, UriKind.Absolute, out var absoluteUri))
                        {
                            var target = new QuickInfoHyperLink(document.Project.Solution.Workspace, absoluteUri);
                            var tooltip = part.NavigationHint;
                            currentRuns.Add(new ClassifiedTextRun(part.Tag.ToClassificationTypeName(), part.Text, target.NavigationAction, tooltip, style));
                        }
                        else
                        {
                            // ⚠ PERF: avoid capturing Solution (including indirectly through Project or Document
                            // instances) as part of the navigationAction delegate.
                            var target = part.NavigationTarget;
                            var tooltip = part.NavigationHint;
                            var documentId = document.Id;
                            var workspace = document.Project.Solution.Workspace;
                            currentRuns.Add(new ClassifiedTextRun(
                                part.Tag.ToClassificationTypeName(), part.Text,
                                () => _ = NavigateToQuickInfoTargetAsync(target, workspace, documentId, threadingContext, operationExecutor, asyncListener, streamingPresenter.Value),
                                tooltip, style));
                        }
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

        private static async Task NavigateToQuickInfoTargetAsync(
            string navigationTarget,
            Workspace workspace,
            DocumentId documentId,
            IThreadingContext threadingContext,
            IUIThreadOperationExecutor operationExecutor,
            IAsynchronousOperationListener asyncListener,
            IStreamingFindUsagesPresenter streamingPresenter)
        {
            try
            {
                using var token = asyncListener.BeginAsyncOperation(nameof(NavigateToQuickInfoTargetAsync));
                using var context = operationExecutor.BeginExecute(EditorFeaturesResources.IntelliSense, EditorFeaturesResources.Navigating, allowCancellation: true, showProgress: false);

                var cancellationToken = context.UserCancellationToken;
                var solution = workspace.CurrentSolution;
                SymbolKeyResolution resolvedSymbolKey;
                try
                {
                    var project = solution.GetRequiredProject(documentId.ProjectId);
                    var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);
                    resolvedSymbolKey = SymbolKey.ResolveString(navigationTarget, compilation, cancellationToken: cancellationToken);
                }
                catch
                {
                    // Ignore symbol resolution failures. It likely is just a badly formed URI.
                    return;
                }

                if (resolvedSymbolKey.GetAnySymbol() is { } symbol)
                {
                    await GoToDefinitionHelpers.TryGoToDefinitionAsync(
                        symbol, solution, threadingContext, streamingPresenter, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex) when (FatalError.ReportAndCatch(ex, ErrorSeverity.Critical))
            {
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
