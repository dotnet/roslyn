// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.DocumentationComments;
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
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.DocumentationComments;

internal abstract class AbstractDocumentationCommentCommandHandler :
    IChainedCommandHandler<TypeCharCommandArgs>,
    ICommandHandler<ReturnKeyCommandArgs>,
    ICommandHandler<InsertCommentCommandArgs>,
    IChainedCommandHandler<OpenLineAboveCommandArgs>,
    IChainedCommandHandler<OpenLineBelowCommandArgs>
{
    private readonly IUIThreadOperationExecutor _uiThreadOperationExecutor;
    private readonly ITextUndoHistoryRegistry _undoHistoryRegistry;
    private readonly IEditorOperationsFactoryService _editorOperationsFactoryService;
    private readonly EditorOptionsService _editorOptionsService;
    private readonly CopilotGenerateDocumentationCommentManager _generateDocumentationCommentManager;

    protected AbstractDocumentationCommentCommandHandler(
        IUIThreadOperationExecutor uiThreadOperationExecutor,
        ITextUndoHistoryRegistry undoHistoryRegistry,
        IEditorOperationsFactoryService editorOperationsFactoryService,
        EditorOptionsService editorOptionsService,
        CopilotGenerateDocumentationCommentManager generateDocumentationCommentManager)
    {
        Contract.ThrowIfNull(uiThreadOperationExecutor);
        Contract.ThrowIfNull(undoHistoryRegistry);
        Contract.ThrowIfNull(editorOperationsFactoryService);

        _uiThreadOperationExecutor = uiThreadOperationExecutor;
        _undoHistoryRegistry = undoHistoryRegistry;
        _editorOperationsFactoryService = editorOperationsFactoryService;
        _editorOptionsService = editorOptionsService;
        _generateDocumentationCommentManager = generateDocumentationCommentManager;
    }

    protected abstract string ExteriorTriviaText { get; }

    private char TriggerCharacter => ExteriorTriviaText[^1];

    public string DisplayName => EditorFeaturesResources.Documentation_Comment;

    private static DocumentationCommentSnippet? InsertOnCharacterTyped(IDocumentationCommentSnippetService service, ParsedDocument document, int position, DocumentationCommentOptions options, CancellationToken cancellationToken)
        => service.GetDocumentationCommentSnippetOnCharacterTyped(document, position, options, cancellationToken);

    private static DocumentationCommentSnippet? InsertOnEnterTyped(IDocumentationCommentSnippetService service, ParsedDocument document, int position, DocumentationCommentOptions options, CancellationToken cancellationToken)
        => service.GetDocumentationCommentSnippetOnEnterTyped(document, position, options, cancellationToken);

    private static DocumentationCommentSnippet? InsertOnCommandInvoke(IDocumentationCommentSnippetService service, ParsedDocument document, int position, DocumentationCommentOptions options, CancellationToken cancellationToken)
        => service.GetDocumentationCommentSnippetOnCommandInvoke(document, position, options, cancellationToken);

    private static void ApplySnippet(DocumentationCommentSnippet snippet, ITextBuffer subjectBuffer, ITextView textView)
    {
        var replaceSpan = snippet.SpanToReplace.ToSpan();
        subjectBuffer.Replace(replaceSpan, snippet.SnippetText);
        textView.TryMoveCaretToAndEnsureVisible(subjectBuffer.CurrentSnapshot.GetPoint(replaceSpan.Start + snippet.CaretOffset));
    }

    private bool CompleteComment(
        ITextBuffer subjectBuffer,
        ITextView textView,
        Func<IDocumentationCommentSnippetService, ParsedDocument, int, DocumentationCommentOptions, CancellationToken, DocumentationCommentSnippet?> getSnippetAction,
        CancellationToken cancellationToken)
    {
        var caretPosition = textView.GetCaretPoint(subjectBuffer) ?? -1;
        if (caretPosition < 0)
            return false;

        var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
        if (document == null)
            return false;

        var service = document.GetRequiredLanguageService<IDocumentationCommentSnippetService>();
        var parsedDocument = ParsedDocument.CreateSynchronously(document, cancellationToken);
        var options = subjectBuffer.GetDocumentationCommentOptions(_editorOptionsService, document.Project.Services);

        // Apply snippet in reverse order so that the first applied snippet doesn't affect span of next snippets.
        var snapshots = textView.Selection.GetSnapshotSpansOnBuffer(subjectBuffer).OrderByDescending(s => s.Span.Start);
        var returnValue = false;
        foreach (var snapshot in snapshots)
        {
            var snippet = getSnippetAction(service, parsedDocument, snapshot.Span.Start, options, cancellationToken);
            if (snippet != null)
            {
                ApplySnippet(snippet, subjectBuffer, textView);
                var oldSnapshot = subjectBuffer.CurrentSnapshot;
                var oldCaret = textView.Caret.Position.VirtualBufferPosition;

                returnValue = true;

                _generateDocumentationCommentManager.TriggerDocumentationCommentProposalGeneration(document, snippet, oldSnapshot, oldCaret, textView, cancellationToken);
            }
        }

        return returnValue;
    }
    public CommandState GetCommandState(TypeCharCommandArgs args, Func<CommandState> nextHandler)
        => nextHandler();

    public void ExecuteCommand(TypeCharCommandArgs args, Action nextHandler, CommandExecutionContext context)
    {
        // Ensure the character is actually typed in the editor
        nextHandler();

        if (args.TypedChar != TriggerCharacter)
            return;

        // Don't execute in cloud environment, as we let LSP handle that
        if (args.SubjectBuffer.IsInLspEditorContext())
            return;

        CompleteComment(args.SubjectBuffer, args.TextView, InsertOnCharacterTyped, CancellationToken.None);
    }

    public CommandState GetCommandState(ReturnKeyCommandArgs args)
        => CommandState.Unspecified;

    public bool ExecuteCommand(ReturnKeyCommandArgs args, CommandExecutionContext context)
    {
        var cancellationToken = context.OperationContext.UserCancellationToken;

        // Don't execute in cloud environment, as we let LSP handle that
        if (args.SubjectBuffer.IsInLspEditorContext())
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

        if (!CurrentLineStartsWithExteriorTrivia(args.SubjectBuffer, originalPosition, cancellationToken))
        {
            return false;
        }

        // According to JasonMal, the text undo history is associated with the surface buffer
        // in projection buffer scenarios, so the following line's usage of the surface buffer
        // is correct.
        using var transaction = _undoHistoryRegistry.GetHistory(args.TextView.TextBuffer).CreateTransaction(EditorFeaturesResources.Insert_new_line);
        var editorOperations = _editorOperationsFactoryService.GetEditorOperations(args.TextView);
        editorOperations.InsertNewLine();

        CompleteComment(args.SubjectBuffer, args.TextView, InsertOnEnterTyped, CancellationToken.None);

        // Since we're wrapping the ENTER key undo transaction, we always complete
        // the transaction -- even if we didn't generate anything.
        transaction.Complete();

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
        _uiThreadOperationExecutor.Execute("IntelliSense", defaultDescription: "", allowCancellation: true, showProgress: false, action: c =>
        {
            var parsedDocument = ParsedDocument.CreateSynchronously(document, c.UserCancellationToken);
            isValidTargetMember = service.IsValidTargetMember(parsedDocument, caretPosition, c.UserCancellationToken);
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

        if (!CurrentLineStartsWithExteriorTrivia(subjectBuffer, caretPosition, context.OperationContext.UserCancellationToken))
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

        InsertExteriorTriviaIfNeeded(service, args.TextView, subjectBuffer, context.OperationContext.UserCancellationToken);
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

        if (!CurrentLineStartsWithExteriorTrivia(subjectBuffer, caretPosition, context.OperationContext.UserCancellationToken))
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

        InsertExteriorTriviaIfNeeded(service, args.TextView, subjectBuffer, context.OperationContext.UserCancellationToken);
    }

    private void InsertExteriorTriviaIfNeeded(IDocumentationCommentSnippetService service, ITextView textView, ITextBuffer subjectBuffer, CancellationToken cancellationToken)
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

        var parsedDocument = ParsedDocument.CreateSynchronously(document, cancellationToken);

        // We only insert exterior trivia if the current line does not start with exterior trivia
        // and the previous line does.

        var currentLine = parsedDocument.Text.Lines.GetLineFromPosition(caretPosition);
        if (currentLine.LineNumber <= 0)
        {
            return;
        }

        var previousLine = parsedDocument.Text.Lines[currentLine.LineNumber - 1];

        if (LineStartsWithExteriorTrivia(currentLine) || !LineStartsWithExteriorTrivia(previousLine))
        {
            return;
        }

        var options = subjectBuffer.GetDocumentationCommentOptions(_editorOptionsService, document.Project.Services);

        var snippet = service.GetDocumentationCommentSnippetFromPreviousLine(options, currentLine, previousLine);
        if (snippet != null)
        {
            ApplySnippet(snippet, subjectBuffer, textView);
        }
    }

    private bool CurrentLineStartsWithExteriorTrivia(ITextBuffer subjectBuffer, int position, CancellationToken cancellationToken)
    {
        var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
        if (document == null)
        {
            return false;
        }

        var parsedDocument = ParsedDocument.CreateSynchronously(document, cancellationToken);
        var currentLine = parsedDocument.Text.Lines.GetLineFromPosition(position);

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
