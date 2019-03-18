// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommentSelection;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Utilities;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CommentSelection
{
    internal enum Operation
    {
        /// <summary>
        /// The operation is a comment action.
        /// </summary>
        Comment,

        /// <summary>
        /// The operation is an uncomment action.
        /// </summary>
        Uncomment
    }

    internal abstract class AbstractCommentSelectionBase<TCommand>
    {
        private readonly ITextUndoHistoryRegistry _undoHistoryRegistry;
        private readonly IEditorOperationsFactoryService _editorOperationsFactoryService;

        internal AbstractCommentSelectionBase(
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IEditorOperationsFactoryService editorOperationsFactoryService)
        {
            Contract.ThrowIfNull(undoHistoryRegistry);
            Contract.ThrowIfNull(editorOperationsFactoryService);

            _undoHistoryRegistry = undoHistoryRegistry;
            _editorOperationsFactoryService = editorOperationsFactoryService;
        }

        public abstract string DisplayName { get; }

        protected abstract string GetTitle(TCommand command);

        protected abstract string GetMessage(TCommand command);

        // Internal as tests currently rely on this method.
        internal abstract Task<CommentSelectionResult> CollectEdits(
            Document document, ICommentSelectionService service, NormalizedSnapshotSpanCollection selectedSpans,
            TCommand command, CancellationToken cancellationToken);

        protected static VSCommanding.CommandState GetCommandState(ITextBuffer buffer)
        {
            return buffer.CanApplyChangeDocumentToWorkspace()
                ? VSCommanding.CommandState.Available
                : VSCommanding.CommandState.Unspecified;
        }

        protected static void InsertText(List<TextChange> textChanges, int position, string text)
        {
            textChanges.Add(new TextChange(new TextSpan(position, 0), text));
        }

        protected static void DeleteText(List<TextChange> textChanges, TextSpan span)
        {
            textChanges.Add(new TextChange(span, string.Empty));
        }

        internal bool ExecuteCommand(ITextView textView, ITextBuffer subjectBuffer, TCommand command, CommandExecutionContext context)
        {
            var title = GetTitle(command);
            var message = GetMessage(command);

            using (context.OperationContext.AddScope(allowCancellation: true, message))
            {
                var cancellationToken = context.OperationContext.UserCancellationToken;

                var selectedSpans = textView.Selection.GetSnapshotSpansOnBuffer(subjectBuffer);
                if (selectedSpans.IsEmpty())
                {
                    return true;
                }

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

                var edits = CollectEdits(document, service, selectedSpans, command, cancellationToken).WaitAndGetResult(cancellationToken);

                ApplyEdits(document, textView, subjectBuffer, service, title, edits);
            }

            return true;
        }

        /// <summary>
        /// Applies the requested edits and sets the selection.
        /// This operation is not cancellable.
        /// </summary>
        private void ApplyEdits(Document document, ITextView textView, ITextBuffer subjectBuffer,
            ICommentSelectionService service, string title, CommentSelectionResult edits)
        {
            // Create tracking spans to track the text changes.
            var currentSnapshot = subjectBuffer.CurrentSnapshot;
            var trackingSpans = edits.TrackingSpans
                .Select(textSpan => (originalSpan: textSpan, trackingSpan: CreateTrackingSpan(edits.ResultOperation, currentSnapshot, textSpan.TrackingTextSpan)))
                .ToImmutableList();

            // Apply the text changes.
            using (var transaction = new CaretPreservingEditTransaction(title, textView, _undoHistoryRegistry, _editorOperationsFactoryService))
            {
                document.Project.Solution.Workspace.ApplyTextChanges(document.Id, edits.TextChanges.Distinct(), CancellationToken.None);
                transaction.Complete();
            }

            // Convert the tracking spans into snapshot spans for formatting and selection.
            var trackingSnapshotSpans = trackingSpans.Select(s => CreateSnapshotSpan(subjectBuffer.CurrentSnapshot, s.trackingSpan, s.originalSpan));

            if (trackingSnapshotSpans.Any())
            {
                if (edits.ResultOperation == Operation.Uncomment)
                {
                    // Format the document only during uncomment operations.  Use second transaction so it can be undone.
                    using (var transaction = new CaretPreservingEditTransaction(title, textView, _undoHistoryRegistry, _editorOperationsFactoryService))
                    {
                        Format(service, subjectBuffer.CurrentSnapshot, trackingSnapshotSpans, CancellationToken.None);
                        transaction.Complete();
                    }
                }

                // Set the selection after the edits have been applied.
                var spansToSelect = trackingSnapshotSpans.Select(s => new Selection(s));
                textView.GetMultiSelectionBroker().SetSelectionRange(spansToSelect, spansToSelect.Last());
            }
        }

        /// <summary>
        /// Creates a tracking span for the operation.
        /// Internal for tests.
        /// </summary>
        internal static ITrackingSpan CreateTrackingSpan(Operation operation, ITextSnapshot snapshot, TextSpan textSpan)
        {
            var spanTrackingMode = operation == Operation.Comment
                ? SpanTrackingMode.EdgeInclusive
                : SpanTrackingMode.EdgeExclusive;
            return snapshot.CreateTrackingSpan(Span.FromBounds(textSpan.Start, textSpan.End), spanTrackingMode);
        }

        /// <summary>
        /// Retrieves the snapshot span from a post edited tracking span.
        /// Additionally applies any extra modifications to the tracking span post edit.
        /// Internal for tests.
        /// </summary>
        private static SnapshotSpan CreateSnapshotSpan(ITextSnapshot snapshot, ITrackingSpan trackingSpan, CommentTrackingSpan originalSpan)
        {
            var snapshotSpan = trackingSpan.GetSpan(snapshot);
            if (originalSpan.HasPostApplyChanges())
            {
                var updatedStart = snapshotSpan.Start.Position + originalSpan.AmountToAddToStart;
                var updatedEnd = snapshotSpan.End.Position + originalSpan.AmountToAddToEnd;
                if (updatedStart >= snapshotSpan.Start.Position && updatedEnd <= snapshotSpan.End.Position)
                {
                    snapshotSpan = new SnapshotSpan(snapshot, Span.FromBounds(updatedStart, updatedEnd));
                }
            }

            return snapshotSpan;
        }

        private static void Format(ICommentSelectionService service, ITextSnapshot snapshot, IEnumerable<SnapshotSpan> changes, CancellationToken cancellationToken)
        {
            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return;
            }

            var textSpans = changes.Select(change => change.Span.ToTextSpan()).ToImmutableArray();
            var newDocument = service.FormatAsync(document, textSpans, cancellationToken).WaitAndGetResult(cancellationToken);
            newDocument.Project.Solution.Workspace.ApplyDocumentChanges(newDocument, cancellationToken);
        }

        private static ICommentSelectionService GetService(Document document)
        {
            var service = document.GetLanguageService<ICommentSelectionService>();
            if (service != null)
            {
                return service;
            }

            return null;
        }
    }
}
