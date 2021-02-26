// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CommentSelection;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.CommentSelection
{
    [Export(typeof(ICommandHandler))]
    [VisualStudio.Utilities.ContentType(ContentTypeNames.RoslynContentType)]
    [VisualStudio.Utilities.Name(PredefinedCommandHandlerNames.ToggleLineComment)]
    internal class ToggleLineCommentCommandHandler :
        // Value tuple to represent that there is no distinct command to be passed in.
        AbstractCommentSelectionBase<ValueTuple>,
        ICommandHandler<ToggleLineCommentCommandArgs>
    {
        private static readonly CommentSelectionResult s_emptyCommentSelectionResult =
            new(new List<TextChange>(), new List<CommentTrackingSpan>(), Operation.Uncomment);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ToggleLineCommentCommandHandler(
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IEditorOperationsFactoryService editorOperationsFactoryService)
            : base(undoHistoryRegistry, editorOperationsFactoryService)
        {
        }

        public CommandState GetCommandState(ToggleLineCommentCommandArgs args)
            => GetCommandState(args.SubjectBuffer);

        public bool ExecuteCommand(ToggleLineCommentCommandArgs args, CommandExecutionContext context)
            => ExecuteCommand(args.TextView, args.SubjectBuffer, ValueTuple.Create(), context);

        public override string DisplayName => EditorFeaturesResources.Toggle_Line_Comment;

        protected override string GetTitle(ValueTuple command) => EditorFeaturesResources.Toggle_Line_Comment;

        protected override string GetMessage(ValueTuple command) => EditorFeaturesResources.Toggling_line_comment;

        internal override async Task<CommentSelectionResult> CollectEditsAsync(Document document, ICommentSelectionService service,
            ITextBuffer subjectBuffer, NormalizedSnapshotSpanCollection selectedSpans, ValueTuple command, IEditorOptions editorOptions, CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(FunctionId.CommandHandler_ToggleLineComment, KeyValueLogMessage.Create(LogType.UserAction, m =>
            {
                m[LanguageNameString] = document.Project.Language;
                m[LengthString] = subjectBuffer.CurrentSnapshot.Length;
            }), cancellationToken))
            {
                var commentInfo = await service.GetInfoAsync(document, selectedSpans.First().Span.ToTextSpan(), cancellationToken).ConfigureAwait(false);
                if (commentInfo.SupportsSingleLineComment)
                {
                    return ToggleLineComment(commentInfo, selectedSpans, editorOptions);
                }

                return s_emptyCommentSelectionResult;
            }
        }

        private static CommentSelectionResult ToggleLineComment(CommentSelectionInfo commentInfo,
            NormalizedSnapshotSpanCollection selectedSpans, IEditorOptions editorOptions)
        {
            var textChanges = ArrayBuilder<TextChange>.GetInstance();
            var trackingSpans = ArrayBuilder<CommentTrackingSpan>.GetInstance();

            var linesInSelections = selectedSpans.ToDictionary(
                span => span,
                span => GetLinesFromSelectedSpan(span).ToImmutableArray());

            var isMultiCaret = selectedSpans.Count > 1;

            Operation operation;
            // If any of the lines are uncommented, add comments.
            if (linesInSelections.Values.Any(lines => SelectionHasUncommentedLines(lines, commentInfo)))
            {
                foreach (var selection in linesInSelections)
                {
                    CommentLines(selection.Key, selection.Value, editorOptions, isMultiCaret, textChanges, trackingSpans, commentInfo);
                }

                operation = Operation.Comment;
            }
            else
            {
                foreach (var selection in linesInSelections)
                {
                    UncommentLines(selection.Key, selection.Value, editorOptions, isMultiCaret, textChanges, trackingSpans, commentInfo);
                }

                operation = Operation.Uncomment;
            }

            return new CommentSelectionResult(textChanges, trackingSpans, operation);
        }

        private static void UncommentLines(
            SnapshotSpan selectedSpan,
            ImmutableArray<ITextSnapshotLine> commentedLines,
            IEditorOptions editorOptions,
            bool isMultiCaret,
            ArrayBuilder<TextChange> textChanges,
            ArrayBuilder<CommentTrackingSpan> trackingSpans,
            CommentSelectionInfo commentInfo)
        {
            foreach (var line in commentedLines)
            {
                if (!line.IsEmptyOrWhitespace())
                {
                    var text = line.GetText();
                    var commentIndex = text.IndexOf(commentInfo.SingleLineCommentString) + line.Start;
                    var spanToRemove = TextSpan.FromBounds(commentIndex, commentIndex + commentInfo.SingleLineCommentString.Length);
                    DeleteText(textChanges, spanToRemove);
                }
            }

            var commentTrackingSpan = GetNewSelection(selectedSpan, editorOptions, isMultiCaret);
            trackingSpans.Add(commentTrackingSpan);
        }

        private static void CommentLines(
            SnapshotSpan selectedSpan,
            ImmutableArray<ITextSnapshotLine> linesInSelection,
            IEditorOptions editorOptions,
            bool isMultiCaret,
            ArrayBuilder<TextChange> textChanges,
            ArrayBuilder<CommentTrackingSpan> trackingSpans,
            CommentSelectionInfo commentInfo)
        {
            var indentation = DetermineSmallestIndent(selectedSpan, linesInSelection.First(), linesInSelection.Last());
            foreach (var line in linesInSelection)
            {
                if (!line.IsEmptyOrWhitespace())
                {
                    InsertText(textChanges, line.Start + indentation, commentInfo.SingleLineCommentString);
                }
            }

            var commentTrackingSpan = GetNewSelection(selectedSpan, editorOptions, isMultiCaret);
            trackingSpans.Add(commentTrackingSpan);
        }

        private static CommentTrackingSpan GetNewSelection(SnapshotSpan selectedSpan, IEditorOptions editorOptions, bool isMultiCaret)
        {
            if (!selectedSpan.IsEmpty || isMultiCaret)
            {
                // In selection or any multi-caret scenarios, leave the selection unchanged.
                // We bail in multi caret scenarios as the next line could already be modified as part of this comment operation.
                return new CommentTrackingSpan(selectedSpan.Span.ToTextSpan());
            }

            // If the span is just the caret location without a selection move the caret
            // down to the next line at the same column (or end of line).
            selectedSpan.End.GetLineAndCharacter(out var caretLineNumber, out var caretOffset);
            var caretLine = selectedSpan.Snapshot.GetLineFromLineNumber(caretLineNumber);

            var nextLineNumber = caretLineNumber + 1;
            if (nextLineNumber < selectedSpan.Snapshot.LineCount)
            {
                // Get the column in the line of the current caret location.
                var caretColumn = caretLine.GetColumnFromLineOffset(caretOffset, editorOptions);

                var nextLine = selectedSpan.Snapshot.GetLineFromLineNumber(nextLineNumber);
                // Compute the correct offset location from the column in the previous line.
                var offsetInNextLine = nextLine.GetLineOffsetFromColumn(caretColumn, editorOptions);
                var position = selectedSpan.Snapshot.GetPosition(nextLineNumber, offsetInNextLine);
                return new CommentTrackingSpan(TextSpan.FromBounds(position, position));
            }
            else
            {
                return new CommentTrackingSpan(TextSpan.FromBounds(caretLine.End, caretLine.End));
            }
        }

        private static List<ITextSnapshotLine> GetLinesFromSelectedSpan(SnapshotSpan span)
        {
            var lines = new List<ITextSnapshotLine>();
            var startLine = span.Snapshot.GetLineFromPosition(span.Start);
            var endLine = span.Snapshot.GetLineFromPosition(span.End);
            // Don't include the last line if the span is just the start of the line.
            if (endLine.Start == span.End.Position && !span.IsEmpty)
            {
                endLine = endLine.GetPreviousMatchingLine(_ => true);
            }

            if (startLine.LineNumber <= endLine.LineNumber)
            {
                for (var i = startLine.LineNumber; i <= endLine.LineNumber; i++)
                {
                    lines.Add(span.Snapshot.GetLineFromLineNumber(i));
                }
            }

            return lines;
        }

        private static bool SelectionHasUncommentedLines(ImmutableArray<ITextSnapshotLine> linesInSelection, CommentSelectionInfo commentInfo)
            => linesInSelection.Any(l => !IsLineCommentedOrEmpty(l, commentInfo));

        private static bool IsLineCommentedOrEmpty(ITextSnapshotLine line, CommentSelectionInfo info)
        {
            var lineText = line.GetText();
            // We don't add / remove anything for empty lines.
            return lineText.Trim().StartsWith(info.SingleLineCommentString, StringComparison.Ordinal) || line.IsEmptyOrWhitespace();
        }
    }
}
