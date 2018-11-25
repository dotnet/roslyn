﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CommentSelection;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
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

namespace Microsoft.CodeAnalysis.Editor.Implementation.CommentSelection
{
    [Export(typeof(VSCommanding.ICommandHandler))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name(PredefinedCommandHandlerNames.CommentSelection)]
    internal class CommentUncommentSelectionCommandHandler :
        VSCommanding.ICommandHandler<CommentSelectionCommandArgs>,
        VSCommanding.ICommandHandler<UncommentSelectionCommandArgs>
    {
        private readonly ITextUndoHistoryRegistry _undoHistoryRegistry;
        private readonly IEditorOperationsFactoryService _editorOperationsFactoryService;

        [ImportingConstructor]
        internal CommentUncommentSelectionCommandHandler(
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IEditorOperationsFactoryService editorOperationsFactoryService)
        {
            Contract.ThrowIfNull(undoHistoryRegistry);
            Contract.ThrowIfNull(editorOperationsFactoryService);

            _undoHistoryRegistry = undoHistoryRegistry;
            _editorOperationsFactoryService = editorOperationsFactoryService;
        }

        public string DisplayName => EditorFeaturesResources.Comment_Uncomment_Selection;

        private static VSCommanding.CommandState GetCommandState(ITextBuffer buffer)
        {
            if (!buffer.CanApplyChangeDocumentToWorkspace())
            {
                return VSCommanding.CommandState.Unspecified;
            }

            return VSCommanding.CommandState.Available;
        }

        public VSCommanding.CommandState GetCommandState(CommentSelectionCommandArgs args)
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

        public VSCommanding.CommandState GetCommandState(UncommentSelectionCommandArgs args)
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

        internal bool ExecuteCommand(ITextView textView, ITextBuffer subjectBuffer, Operation operation, CommandExecutionContext context)
        {
            var title = operation == Operation.Comment ? EditorFeaturesResources.Comment_Selection
                                                       : EditorFeaturesResources.Uncomment_Selection;

            var message = operation == Operation.Comment ? EditorFeaturesResources.Commenting_currently_selected_text
                                                         : EditorFeaturesResources.Uncommenting_currently_selected_text;

            using (context.OperationContext.AddScope(allowCancellation: false, message))
            {

                var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
                if (document == null)
                {
                    return true;
                }

                var service = GetService(document);
                if (service == null)
                {
                    return true;
                }

                var trackingSpans = new List<ITrackingSpan>();
                var textChanges = new List<TextChange>();
                CollectEdits(
                    document, service, textView.Selection.GetSnapshotSpansOnBuffer(subjectBuffer),
                    textChanges, trackingSpans, operation, CancellationToken.None);

                using (var transaction = new CaretPreservingEditTransaction(title, textView, _undoHistoryRegistry, _editorOperationsFactoryService))
                {
                    document.Project.Solution.Workspace.ApplyTextChanges(document.Id, textChanges, CancellationToken.None);
                    transaction.Complete();
                }

                if (operation == Operation.Uncomment)
                {
                    using (var transaction = new CaretPreservingEditTransaction(title, textView, _undoHistoryRegistry, _editorOperationsFactoryService))
                    {
                        Format(service, subjectBuffer.CurrentSnapshot, trackingSpans, CancellationToken.None);
                        transaction.Complete();
                    }
                }

                if (trackingSpans.Any())
                {
                    // TODO, this doesn't currently handle block selection
                    textView.SetSelection(trackingSpans.First().GetSpan(subjectBuffer.CurrentSnapshot));
                }
            }

            return true;
        }

        private ICommentSelectionService GetService(Document document)
        {
            // First, try to get the new service for comment selection.
            var service = document.GetLanguageService<ICommentSelectionService>();
            if (service != null)
            {
                return service;
            }

            // If we couldn't find one, fallback to the legacy service.
#pragma warning disable CS0618 // Type or member is obsolete
            var legacyService = document.GetLanguageService<ICommentUncommentService>();
#pragma warning restore CS0618 // Type or member is obsolete
            if (legacyService != null)
            {
                return new CommentSelectionServiceProxy(legacyService);
            }

            return null;
        }

        private void Format(ICommentSelectionService service, ITextSnapshot snapshot, IEnumerable<ITrackingSpan> changes, CancellationToken cancellationToken)
        {
            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return;
            }

            var textSpans = changes.Select(s => s.GetSpan(snapshot).Span.ToTextSpan()).ToImmutableArray();
            var newDocument = service.FormatAsync(document, textSpans, cancellationToken).WaitAndGetResult(cancellationToken);
            newDocument.Project.Solution.Workspace.ApplyDocumentChanges(newDocument, cancellationToken);
        }

