// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
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
    internal enum Operation { Comment, Uncomment, Toggle }

    abstract class AbstractCommentSelectionCommandHandler
    {
        protected readonly ITextUndoHistoryRegistry _undoHistoryRegistry;
        protected readonly IEditorOperationsFactoryService _editorOperationsFactoryService;

        internal AbstractCommentSelectionCommandHandler(
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IEditorOperationsFactoryService editorOperationsFactoryService)
        {
            Contract.ThrowIfNull(undoHistoryRegistry);
            Contract.ThrowIfNull(editorOperationsFactoryService);

            _undoHistoryRegistry = undoHistoryRegistry;
            _editorOperationsFactoryService = editorOperationsFactoryService;
        }

        protected static VSCommanding.CommandState GetCommandState(ITextBuffer buffer)
        {
            if (!buffer.CanApplyChangeDocumentToWorkspace())
            {
                return VSCommanding.CommandState.Unspecified;
            }

            return VSCommanding.CommandState.Available;
        }

        protected static void Format(ICommentSelectionService service, ITextSnapshot snapshot, IEnumerable<CommentTrackingSpan> changes, CancellationToken cancellationToken)
        {
            var document = snapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return;
            }

            // Only format uncomment actions.
            var textSpans = changes
                .Where(change => change.Operation == Operation.Uncomment)
                .Select(uncommentChange => uncommentChange.ToSnapshotSpan(snapshot).Span.ToTextSpan())
                .ToImmutableArray();
            var newDocument = service.FormatAsync(document, textSpans, cancellationToken).WaitAndGetResult(cancellationToken);
            newDocument.Project.Solution.Workspace.ApplyDocumentChanges(newDocument, cancellationToken);
        }

        /// <summary>
        /// Record "Insert text" text changes.
        /// </summary>
        protected static void InsertText(List<TextChange> textChanges, int position, string text)
        {
            textChanges.Add(new TextChange(new TextSpan(position, 0), text));
        }

        /// <summary>
        /// Record "Delete text" text changes.
        /// </summary>
        protected static void DeleteText(List<TextChange> textChanges, TextSpan span)
        {
            textChanges.Add(new TextChange(span, string.Empty));
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

        internal bool ExecuteCommand(ITextView textView, ITextBuffer subjectBuffer, Operation operation, CommandExecutionContext context)
        {
            var title = GetTitle(operation);
            var message = GetMessage(operation);

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

                var trackingSpans = new List<CommentTrackingSpan>();
                var textChanges = new List<TextChange>();
                CollectEdits(
                    document, service, textView.Selection.GetSnapshotSpansOnBuffer(subjectBuffer),
                    textChanges, trackingSpans, operation, CancellationToken.None);

                using (var transaction = new CaretPreservingEditTransaction(title, textView, _undoHistoryRegistry, _editorOperationsFactoryService))
                {
                    document.Project.Solution.Workspace.ApplyTextChanges(document.Id, textChanges.Distinct(), CancellationToken.None);
                    transaction.Complete();
                }

                using (var transaction = new CaretPreservingEditTransaction(title, textView, _undoHistoryRegistry, _editorOperationsFactoryService))
                {
                    Format(service, subjectBuffer.CurrentSnapshot, trackingSpans, CancellationToken.None);
                    transaction.Complete();
                }

                if (trackingSpans.Any())
                {
                    SetTrackingSpans(textView, subjectBuffer, trackingSpans);
                }
            }

            return true;
        }

        public abstract string DisplayName { get; }

        internal abstract string GetTitle(Operation operation);

        internal abstract string GetMessage(Operation operation);

        internal abstract void CollectEdits(
            Document document, ICommentSelectionService service, NormalizedSnapshotSpanCollection selectedSpans,
            List<TextChange> textChanges, List<CommentTrackingSpan> trackingSpans, Operation operation, CancellationToken cancellationToken);

        internal abstract void SetTrackingSpans(ITextView textView, ITextBuffer buffer, List<CommentTrackingSpan> trackingSpans);

    }
}
