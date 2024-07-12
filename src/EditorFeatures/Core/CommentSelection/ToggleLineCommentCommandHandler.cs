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
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Host.Mef;
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

[Export(typeof(ICommandHandler))]
[VisualStudio.Utilities.ContentType(ContentTypeNames.RoslynContentType)]
[VisualStudio.Utilities.Name(PredefinedCommandHandlerNames.ToggleLineComment)]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class ToggleLineCommentCommandHandler(
    ITextUndoHistoryRegistry undoHistoryRegistry,
    IEditorOperationsFactoryService editorOperationsFactoryService,
    EditorOptionsService editorOptionsService) :
    // Value tuple to represent that there is no distinct command to be passed in.
    AbstractCommentSelectionBase<ValueTuple>(undoHistoryRegistry, editorOperationsFactoryService, editorOptionsService),
    ICommandHandler<ToggleLineCommentCommandArgs>
{
    private static readonly CommentSelectionResult s_emptyCommentSelectionResult =
        new([], [], Operation.Uncomment);

    public CommandState GetCommandState(ToggleLineCommentCommandArgs args)
        => GetCommandState(args.SubjectBuffer);

    public bool ExecuteCommand(ToggleLineCommentCommandArgs args, CommandExecutionContext context)
        => ExecuteCommand(args.TextView, args.SubjectBuffer, ValueTuple.Create(), context);

    public override string DisplayName => EditorFeaturesResources.Toggle_Line_Comment;

    protected override string GetTitle(ValueTuple command) => EditorFeaturesResources.Toggle_Line_Comment;

    protected override string GetMessage(ValueTuple command) => EditorFeaturesResources.Toggling_line_comment;

    internal override CommentSelectionResult CollectEdits(Document document, ICommentSelectionService service,
        ITextBuffer subjectBuffer, NormalizedSnapshotSpanCollection selectedSpans, ValueTuple command, CancellationToken cancellationToken)
    {
        using (Logger.LogBlock(FunctionId.CommandHandler_ToggleLineComment, KeyValueLogMessage.Create(LogType.UserAction, m =>
        {
            m[LanguageNameString] = document.Project.Language;
            m[LengthString] = subjectBuffer.CurrentSnapshot.Length;
        }), cancellationToken))
        {
            var commentInfo = service.GetInfo();
            if (commentInfo.SupportsSingleLineComment)
            {
                return ToggleLineComment(commentInfo, selectedSpans);
            }

            return s_emptyCommentSelectionResult;
        }
    }

    private static CommentSelectionResult ToggleLineComment(CommentSelectionInfo commentInfo,
        NormalizedSnapshotSpanCollection selectedSpans)
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
                CommentLines(selection.Key, selection.Value, textChanges, trackingSpans, commentInfo);
            }

            operation = Operation.Comment;
        }
        else
        {
            foreach (var selection in linesInSelections)
            {
                UncommentLines(selection.Key, selection.Value, textChanges, trackingSpans, commentInfo);
            }

            operation = Operation.Uncomment;
        }

        return new CommentSelectionResult(textChanges, trackingSpans, operation);
    }

    private static void UncommentLines(
        SnapshotSpan selectedSpan,
        ImmutableArray<ITextSnapshotLine> commentedLines,
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

        var commentTrackingSpan = new CommentTrackingSpan(selectedSpan.Span.ToTextSpan());
        trackingSpans.Add(commentTrackingSpan);
    }

    private static void CommentLines(
        SnapshotSpan selectedSpan,
        ImmutableArray<ITextSnapshotLine> linesInSelection,
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

        var commentTrackingSpan = new CommentTrackingSpan(selectedSpan.Span.ToTextSpan());
        trackingSpans.Add(commentTrackingSpan);
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
        => linesInSelection.Any(predicate: static (l, commentInfo) => !IsLineCommentedOrEmpty(l, commentInfo), arg: commentInfo);

    private static bool IsLineCommentedOrEmpty(ITextSnapshotLine line, CommentSelectionInfo info)
    {
        var lineText = line.GetText();
        // We don't add / remove anything for empty lines.
        return lineText.Trim().StartsWith(info.SingleLineCommentString, StringComparison.Ordinal) || line.IsEmptyOrWhitespace();
    }
}
