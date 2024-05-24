// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CommentSelection;

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
    protected const string LanguageNameString = "languagename";
    protected const string LengthString = "length";

    private readonly ITextUndoHistoryRegistry _undoHistoryRegistry;
    private readonly IEditorOperationsFactoryService _editorOperationsFactoryService;
    private readonly EditorOptionsService _editorOptionsService;

    internal AbstractCommentSelectionBase(
        ITextUndoHistoryRegistry undoHistoryRegistry,
        IEditorOperationsFactoryService editorOperationsFactoryService,
        EditorOptionsService editorOptionsService)
    {
        Contract.ThrowIfNull(undoHistoryRegistry);
        Contract.ThrowIfNull(editorOperationsFactoryService);

        _undoHistoryRegistry = undoHistoryRegistry;
        _editorOperationsFactoryService = editorOperationsFactoryService;
        _editorOptionsService = editorOptionsService;
    }

    public abstract string DisplayName { get; }

    protected abstract string GetTitle(TCommand command);

    protected abstract string GetMessage(TCommand command);

    // Internal as tests currently rely on this method.
    internal abstract CommentSelectionResult CollectEdits(
        Document document, ICommentSelectionService service, ITextBuffer textBuffer, NormalizedSnapshotSpanCollection selectedSpans,
        TCommand command, CancellationToken cancellationToken);

    protected static CommandState GetCommandState(ITextBuffer buffer)
    {
        return buffer.CanApplyChangeDocumentToWorkspace()
            ? CommandState.Available
            : CommandState.Unspecified;
    }

    protected static void InsertText(ArrayBuilder<TextChange> textChanges, int position, string text)
        => textChanges.Add(new TextChange(new TextSpan(position, 0), text));

    protected static void DeleteText(ArrayBuilder<TextChange> textChanges, TextSpan span)
        => textChanges.Add(new TextChange(span, string.Empty));

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

            var service = document.GetLanguageService<ICommentSelectionService>();
            if (service == null)
            {
                return true;
            }

            var edits = CollectEdits(document, service, subjectBuffer, selectedSpans, command, cancellationToken);
            ApplyEdits(document, textView, subjectBuffer, title, edits, cancellationToken);
        }

        return true;
    }

    /// <summary>
    /// Applies the requested edits and sets the selection.
    /// This operation is not cancellable.
    /// </summary>
    private void ApplyEdits(Document document, ITextView textView, ITextBuffer subjectBuffer, string title, CommentSelectionResult edits, CancellationToken cancellationToken)
    {
        var originalSnapshot = subjectBuffer.CurrentSnapshot;

        // Apply the text changes.
        using (var transaction = new CaretPreservingEditTransaction(title, textView, _undoHistoryRegistry, _editorOperationsFactoryService))
        {
            subjectBuffer.ApplyChanges(edits.TextChanges);
            transaction.Complete();
        }

        if (edits.TrackingSpans.Any())
        {
            // Create tracking spans to track the text changes.
            var trackingSpans = edits.TrackingSpans
                .SelectAsArray(textSpan => (originalSpan: textSpan, trackingSpan: CreateTrackingSpan(edits.ResultOperation, originalSnapshot, textSpan.TrackingTextSpan)));

            // Convert the tracking spans into snapshot spans for formatting and selection.
            var trackingSnapshotSpans = trackingSpans.Select(s => CreateSnapshotSpan(subjectBuffer.CurrentSnapshot, s.trackingSpan, s.originalSpan));

            if (edits.ResultOperation == Operation.Uncomment && document.SupportsSyntaxTree)
            {
                // Format the document only during uncomment operations.  Use second transaction so it can be undone.
                using var transaction = new CaretPreservingEditTransaction(title, textView, _undoHistoryRegistry, _editorOperationsFactoryService);

                var newText = subjectBuffer.CurrentSnapshot.AsText();
                var oldSyntaxTree = document.GetSyntaxTreeSynchronously(cancellationToken);
                var newRoot = oldSyntaxTree.WithChangedText(newText).GetRoot(cancellationToken);

                var formattingOptions = subjectBuffer.GetSyntaxFormattingOptions(_editorOptionsService, document.Project.Services, explicitFormat: false);
                var formattingSpans = trackingSnapshotSpans.Select(change => CommonFormattingHelpers.GetFormattingSpan(newRoot, change.Span.ToTextSpan()));
                var formattedChanges = Formatter.GetFormattedTextChanges(newRoot, formattingSpans, document.Project.Solution.Services, formattingOptions, rules: default, cancellationToken);

                subjectBuffer.ApplyChanges(formattedChanges);

                transaction.Complete();
            }

            // Set the multi selection after edits have been applied.
            textView.SetMultiSelection(trackingSnapshotSpans);
        }
    }

    /// <summary>
    /// Creates a tracking span for the operation.
    /// Internal for tests.
    /// </summary>
    internal static ITrackingSpan CreateTrackingSpan(Operation operation, ITextSnapshot snapshot, TextSpan textSpan)
    {
        // If a comment is being added, the tracking span must include changes at the edge.
        var spanTrackingMode = operation == Operation.Comment
            ? SpanTrackingMode.EdgeInclusive
            : SpanTrackingMode.EdgeExclusive;
        return snapshot.CreateTrackingSpan(Span.FromBounds(textSpan.Start, textSpan.End), spanTrackingMode);
    }

    /// <summary>
    /// Retrieves the snapshot span from a post edited tracking span.
    /// Additionally applies any extra modifications to the tracking span post edit.
    /// </summary>
    private static SnapshotSpan CreateSnapshotSpan(ITextSnapshot snapshot, ITrackingSpan trackingSpan, CommentTrackingSpan originalSpan)
    {
        var snapshotSpan = trackingSpan.GetSpan(snapshot);
        if (originalSpan.HasPostApplyChanges())
        {
            var updatedStart = snapshotSpan.Start.Position + originalSpan.AmountToAddToTrackingSpanStart;
            var updatedEnd = snapshotSpan.End.Position + originalSpan.AmountToAddToTrackingSpanEnd;
            if (updatedStart >= snapshotSpan.Start.Position && updatedEnd <= snapshotSpan.End.Position)
            {
                snapshotSpan = new SnapshotSpan(snapshot, Span.FromBounds(updatedStart, updatedEnd));
            }
        }

        return snapshotSpan;
    }

    /// <summary>
    /// Given a set of lines, find the minimum indent of all of the non-blank, non-whitespace lines.
    /// </summary>
    protected static int DetermineSmallestIndent(
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
}
