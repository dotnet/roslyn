﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.DocumentationComments
{
    internal abstract class AbstractDocumentationCommentCommandHandler<TDocumentationComment, TMemberNode> :
        ICommandHandler<TypeCharCommandArgs>,
        ICommandHandler<ReturnKeyCommandArgs>,
        ICommandHandler<InsertCommentCommandArgs>
        where TDocumentationComment : SyntaxNode, IStructuredTriviaSyntax
        where TMemberNode : SyntaxNode
    {
        private readonly IWaitIndicator _waitIndicator;
        private readonly ITextUndoHistoryRegistry _undoHistoryRegistry;
        private readonly IEditorOperationsFactoryService _editorOperationsFactoryService;
        private readonly IAsyncCompletionService _completionService;

        protected AbstractDocumentationCommentCommandHandler(
            IWaitIndicator waitIndicator,
            ITextUndoHistoryRegistry undoHistoryRegistry,
            IEditorOperationsFactoryService editorOperationsFactoryService,
            IAsyncCompletionService completionService)
        {
            Contract.ThrowIfNull(waitIndicator);
            Contract.ThrowIfNull(undoHistoryRegistry);
            Contract.ThrowIfNull(editorOperationsFactoryService);
            Contract.ThrowIfNull(completionService);

            _waitIndicator = waitIndicator;
            _undoHistoryRegistry = undoHistoryRegistry;
            _editorOperationsFactoryService = editorOperationsFactoryService;
            _completionService = completionService;
        }

        protected abstract string ExteriorTriviaText { get; }

        protected abstract TMemberNode GetContainingMember(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken);
        protected abstract bool SupportsDocumentationComments(TMemberNode member);
        protected abstract bool HasDocumentationComment(TMemberNode member);
        protected abstract int GetPrecedingDocumentationCommentCount(TMemberNode member);
        protected abstract bool IsMemberDeclaration(TMemberNode member);
        protected abstract List<string> GetDocumentationCommentStubLines(TMemberNode member);

        protected abstract SyntaxToken GetTokenToRight(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken);
        protected abstract SyntaxToken GetTokenToLeft(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken);
        protected abstract bool IsDocCommentNewLine(SyntaxToken token);
        protected abstract bool IsEndOfLineTrivia(SyntaxTrivia trivia);

        protected abstract bool IsSingleExteriorTrivia(TDocumentationComment documentationComment, bool allowWhitespace = false);
        protected abstract bool EndsWithSingleExteriorTrivia(TDocumentationComment documentationComment);
        protected abstract bool IsMultilineDocComment(TDocumentationComment documentationComment);

        protected abstract bool AddIndent { get; }

        private char TriggerCharacter
        {
            get { return ExteriorTriviaText[ExteriorTriviaText.Length - 1]; }
        }

        private string GetNewLine(SourceText text)
        {
            // return editorOptionsFactoryService.GetEditorOptions(text).GetNewLineCharacter();
            return "\r\n";
        }

        private TMemberNode GetTargetMember(SyntaxTree syntaxTree, SourceText text, int position, CancellationToken cancellationToken)
        {
            var member = GetContainingMember(syntaxTree, position, cancellationToken);
            if (member == null)
            {
                return null;
            }

            if (!SupportsDocumentationComments(member) || HasDocumentationComment(member))
            {
                return null;
            }

            var startPosition = member.GetFirstToken().SpanStart;
            var line = text.Lines.GetLineFromPosition(startPosition);
            var lineOffset = line.GetFirstNonWhitespaceOffset();
            if (!lineOffset.HasValue || line.Start + lineOffset.Value < startPosition)
            {
                return null;
            }

            return member;
        }

        private TMemberNode GetTargetMember(TDocumentationComment documentationComment)
        {
            var targetMember = documentationComment.ParentTrivia.Token.GetAncestor<TMemberNode>();
            if (targetMember == null)
            {
                return null;
            }

            if (!IsMemberDeclaration(targetMember))
            {
                return null;
            }

            if (targetMember.SpanStart < documentationComment.SpanStart)
            {
                return null;
            }

            return targetMember;
        }

        private void AddLineBreaks(SourceText text, IList<string> lines)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                lines[i] = lines[i] + GetNewLine(text);
            }
        }

        private bool InsertOnCharacterTyped(
            SyntaxTree syntaxTree,
            SourceText text,
            int position,
            int originalPosition,
            ITextBuffer subjectBuffer,
            ITextView textView,
            CancellationToken cancellationToken)
        {
            if (!subjectBuffer.GetOption(FeatureOnOffOptions.AutoXmlDocCommentGeneration))
            {
                return false;
            }

            // Only generate if the position is immediately after '///', 
            // and that is the only documentation comment on the target member.

            var token = syntaxTree.GetRoot(cancellationToken).FindToken(position, findInsideTrivia: true);
            if (position != token.SpanStart)
            {
                return false;
            }

            var documentationComment = token.GetAncestor<TDocumentationComment>();
            if (!IsSingleExteriorTrivia(documentationComment))
            {
                return false;
            }

            var targetMember = GetTargetMember(documentationComment);
            if (targetMember == null)
            {
                return false;
            }

            // Ensure that the target member is only preceded by a single documentation comment (i.e. our ///).
            if (GetPrecedingDocumentationCommentCount(targetMember) != 1)
            {
                return false;
            }

            var line = text.Lines.GetLineFromPosition(documentationComment.FullSpan.Start);
            if (line.IsEmptyOrWhitespace())
            {
                return false;
            }

            var lines = GetDocumentationCommentStubLines(targetMember);
            Contract.Assume(lines.Count > 2);

            AddLineBreaks(text, lines);

            // Shave off initial three slashes
            lines[0] = lines[0].Substring(3);

            // Add indents
            var lineOffset = line.GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(subjectBuffer.GetOption(FormattingOptions.TabSize));
            var indentText = lineOffset.CreateIndentationString(subjectBuffer.GetOption(FormattingOptions.UseTabs), subjectBuffer.GetOption(FormattingOptions.TabSize));
            for (int i = 1; i < lines.Count - 1; i++)
            {
                lines[i] = indentText + lines[i];
            }

            var lastLine = lines[lines.Count - 1];
            lastLine = indentText + lastLine.Substring(0, lastLine.Length - GetNewLine(text).Length);
            lines[lines.Count - 1] = lastLine;

            var newText = string.Join(string.Empty, lines);
            var offset = lines[0].Length + lines[1].Length - GetNewLine(text).Length;

            subjectBuffer.Insert(position, newText);
            textView.TryMoveCaretToAndEnsureVisible(subjectBuffer.CurrentSnapshot.GetPoint(position + offset));

            return true;
        }

        private bool InsertOnEnterTyped(
            SyntaxTree syntaxTree,
            SourceText text,
            int position,
            int originalPosition,
            ITextBuffer subjectBuffer,
            ITextView textView,
            CancellationToken cancellationToken)
        {
            if (!subjectBuffer.GetOption(FeatureOnOffOptions.AutoXmlDocCommentGeneration))
            {
                return false;
            }

            if (TryGenerateDocumentationCommentAfterEnter(syntaxTree, text, position, originalPosition, subjectBuffer, textView, cancellationToken))
            {
                return true;
            }

            if (TryGenerateExteriorTriviaAfterEnter(syntaxTree, text, position, originalPosition, subjectBuffer, textView, cancellationToken))
            {
                return true;
            }

            return false;
        }

        private bool TryGenerateDocumentationCommentAfterEnter(
            SyntaxTree syntaxTree,
            SourceText text,
            int position,
            int originalPosition,
            ITextBuffer subjectBuffer,
            ITextView textView,
            CancellationToken cancellationToken)
        {
            // Find the documentation comment before the new line that was just pressed
            var token = GetTokenToLeft(syntaxTree, position, cancellationToken);
            if (!IsDocCommentNewLine(token))
            {
                return false;
            }

            var documentationComment = token.GetAncestor<TDocumentationComment>();
            if (!IsSingleExteriorTrivia(documentationComment))
            {
                return false;
            }

            var targetMember = GetTargetMember(documentationComment);
            if (targetMember == null)
            {
                return false;
            }

            // Ensure that the target member is only preceded by a single documentation comment (our ///).
            if (GetPrecedingDocumentationCommentCount(targetMember) != 1)
            {
                return false;
            }

            var line = text.Lines.GetLineFromPosition(documentationComment.FullSpan.Start);
            if (line.IsEmptyOrWhitespace())
            {
                return false;
            }

            var lines = GetDocumentationCommentStubLines(targetMember);
            Contract.Assume(lines.Count > 2);

            AddLineBreaks(text, lines);

            // Shave off initial exterior trivia
            lines[0] = lines[0].Substring(3);

            // Add indents
            var lineOffset = line.GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(subjectBuffer.GetOption(FormattingOptions.TabSize));
            var indentText = lineOffset.CreateIndentationString(subjectBuffer.GetOption(FormattingOptions.UseTabs), subjectBuffer.GetOption(FormattingOptions.TabSize));
            for (int i = 1; i < lines.Count; i++)
            {
                lines[i] = indentText + lines[i];
            }

            var newText = string.Join(string.Empty, lines);
            var offset = lines[0].Length + lines[1].Length - GetNewLine(text).Length;

            // Shave off final line break or add trailing indent if necessary
            var trivia = syntaxTree.GetRoot(cancellationToken).FindTrivia(position, findInsideTrivia: false);
            if (IsEndOfLineTrivia(trivia))
            {
                newText = newText.Substring(0, newText.Length - GetNewLine(text).Length);
            }
            else
            {
                newText += indentText;
            }

            var replaceSpan = token.Span.ToSpan();
            var currentLine = text.Lines.GetLineFromPosition(position);
            var currentLinePosition = currentLine.GetFirstNonWhitespacePosition();
            if (currentLinePosition.HasValue)
            {
                replaceSpan = Span.FromBounds(replaceSpan.Start, currentLinePosition.Value);
            }

            subjectBuffer.Replace(replaceSpan, newText);
            textView.TryMoveCaretToAndEnsureVisible(subjectBuffer.CurrentSnapshot.GetPoint(replaceSpan.Start + offset));

            return true;
        }

        private bool TryGenerateExteriorTriviaAfterEnter(
            SyntaxTree syntaxTree,
            SourceText text,
            int position,
            int originalPosition,
            ITextBuffer subjectBuffer,
            ITextView textView,
            CancellationToken cancellationToken)
        {
            // Find the documentation comment before the new line that was just pressed
            var token = GetTokenToRight(syntaxTree, originalPosition, cancellationToken);
            if (!IsDocCommentNewLine(token) || token.SpanStart != originalPosition)
            {
                return false;
            }

            var currentLine = text.Lines.GetLineFromPosition(position);
            if (currentLine.LineNumber == 0)
            {
                return false;
            }

            // Previous line must begin with a doc comment
            var previousLine = text.Lines[currentLine.LineNumber - 1];
            var previousLineText = previousLine.ToString().Trim();
            if (!previousLineText.StartsWith(ExteriorTriviaText, StringComparison.Ordinal))
            {
                return false;
            }

            var nextLineStartsWithDocComment = text.Lines.Count > currentLine.LineNumber + 1 &&
                text.Lines[currentLine.LineNumber + 1].ToString().Trim().StartsWith(ExteriorTriviaText, StringComparison.Ordinal);

            // if previous line has only exterior trivia, current line is empty and next line doesn't begin
            // with exterior trivia then stop inserting auto generated xml doc string
            if (previousLineText.Equals(ExteriorTriviaText) &&
                string.IsNullOrWhiteSpace(currentLine.ToString()) &&
                !nextLineStartsWithDocComment)
            {
                return false;
            }

            var documentationComment = token.GetAncestor<TDocumentationComment>();
            if (IsMultilineDocComment(documentationComment))
            {
                return false;
            }

            if (EndsWithSingleExteriorTrivia(documentationComment) && currentLine.IsEmptyOrWhitespace() && !nextLineStartsWithDocComment)
            {
                return false;
            }

            var tabSize = subjectBuffer.GetOption(FormattingOptions.TabSize);
            var previousLineOffset = previousLine.GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(tabSize);
            var currentLineOffset = currentLine.GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(tabSize);
            var indentText = currentLineOffset < previousLineOffset
                ? (previousLineOffset - currentLineOffset).CreateIndentationString(subjectBuffer.GetOption(FormattingOptions.UseTabs), tabSize)
                : string.Empty;

            var newText = indentText + ExteriorTriviaText + " ";
            subjectBuffer.Insert(position, newText);

            textView.TryMoveCaretToAndEnsureVisible(subjectBuffer.CurrentSnapshot.GetPoint(position + newText.Length));

            return true;
        }

        private bool InsertOnCommandInvoke(
            SyntaxTree syntaxTree,
            SourceText text,
            int position,
            int originalPosition,
            ITextBuffer subjectBuffer,
            ITextView textView,
            CancellationToken cancellationToken)
        {
            var targetMember = GetTargetMember(syntaxTree, text, position, cancellationToken);

            if (targetMember == null)
            {
                return false;
            }

            var startPosition = targetMember.GetFirstToken().SpanStart;
            var line = text.Lines.GetLineFromPosition(startPosition);
            Contract.Assume(!line.IsEmptyOrWhitespace());

            var lines = GetDocumentationCommentStubLines(targetMember);
            Contract.Assume(lines.Count > 2);

            AddLineBreaks(text, lines);

            // Add indents
            var lineOffset = line.GetColumnOfFirstNonWhitespaceCharacterOrEndOfLine(subjectBuffer.GetOption(FormattingOptions.TabSize));
            Contract.Assume(line.Start + lineOffset == startPosition);

            var indentText = lineOffset.CreateIndentationString(subjectBuffer.GetOption(FormattingOptions.UseTabs), subjectBuffer.GetOption(FormattingOptions.TabSize));
            for (int i = 1; i < lines.Count; i++)
            {
                lines[i] = indentText + lines[i];
            }

            lines[lines.Count - 1] = lines[lines.Count - 1] + indentText;

            var newText = string.Join(string.Empty, lines);
            var offset = lines[0].Length + lines[1].Length - GetNewLine(text).Length;

            subjectBuffer.Insert(startPosition, newText);

            textView.TryMoveCaretToAndEnsureVisible(subjectBuffer.CurrentSnapshot.GetPoint(startPosition + offset));

            return true;
        }

        private static bool CompleteComment(
            ITextBuffer subjectBuffer,
            ITextView textView,
            int originalCaretPosition,
            Func<SyntaxTree, SourceText, int, int, ITextBuffer, ITextView, CancellationToken, bool> insertAction,
            CancellationToken cancellationToken)
        {
            var caretPosition = textView.GetCaretPoint(subjectBuffer) ?? -1;
            if (caretPosition < 0)
            {
                return false;
            }

            var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return false;
            }

            var text = document.GetTextAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            var syntaxTree = document.GetSyntaxTreeAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            return insertAction(syntaxTree, text, caretPosition, originalCaretPosition, subjectBuffer, textView, cancellationToken);
        }

        public CommandState GetCommandState(TypeCharCommandArgs args, Func<CommandState> nextHandler)
        {
            return nextHandler();
        }

        public void ExecuteCommand(TypeCharCommandArgs args, Action nextHandler)
        {
            var originalCaretPosition = args.TextView.GetCaretPoint(args.SubjectBuffer) ?? -1;

            // Ensure the character is actually typed in the editor
            nextHandler();

            if (args.TypedChar != TriggerCharacter)
            {
                return;
            }

            CompleteComment(args.SubjectBuffer, args.TextView, originalCaretPosition, InsertOnCharacterTyped, CancellationToken.None);
        }

        public CommandState GetCommandState(ReturnKeyCommandArgs args, Func<CommandState> nextHandler)
        {
            return nextHandler();
        }

        public void ExecuteCommand(ReturnKeyCommandArgs args, Action nextHandler)
        {
            // Check to see if the current line starts with exterior trivia. If so, we'll take over.
            // If not, let the nextHandler run.

            var caretPosition = args.TextView.GetCaretPoint(args.SubjectBuffer) ?? -1;
            if (caretPosition < 0)
            {
                nextHandler();
                return;
            }

            var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                nextHandler();
                return;
            }

            var text = document
                .GetTextAsync(CancellationToken.None)
                .WaitAndGetResult(CancellationToken.None);

            var currentLine = text.Lines.GetLineFromPosition(caretPosition);
            var currentLineText = currentLine.ToString();
            var offset = currentLineText.GetFirstNonWhitespaceOffset();
            if (offset == null)
            {
                nextHandler();
                return;
            }

            if (currentLineText.IndexOf(ExteriorTriviaText, StringComparison.Ordinal) != offset)
            {
                nextHandler();
                return;
            }

            // Finally, wait and see if completion is computing. If it is, we want to allow
            // the list to pop up rather than insert a blank line in the buffer.
            if (_completionService.WaitForComputation(args.TextView, args.SubjectBuffer))
            {
                nextHandler();
                return;
            }

            // According to JasonMal, the text undo history is associated with the surface buffer
            // in projection buffer scenarios, so the following line's usage of the surface buffer
            // is correct.
            using (var transaction = _undoHistoryRegistry.GetHistory(args.TextView.TextBuffer).CreateTransaction(EditorFeaturesResources.InsertNewLine))
            {
                var editorOperations = _editorOperationsFactoryService.GetEditorOperations(args.TextView);
                editorOperations.InsertNewLine();

                CompleteComment(args.SubjectBuffer, args.TextView, caretPosition, InsertOnEnterTyped, CancellationToken.None);

                // Since we're wrapping the ENTER key undo transaction, we always complete
                // the transaction -- even if we didn't generate anything.
                transaction.Complete();
            }
        }

        public CommandState GetCommandState(InsertCommentCommandArgs args, Func<CommandState> nextHandler)
        {
            var caretPosition = args.TextView.GetCaretPoint(args.SubjectBuffer) ?? -1;
            if (caretPosition < 0)
            {
                return CommandState.Unavailable;
            }

            var document = args.SubjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return CommandState.Unavailable;
            }

            TMemberNode targetMember = null;
            _waitIndicator.Wait("IntelliSense", allowCancel: true, action: c =>
            {
                var text = document.GetTextAsync(c.CancellationToken).WaitAndGetResult(c.CancellationToken);
                var syntaxTree = document.GetSyntaxTreeAsync(c.CancellationToken).WaitAndGetResult(c.CancellationToken);
                targetMember = GetTargetMember(syntaxTree, text, caretPosition, c.CancellationToken);
            });

            return targetMember != null
                ? CommandState.Available
                : CommandState.Unavailable;
        }

        public void ExecuteCommand(InsertCommentCommandArgs args, Action nextHandler)
        {
            var originalCaretPosition = args.TextView.GetCaretPoint(args.SubjectBuffer) ?? -1;

            _waitIndicator.Wait(
                title: EditorFeaturesResources.DocumentationComment,
                message: EditorFeaturesResources.InsertingDocumentationComment,
                allowCancel: true,
                action: w =>
                {
                    if (!CompleteComment(args.SubjectBuffer, args.TextView, originalCaretPosition, InsertOnCommandInvoke, w.CancellationToken))
                    {
                        nextHandler();
                    }
                });
        }
    }
}
