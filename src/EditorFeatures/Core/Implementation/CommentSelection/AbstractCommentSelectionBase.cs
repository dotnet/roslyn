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
        Uncomment,

        /// <summary>
        /// The operation is not yet determined.
        /// </summary>
        Undefined
    }

    internal abstract class AbstractCommentSelectionBase
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

        protected abstract string GetTitle(Operation operation);

        protected abstract string GetMessage(Operation operation);

        internal abstract Task<CommentSelectionResult> CollectEdits(
            Document document, ICommentSelectionService service, NormalizedSnapshotSpanCollection selectedSpans,
            Operation operation, CancellationToken cancellationToken);

        protected static VSCommanding.CommandState GetCommandState(ITextBuffer buffer)
        {
            return buffer.CanApplyChangeDocumentToWorkspace()
                ? VSCommanding.CommandState.Available
                : VSCommanding.CommandState.Unspecified;
        }

        protected static void Format(ICommentSelectionService service, ITextSnapshot snapshot, IEnumerable<CommentTrackingSpan> changes, CancellationToken cancellationToken)
        {
            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return;
            }

            var textSpans = changes
                .Select(change => change.ToSnapshotSpan(snapshot).Span.ToTextSpan())
                .ToImmutableArray();
            var newDocument = service.FormatAsync(document, textSpans, cancellationToken).WaitAndGetResult(cancellationToken);
            newDocument.Project.Solution.Workspace.ApplyDocumentChanges(newDocument, cancellationToken);
        }

        protected static void InsertText(List<TextChange> textChanges, int position, string text)
        {
            textChanges.Add(new TextChange(new TextSpan(position, 0), text));
        }

        protected static void DeleteText(List<TextChange> textChanges, TextSpan span)
        {
            textChanges.Add(new TextChange(span, string.Empty));
        }

        internal bool ExecuteCommand(ITextView textView, ITextBuffer subjectBuffer, Operation operation, CommandExecutionContext context)
        {
            var title = GetTitle(operation);
            var message = GetMessage(operation);

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

                var edits = CollectEdits(document, service, selectedSpans, operation, cancellationToken).WaitAndGetResult(cancellationToken);

                // Apply the text changes.
                using (var transaction = new CaretPreservingEditTransaction(title, textView, _undoHistoryRegistry, _editorOperationsFactoryService))
                {
                    document.Project.Solution.Workspace.ApplyTextChanges(document.Id, edits.TextChanges.Distinct(), cancellationToken);
                    transaction.Complete();
                }

                if (edits.ResultOperation == Operation.Uncomment)
                {
                    // Format the document only during uncomment operations.  Use second transaction so it can be undone.
                    using (var transaction = new CaretPreservingEditTransaction(title, textView, _undoHistoryRegistry, _editorOperationsFactoryService))
                    {
                        Format(service, subjectBuffer.CurrentSnapshot, edits.TrackingSpans, cancellationToken);
                        transaction.Complete();
                    }
                }

                // Set the selection after the edits have been applied.
                if (edits.TrackingSpans.Any())
                {
                    var spans = edits.TrackingSpans.Select(trackingSpan => trackingSpan.ToSelection(subjectBuffer));
                    textView.GetMultiSelectionBroker().SetSelectionRange(spans, spans.Last());
                }
            }

            return true;
        }

        private static ICommentSelectionService GetService(Document document)
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
    }
}
