// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CommentSelection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.Implementation.CommentSelection;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.CSharp.CommentSelection
{
    /* TODO - Modify these once the toggle block comment handler is added.
    [Export(typeof(VSCommanding.ICommandHandler))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name(PredefinedCommandHandlerNames.CommentSelection)]*/
    internal class ToggleBlockCommentCommandHandler :
        AbstractCommentSelectionCommandHandler/*,
        VSCommanding.ICommandHandler<CommentSelectionCommandArgs>*/
    {
        [ImportingConstructor]
        internal ToggleBlockCommentCommandHandler(
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IEditorOperationsFactoryService editorOperationsFactoryService) : base(undoHistoryRegistry, editorOperationsFactoryService)
        { }

        /* TODO - modify once the toggle block comment handler is added.
        public VSCommanding.CommandState GetCommandState(CommentSelectionCommandArgs args)
        {
            return GetCommandState(args.SubjectBuffer);
        }

        public bool ExecuteCommand(CommentSelectionCommandArgs args, CommandExecutionContext context)
        {
            return ExecuteCommand(args.TextView, args.SubjectBuffer, Operation.Toggle, context);
        }*/

        public override string DisplayName => EditorFeaturesResources.Toggle_Block_Comment;

        internal override string GetTitle(Operation operation)
        {
            return EditorFeaturesResources.Toggle_Block_Comment;
        }

        internal override string GetMessage(Operation operation)
        {
            return EditorFeaturesResources.Toggling_block_comment_on_selection;
        }

        internal override void SetTrackingSpans(ITextView textView, ITextBuffer buffer, List<CommentTrackingSpan> trackingSpans)
        {
            var spans = trackingSpans.Select(trackingSpan => trackingSpan.ToSelection(buffer));
            textView.GetMultiSelectionBroker().SetSelectionRange(spans, spans.Last());
        }

        internal override void CollectEdits(Document document, ICommentSelectionService service, NormalizedSnapshotSpanCollection selectedSpans,
            List<TextChange> textChanges, List<CommentTrackingSpan> trackingSpans, Operation operation, CancellationToken cancellationToken)
        {
            var experimentationService = document.Project.Solution.Workspace.Services.GetRequiredService<IExperimentationService>();
            if (!experimentationService.IsExperimentEnabled(WellKnownExperimentNames.RoslynToggleBlockComment))
            {
                return;
            }

            if (selectedSpans.IsEmpty())
            {
                return;
            }

            var getRoot = document.GetSyntaxRootAsync();
            var commentInfo = service.GetInfoAsync(document, selectedSpans.First().Span.ToTextSpan(), cancellationToken).WaitAndGetResult(cancellationToken);
            var root = getRoot.WaitAndGetResult(cancellationToken);
            if (commentInfo.SupportsBlockComment)
            {
                ToggleBlockComments(commentInfo, root, selectedSpans, textChanges, trackingSpans);
            }
        }

        private void ToggleBlockComments(CommentSelectionInfo commentInfo, SyntaxNode root, NormalizedSnapshotSpanCollection selectedSpans,
            List<TextChange> textChanges, List<CommentTrackingSpan> trackingSpans)
        {
            var blockCommentedSpans = GetDescendentBlockCommentSpansFromRoot(root);
            var blockCommentSelectionHelpers = selectedSpans.Select(span => new BlockCommentSelectionHelper(blockCommentedSpans, span));

            // If there is a multi selection, either uncomment all or comment all.
            var onlyAddComment = false;
            if (selectedSpans.Count > 1)
            {
                onlyAddComment = blockCommentSelectionHelpers.Where(helper => !helper.IsEntirelyCommented()).Any();
            }

            foreach (var blockCommentSelection in blockCommentSelectionHelpers)
            {
                if (commentInfo.SupportsBlockComment)
                {
                    if (!onlyAddComment && TryUncommentBlockComment(blockCommentedSpans, blockCommentSelection, textChanges, trackingSpans, commentInfo))
                    { }
                    else
                    {
                        BlockCommentSpan(blockCommentSelection, textChanges, trackingSpans, root, commentInfo);
                    }
                }
            }
        }

        private static bool TryUncommentBlockComment(IEnumerable<Span> blockCommentedSpans, BlockCommentSelectionHelper blockCommentSelectionHelper,
            List<TextChange> textChanges, List<CommentTrackingSpan> trackingSpans, CommentSelectionInfo commentInfo)
        {
            // If the selection is just a caret, try and uncomment blocks on the same line with only whitespace on the line.
            if (blockCommentSelectionHelper.SelectedSpan.IsEmpty && blockCommentSelectionHelper.TryGetBlockCommentOnSameLine(blockCommentedSpans, out var blockCommentOnSameLine))
            {
                DeleteBlockComment(blockCommentSelectionHelper, blockCommentOnSameLine, textChanges, commentInfo);
                trackingSpans.Add(blockCommentSelectionHelper.GetTrackingSpan(blockCommentOnSameLine, SpanTrackingMode.EdgeExclusive, Operation.Uncomment));
                return true;
            }

            // If there are not any block comments intersecting the selection, there is nothing to uncomment.
            if (!blockCommentSelectionHelper.HasIntersectingBlockComments())
            {
                return false;
            }

            // If the selection is entirely commented, remove any block comments that intersect.
            if (blockCommentSelectionHelper.IsEntirelyCommented())
            {
                var intersectingBlockComments = blockCommentSelectionHelper.IntersectingBlockComments;
                foreach (var spanToRemove in intersectingBlockComments)
                {
                    DeleteBlockComment(blockCommentSelectionHelper, spanToRemove, textChanges, commentInfo);
                }
                var trackingSpan = Span.FromBounds(intersectingBlockComments.First().Start, intersectingBlockComments.Last().End);
                trackingSpans.Add(blockCommentSelectionHelper.GetTrackingSpan(trackingSpan, SpanTrackingMode.EdgeExclusive, Operation.Uncomment));
                return true;
            }

            // If the selection is entirely inside a block comment, remove the comment.
            if (blockCommentSelectionHelper.TryGetSurroundingBlockComment(out var containingBlockComment))
            {
                DeleteBlockComment(blockCommentSelectionHelper, containingBlockComment, textChanges, commentInfo);
                trackingSpans.Add(blockCommentSelectionHelper.GetTrackingSpan(containingBlockComment, SpanTrackingMode.EdgeExclusive, Operation.Uncomment));
                return true;
            }

            return false;
        }

        private static void BlockCommentSpan(BlockCommentSelectionHelper blockCommentSelectionHelper, List<TextChange> textChanges,
            List<CommentTrackingSpan> trackingSpans, SyntaxNode root, CommentSelectionInfo commentInfo)
        {
            if (blockCommentSelectionHelper.HasIntersectingBlockComments())
            {
                AddBlockCommentWithIntersectingSpans(blockCommentSelectionHelper, textChanges, trackingSpans, commentInfo);
            }
            else
            {
                Span spanToAdd = blockCommentSelectionHelper.SelectedSpan;
                if (blockCommentSelectionHelper.SelectedSpan.IsEmpty)
                {
                    // The location for the comment should be the caret or the location after the end of the token the caret is inside of.
                    var caretLocation = GetLocationAfterToken(blockCommentSelectionHelper.SelectedSpan.Start, root);
                    spanToAdd = Span.FromBounds(caretLocation, caretLocation);
                }

                AddBlockComment(commentInfo, spanToAdd, textChanges);
                trackingSpans.Add(blockCommentSelectionHelper.GetTrackingSpan(spanToAdd, SpanTrackingMode.EdgeInclusive, Operation.Comment));
            }
        }

        /// <summary>
        /// Adds a block comment when the selection already contains block comment(s).
        /// The result will be sequential block comments with the entire selection being commented out.
        /// </summary>
        private static void AddBlockCommentWithIntersectingSpans(BlockCommentSelectionHelper blockCommentSelectionHelper, List<TextChange> textChanges,
            List<CommentTrackingSpan> trackingSpans, CommentSelectionInfo commentInfo)
        {
            var selectedSpan = blockCommentSelectionHelper.SelectedSpan;
            var spanTrackingMode = SpanTrackingMode.EdgeInclusive;

            var amountToAddToStart = 0;
            var amountToAddToEnd = 0;

            // Add comments to all uncommented spans in the selection.
            foreach (var uncommentedSpan in blockCommentSelectionHelper.UncommentedSpansInSelection)
            {
                AddBlockComment(commentInfo, uncommentedSpan, textChanges);
            }

            // If the start is commented (and not a comment marker), close the current comment and open a new one.
            if (blockCommentSelectionHelper.IsLocationCommented(selectedSpan.Start) && !blockCommentSelectionHelper.DoesBeginWithBlockComment(commentInfo))
            {
                InsertText(textChanges, selectedSpan.Start, commentInfo.BlockCommentEndString);
                InsertText(textChanges, selectedSpan.Start, commentInfo.BlockCommentStartString);
                // Shrink the tracking so the previous comment start marker is not included in selection.
                amountToAddToStart = commentInfo.BlockCommentEndString.Length;
            }

            // If the end is commented (and not a comment marker), close the current comment and open a new one.
            if (blockCommentSelectionHelper.IsLocationCommented(selectedSpan.End) && !blockCommentSelectionHelper.DoesEndWithBlockComment(commentInfo))
            {
                InsertText(textChanges, selectedSpan.End, commentInfo.BlockCommentEndString);
                InsertText(textChanges, selectedSpan.End, commentInfo.BlockCommentStartString);
                // Shrink the tracking span so the next comment start marker is not included in selection.
                amountToAddToEnd = -commentInfo.BlockCommentStartString.Length;
            }

            trackingSpans.Add(blockCommentSelectionHelper.GetTrackingSpan(selectedSpan, spanTrackingMode, Operation.Comment, amountToAddToStart, amountToAddToEnd));
        }

        private static void AddBlockComment(CommentSelectionInfo commentInfo, Span span, List<TextChange> textChanges)
        {
            InsertText(textChanges, span.Start, commentInfo.BlockCommentStartString);
            InsertText(textChanges, span.End, commentInfo.BlockCommentEndString);
        }

        private static void DeleteBlockComment(BlockCommentSelectionHelper blockCommentSelectionHelper, Span spanToRemove, List<TextChange> textChanges,
            CommentSelectionInfo commentInfo)
        {
            DeleteText(textChanges, new TextSpan(spanToRemove.Start, commentInfo.BlockCommentStartString.Length));

            var blockCommentMarkerPosition = spanToRemove.End - commentInfo.BlockCommentEndString.Length;
            // Sometimes the block comment will be missing a close marker.
            if (Equals(blockCommentSelectionHelper.GetSubstringFromText(blockCommentMarkerPosition, commentInfo.BlockCommentEndString.Length), commentInfo.BlockCommentEndString))
            {
                DeleteText(textChanges, new TextSpan(blockCommentMarkerPosition, commentInfo.BlockCommentEndString.Length));
            }
        }

        private static IEnumerable<Span> GetDescendentBlockCommentSpansFromRoot(SyntaxNode root)
        {
            return root.DescendantTrivia()
                .Where(trivia => trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) || trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
                .Select(blockCommentTrivia => blockCommentTrivia.Span.ToSpan());
        }

        /// <summary>
        /// Get a location of itself or the end of the token it is located in.
        /// </summary>
        private static int GetLocationAfterToken(int location, SyntaxNode root)
        {
            var token = root.FindToken(location);
            if (token.Span.Contains(location))
            {
                return token.Span.End;
            }
            return location;
        }

        private class BlockCommentSelectionHelper
        {
            private readonly string _text;

            public SnapshotSpan SelectedSpan { get; }

            public IEnumerable<Span> IntersectingBlockComments { get; }

            public IEnumerable<Span> UncommentedSpansInSelection { get; }

            public BlockCommentSelectionHelper(IEnumerable<Span> allBlockComments, SnapshotSpan selectedSpan)
            {
                _text = selectedSpan.GetText().Trim();

                SelectedSpan = selectedSpan;
                IntersectingBlockComments = GetIntersectingBlockComments(allBlockComments, selectedSpan);
                UncommentedSpansInSelection = GetUncommentedSpansInSelection();
            }

            /// <summary>
            /// Determines if the given span is entirely whitespace.
            /// </summary>
            /// <param name="span">the span to check for whitespace.</param>
            /// <returns>true if the span is entirely whitespace.</returns>
            public bool IsSpanWhitespace(Span span)
            {
                for (var i = span.Start; i < span.End; i++)
                {
                    if (!IsLocationWhitespace(i))
                    {
                        return false;
                    }
                }

                return true;

                bool IsLocationWhitespace(int location)
                {
                    var character = SelectedSpan.Snapshot.GetPoint(location).GetChar();
                    return SyntaxFacts.IsWhitespace(character) || SyntaxFacts.IsNewLine(character);
                }
            }

            /// <summary>
            /// Determines if the location falls inside a commented span.
            /// </summary>
            public bool IsLocationCommented(int location)
            {
                return IntersectingBlockComments.Contains(span => span.Contains(location));
            }

            public bool DoesBeginWithBlockComment(CommentSelectionInfo commentInfo)
            {
                return _text.StartsWith(commentInfo.BlockCommentStartString, StringComparison.Ordinal)
                       || _text.StartsWith(commentInfo.BlockCommentEndString, StringComparison.Ordinal);
            }

            public bool DoesEndWithBlockComment(CommentSelectionInfo commentInfo)
            {
                return _text.EndsWith(commentInfo.BlockCommentStartString, StringComparison.Ordinal)
                       || _text.EndsWith(commentInfo.BlockCommentEndString, StringComparison.Ordinal);
            }

            /// <summary>
            /// Checks if the selected span contains any uncommented non whitespace characters.
            /// </summary>
            public bool IsEntirelyCommented()
            {
                return !UncommentedSpansInSelection.Any();
            }

            /// <summary>
            /// Returns if the selection intersects with any block comments.
            /// </summary>
            public bool HasIntersectingBlockComments()
            {
                return IntersectingBlockComments.Any();
            }

            public string GetSubstringFromText(int position, int length)
            {
                return SelectedSpan.Snapshot.GetText().Substring(position, length);
            }

            /// <summary>
            /// Returns a tracking span associated with the selected span.
            /// </summary>
            public CommentTrackingSpan GetTrackingSpan(Span span, SpanTrackingMode spanTrackingMode, Operation operation,
                int addToStart = 0, int addToEnd = 0)
            {
                var trackingSpan = SelectedSpan.Snapshot.CreateTrackingSpan(Span.FromBounds(span.Start, span.End), spanTrackingMode);
                return new CommentTrackingSpan(trackingSpan, operation, addToStart, addToEnd);
            }

            /// <summary>
            /// Retrive the block comment entirely surrounding the selection if it exists.
            /// </summary>
            public bool TryGetSurroundingBlockComment(out Span containingSpan)
            {
                containingSpan = IntersectingBlockComments.FirstOrDefault(commentedSpan => commentedSpan.Contains(SelectedSpan));
                if (containingSpan.Start == 0 && containingSpan.End == 0)
                {
                    return false;
                }

                return true;
            }

            /// <summary>
            /// Tries to get a block comment on the same line.  There are two cases:
            ///     1.  The caret is preceding a block comment on the same line, with only whitespace before the comment.
            ///     2.  The caret is following a block comment on the same line, with only whitespace after the comment.
            /// </summary>
            public bool TryGetBlockCommentOnSameLine(IEnumerable<Span> allBlockComments, out Span commentedSpanOnSameLine)
            {
                var selectedLine = SelectedSpan.Snapshot.GetLineFromPosition(SelectedSpan.Start);
                var lineStartToCaretIsWhitespace = IsSpanWhitespace(Span.FromBounds(selectedLine.Start, SelectedSpan.Start));
                var caretToLineEndIsWhitespace = IsSpanWhitespace(Span.FromBounds(SelectedSpan.Start, selectedLine.End));
                foreach (var blockComment in allBlockComments)
                {
                    if (lineStartToCaretIsWhitespace && SelectedSpan.Start < blockComment.Start && SelectedSpan.Snapshot.AreOnSameLine(SelectedSpan.Start, blockComment.Start))
                    {
                        if (IsSpanWhitespace(Span.FromBounds(SelectedSpan.Start, blockComment.Start)))
                        {
                            commentedSpanOnSameLine = blockComment;
                            return true;
                        }
                    }
                    else if (caretToLineEndIsWhitespace && SelectedSpan.Start > blockComment.End && SelectedSpan.Snapshot.AreOnSameLine(SelectedSpan.Start, blockComment.End))
                    {
                        if (IsSpanWhitespace(Span.FromBounds(blockComment.End, SelectedSpan.Start)))
                        {
                            commentedSpanOnSameLine = blockComment;
                            return true;
                        }
                    }
                }

                commentedSpanOnSameLine = new Span();
                return false;
            }

            /// <summary>
            /// Gets a list of block comments that intersect the span.
            /// Spans are intersecting if 1 location is the same between them (empty spans look at the start).
            /// </summary>
            private IEnumerable<Span> GetIntersectingBlockComments(IEnumerable<Span> allBlockComments, Span span)
            {
                return allBlockComments.Where(blockCommentSpan => span.OverlapsWith(blockCommentSpan) || blockCommentSpan.Contains(span));
            }

            /// <summary>
            /// Retrieves all non commented, non whitespace spans.
            /// </summary>
            private IEnumerable<Span> GetUncommentedSpansInSelection()
            {
                var uncommentedSpans = new List<Span>();

                // Invert the commented spans to get the uncommented spans.
                int spanStart = SelectedSpan.Start;
                foreach (var commentedSpan in IntersectingBlockComments)
                {
                    if (commentedSpan.Start > spanStart)
                    {
                        // Get span up until the comment and check to make sure it is not whitespace.
                        var possibleUncommentedSpan = Span.FromBounds(spanStart, commentedSpan.Start);
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
                    var uncommentedSpan = Span.FromBounds(spanStart, SelectedSpan.End);
                    if (!IsSpanWhitespace(uncommentedSpan))
                    {
                        uncommentedSpans.Add(uncommentedSpan);
                    }
                }

                return uncommentedSpans;
            }
        }
    }
}
