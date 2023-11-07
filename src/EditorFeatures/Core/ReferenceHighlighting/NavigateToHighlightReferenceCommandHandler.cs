// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Tagging;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Outlining;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.ReferenceHighlighting
{
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name(PredefinedCommandHandlerNames.NavigateToHighlightedReference)]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal partial class NavigateToHighlightReferenceCommandHandler(
        IOutliningManagerService outliningManagerService,
        IViewTagAggregatorFactoryService tagAggregatorFactory) :
        ICommandHandler<NavigateToNextHighlightedReferenceCommandArgs>,
        ICommandHandler<NavigateToPreviousHighlightedReferenceCommandArgs>
    {
        private readonly IOutliningManagerService _outliningManagerService = outliningManagerService ?? throw new ArgumentNullException(nameof(outliningManagerService));
        private readonly IViewTagAggregatorFactoryService _tagAggregatorFactory = tagAggregatorFactory ?? throw new ArgumentNullException(nameof(tagAggregatorFactory));

        public string DisplayName => EditorFeaturesResources.Navigate_To_Highlight_Reference;

        public CommandState GetCommandState(NavigateToNextHighlightedReferenceCommandArgs args)
            => GetCommandStateImpl(args);

        public CommandState GetCommandState(NavigateToPreviousHighlightedReferenceCommandArgs args)
            => GetCommandStateImpl(args);

        private CommandState GetCommandStateImpl(EditorCommandArgs args)
        {
            using var tagAggregator = _tagAggregatorFactory.CreateTagAggregator<NavigableHighlightTag>(args.TextView);

            var tagUnderCursor = FindTagUnderCaret(tagAggregator, args.TextView);
            return tagUnderCursor == null ? CommandState.Unavailable : CommandState.Available;
        }

        public bool ExecuteCommand(NavigateToNextHighlightedReferenceCommandArgs args, CommandExecutionContext context)
            => ExecuteCommandImpl(args, navigateToNext: true);

        public bool ExecuteCommand(NavigateToPreviousHighlightedReferenceCommandArgs args, CommandExecutionContext context)
            => ExecuteCommandImpl(args, navigateToNext: false);

        private bool ExecuteCommandImpl(EditorCommandArgs args, bool navigateToNext)
        {
            using (var tagAggregator = _tagAggregatorFactory.CreateTagAggregator<NavigableHighlightTag>(args.TextView))
            {
                var tagUnderCursor = FindTagUnderCaret(tagAggregator, args.TextView);

                if (tagUnderCursor == null)
                {
                    return false;
                }

                var spans = GetTags(tagAggregator, args.TextView.TextSnapshot.GetFullSpan()).ToList();

                Contract.ThrowIfFalse(spans.Any(), "We should have at least found the tag under the cursor!");

                var destTag = GetDestinationTag(tagUnderCursor.Value, spans, navigateToNext);

                if (args.TextView.TryMoveCaretToAndEnsureVisible(destTag.Start, _outliningManagerService))
                {
                    args.TextView.SetSelection(destTag);
                }
            }

            return true;
        }

        private static IEnumerable<SnapshotSpan> GetTags(
            ITagAggregator<NavigableHighlightTag> tagAggregator,
            SnapshotSpan span)
        {
            return tagAggregator.GetTags(span)
                                .SelectMany(tag => tag.Span.GetSpans(span.Snapshot.TextBuffer))
                                .OrderBy(tag => tag.Start);
        }

        private static SnapshotSpan GetDestinationTag(
            SnapshotSpan tagUnderCursor,
            List<SnapshotSpan> orderedTagSpans,
            bool navigateToNext)
        {
            var destIndex = orderedTagSpans.BinarySearch(tagUnderCursor, new StartComparer());

            Contract.ThrowIfFalse(destIndex >= 0, "Expected to find start tag in the collection");

            destIndex += navigateToNext ? 1 : -1;
            if (destIndex < 0)
            {
                destIndex = orderedTagSpans.Count - 1;
            }
            else if (destIndex == orderedTagSpans.Count)
            {
                destIndex = 0;
            }

            return orderedTagSpans[destIndex];
        }

        private static SnapshotSpan? FindTagUnderCaret(
            ITagAggregator<NavigableHighlightTag> tagAggregator,
            ITextView textView)
        {
            // We always want to be working with the surface buffer here, so this line is correct
            var caretPosition = textView.Caret.Position.BufferPosition.Position;

            var tags = GetTags(tagAggregator, new SnapshotSpan(textView.TextSnapshot, new Span(caretPosition, 0)));
            return tags.Any()
                ? tags.First()
                : null;
        }
    }
}
