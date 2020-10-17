﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.SplitComment
{
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.CSharpContentType)]
    [Name(nameof(SplitCommentCommandHandler))]
    [Order(After = PredefinedCompletionNames.CompletionCommandHandler)]
    internal sealed class SplitCommentCommandHandler : ICommandHandler<ReturnKeyCommandArgs>
    {
        private readonly ITextUndoHistoryRegistry _undoHistoryRegistry;
        private readonly IEditorOperationsFactoryService _editorOperationsFactoryService;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public SplitCommentCommandHandler(
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IEditorOperationsFactoryService editorOperationsFactoryService)
        {
            _undoHistoryRegistry = undoHistoryRegistry;
            _editorOperationsFactoryService = editorOperationsFactoryService;
        }

        public string DisplayName => EditorFeaturesResources.Split_comment;

        public CommandState GetCommandState(ReturnKeyCommandArgs args)
            => CommandState.Unspecified;

        public bool ExecuteCommand(ReturnKeyCommandArgs args, CommandExecutionContext context)
        {
            var textView = args.TextView;
            var subjectBuffer = args.SubjectBuffer;
            var spans = textView.Selection.GetSnapshotSpansOnBuffer(subjectBuffer);

            // Don't do anything special if there is multi-selection.  It's not clear what sort of semantics that should have.
            if (spans.Count != 1)
                return false;

            var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
                return false;

            // If there is a selection, ensure that it's all on one-line.  It's not clear what sort of semantics we
            // would want if this spanned multiple lines.
            var selectionSpan = spans[0].Span;
            var position = selectionSpan.Start;
            var line = subjectBuffer.CurrentSnapshot.GetLineFromPosition(position);
            var endLine = subjectBuffer.CurrentSnapshot.GetLineFromPosition(selectionSpan.End);
            if (line.LineNumber != endLine.LineNumber)
                return false;

            // Quick check.  If the line doesn't contain a comment in it before the caret,
            // then no point in doing any more expensive synchronous work.
            var splitCommentService = document.GetRequiredLanguageService<ISplitCommentService>();
            if (!LineProbablyContainsComment(splitCommentService, line, position))
                return false;

            using (context.OperationContext.AddScope(allowCancellation: true, EditorFeaturesResources.Split_comment))
            {
                var cancellationToken = context.OperationContext.UserCancellationToken;
                var result = SplitCommentAsync(textView, subjectBuffer, document, selectionSpan, cancellationToken).WaitAndGetResult(cancellationToken);
                if (result == null)
                    return false;

                using var transaction = CaretPreservingEditTransaction.TryCreate(
                    EditorFeaturesResources.Split_comment, textView, _undoHistoryRegistry, _editorOperationsFactoryService);

                subjectBuffer.Replace(result.Value.replacementSpan, result.Value.replacementText);

                transaction.Complete();
                return true;
            }
        }

        private static bool LineProbablyContainsComment(ISplitCommentService service, ITextSnapshotLine line, int caretPosition)
        {
            var commentStart = service.CommentStart;

            var end = Math.Max(caretPosition, line.Length);
            for (var i = 0; i < end; i++)
            {
                if (MatchesCommentStart(line, commentStart, i))
                    return true;
            }

            return false;
        }

        private static bool MatchesCommentStart(ITextSnapshotLine line, string commentStart, int index)
        {
            var lineStart = line.Start;
            for (var c = 0; c < commentStart.Length; c++)
            {
                if (lineStart.Position + index >= line.Snapshot.Length)
                    return false;

                if (line.Snapshot[lineStart + index] != commentStart[c])
                    return false;
            }

            return true;
        }

        private static async Task<(Span replacementSpan, string replacementText)?> SplitCommentAsync(
            ITextView textView,
            ITextBuffer subjectBuffer,
            Document document,
            Span selectionSpan,
            CancellationToken cancellationToken)
        {
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            var enabled = options.GetOption(SplitCommentOptions.Enabled);
            if (!enabled)
                return null;

            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var syntaxKinds = document.GetRequiredLanguageService<ISyntaxKindsService>();
            var trivia = root.FindTrivia(selectionSpan.Start);
            if (syntaxKinds.SingleLineCommentTrivia != trivia.RawKind)
                return null;

            var splitCommentService = document.GetRequiredLanguageService<ISplitCommentService>();

            // if the user hits enter at `/$$/` we don't want to consider this a comment continuation.
            if (selectionSpan.Start < (trivia.SpanStart + splitCommentService.CommentStart.Length))
                return null;

            if (!splitCommentService.IsAllowed(root, trivia))
                return null;

            var textSnapshot = subjectBuffer.CurrentSnapshot;
            var triviaLine = textSnapshot.GetLineFromPosition(trivia.SpanStart);

            var replacementSpan = GetReplacementSpan(triviaLine, selectionSpan);
            var replacementText = GetReplacementText(textView, options, triviaLine, trivia, selectionSpan.Start);
            return (replacementSpan, replacementText);
        }

        private static string GetReplacementText(
            ITextView textView, DocumentOptionSet options, ITextSnapshotLine triviaLine, SyntaxTrivia trivia, int position)
        {
            // We're inside a comment.  Instead of inserting just a newline here, insert
            // 1. a newline
            // 2. spaces up to the indentation of the current comment
            // 3. the comment prefix (extended out for repeated chars).

            // Then, depending on if the current comment starts with whitespace or not, we will insert those same spaces
            // to match.

            var commentStartColumn = triviaLine.GetColumnFromLineOffset(trivia.SpanStart - triviaLine.Start, textView.Options);

            var useTabs = options.GetOption(FormattingOptions.UseTabs);
            var tabSize = options.GetOption(FormattingOptions.TabSize);

            var prefix = GetCommentPrefix(triviaLine.Snapshot, trivia, position);
            var replacementText = options.GetOption(FormattingOptions.NewLine) +
                commentStartColumn.CreateIndentationString(useTabs, tabSize) +
                prefix +
                GetWhitespaceAfterCommentPrefix(trivia, triviaLine, prefix, position);

            return replacementText;
        }

        private static string GetCommentPrefix(ITextSnapshot snapshot, SyntaxTrivia trivia, int position)
        {
            // Consume as many of the comment start character as we can.  That way if someone has something like
            // `//// $$Goo` then hitting enter will respect that the next line should start with `////`.

            var triviaPrefixStart = trivia.SpanStart;
            var triviaPrefixEnd = triviaPrefixStart;

            var triviaStartChar = snapshot[trivia.SpanStart];
            while (snapshot[triviaPrefixEnd] == triviaStartChar && triviaPrefixEnd < position)
                triviaPrefixEnd++;

            return snapshot.GetText(Span.FromBounds(triviaPrefixStart, triviaPrefixEnd));
        }

        private static string GetWhitespaceAfterCommentPrefix(SyntaxTrivia trivia, ITextSnapshotLine triviaLine, string commentPrefix, int position)
        {
            var startIndex = trivia.SpanStart + commentPrefix.Length;
            var endIndex = startIndex;

            while (endIndex < position && char.IsWhiteSpace(triviaLine.Snapshot[endIndex]))
                endIndex++;

            return triviaLine.Snapshot.GetText(Span.FromBounds(startIndex, endIndex));
        }

        private static Span GetReplacementSpan(ITextSnapshotLine triviaLine, Span selectionSpan)
        {
            var textSnapshot = triviaLine.Snapshot;

            // When hitting enter in a comment consume the whitespace around the caret.  That way the previous line
            // doesn't have trailing whitespace, and the text following the caret is placed at the right location.
            var replacementStart = selectionSpan.Start;
            var replacementEnd = selectionSpan.End;
            while (replacementStart > triviaLine.Start && textSnapshot[replacementStart - 1] == ' ')
                replacementStart--;

            while (replacementEnd < triviaLine.End && textSnapshot[replacementEnd] == ' ')
                replacementEnd++;

            var replacementSpan = Span.FromBounds(replacementStart, replacementEnd);
            return replacementSpan;
        }
    }
}