        internal enum Operation { Comment, Uncomment }

        /// <summary>
        /// Add the necessary edits to the given spans. Also collect tracking spans over each span.
        ///
        /// Internal so that it can be called by unit tests.
        /// </summary>
        internal void CollectEdits(
            Document document, ICommentSelectionService service, NormalizedSnapshotSpanCollection selectedSpans, 
            List<TextChange> textChanges, List<ITrackingSpan> trackingSpans, Operation operation, CancellationToken cancellationToken)
        {
            foreach (var span in selectedSpans)
            {
                if (operation == Operation.Comment)
                {
                    CommentSpan(document, service, span, textChanges, trackingSpans, cancellationToken);
                }
                else
                {
                    UncommentSpan(document, service, span, textChanges, trackingSpans, cancellationToken);
                }
            }
        }

        /// <summary>
        /// Add the necessary edits to comment out a single span.
        /// </summary>
        private void CommentSpan(
            Document document, ICommentSelectionService service, SnapshotSpan span, 
            List<TextChange> textChanges, List<ITrackingSpan> trackingSpans, CancellationToken cancellationToken)
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

        private void AddSingleLineComments(SnapshotSpan span, List<TextChange> textChanges, List<ITrackingSpan> trackingSpans, ITextSnapshotLine firstLine, ITextSnapshotLine lastLine, CommentSelectionInfo commentInfo)
        {
            // Select the entirety of the lines, so that another comment operation will add more 
            // comments, not insert block comments.
            trackingSpans.Add(span.Snapshot.CreateTrackingSpan(Span.FromBounds(firstLine.Start.Position, lastLine.End.Position), SpanTrackingMode.EdgeInclusive));
            var indentToCommentAt = DetermineSmallestIndent(span, firstLine, lastLine);
            ApplySingleLineCommentToNonBlankLines(commentInfo, textChanges, firstLine, lastLine, indentToCommentAt);
        }

