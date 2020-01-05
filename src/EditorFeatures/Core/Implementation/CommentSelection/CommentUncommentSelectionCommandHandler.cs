// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommentSelection;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CommentSelection
{
    [Export(typeof(ICommandHandler))]
    [VisualStudio.Utilities.ContentType(ContentTypeNames.RoslynContentType)]
    [VisualStudio.Utilities.Name(PredefinedCommandHandlerNames.CommentSelection)]
    internal class CommentUncommentSelectionCommandHandler :
        AbstractCommentSelectionBase<Operation>,
        ICommandHandler<CommentSelectionCommandArgs>,
        ICommandHandler<UncommentSelectionCommandArgs>
    {
        [ImportingConstructor]
        public CommentUncommentSelectionCommandHandler(
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IEditorOperationsFactoryService editorOperationsFactoryService)
            : base(undoHistoryRegistry, editorOperationsFactoryService)
        {
        }

        public CommandState GetCommandState(CommentSelectionCommandArgs args)
        {
            return GetCommandState(args.SubjectBuffer);
        }

        /// <summary>
        /// Comment the selected spans, and reset the selection.
        /// </summary>
        public bool ExecuteCommand(CommentSelectionCommandArgs args, CommandExecutionContext context)
        {
            return this.ExecuteCommand(args.TextView, args.SubjectBuffer, Operation.Comment, context);
        }

        public CommandState GetCommandState(UncommentSelectionCommandArgs args)
        {
            return GetCommandState(args.SubjectBuffer);
        }

        /// <summary>
        /// Uncomment the selected spans, and reset the selection.
        /// </summary>
        public bool ExecuteCommand(UncommentSelectionCommandArgs args, CommandExecutionContext context)
        {
            return this.ExecuteCommand(args.TextView, args.SubjectBuffer, Operation.Uncomment, context);
        }

        public override string DisplayName => EditorFeaturesResources.Comment_Uncomment_Selection;

        protected override string GetTitle(Operation operation) =>
            operation == Operation.Comment
                ? EditorFeaturesResources.Comment_Selection
                : EditorFeaturesResources.Uncomment_Selection;

        protected override string GetMessage(Operation operation) =>
            operation == Operation.Comment
                ? EditorFeaturesResources.Commenting_currently_selected_text
                : EditorFeaturesResources.Uncommenting_currently_selected_text;

        /// <summary>
        /// Add the necessary edits to the given spans. Also collect tracking spans over each span.
        ///
        /// Internal so that it can be called by unit tests.
        /// </summary>
        internal override Task<CommentSelectionResult> CollectEditsAsync(
            Document document, ICommentSelectionService service, ITextBuffer subjectBuffer, NormalizedSnapshotSpanCollection selectedSpans,
            Operation operation, CancellationToken cancellationToken)
        {
            var spanTrackingList = ArrayBuilder<CommentTrackingSpan>.GetInstance();
            var textChanges = ArrayBuilder<TextChange>.GetInstance();
            foreach (var span in selectedSpans)
            {
                if (operation == Operation.Comment)
                {
                    CommentSpan(document, service, span, textChanges, spanTrackingList, cancellationToken);
                }
                else
                {
                    UncommentSpan(document, service, span, textChanges, spanTrackingList, cancellationToken);
                }
            }

            return Task.FromResult(new CommentSelectionResult(textChanges.ToArrayAndFree(), spanTrackingList.ToArrayAndFree(), operation));
        }

        /// <summary>
        /// Add the necessary edits to comment out a single span.
        /// </summary>
        private void CommentSpan(
            Document document, ICommentSelectionService service, SnapshotSpan span,
            ArrayBuilder<TextChange> textChanges, ArrayBuilder<CommentTrackingSpan> trackingSpans, CancellationToken cancellationToken)
        {
            var (firstLine, lastLine) = DetermineFirstAndLastLine(span);

            if (span.IsEmpty && firstLine.IsEmptyOrWhitespace())
            {
                // No selection, and on an empty line, don't do anything.
                return;
            }

            if (!span.IsEmpty && string.IsNullOrWhiteSpace(span.GetText()))
            {
                // Just whitespace selected, don't do anything.
                return;
            }

            // Get the information from the language as to how they'd like to comment this region.
            var commentInfo = service.GetInfoAsync(document, span.Span.ToTextSpan(), cancellationToken).WaitAndGetResult(cancellationToken);
            if (!commentInfo.SupportsBlockComment && !commentInfo.SupportsSingleLineComment)
            {
                // Neither type of comment supported.
                return;
            }

            if (commentInfo.SupportsBlockComment && !commentInfo.SupportsSingleLineComment)
            {
                // Only block comments supported here.  If there is a span, just surround that
                // span with a block comment.  If tehre is no span then surround the entire line 
                // with a block comment.
                if (span.IsEmpty)
                {
                    var firstNonWhitespaceOnLine = firstLine.GetFirstNonWhitespacePosition();
                    var insertPosition = firstNonWhitespaceOnLine ?? firstLine.Start;

                    span = new SnapshotSpan(span.Snapshot, Span.FromBounds(insertPosition, firstLine.End));
                }

                AddBlockComment(span, textChanges, trackingSpans, commentInfo);
            }
            else if (!commentInfo.SupportsBlockComment && commentInfo.SupportsSingleLineComment)
            {
                // Only single line comments supported here.
                AddSingleLineComments(span, textChanges, trackingSpans, firstLine, lastLine, commentInfo);
            }
            else
            {
                // both comment forms supported.  Do a block comment only if a portion of code is
                // selected on a single line, otherwise comment out all the lines using single-line
                // comments.
                if (!span.IsEmpty &&
                    !SpanIncludesAllTextOnIncludedLines(span) &&
                    firstLine.LineNumber == lastLine.LineNumber)
                {
                    AddBlockComment(span, textChanges, trackingSpans, commentInfo);
                }
                else
                {
                    AddSingleLineComments(span, textChanges, trackingSpans, firstLine, lastLine, commentInfo);
                }
            }
        }

        private void AddSingleLineComments(SnapshotSpan span, ArrayBuilder<TextChange> textChanges, ArrayBuilder<CommentTrackingSpan> trackingSpans, ITextSnapshotLine firstLine, ITextSnapshotLine lastLine, CommentSelectionInfo commentInfo)
        {
            // Select the entirety of the lines, so that another comment operation will add more 
            // comments, not insert block comments.
            trackingSpans.Add(new CommentTrackingSpan(TextSpan.FromBounds(firstLine.Start.Position, lastLine.End.Position)));
            var indentToCommentAt = DetermineSmallestIndent(span, firstLine, lastLine);
            ApplySingleLineCommentToNonBlankLines(commentInfo, textChanges, firstLine, lastLine, indentToCommentAt);
        }

        private void AddBlockComment(SnapshotSpan span, ArrayBuilder<TextChange> textChanges, ArrayBuilder<CommentTrackingSpan> trackingSpans, CommentSelectionInfo commentInfo)
        {
            trackingSpans.Add(new CommentTrackingSpan(TextSpan.FromBounds(span.Start, span.End)));
            InsertText(textChanges, span.Start, commentInfo.BlockCommentStartString);
            InsertText(textChanges, span.End, commentInfo.BlockCommentEndString);
        }

        /// <summary>
        /// Add the necessary edits to uncomment out a single span.
        /// </summary>
        private void UncommentSpan(
            Document document, ICommentSelectionService service, SnapshotSpan span,
            ArrayBuilder<TextChange> textChanges, ArrayBuilder<CommentTrackingSpan> spansToSelect, CancellationToken cancellationToken)
        {
            var info = service.GetInfoAsync(document, span.Span.ToTextSpan(), cancellationToken).WaitAndGetResult(cancellationToken);

            // If the selection is exactly a block comment, use it as priority over single line comments.
            if (info.SupportsBlockComment && TryUncommentExactlyBlockComment(info, span, textChanges, spansToSelect))
            {
                return;
            }

            if (info.SupportsSingleLineComment &&
                TryUncommentSingleLineComments(info, span, textChanges, spansToSelect))
            {
                return;
            }

            // We didn't make any single line changes.  If the language supports block comments, see 
            // if we're inside a containing block comment and uncomment that.
            if (info.SupportsBlockComment)
            {
                UncommentContainingBlockComment(info, span, textChanges, spansToSelect);
            }
        }

        /// <summary>
        /// Check if the selected span matches an entire block comment.
        /// If it does, uncomment it and return true.
        /// </summary>
        private bool TryUncommentExactlyBlockComment(CommentSelectionInfo info, SnapshotSpan span, ArrayBuilder<TextChange> textChanges,
            ArrayBuilder<CommentTrackingSpan> spansToSelect)
        {
            var spanText = span.GetText();
            var trimmedSpanText = spanText.Trim();

            // See if the selection includes just a block comment (plus whitespace)
            if (trimmedSpanText.StartsWith(info.BlockCommentStartString, StringComparison.Ordinal) && trimmedSpanText.EndsWith(info.BlockCommentEndString, StringComparison.Ordinal))
            {
                var positionOfStart = span.Start + spanText.IndexOf(info.BlockCommentStartString, StringComparison.Ordinal);
                var positionOfEnd = span.Start + spanText.LastIndexOf(info.BlockCommentEndString, StringComparison.Ordinal);
                UncommentPosition(info, span, textChanges, spansToSelect, positionOfStart, positionOfEnd);
                return true;
            }

            return false;
        }

        private void UncommentContainingBlockComment(CommentSelectionInfo info, SnapshotSpan span, ArrayBuilder<TextChange> textChanges,
            ArrayBuilder<CommentTrackingSpan> spansToSelect)
        {
            // See if we are (textually) contained in a block comment.
            // This could allow a selection that spans multiple block comments to uncomment the beginning of
            // the first and end of the last.  Oh well.
            var positionOfEnd = -1;
            var text = span.Snapshot.AsText();
            var positionOfStart = text.LastIndexOf(info.BlockCommentStartString, span.Start, caseSensitive: true);

            // If we found a start comment marker, make sure there isn't an end comment marker after it but before our span.
            if (positionOfStart >= 0)
            {
                var lastEnd = text.LastIndexOf(info.BlockCommentEndString, span.Start, caseSensitive: true);
                if (lastEnd < positionOfStart)
                {
                    positionOfEnd = text.IndexOf(info.BlockCommentEndString, span.End, caseSensitive: true);
                }
                else if (lastEnd + info.BlockCommentEndString.Length > span.End)
                {
                    // The end of the span is *inside* the end marker, so searching backwards found it.
                    positionOfEnd = lastEnd;
                }
            }

            UncommentPosition(info, span, textChanges, spansToSelect, positionOfStart, positionOfEnd);
        }

        private void UncommentPosition(CommentSelectionInfo info, SnapshotSpan span, ArrayBuilder<TextChange> textChanges,
            ArrayBuilder<CommentTrackingSpan> spansToSelect, int positionOfStart, int positionOfEnd)
        {
            if (positionOfStart < 0 || positionOfEnd < 0)
            {
                return;
            }

            spansToSelect.Add(new CommentTrackingSpan(TextSpan.FromBounds(positionOfStart, positionOfEnd + info.BlockCommentEndString.Length)));
            DeleteText(textChanges, new TextSpan(positionOfStart, info.BlockCommentStartString.Length));
            DeleteText(textChanges, new TextSpan(positionOfEnd, info.BlockCommentEndString.Length));
        }

        private bool TryUncommentSingleLineComments(CommentSelectionInfo info, SnapshotSpan span, ArrayBuilder<TextChange> textChanges,
            ArrayBuilder<CommentTrackingSpan> spansToSelect)
        {
            // First see if we're selecting any lines that have the single-line comment prefix.
            // If so, then we'll just remove the single-line comment prefix from those lines.
            var (firstLine, lastLine) = DetermineFirstAndLastLine(span);

            for (var lineNumber = firstLine.LineNumber; lineNumber <= lastLine.LineNumber; ++lineNumber)
            {
                var line = span.Snapshot.GetLineFromLineNumber(lineNumber);
                var lineText = line.GetText();
                if (lineText.Trim().StartsWith(info.SingleLineCommentString, StringComparison.Ordinal))
                {
                    DeleteText(textChanges, new TextSpan(line.Start.Position + lineText.IndexOf(info.SingleLineCommentString, StringComparison.Ordinal), info.SingleLineCommentString.Length));
                }
            }

            // If we made any changes, select the entirety of the lines we change, so that subsequent invocations will
            // affect the same lines.
            if (textChanges.Count == 0)
            {
                return false;
            }

            spansToSelect.Add(new CommentTrackingSpan(TextSpan.FromBounds(firstLine.Start.Position, lastLine.End.Position)));
            return true;
        }

        /// <summary>
        /// Adds edits to comment out each non-blank line, at the given indent.
        /// </summary>
        private void ApplySingleLineCommentToNonBlankLines(
            CommentSelectionInfo info, ArrayBuilder<TextChange> textChanges, ITextSnapshotLine firstLine, ITextSnapshotLine lastLine, int indentToCommentAt)
        {
            var snapshot = firstLine.Snapshot;
            for (var lineNumber = firstLine.LineNumber; lineNumber <= lastLine.LineNumber; ++lineNumber)
            {
                var line = snapshot.GetLineFromLineNumber(lineNumber);
                if (!line.IsEmptyOrWhitespace())
                {
                    InsertText(textChanges, line.Start + indentToCommentAt, info.SingleLineCommentString);
                }
            }
        }

        /// <summary>
        /// Given a span, find the first and last line that are part of the span.  NOTE: If the 
        /// span ends in column zero, we back up to the previous line, to handle the case where 
        /// the user used shift + down to select a bunch of lines.  They probably don't want the 
        /// last line commented in that case.
        /// </summary>
        private static (ITextSnapshotLine firstLine, ITextSnapshotLine lastLine) DetermineFirstAndLastLine(SnapshotSpan span)
        {
            var firstLine = span.Snapshot.GetLineFromPosition(span.Start.Position);
            var lastLine = span.Snapshot.GetLineFromPosition(span.End.Position);
            if (lastLine.Start == span.End.Position && !span.IsEmpty)
            {
                lastLine = lastLine.GetPreviousMatchingLine(_ => true);
            }

            return (firstLine, lastLine);
        }

        /// <summary>
        /// Returns true if the span includes all of the non-whitespace text on the first and last line.
        /// </summary>
        private static bool SpanIncludesAllTextOnIncludedLines(SnapshotSpan span)
        {
            var (firstLine, lastLine) = DetermineFirstAndLastLine(span);

            var firstNonWhitespacePosition = firstLine.GetFirstNonWhitespacePosition();
            var lastNonWhitespacePosition = lastLine.GetLastNonWhitespacePosition();

            var allOnFirst = !firstNonWhitespacePosition.HasValue ||
                              span.Start.Position <= firstNonWhitespacePosition.Value;
            var allOnLast = !lastNonWhitespacePosition.HasValue ||
                             span.End.Position > lastNonWhitespacePosition.Value;

            return allOnFirst && allOnLast;
        }
    }
}
