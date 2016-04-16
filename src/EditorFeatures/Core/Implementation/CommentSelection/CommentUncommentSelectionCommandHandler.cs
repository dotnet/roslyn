// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CommentSelection
{
    [ExportCommandHandler(PredefinedCommandHandlerNames.CommentSelection, ContentTypeNames.RoslynContentType)]
    internal class CommentUncommentSelectionCommandHandler :
        ICommandHandler<CommentSelectionCommandArgs>,
        ICommandHandler<UncommentSelectionCommandArgs>
    {
        private readonly IWaitIndicator _waitIndicator;
        private readonly ITextUndoHistoryRegistry _undoHistoryRegistry;
        private readonly IEditorOperationsFactoryService _editorOperationsFactoryService;

        [ImportingConstructor]
        internal CommentUncommentSelectionCommandHandler(
            IWaitIndicator waitIndicator,
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IEditorOperationsFactoryService editorOperationsFactoryService)
        {
            Contract.ThrowIfNull(waitIndicator);
            Contract.ThrowIfNull(undoHistoryRegistry);
            Contract.ThrowIfNull(editorOperationsFactoryService);

            _waitIndicator = waitIndicator;
            _undoHistoryRegistry = undoHistoryRegistry;
            _editorOperationsFactoryService = editorOperationsFactoryService;
        }

        private static CommandState GetCommandState(ITextBuffer buffer, Func<CommandState> nextHandler)
        {
            if (!buffer.CanApplyChangeDocumentToWorkspace())
            {
                return nextHandler();
            }

            return CommandState.Available;
        }

        public CommandState GetCommandState(CommentSelectionCommandArgs args, Func<CommandState> nextHandler)
        {
            return GetCommandState(args.SubjectBuffer, nextHandler);
        }

        /// <summary>
        /// Comment the selected spans, and reset the selection.
        /// </summary>
        public void ExecuteCommand(CommentSelectionCommandArgs args, Action nextHandler)
        {
            this.ExecuteCommand(args.TextView, args.SubjectBuffer, Operation.Comment);
        }

        public CommandState GetCommandState(UncommentSelectionCommandArgs args, Func<CommandState> nextHandler)
        {
            return GetCommandState(args.SubjectBuffer, nextHandler);
        }

        /// <summary>
        /// Uncomment the selected spans, and reset the selection.
        /// </summary>
        public void ExecuteCommand(UncommentSelectionCommandArgs args, Action nextHandler)
        {
            this.ExecuteCommand(args.TextView, args.SubjectBuffer, Operation.Uncomment);
        }

        internal void ExecuteCommand(ITextView textView, ITextBuffer subjectBuffer, Operation operation)
        {
            var title = operation == Operation.Comment ? EditorFeaturesResources.CommentSelection
                                                       : EditorFeaturesResources.UncommentSelection;

            var message = operation == Operation.Comment ? EditorFeaturesResources.CommentingCurrentlySelected
                                                         : EditorFeaturesResources.UncommentingCurrentlySelected;

            _waitIndicator.Wait(
                title,
                message,
                allowCancel: false,
                action: waitContext =>
                {
                    var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
                    if (document == null)
                    {
                        return;
                    }

                    var service = document.GetLanguageService<ICommentUncommentService>();
                    if (service == null)
                    {
                        return;
                    }

                    var trackingSpans = new List<ITrackingSpan>();
                    var textChanges = new List<TextChange>();

                    CollectEdits(service, textView.Selection.GetSnapshotSpansOnBuffer(subjectBuffer), textChanges, trackingSpans, operation);

                    using (var transaction = new CaretPreservingEditTransaction(title, textView, _undoHistoryRegistry, _editorOperationsFactoryService))
                    {
                        document.Project.Solution.Workspace.ApplyTextChanges(document.Id, textChanges, waitContext.CancellationToken);
                        transaction.Complete();
                    }

                    if (operation == Operation.Uncomment)
                    {
                        using (var transaction = new CaretPreservingEditTransaction(title, textView, _undoHistoryRegistry, _editorOperationsFactoryService))
                        {
                            Format(service, subjectBuffer.CurrentSnapshot, trackingSpans, waitContext.CancellationToken);
                            transaction.Complete();
                        }
                    }

                    if (trackingSpans.Any())
                    {
                        // TODO, this doesn't currently handle block selection
                        textView.SetSelection(trackingSpans.First().GetSpan(subjectBuffer.CurrentSnapshot));
                    }
                });
        }

        private void Format(ICommentUncommentService service, ITextSnapshot snapshot, IEnumerable<ITrackingSpan> changes, CancellationToken cancellationToken)
        {
            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return;
            }

            var textSpans = changes.Select(s => s.GetSpan(snapshot)).Select(s => s.Span.ToTextSpan()).ToList();
            var newDocument = service.Format(document, textSpans, cancellationToken);
            newDocument.Project.Solution.Workspace.ApplyDocumentChanges(newDocument, cancellationToken);
        }

        internal enum Operation { Comment, Uncomment }

        /// <summary>
        /// Add the necessary edits to the given spans. Also collect tracking spans over each span.
        ///
        /// Internal so that it can be called by unit tests.
        /// </summary>
        internal void CollectEdits(ICommentUncommentService service, NormalizedSnapshotSpanCollection selectedSpans, List<TextChange> textChanges, List<ITrackingSpan> trackingSpans, Operation operation)
        {
            foreach (var span in selectedSpans)
            {
                if (operation == Operation.Comment)
                {
                    CommentSpan(service, span, textChanges, trackingSpans);
                }
                else
                {
                    UncommentSpan(service, span, textChanges, trackingSpans);
                }
            }
        }

        /// <summary>
        /// Add the necessary edits to comment out a single span.
        /// </summary>
        private void CommentSpan(ICommentUncommentService service, SnapshotSpan span, List<TextChange> textChanges, List<ITrackingSpan> trackingSpans)
        {
            var firstAndLastLine = DetermineFirstAndLastLine(span);

            if (span.IsEmpty && firstAndLastLine.Item1.IsEmptyOrWhitespace())
            {
                return;
            }

            if (!span.IsEmpty && string.IsNullOrWhiteSpace(span.GetText()))
            {
                return;
            }

            if (span.IsEmpty || string.IsNullOrWhiteSpace(span.GetText()))
            {
                var firstNonWhitespaceOnLine = firstAndLastLine.Item1.GetFirstNonWhitespacePosition();
                var insertPosition = firstNonWhitespaceOnLine.HasValue
                    ? firstNonWhitespaceOnLine.Value
                    : firstAndLastLine.Item1.Start;

                // If there isn't a selection, we select the whole line
                trackingSpans.Add(span.Snapshot.CreateTrackingSpan(Span.FromBounds(firstAndLastLine.Item1.Start, firstAndLastLine.Item1.End), SpanTrackingMode.EdgeInclusive));
                InsertText(textChanges, insertPosition, service.SingleLineCommentString);
            }
            else
            {
                if (service.SupportsBlockComment &&
                    !SpanIncludesAllTextOnIncludedLines(span) &&
                    firstAndLastLine.Item1.LineNumber == firstAndLastLine.Item2.LineNumber)
                {
                    trackingSpans.Add(span.Snapshot.CreateTrackingSpan(span, SpanTrackingMode.EdgeInclusive));
                    InsertText(textChanges, span.Start, service.BlockCommentStartString);
                    InsertText(textChanges, span.End, service.BlockCommentEndString);
                }
                else
                {
                    // Select the entirety of the lines, so that another comment operation will add more comments, not insert block comments.
                    trackingSpans.Add(span.Snapshot.CreateTrackingSpan(Span.FromBounds(firstAndLastLine.Item1.Start.Position, firstAndLastLine.Item2.End.Position), SpanTrackingMode.EdgeInclusive));
                    var indentToCommentAt = DetermineSmallestIndent(span, firstAndLastLine);
                    ApplyCommentToNonBlankLines(service, textChanges, firstAndLastLine, indentToCommentAt);
                }
            }
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
        private void UncommentSpan(ICommentUncommentService service, SnapshotSpan span, List<TextChange> textChanges, List<ITrackingSpan> spansToSelect)
        {
            if (TryUncommentSingleLineComments(service, span, textChanges, spansToSelect))
            {
                return;
            }

            TryUncommentContainingBlockComment(service, span, textChanges, spansToSelect);
        }

        private bool TryUncommentContainingBlockComment(ICommentUncommentService service, SnapshotSpan span, List<TextChange> textChanges, List<ITrackingSpan> spansToSelect)
        {
            // We didn't make any single line changes.  If the language supports block comments, see 
            // if we're inside a containing block comment and uncomment that.

            if (!service.SupportsBlockComment)
            {
                return false;
            }

            var positionOfStart = -1;
            var positionOfEnd = -1;
            var spanText = span.GetText();
            var trimmedSpanText = spanText.Trim();

            // See if the selection includes just a block comment (plus whitespace)
            if (trimmedSpanText.StartsWith(service.BlockCommentStartString, StringComparison.Ordinal) && trimmedSpanText.EndsWith(service.BlockCommentEndString, StringComparison.Ordinal))
            {
                positionOfStart = span.Start + spanText.IndexOf(service.BlockCommentStartString, StringComparison.Ordinal);
                positionOfEnd = span.Start + spanText.LastIndexOf(service.BlockCommentEndString, StringComparison.Ordinal);
            }
            else
            {
                // See if we are (textually) contained in a block comment.
                // This could allow a selection that spans multiple block comments to uncomment the beginning of
                // the first and end of the last.  Oh well.
                var text = span.Snapshot.AsText();
                positionOfStart = text.LastIndexOf(service.BlockCommentStartString, span.Start, caseSensitive: true);

                // If we found a start comment marker, make sure there isn't an end comment marker after it but before our span.
                if (positionOfStart >= 0)
                {
                    var lastEnd = text.LastIndexOf(service.BlockCommentEndString, span.Start, caseSensitive: true);
                    if (lastEnd < positionOfStart)
                    {
                        positionOfEnd = text.IndexOf(service.BlockCommentEndString, span.End, caseSensitive: true);
                    }
                    else if (lastEnd + service.BlockCommentEndString.Length > span.End)
                    {
                        // The end of the span is *inside* the end marker, so searching backwards found it.
                        positionOfEnd = lastEnd;
                    }
                }
            }

            if (positionOfStart < 0 || positionOfEnd < 0)
            {
                return false;
            }

            spansToSelect.Add(span.Snapshot.CreateTrackingSpan(Span.FromBounds(positionOfStart, positionOfEnd + service.BlockCommentEndString.Length), SpanTrackingMode.EdgeExclusive));
            DeleteText(textChanges, new TextSpan(positionOfStart, service.BlockCommentStartString.Length));
            DeleteText(textChanges, new TextSpan(positionOfEnd, service.BlockCommentEndString.Length));
            return true;
        }

        private bool TryUncommentSingleLineComments(ICommentUncommentService service, SnapshotSpan span, List<TextChange> textChanges, List<ITrackingSpan> spansToSelect)
        {
            // First see if we're selecting any lines that have the single-line comment prefix.
            // If so, then we'll just remove the single-line comment prefix from those lines.
            var firstAndLastLine = DetermineFirstAndLastLine(span);
            for (int lineNumber = firstAndLastLine.Item1.LineNumber; lineNumber <= firstAndLastLine.Item2.LineNumber; ++lineNumber)
            {
                var line = span.Snapshot.GetLineFromLineNumber(lineNumber);
                var lineText = line.GetText();
                if (lineText.Trim().StartsWith(service.SingleLineCommentString, StringComparison.Ordinal))
                {
                    DeleteText(textChanges, new TextSpan(line.Start.Position + lineText.IndexOf(service.SingleLineCommentString, StringComparison.Ordinal), service.SingleLineCommentString.Length));
                }
            }

            // If we made any changes, select the entirety of the lines we change, so that subsequent invocations will
            // affect the same lines.
            if (!textChanges.Any())
            {
                return false;
            }

            spansToSelect.Add(span.Snapshot.CreateTrackingSpan(Span.FromBounds(firstAndLastLine.Item1.Start.Position,
                                                                               firstAndLastLine.Item2.End.Position),
                                                               SpanTrackingMode.EdgeExclusive));
            return true;
        }

        /// <summary>
        /// Adds edits to comment out each non-blank line, at the given indent.
        /// </summary>
        private void ApplyCommentToNonBlankLines(ICommentUncommentService service, List<TextChange> textChanges, Tuple<ITextSnapshotLine, ITextSnapshotLine> firstAndLastLine, int indentToCommentAt)
        {
            for (int lineNumber = firstAndLastLine.Item1.LineNumber; lineNumber <= firstAndLastLine.Item2.LineNumber; ++lineNumber)
            {
                var line = firstAndLastLine.Item1.Snapshot.GetLineFromLineNumber(lineNumber);
                if (!line.IsEmptyOrWhitespace())
                {
                    InsertText(textChanges, line.Start + indentToCommentAt, service.SingleLineCommentString);
                }
            }
        }

        /// <summary> Given a set of lines, find the minimum indent of all of the non-blank, non-whitespace lines.</summary>
        private static int DetermineSmallestIndent(SnapshotSpan span, Tuple<ITextSnapshotLine, ITextSnapshotLine> firstAndLastLine)
        {
            // TODO: This breaks if you have mixed tabs/spaces, and/or tabsize != indentsize.
            var indentToCommentAt = int.MaxValue;
            for (int lineNumber = firstAndLastLine.Item1.LineNumber; lineNumber <= firstAndLastLine.Item2.LineNumber; ++lineNumber)
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
        /// Given a span, find the first and last line that are part of the span.  NOTE: If the span ends in column zero,
        /// we back up to the previous line, to handle the case where the user used shift + down to select a bunch of
        /// lines.  They probably don't want the last line commented in that case.
        /// </summary>
        private static Tuple<ITextSnapshotLine, ITextSnapshotLine> DetermineFirstAndLastLine(SnapshotSpan span)
        {
            var firstLine = span.Snapshot.GetLineFromPosition(span.Start.Position);
            var lastLine = span.Snapshot.GetLineFromPosition(span.End.Position);
            if (lastLine.Start == span.End.Position && !span.IsEmpty)
            {
                lastLine = lastLine.GetPreviousMatchingLine(_ => true);
            }

            return Tuple.Create(firstLine, lastLine);
        }

        /// <summary>
        /// Returns true if the span includes all of the non-whitespace text on the first and last line.
        /// </summary>
        private static bool SpanIncludesAllTextOnIncludedLines(SnapshotSpan span)
        {
            var firstAndLastLine = DetermineFirstAndLastLine(span);

            var firstNonWhitespacePosition = firstAndLastLine.Item1.GetFirstNonWhitespacePosition();
            var lastNonWhitespacePosition = firstAndLastLine.Item2.GetLastNonWhitespacePosition();

            var allOnFirst = !firstNonWhitespacePosition.HasValue ||
                              span.Start.Position <= firstNonWhitespacePosition.Value;
            var allOnLast = !lastNonWhitespacePosition.HasValue ||
                             span.End.Position > lastNonWhitespacePosition.Value;

            return allOnFirst && allOnLast;
        }
    }
}
