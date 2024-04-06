// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
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

namespace Microsoft.CodeAnalysis.Editor.ReferenceHighlighting;

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

    // We always want to support these commands.  If we don't, then they will be processed by the next higher item in
    // the command stack, which can cause VS to cycle focus to some other control other than the actual editor.  Once
    // the user is in a roslyn editing session, we always want to be processing this, even if the user is not actually
    // on a symbol (or symbol information hasn't been computed yet).
    //
    // https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1875365

    public CommandState GetCommandState(NavigateToNextHighlightedReferenceCommandArgs args)
        => CommandState.Available;

    public CommandState GetCommandState(NavigateToPreviousHighlightedReferenceCommandArgs args)
        => CommandState.Available;

    public bool ExecuteCommand(NavigateToNextHighlightedReferenceCommandArgs args, CommandExecutionContext context)
        => ExecuteCommandImpl(args, navigateToNext: true);

    public bool ExecuteCommand(NavigateToPreviousHighlightedReferenceCommandArgs args, CommandExecutionContext context)
        => ExecuteCommandImpl(args, navigateToNext: false);

    private bool ExecuteCommandImpl(EditorCommandArgs args, bool navigateToNext)
    {
        using var tagAggregator = _tagAggregatorFactory.CreateTagAggregator<NavigableHighlightTag>(args.TextView);
        var tagUnderCursor = FindTagUnderCaret(tagAggregator, args.TextView);

        if (tagUnderCursor == null)
            return false;

        var spans = GetTags(tagAggregator, args.TextView.TextSnapshot.GetFullSpan());

        Contract.ThrowIfFalse(spans.Any(), "We should have at least found the tag under the cursor!");

        var destTag = GetDestinationTag(tagUnderCursor.Value, spans, navigateToNext);

        if (args.TextView.TryMoveCaretToAndEnsureVisible(destTag.Start, _outliningManagerService))
            args.TextView.SetSelection(destTag);

        return true;
    }

    private static ImmutableArray<SnapshotSpan> GetTags(
        ITagAggregator<NavigableHighlightTag> tagAggregator,
        SnapshotSpan span)
    {
        using var _ = PooledObjects.ArrayBuilder<SnapshotSpan>.GetInstance(out var tags);

        foreach (var tag in tagAggregator.GetTags(span))
            tags.AddRange(tag.Span.GetSpans(span.Snapshot.TextBuffer));

        tags.Sort(static (ss1, ss2) => ss1.Start - ss2.Start);
        return tags.ToImmutable();
    }

    private static SnapshotSpan GetDestinationTag(
        SnapshotSpan tagUnderCursor,
        ImmutableArray<SnapshotSpan> orderedTagSpans,
        bool navigateToNext)
    {
        var destIndex = orderedTagSpans.BinarySearch(tagUnderCursor, StartComparer.Instance);

        Contract.ThrowIfFalse(destIndex >= 0, "Expected to find start tag in the collection");

        destIndex += navigateToNext ? 1 : -1;

        // Handle wraparound
        var length = orderedTagSpans.Length;
        destIndex = ((destIndex % length) + length) % length;

        return orderedTagSpans[destIndex];
    }

    private static SnapshotSpan? FindTagUnderCaret(
        ITagAggregator<NavigableHighlightTag> tagAggregator,
        ITextView textView)
    {
        // We always want to be working with the surface buffer here, so this line is correct
        var caretPosition = textView.Caret.Position.BufferPosition.Position;

        var tags = GetTags(tagAggregator, new SnapshotSpan(textView.TextSnapshot, new Span(caretPosition, 0)));
        return tags.FirstOrNull();
    }
}