        private void AddBlockComment(SnapshotSpan span, List<TextChange> textChanges, List<ITrackingSpan> trackingSpans, CommentSelectionInfo commentInfo)
        {
            trackingSpans.Add(span.Snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeInclusive));
            InsertText(textChanges, span.Start, commentInfo.BlockCommentStartString);
            InsertText(textChanges, span.End, commentInfo.BlockCommentEndString);
        }

        /// <summary>
        /// Record "Insert text" text changes.
        /// </summary>
        private void InsertText(List<TextChange> textChanges, int position, string text)
        {
            textChanges.Add(new TextChange(new TextSpan(position, 0), text));
        }

        /// <summary>
        /// Record "Delete text" text changes.
        /// </summary>
        private void DeleteText(List<TextChange> textChanges, TextSpan span)
        {
            textChanges.Add(new TextChange(span, string.Empty));
        }

        /// <summary>
        /// Add the necessary edits to uncomment out a single span.
        /// </summary>
        private void UncommentSpan(
            Document document, ICommentSelectionService service, SnapshotSpan span, 
            List<TextChange> textChanges, List<ITrackingSpan> spansToSelect, CancellationToken cancellationToken)
        {
            var info = service.GetInfoAsync(document, span.Span.ToTextSpan(), cancellationToken).WaitAndGetResult(cancellationToken);

            if (info.SupportsSingleLineComment &&
                TryUncommentSingleLineComments(info, span, textChanges, spansToSelect))
            {
                return;
            }

            if (info.SupportsBlockComment)
            {
                UncommentContainingBlockComment(info, span, textChanges, spansToSelect);
            }
        }

        private void UncommentContainingBlockComment(CommentSelectionInfo info, SnapshotSpan span, List<TextChange> textChanges, List<ITrackingSpan> spansToSelect)
        {
            // We didn't make any single line changes.  If the language supports block comments, see 
            // if we're inside a containing block comment and uncomment that.

            var positionOfStart = -1;
            var positionOfEnd = -1;
            var spanText = span.GetText();
            var trimmedSpanText = spanText.Trim();

            // See if the selection includes just a block comment (plus whitespace)
            if (trimmedSpanText.StartsWith(info.BlockCommentStartString, StringComparison.Ordinal) && trimmedSpanText.EndsWith(info.BlockCommentEndString, StringComparison.Ordinal))
            {
                positionOfStart = span.Start + spanText.IndexOf(info.BlockCommentStartString, StringComparison.Ordinal);
                positionOfEnd = span.Start + spanText.LastIndexOf(info.BlockCommentEndString, StringComparison.Ordinal);
            }
            else
            {
                // See if we are (textually) contained in a block comment.
                // This could allow a selection that spans multiple block comments to uncomment the beginning of
                // the first and end of the last.  Oh well.
                var text = span.Snapshot.AsText();
                positionOfStart = text.LastIndexOf(info.BlockCommentStartString, span.Start, caseSensitive: true);

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
            }

            if (positionOfStart < 0 || positionOfEnd < 0)
            {
                return;
            }

            spansToSelect.Add(span.Snapshot.CreateTrackingSpan(Span.FromBounds(positionOfStart, positionOfEnd + info.BlockCommentEndString.Length), SpanTrackingMode.EdgeExclusive));
            DeleteText(textChanges, new TextSpan(positionOfStart, info.BlockCommentStartString.Length));
            DeleteText(textChanges, new TextSpan(positionOfEnd, info.BlockCommentEndString.Length));
        }

        private bool TryUncommentSingleLineComments(CommentSelectionInfo info, SnapshotSpan span, List<TextChange> textChanges, List<ITrackingSpan> spansToSelect)
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

            spansToSelect.Add(span.Snapshot.CreateTrackingSpan(Span.FromBounds(firstLine.Start.Position,
                                                                               lastLine.End.Position),
                                                               SpanTrackingMode.EdgeExclusive));
            return true;
        }

        /// <summary>
        /// Adds edits to comment out each non-blank line, at the given indent.
        /// </summary>
        private void ApplySingleLineCommentToNonBlankLines(
            CommentSelectionInfo info, List<TextChange> textChanges, ITextSnapshotLine firstLine, ITextSnapshotLine lastLine, int indentToCommentAt)
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

        /// <summary> Given a set of lines, find the minimum indent of all of the non-blank, non-whitespace lines.</summary>
        private static int DetermineSmallestIndent(
            SnapshotSpan span, ITextSnapshotLine firstLine, ITextSnapshotLine lastLine)
        {
            // TODO: This breaks if you have mixed tabs/spaces, and/or tabsize != indentsize.
            var indentToCommentAt = int.MaxValue;
            for (var lineNumber = firstLine.LineNumber; lineNumber <= lastLine.LineNumber; ++lineNumber)
            {
                var line = span.Snapshot.GetLineFromLineNumber(lineNumber);
                var firstNonWhitespacePosition = line.GetFirstNonWhitespacePosition();
                var firstNonWhitespaceOnLine = firstNonWhitespacePosition.HasValue
                    ? firstNonWhitespacePosition.Value - line.Start
                    : int.MaxValue;
                indentToCommentAt = Math.Min(indentToCommentAt, firstNonWhitespaceOnLine);
            }

            return indentToCommentAt;
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
            var firstAndLastLine = DetermineFirstAndLastLine(span);

            var firstNonWhitespacePosition = firstAndLastLine.firstLine.GetFirstNonWhitespacePosition();
            var lastNonWhitespacePosition = firstAndLastLine.lastLine.GetLastNonWhitespacePosition();

            var allOnFirst = !firstNonWhitespacePosition.HasValue ||
                              span.Start.Position <= firstNonWhitespacePosition.Value;
            var allOnLast = !lastNonWhitespacePosition.HasValue ||
                             span.End.Position > lastNonWhitespacePosition.Value;

            return allOnFirst && allOnLast;
        }
    }
}
