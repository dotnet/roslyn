// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CommentSelection;

internal abstract class AbstractToggleBlockCommentBase :
    // Value tuple to represent that there is no distinct command to be passed in.
    AbstractCommentSelectionBase<ValueTuple>,
    ICommandHandler<ToggleBlockCommentCommandArgs>
{
    private static readonly CommentSelectionResult s_emptyCommentSelectionResult =
        new([], [], Operation.Uncomment);

    private readonly ITextStructureNavigatorSelectorService _navigatorSelectorService;

    internal AbstractToggleBlockCommentBase(
        ITextUndoHistoryRegistry undoHistoryRegistry,
        IEditorOperationsFactoryService editorOperationsFactoryService,
        ITextStructureNavigatorSelectorService navigatorSelectorService,
        EditorOptionsService editorOptionsService)
        : base(undoHistoryRegistry, editorOperationsFactoryService, editorOptionsService)
    {
        _navigatorSelectorService = navigatorSelectorService;
    }

    /// <summary>
    /// Retrieves data about the commented spans near the selection.
    /// </summary>
    /// <param name="document">the current document.</param>
    /// <param name="snapshot">the current text snapshot.</param>
    /// <param name="linesContainingSelections">
    ///     a span that contains text from the first character of the first line in the selection(s)
    ///     until the last character of the last line in the selection(s)
    /// </param>
    /// <param name="commentInfo">the comment information for the document.</param>
    /// <returns>any commented spans relevant to the selection in the document.</returns>
    protected abstract ImmutableArray<TextSpan> GetBlockCommentsInDocument(Document document, ITextSnapshot snapshot,
        TextSpan linesContainingSelections, CommentSelectionInfo commentInfo, CancellationToken cancellationToken);

    public CommandState GetCommandState(ToggleBlockCommentCommandArgs args)
        => GetCommandState(args.SubjectBuffer);

    public bool ExecuteCommand(ToggleBlockCommentCommandArgs args, CommandExecutionContext context)
        => ExecuteCommand(args.TextView, args.SubjectBuffer, ValueTuple.Create(), context);

    public override string DisplayName => EditorFeaturesResources.Toggle_Block_Comment;

    protected override string GetTitle(ValueTuple command) => EditorFeaturesResources.Toggle_Block_Comment;

    protected override string GetMessage(ValueTuple command) => EditorFeaturesResources.Toggling_block_comment;

    internal override CommentSelectionResult CollectEdits(Document document, ICommentSelectionService service,
        ITextBuffer subjectBuffer, NormalizedSnapshotSpanCollection selectedSpans, ValueTuple command, CancellationToken cancellationToken)
    {
        using (Logger.LogBlock(FunctionId.CommandHandler_ToggleBlockComment, KeyValueLogMessage.Create(LogType.UserAction, m =>
        {
            m[LanguageNameString] = document.Project.Language;
            m[LengthString] = subjectBuffer.CurrentSnapshot.Length;
        }), cancellationToken))
        {
            var navigator = _navigatorSelectorService.GetTextStructureNavigator(subjectBuffer);

            var commentInfo = service.GetInfo();
            if (commentInfo.SupportsBlockComment)
            {
                return ToggleBlockComments(document, commentInfo, navigator, selectedSpans, cancellationToken);
            }

            return s_emptyCommentSelectionResult;
        }
    }

    private CommentSelectionResult ToggleBlockComments(Document document, CommentSelectionInfo commentInfo,
        ITextStructureNavigator navigator, NormalizedSnapshotSpanCollection selectedSpans, CancellationToken cancellationToken)
    {
        var firstLineAroundSelection = selectedSpans.First().Start.GetContainingLine().Start;
        var lastLineAroundSelection = selectedSpans.Last().End.GetContainingLine().End;
        var linesContainingSelection = TextSpan.FromBounds(firstLineAroundSelection, lastLineAroundSelection);
        var blockCommentedSpans = GetBlockCommentsInDocument(
            document, selectedSpans.First().Snapshot, linesContainingSelection, commentInfo, cancellationToken);

        var blockCommentSelections = selectedSpans.SelectAsArray(span => new BlockCommentSelectionHelper(blockCommentedSpans, span));

        var returnOperation = Operation.Uncomment;

        var textChanges = ArrayBuilder<TextChange>.GetInstance();
        var trackingSpans = ArrayBuilder<CommentTrackingSpan>.GetInstance();
        // Try to uncomment until an already uncommented span is found.
        foreach (var blockCommentSelection in blockCommentSelections)
        {
            // If any selection does not have comments to remove, then the operation should be comment.
            if (!TryUncommentBlockComment(blockCommentedSpans, blockCommentSelection, textChanges, trackingSpans, commentInfo))
            {
                returnOperation = Operation.Comment;
                break;
            }
        }

        if (returnOperation == Operation.Comment)
        {
            textChanges.Clear();
            trackingSpans.Clear();
            foreach (var blockCommentSelection in blockCommentSelections)
            {
                BlockCommentSpan(blockCommentSelection, navigator, textChanges, trackingSpans, commentInfo);
            }
        }

        return new CommentSelectionResult(textChanges.ToArrayAndFree(), trackingSpans.ToArrayAndFree(), returnOperation);
    }

    private static bool TryUncommentBlockComment(ImmutableArray<TextSpan> blockCommentedSpans,
        BlockCommentSelectionHelper blockCommentSelection, ArrayBuilder<TextChange> textChanges,
        ArrayBuilder<CommentTrackingSpan> trackingSpans, CommentSelectionInfo commentInfo)
    {
        // If the selection is just a caret, try and uncomment blocks on the same line with only whitespace on the line.
        if (blockCommentSelection.SelectedSpan.IsEmpty
            && blockCommentSelection.TryGetBlockCommentOnSameLine(blockCommentedSpans, out var blockCommentOnSameLine))
        {
            DeleteBlockComment(blockCommentSelection, blockCommentOnSameLine, textChanges, commentInfo);
            trackingSpans.Add(new CommentTrackingSpan(blockCommentOnSameLine));
            return true;
        }
        // If the selection is entirely commented, remove any block comments that intersect.
        else if (blockCommentSelection.IsEntirelyCommented())
        {
            var intersectingBlockComments = blockCommentSelection.IntersectingBlockComments;
            foreach (var spanToRemove in intersectingBlockComments)
            {
                DeleteBlockComment(blockCommentSelection, spanToRemove, textChanges, commentInfo);
            }

            var trackingSpan = TextSpan.FromBounds(intersectingBlockComments.First().Start, intersectingBlockComments.Last().End);
            trackingSpans.Add(new CommentTrackingSpan(trackingSpan));
            return true;
        }
        else
        {
            return false;
        }
    }

    private static void BlockCommentSpan(BlockCommentSelectionHelper blockCommentSelection, ITextStructureNavigator navigator,
        ArrayBuilder<TextChange> textChanges, ArrayBuilder<CommentTrackingSpan> trackingSpans, CommentSelectionInfo commentInfo)
    {
        // Add sequential block comments if the selection contains any intersecting comments.
        if (blockCommentSelection.HasIntersectingBlockComments())
        {
            AddBlockCommentWithIntersectingSpans(blockCommentSelection, textChanges, trackingSpans, commentInfo);
        }
        else
        {
            // Comment the selected span or caret location.
            var spanToAdd = blockCommentSelection.SelectedSpan;
            if (spanToAdd.IsEmpty)
            {
                var caretLocation = GetCaretLocationAfterToken(navigator, blockCommentSelection);
                spanToAdd = TextSpan.FromBounds(caretLocation, caretLocation);
            }

            trackingSpans.Add(new CommentTrackingSpan(spanToAdd));
            AddBlockComment(commentInfo, spanToAdd, textChanges);
        }
    }

    /// <summary>
    /// Returns a caret location of itself or the location after the token the caret is inside of.
    /// </summary>
    private static int GetCaretLocationAfterToken(ITextStructureNavigator navigator, BlockCommentSelectionHelper blockCommentSelection)
    {
        var snapshotSpan = blockCommentSelection.SnapshotSpan;
        if (navigator == null)
        {
            return snapshotSpan.Start;
        }

        var extent = navigator.GetExtentOfWord(snapshotSpan.Start);
        int locationAfterToken = extent.Span.End;
        // Don't move to the end if it's already before the token.
        if (snapshotSpan.Start == extent.Span.Start)
        {
            locationAfterToken = extent.Span.Start;
        }
        // If the 'word' is just whitespace, use the selected location.
        if (blockCommentSelection.IsSpanWhitespace(TextSpan.FromBounds(extent.Span.Start, extent.Span.End)))
        {
            locationAfterToken = snapshotSpan.Start;
        }

        return locationAfterToken;
    }

    /// <summary>
    /// Adds a block comment when the selection already contains block comment(s).
    /// The result will be sequential block comments with the entire selection being commented out.
    /// </summary>
    private static void AddBlockCommentWithIntersectingSpans(BlockCommentSelectionHelper blockCommentSelection,
        ArrayBuilder<TextChange> textChanges, ArrayBuilder<CommentTrackingSpan> trackingSpans, CommentSelectionInfo commentInfo)
    {
        var selectedSpan = blockCommentSelection.SelectedSpan;

        var amountToAddToStart = 0;
        var amountToAddToEnd = 0;

        // Add comments to all uncommented spans in the selection.
        foreach (var uncommentedSpan in blockCommentSelection.UncommentedSpansInSelection)
        {
            AddBlockComment(commentInfo, uncommentedSpan, textChanges);
        }

        var startsWithCommentMarker = blockCommentSelection.StartsWithAnyBlockCommentMarker(commentInfo);
        var endsWithCommentMarker = blockCommentSelection.EndsWithAnyBlockCommentMarker(commentInfo);
        // If the start is commented (and not a comment marker), close the current comment and open a new one.
        if (blockCommentSelection.IsLocationCommented(selectedSpan.Start) && !startsWithCommentMarker)
        {
            InsertText(textChanges, selectedSpan.Start, commentInfo.BlockCommentEndString);
            InsertText(textChanges, selectedSpan.Start, commentInfo.BlockCommentStartString);
            // Shrink the tracking so the previous comment start marker is not included in selection.
            amountToAddToStart = commentInfo.BlockCommentEndString.Length;
        }

        // If the end is commented (and not a comment marker), close the current comment and open a new one.
        if (blockCommentSelection.IsLocationCommented(selectedSpan.End) && !endsWithCommentMarker)
        {
            InsertText(textChanges, selectedSpan.End, commentInfo.BlockCommentEndString);
            InsertText(textChanges, selectedSpan.End, commentInfo.BlockCommentStartString);
            // Shrink the tracking span so the next comment start marker is not included in selection.
            amountToAddToEnd = -commentInfo.BlockCommentStartString.Length;
        }

        trackingSpans.Add(new CommentTrackingSpan(selectedSpan, amountToAddToStart, amountToAddToEnd));
    }

    private static void AddBlockComment(CommentSelectionInfo commentInfo, TextSpan span, ArrayBuilder<TextChange> textChanges)
    {
        InsertText(textChanges, span.Start, commentInfo.BlockCommentStartString);
        InsertText(textChanges, span.End, commentInfo.BlockCommentEndString);
    }

    private static void DeleteBlockComment(BlockCommentSelectionHelper blockCommentSelection, TextSpan spanToRemove,
        ArrayBuilder<TextChange> textChanges, CommentSelectionInfo commentInfo)
    {
        DeleteText(textChanges, new TextSpan(spanToRemove.Start, commentInfo.BlockCommentStartString.Length));
        var endMarkerPosition = spanToRemove.End - commentInfo.BlockCommentEndString.Length;
        // Sometimes the block comment will be missing a close marker.
        if (Equals(blockCommentSelection.GetSubstringFromText(endMarkerPosition, commentInfo.BlockCommentEndString.Length),
            commentInfo.BlockCommentEndString))
        {
            DeleteText(textChanges, new TextSpan(endMarkerPosition, commentInfo.BlockCommentEndString.Length));
        }
    }

    private sealed class BlockCommentSelectionHelper
    {
        /// <summary>
        /// Trimmed text of the selection.
        /// </summary>
        private readonly string _trimmedText;

        public SnapshotSpan SnapshotSpan { get; }

        public TextSpan SelectedSpan { get; }

        public ImmutableArray<TextSpan> IntersectingBlockComments { get; }

        public ImmutableArray<TextSpan> UncommentedSpansInSelection { get; }

        public BlockCommentSelectionHelper(ImmutableArray<TextSpan> allBlockComments, SnapshotSpan selectedSnapshotSpan)
        {
            _trimmedText = selectedSnapshotSpan.GetText().Trim();
            SnapshotSpan = selectedSnapshotSpan;

            SelectedSpan = TextSpan.FromBounds(selectedSnapshotSpan.Start, selectedSnapshotSpan.End);
            IntersectingBlockComments = GetIntersectingBlockComments(allBlockComments, SelectedSpan);
            UncommentedSpansInSelection = GetUncommentedSpansInSelection();
        }

        /// <summary>
        /// Determines if the given span is entirely whitespace.
        /// </summary>
        public bool IsSpanWhitespace(TextSpan span)
        {
            for (var i = span.Start; i < span.End; i++)
            {
                if (!char.IsWhiteSpace(SnapshotSpan.Snapshot[i]))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Determines if the location falls inside a commented span.
        /// </summary>
        public bool IsLocationCommented(int location)
            => IntersectingBlockComments.Contains(span => span.Contains(location));

        /// <summary>
        /// Checks if the selection already starts with a comment marker.
        /// This prevents us from adding an extra marker.
        /// </summary>
        public bool StartsWithAnyBlockCommentMarker(CommentSelectionInfo commentInfo)
        {
            return _trimmedText.StartsWith(commentInfo.BlockCommentStartString, StringComparison.Ordinal)
                   || _trimmedText.StartsWith(commentInfo.BlockCommentEndString, StringComparison.Ordinal);
        }

        /// <summary>
        /// Checks if the selection already ends with a comment marker.
        /// This prevents us from adding an extra marker.
        /// </summary>
        public bool EndsWithAnyBlockCommentMarker(CommentSelectionInfo commentInfo)
        {
            return _trimmedText.EndsWith(commentInfo.BlockCommentStartString, StringComparison.Ordinal)
                   || _trimmedText.EndsWith(commentInfo.BlockCommentEndString, StringComparison.Ordinal);
        }

        /// <summary>
        /// Checks if the selected span contains any uncommented non whitespace characters.
        /// </summary>
        public bool IsEntirelyCommented()
            => !UncommentedSpansInSelection.Any() && HasIntersectingBlockComments();

        /// <summary>
        /// Returns if the selection intersects with any block comments.
        /// </summary>
        public bool HasIntersectingBlockComments()
            => IntersectingBlockComments.Any();

        public string GetSubstringFromText(int position, int length)
            => SnapshotSpan.Snapshot.GetText().Substring(position, length);

        /// <summary>
        /// Tries to get a block comment on the same line.  There are two cases:
        ///     1.  The caret is preceding a block comment on the same line, with only whitespace before the comment.
        ///     2.  The caret is following a block comment on the same line, with only whitespace after the comment.
        /// </summary>
        public bool TryGetBlockCommentOnSameLine(ImmutableArray<TextSpan> allBlockComments, out TextSpan commentedSpanOnSameLine)
        {
            var snapshot = SnapshotSpan.Snapshot;
            var selectedLine = snapshot.GetLineFromPosition(SelectedSpan.Start);
            var lineStartToCaretIsWhitespace = IsSpanWhitespace(TextSpan.FromBounds(selectedLine.Start, SelectedSpan.Start));
            var caretToLineEndIsWhitespace = IsSpanWhitespace(TextSpan.FromBounds(SelectedSpan.Start, selectedLine.End));
            foreach (var blockComment in allBlockComments)
            {
                if (lineStartToCaretIsWhitespace
                    && SelectedSpan.Start < blockComment.Start
                    && snapshot.AreOnSameLine(SelectedSpan.Start, blockComment.Start))
                {
                    if (IsSpanWhitespace(TextSpan.FromBounds(SelectedSpan.Start, blockComment.Start)))
                    {
                        commentedSpanOnSameLine = blockComment;
                        return true;
                    }
                }
                else if (caretToLineEndIsWhitespace
                         && SelectedSpan.Start > blockComment.End
                         && snapshot.AreOnSameLine(SelectedSpan.Start, blockComment.End))
                {
                    if (IsSpanWhitespace(TextSpan.FromBounds(blockComment.End, SelectedSpan.Start)))
                    {
                        commentedSpanOnSameLine = blockComment;
                        return true;
                    }
                }
            }

            commentedSpanOnSameLine = new TextSpan();
            return false;
        }

        /// <summary>
        /// Gets a list of block comments that intersect the span.
        /// Spans are intersecting if 1 location is the same between them (empty spans look at the start).
        /// </summary>
        private static ImmutableArray<TextSpan> GetIntersectingBlockComments(ImmutableArray<TextSpan> allBlockComments, TextSpan span)
            => allBlockComments.WhereAsArray(blockCommentSpan => span.OverlapsWith(blockCommentSpan) || blockCommentSpan.Contains(span));

        /// <summary>
        /// Retrieves all non commented, non whitespace spans.
        /// </summary>
        private ImmutableArray<TextSpan> GetUncommentedSpansInSelection()
        {
            var uncommentedSpans = new List<TextSpan>();

            // Invert the commented spans to get the uncommented spans.
            var spanStart = SelectedSpan.Start;
            foreach (var commentedSpan in IntersectingBlockComments)
            {
                if (commentedSpan.Start > spanStart)
                {
                    // Get span up until the comment and check to make sure it is not whitespace.
                    var possibleUncommentedSpan = TextSpan.FromBounds(spanStart, commentedSpan.Start);
                    if (!IsSpanWhitespace(possibleUncommentedSpan))
                    {
                        uncommentedSpans.Add(possibleUncommentedSpan);
                    }
                }

                // The next possible uncommented span starts at the end of this commented span.
                spanStart = commentedSpan.End;
            }

            // If part of the selection is left over, it's not commented.  Add if not whitespace.
            if (spanStart < SelectedSpan.End)
            {
                var uncommentedSpan = TextSpan.FromBounds(spanStart, SelectedSpan.End);
                if (!IsSpanWhitespace(uncommentedSpan))
                {
                    uncommentedSpans.Add(uncommentedSpan);
                }
            }

            return [.. uncommentedSpans];
        }
    }
}
