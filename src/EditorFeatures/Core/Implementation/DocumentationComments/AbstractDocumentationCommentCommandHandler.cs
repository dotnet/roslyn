// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Text.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Operations;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.DocumentationComments
{
    internal abstract class AbstractDocumentationCommentCommandHandler<TDocumentationComment, TMemberNode> :
        IChainedCommandHandler<TypeCharCommandArgs>,
        ICommandHandler<ReturnKeyCommandArgs>,
        ICommandHandler<InsertCommentCommandArgs>,
        IChainedCommandHandler<OpenLineAboveCommandArgs>,
        IChainedCommandHandler<OpenLineBelowCommandArgs>
        where TDocumentationComment : SyntaxNode, IStructuredTriviaSyntax
        where TMemberNode : SyntaxNode
    {
        private readonly IWaitIndicator _waitIndicator;
        private readonly ITextUndoHistoryRegistry _undoHistoryRegistry;
        private readonly IEditorOperationsFactoryService _editorOperationsFactoryService;

        protected AbstractDocumentationCommentCommandHandler(
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

        protected abstract string ExteriorTriviaText { get; }

        private char TriggerCharacter
        {
            get { return ExteriorTriviaText[^1]; }
        }

        public string DisplayName => EditorFeaturesResources.Documentation_Comment;

        private static DocumentationCommentSnippet? InsertOnCharacterTyped(IDocumentationCommentSnippetService service, SyntaxTree syntaxTree, SourceText text, int position, DocumentOptionSet options, CancellationToken cancellationToken)
            => service.GetDocumentationCommentSnippetOnCharacterTyped(syntaxTree, text, position, options, cancellationToken);

        private static DocumentationCommentSnippet? InsertOnEnterTyped(IDocumentationCommentSnippetService service, SyntaxTree syntaxTree, SourceText text, int position, DocumentOptionSet options, CancellationToken cancellationToken)
            => service.GetDocumentationCommentSnippetOnEnterTyped(syntaxTree, text, position, options, cancellationToken);

        private static DocumentationCommentSnippet? InsertOnCommandInvoke(IDocumentationCommentSnippetService service, SyntaxTree syntaxTree, SourceText text, int position, DocumentOptionSet options, CancellationToken cancellationToken)
            => service.GetDocumentationCommentSnippetOnCommandInvoke(syntaxTree, text, position, options, cancellationToken);

        private static void ApplySnippet(DocumentationCommentSnippet snippet, ITextBuffer subjectBuffer, ITextView textView)
        {
            var replaceSpan = snippet.SpanToReplace.ToSpan();
            subjectBuffer.Replace(replaceSpan, snippet.SnippetText);
            textView.TryMoveCaretToAndEnsureVisible(subjectBuffer.CurrentSnapshot.GetPoint(replaceSpan.Start + snippet.CaretOffset));
        }

        private static bool CompleteComment(
            ITextBuffer subjectBuffer,
            ITextView textView,
            Func<IDocumentationCommentSnippetService, SyntaxTree, SourceText, int, DocumentOptionSet, CancellationToken, DocumentationCommentSnippet?> getSnippetAction,
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

            var service = document.GetRequiredLanguageService<IDocumentationCommentSnippetService>();
            var syntaxTree = document.GetRequiredSyntaxTreeSynchronously(cancellationToken);
            var text = syntaxTree.GetText(cancellationToken);
            var documentOptions = document.GetOptionsAsync(cancellationToken).WaitAndGetResult(cancellationToken);

            var snippet = getSnippetAction(service, syntaxTree, text, caretPosition, documentOptions, cancellationToken);
            if (snippet != null)
            {
                ApplySnippet(snippet, subjectBuffer, textView);
                return true;
            }
            return false;
        }

        public CommandState GetCommandState(TypeCharCommandArgs args, Func<CommandState> nextHandler)
            => nextHandler();

        public void ExecuteCommand(TypeCharCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            // Ensure the character is actually typed in the editor
            nextHandler();

            if (args.TypedChar != TriggerCharacter)
            {
                return;
            }

            // Don't execute in cloud environment, as we let LSP handle that
            if (args.SubjectBuffer.IsInCloudEnvironmentClientContext())
            {
                return;
            }

            CompleteComment(args.SubjectBuffer, args.TextView, InsertOnCharacterTyped, CancellationToken.None);
        }

        public CommandState GetCommandState(ReturnKeyCommandArgs args)
            => CommandState.Unspecified;

        public bool ExecuteCommand(ReturnKeyCommandArgs args, CommandExecutionContext context)
        {
            // Don't execute in cloud environment, as we let LSP handle that
            if (args.SubjectBuffer.IsInCloudEnvironmentClientContext())
            {
                return false;
            }

            // Check to see if the current line starts with exterior trivia. If so, we'll take over.
            // If not, let the nextHandler run.

            var originalPosition = -1;

            // The original position should be a position that is consistent with the syntax tree, even
            // after Enter is pressed. Thus, we use the start of the first selection if there is one.
            // Otherwise, getting the tokens to the right or the left might return unexpected results.

            if (args.TextView.Selection.SelectedSpans.Count > 0)
            {
                var selectedSpan = args.TextView.Selection
                    .GetSnapshotSpansOnBuffer(args.SubjectBuffer)
                    .FirstOrNull();

                originalPosition = selectedSpan != null
                    ? selectedSpan.Value.Start
                    : args.TextView.GetCaretPoint(args.SubjectBuffer) ?? -1;
            }

            if (originalPosition < 0)
            {
                return false;
            }

            if (!CurrentLineStartsWithExteriorTrivia(args.SubjectBuffer, originalPosition))
            {
                return false;
            }

            // According to JasonMal, the text undo history is associated with the surface buffer
            // in projection buffer scenarios, so the following line's usage of the surface buffer
            // is correct.
            using (var transaction = _undoHistoryRegistry.GetHistory(args.TextView.TextBuffer).CreateTransaction(EditorFeaturesResources.Insert_new_line))
            {
                var editorOperations = _editorOperationsFactoryService.GetEditorOperations(args.TextView);
                editorOperations.InsertNewLine();

                CompleteComment(args.SubjectBuffer, args.TextView, InsertOnEnterTyped, CancellationToken.None);

                // Since we're wrapping the ENTER key undo transaction, we always complete
                // the transaction -- even if we didn't generate anything.
                transaction.Complete();
            }

            return true;
        }

        public CommandState GetCommandState(InsertCommentCommandArgs args)
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
            var service = document.GetRequiredLanguageService<IDocumentationCommentSnippetService>();

            var isValidTargetMember = false;
            _waitIndicator.Wait("IntelliSense", allowCancel: true, action: c =>
            {
                var syntaxTree = document.GetRequiredSyntaxTreeSynchronously(c.CancellationToken);
                var text = syntaxTree.GetText(c.CancellationToken);
                isValidTargetMember = service.IsValidTargetMember(syntaxTree, text, caretPosition, c.CancellationToken);
            });

            return isValidTargetMember
                ? CommandState.Available
                : CommandState.Unavailable;
        }

        public bool ExecuteCommand(InsertCommentCommandArgs args, CommandExecutionContext context)
        {
            using (context.OperationContext.AddScope(allowCancellation: true, EditorFeaturesResources.Inserting_documentation_comment))
            {
                return CompleteComment(args.SubjectBuffer, args.TextView, InsertOnCommandInvoke, context.OperationContext.UserCancellationToken);
            }
        }

        public CommandState GetCommandState(OpenLineAboveCommandArgs args, Func<CommandState> nextHandler)
            => nextHandler();

        public void ExecuteCommand(OpenLineAboveCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            // Check to see if the current line starts with exterior trivia. If so, we'll take over.
            // If not, let the nextHandler run.

            var subjectBuffer = args.SubjectBuffer;
            var caretPosition = args.TextView.GetCaretPoint(subjectBuffer) ?? -1;
            if (caretPosition < 0)
            {
                nextHandler();
                return;
            }

            if (!CurrentLineStartsWithExteriorTrivia(subjectBuffer, caretPosition))
            {
                nextHandler();
                return;
            }

            // Allow nextHandler() to run and then insert exterior trivia if necessary.
            nextHandler();

            var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return;
            }

            var service = document.GetRequiredLanguageService<IDocumentationCommentSnippetService>();

            InsertExteriorTriviaIfNeeded(service, args.TextView, subjectBuffer);
        }

        public CommandState GetCommandState(OpenLineBelowCommandArgs args, Func<CommandState> nextHandler)
            => nextHandler();

        public void ExecuteCommand(OpenLineBelowCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            // Check to see if the current line starts with exterior trivia. If so, we'll take over.
            // If not, let the nextHandler run.

            var subjectBuffer = args.SubjectBuffer;
            var caretPosition = args.TextView.GetCaretPoint(subjectBuffer) ?? -1;
            if (caretPosition < 0)
            {
                nextHandler();
                return;
            }

            if (!CurrentLineStartsWithExteriorTrivia(subjectBuffer, caretPosition))
            {
                nextHandler();
                return;
            }

            var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return;
            }

            var service = document.GetRequiredLanguageService<IDocumentationCommentSnippetService>();

            // Allow nextHandler() to run and the insert exterior trivia if necessary.
            nextHandler();

            InsertExteriorTriviaIfNeeded(service, args.TextView, subjectBuffer);
        }

        private void InsertExteriorTriviaIfNeeded(IDocumentationCommentSnippetService service, ITextView textView, ITextBuffer subjectBuffer)
        {
            var caretPosition = textView.GetCaretPoint(subjectBuffer) ?? -1;
            if (caretPosition < 0)
            {
                return;
            }

            var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return;
            }

            var text = document
                .GetTextAsync(CancellationToken.None)
                .WaitAndGetResult(CancellationToken.None);

            // We only insert exterior trivia if the current line does not start with exterior trivia
            // and the previous line does.

            var currentLine = text.Lines.GetLineFromPosition(caretPosition);
            if (currentLine.LineNumber <= 0)
            {
                return;
            }

            var previousLine = text.Lines[currentLine.LineNumber - 1];

            if (LineStartsWithExteriorTrivia(currentLine) || !LineStartsWithExteriorTrivia(previousLine))
            {
                return;
            }

            var documentOptions = document.GetOptionsAsync(CancellationToken.None).WaitAndGetResult(CancellationToken.None);

            var snippet = service.GetDocumentationCommentSnippetFromPreviousLine(documentOptions, currentLine, previousLine);
            if (snippet != null)
            {
                ApplySnippet(snippet, subjectBuffer, textView);
            }
        }

        private bool CurrentLineStartsWithExteriorTrivia(ITextBuffer subjectBuffer, int position)
        {
            var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
            if (document == null)
            {
                return false;
            }

            var text = document
                .GetTextAsync(CancellationToken.None)
                .WaitAndGetResult(CancellationToken.None);

            var currentLine = text.Lines.GetLineFromPosition(position);

            return LineStartsWithExteriorTrivia(currentLine);
        }

        private bool LineStartsWithExteriorTrivia(TextLine line)
        {
            var lineText = line.ToString();

            var lineOffset = lineText.GetFirstNonWhitespaceOffset() ?? -1;
            if (lineOffset < 0)
            {
                return false;
            }

            return string.CompareOrdinal(lineText, lineOffset, ExteriorTriviaText, 0, ExteriorTriviaText.Length) == 0;
        }
    }
}
