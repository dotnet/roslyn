// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.QuickInfo.Presentation;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.VisualStudio.Text.Adornments;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense;

internal static class TaggedTextExtensions
{
    internal static ImmutableArray<object> ToInteractiveTextElements(
        this ImmutableArray<TaggedText> taggedTexts,
        INavigationActionFactory? navigationActionFactory)
    {
        using var builder = TextElementBuilder.Empty;
        var span = taggedTexts.AsSpan();

        BuildInteractiveTextElements(ref span, ref TextElementBuilder.AsRef(in builder), navigationActionFactory);

        return builder.ToImmutableAndClear();
    }

    private static void BuildInteractiveTextElements(
        ref ReadOnlySpan<TaggedText> taggedTexts,
        ref TextElementBuilder builder,
        INavigationActionFactory? navigationActionFactory)
    {
        var done = false;

        while (!done && taggedTexts is [var part, .. var remaining])
        {
            taggedTexts = remaining;

            switch (part.Tag)
            {
                case TextTags.CodeBlockStart or TextTags.CodeBlockEnd:
                    // These tags can be ignored - they are for markdown formatting only.
                    break;

                case TextTags.ContainerStart:
                    // This is the start of a set of inline elements.
                    {
                        using var nestedBuilder = TextElementBuilder.Empty;
                        BuildInteractiveTextElements(
                            ref taggedTexts,
                            ref TextElementBuilder.AsRef(in nestedBuilder),
                            navigationActionFactory);

                        var nestedElements = nestedBuilder.ToImmutableAndClear();
                        builder.AddContainer(nestedElements, part.Text);
                    }

                    break;

                case TextTags.ContainerEnd:
                    // We're finished processing inline elements. Break out and let the caller continue
                    done = true;
                    break;

                case TextTags.LineBreak:
                    builder.LineBreak();
                    break;

                default: // This is tagged text getting added to the current line we are building.

                    // If the tagged text has navigation target AND a NavigationActionFactory
                    // is available, we'll create the classified run with a navigation action.
                    var run = part.NavigationTarget is not null && navigationActionFactory is not null
                        ? CreateRunWithNavigationAction(part, navigationActionFactory)
                        : CreateRun(part);

                    builder.Add(run);
                    break;
            }
        }

        static ClassifiedTextRun CreateRun(TaggedText part)
        {
            return new(
                part.Tag.ToClassificationTypeName(),
                part.Text,
                part.Style.ToClassifiedTextRunStyle());
        }

        static ClassifiedTextRun CreateRunWithNavigationAction(TaggedText part, INavigationActionFactory navigationActionFactory)
        {
            Contract.ThrowIfNull(part.NavigationTarget);

            return new(
                part.Tag.ToClassificationTypeName(),
                part.Text,
                navigationActionFactory.CreateNavigationAction(part.NavigationTarget),
                tooltip: part.NavigationHint,
                part.Style.ToClassifiedTextRunStyle());
        }
    }

    [NonCopyable]
    private struct TextElementBuilder : IDisposable
    {
        public static TextElementBuilder Empty => default;

        // This builder will produce zero or more paragraphs.
        private TemporaryArray<object> _paragraphs;

        // Each paragraph is constructed from one or more lines
        private TemporaryArray<object> _lines;

        // Each line is constructed from one or more runs
        private TemporaryArray<ClassifiedTextRun> _runs;

        /// <summary>
        /// Gets a mutable reference to a <see cref="TextElementBuilder"/> stored in a <c>using</c> variable.
        /// </summary>
        public static ref TextElementBuilder AsRef(ref readonly TextElementBuilder builder)
#pragma warning disable RS0042 // Do not copy value
            => ref Unsafe.AsRef(in builder);
#pragma warning restore RS0042 // Do not copy value

        public void Dispose()
        {
            Contract.ThrowIfFalse(_paragraphs.Count == 0);
            Contract.ThrowIfFalse(_lines.Count == 0);
            Contract.ThrowIfFalse(_runs.Count == 0);

            _paragraphs.Dispose();
            _lines.Dispose();
            _runs.Dispose();
        }

        public void Add(ClassifiedTextRun run)
        {
            _runs.Add(run);
        }

        public void LineBreak()
        {
            if (_runs.Count > 0)
            {
                // This line break means the end of a line within a paragraph.
                _lines.Add(new ClassifiedTextElement(_runs.ToImmutableAndClear()));
            }
            else
            {
                // This line break means the end of a paragraph. Empty paragraphs are ignored, but could appear
                // in the input to this method:
                //
                // * Empty <para> elements
                // * Explicit line breaks at the start of a comment
                // * Multiple line breaks between paragraphs
                AddLinesAndClear();
            }
        }

        public void AddContainer(ImmutableArray<object> nestedElements, string text)
        {
            if (_runs.Count > 0)
            {
                // This line break means the end of a line within a paragraph.
                _lines.Add(new ClassifiedTextElement(_runs.ToImmutableAndClear()));
            }

            var innerContainer = nestedElements is [var item, .. var rest]
                ? new ContainerElement(
                    ContainerElementStyle.Stacked,
                    item,
                    new ContainerElement(
                        ContainerElementStyle.Stacked | ContainerElementStyle.VerticalPadding,
                        rest))
                : new ContainerElement(
                    ContainerElementStyle.Stacked,
                    nestedElements);

            var container = new ContainerElement(
                ContainerElementStyle.Wrapped,
                new ClassifiedTextElement(new ClassifiedTextRun(ClassificationTypeNames.Text, text)),
                innerContainer);

            _paragraphs.Add(container);
        }

        public ImmutableArray<object> ToImmutableAndClear()
        {
            if (_runs.Count > 0)
            {
                _lines.Add(new ClassifiedTextElement(_runs.ToImmutableAndClear()));
            }

            if (_lines.Count > 0)
            {
                AddLinesAndClear();
            }

            return _paragraphs.ToImmutableAndClear();
        }

        private void AddLinesAndClear()
        {
            Contract.ThrowIfTrue(_lines.Count == 0);

            if (_lines.Count == 1)
            {
                // The paragraph contains only one line, so it doesn't need to be added to a container. Avoiding the
                // wrapping container here also avoids a wrapping element in the WPF elements used for rendering,
                // improving efficiency.
                _paragraphs.Add(_lines[0]);
                _lines.Clear();
            }
            else
            {
                // The lines of a multi-line paragraph are stacked to produce the full paragraph.
                var container = new ContainerElement(ContainerElementStyle.Stacked, _lines.ToImmutableAndClear());
                _paragraphs.Add(container);
            }
        }
    }

    public static ClassifiedTextRunStyle ToClassifiedTextRunStyle(this TaggedTextStyle style)
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
}
