// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Snippets;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Snippets;

using Workspace = Microsoft.CodeAnalysis.Workspace;

internal abstract class AbstractSnippetCommandHandler(
    IThreadingContext threadingContext,
    EditorOptionsService editorOptionsService,
    IVsService<IVsTextManager2> textManager) :
    ICommandHandler<TabKeyCommandArgs>,
    ICommandHandler<BackTabKeyCommandArgs>,
    ICommandHandler<ReturnKeyCommandArgs>,
    ICommandHandler<EscapeKeyCommandArgs>,
    ICommandHandler<InsertSnippetCommandArgs>,
    IChainedCommandHandler<AutomaticLineEnderCommandArgs>
{
    protected readonly IThreadingContext ThreadingContext = threadingContext;
    private readonly EditorOptionsService _editorOptionsService = editorOptionsService;
    private readonly IVsService<IVsTextManager2> _textManager = textManager;

    public string DisplayName => FeaturesResources.Snippets;

    protected ISnippetExpansionClientFactory GetSnippetExpansionClientFactory(Document document)
        => document.Project.Services.SolutionServices.GetRequiredService<ISnippetExpansionClientFactory>();

    protected abstract bool IsSnippetExpansionContext(Document document, int startPosition, CancellationToken cancellationToken);
    protected abstract bool TryInvokeInsertionUI(ITextView textView, ITextBuffer subjectBuffer, bool surroundWith = false);

    protected virtual bool TryInvokeSnippetPickerOnQuestionMark(ITextView textView, ITextBuffer textBuffer)
        => false;

    public bool ExecuteCommand(TabKeyCommandArgs args, CommandExecutionContext context)
    {
        this.ThreadingContext.ThrowIfNotOnUIThread();
        if (AreSnippetsEnabledWithClient(args, out var snippetExpansionClient)
            && snippetExpansionClient.TryHandleTab())
        {
            return true;
        }

        if (!AreSnippetsEnabled(args))
        {
            return false;
        }

        // Insert snippet/show picker only if we don't have a selection: the user probably wants to indent instead
        if (args.TextView.Selection.IsEmpty)
        {
            if (TryHandleTypedSnippet(args.TextView, args.SubjectBuffer, context.OperationContext.UserCancellationToken))
            {
                return true;
            }

            if (TryInvokeSnippetPickerOnQuestionMark(args.TextView, args.SubjectBuffer))
            {
                return true;
            }
        }

        return false;
    }

    public CommandState GetCommandState(TabKeyCommandArgs args)
    {
        this.ThreadingContext.ThrowIfNotOnUIThread();

        if (!AreSnippetsEnabled(args))
        {
            return CommandState.Unspecified;
        }

        if (!Workspace.TryGetWorkspace(args.SubjectBuffer.AsTextContainer(), out _))
        {
            return CommandState.Unspecified;
        }

        return CommandState.Available;
    }

    public CommandState GetCommandState(AutomaticLineEnderCommandArgs args, Func<CommandState> nextCommandHandler)
    {
        return nextCommandHandler();
    }

    public void ExecuteCommand(AutomaticLineEnderCommandArgs args, Action nextCommandHandler, CommandExecutionContext executionContext)
    {
        this.ThreadingContext.ThrowIfNotOnUIThread();
        if (AreSnippetsEnabledWithClient(args, out var snippetExpansionClient)
            && snippetExpansionClient.IsFullMethodCallSnippet)
        {
            // Commit the snippet. Leave the caret in place, but clear the selection. Subsequent handlers in the
            // chain will handle the remaining Smart Break Line operations.
            snippetExpansionClient.CommitSnippet(leaveCaret: true);
            args.TextView.Selection.Clear();
        }

        nextCommandHandler();
    }

    public bool ExecuteCommand(ReturnKeyCommandArgs args, CommandExecutionContext context)
    {
        this.ThreadingContext.ThrowIfNotOnUIThread();
        if (!AreSnippetsEnabledWithClient(args, out var snippetExpansionClient))
        {
            return false;
        }

        if (snippetExpansionClient.TryHandleReturn())
        {
            return true;
        }

        return false;
    }

    public CommandState GetCommandState(ReturnKeyCommandArgs args)
    {
        this.ThreadingContext.ThrowIfNotOnUIThread();

        if (!AreSnippetsEnabled(args))
        {
            return CommandState.Unspecified;
        }

        if (!Workspace.TryGetWorkspace(args.SubjectBuffer.AsTextContainer(), out _))
        {
            return CommandState.Unspecified;
        }

        return CommandState.Available;
    }

    public bool ExecuteCommand(EscapeKeyCommandArgs args, CommandExecutionContext context)
    {
        this.ThreadingContext.ThrowIfNotOnUIThread();
        if (!AreSnippetsEnabledWithClient(args, out var snippetExpansionClient))
        {
            return false;
        }

        if (snippetExpansionClient.TryHandleEscape())
        {
            return true;
        }

        return false;
    }

    public CommandState GetCommandState(EscapeKeyCommandArgs args)
    {
        this.ThreadingContext.ThrowIfNotOnUIThread();

        if (!AreSnippetsEnabled(args))
        {
            return CommandState.Unspecified;
        }

        if (!Workspace.TryGetWorkspace(args.SubjectBuffer.AsTextContainer(), out _))
        {
            return CommandState.Unspecified;
        }

        return CommandState.Available;
    }

    public bool ExecuteCommand(BackTabKeyCommandArgs args, CommandExecutionContext context)
    {
        this.ThreadingContext.ThrowIfNotOnUIThread();
        if (!AreSnippetsEnabledWithClient(args, out var snippetExpansionClient))
        {
            return false;
        }

        if (snippetExpansionClient.TryHandleBackTab())
        {
            return true;
        }

        return false;
    }

    public CommandState GetCommandState(BackTabKeyCommandArgs args)
    {
        this.ThreadingContext.ThrowIfNotOnUIThread();

        if (!AreSnippetsEnabled(args))
        {
            return CommandState.Unspecified;
        }

        if (!Workspace.TryGetWorkspace(args.SubjectBuffer.AsTextContainer(), out _))
        {
            return CommandState.Unspecified;
        }

        return CommandState.Available;
    }

    public bool ExecuteCommand(InsertSnippetCommandArgs args, CommandExecutionContext context)
    {
        this.ThreadingContext.ThrowIfNotOnUIThread();

        if (!AreSnippetsEnabled(args))
        {
            return false;
        }

        return TryInvokeInsertionUI(args.TextView, args.SubjectBuffer);
    }

    public CommandState GetCommandState(InsertSnippetCommandArgs args)
    {
        this.ThreadingContext.ThrowIfNotOnUIThread();

        if (!AreSnippetsEnabled(args))
        {
            return CommandState.Unspecified;
        }

        if (!args.SubjectBuffer.TryGetWorkspace(out var workspace) ||
            !workspace.CanApplyChange(ApplyChangesKind.ChangeDocument))
        {
            return CommandState.Unspecified;
        }

        return CommandState.Available;
    }

    protected bool TryHandleTypedSnippet(ITextView textView, ITextBuffer subjectBuffer, CancellationToken cancellationToken)
    {
        this.ThreadingContext.ThrowIfNotOnUIThread();

        var document = subjectBuffer.CurrentSnapshot.GetOpenDocumentInCurrentContextWithChanges();
        if (document == null)
        {
            return false;
        }

        var currentText = subjectBuffer.AsTextContainer().CurrentText;
        var syntaxFactsService = document.GetLanguageService<ISyntaxFactsService>();

        var endPositionInSubjectBuffer = textView.GetCaretPoint(subjectBuffer);
        if (endPositionInSubjectBuffer == null)
        {
            return false;
        }

        if (!SnippetUtilities.TryGetWordOnLeft(endPositionInSubjectBuffer.Value.Position, currentText, syntaxFactsService, out var span))
        {
            return false;
        }

        if (!IsSnippetExpansionContext(document, span.Value.Start, cancellationToken))
        {
            return false;
        }

        return GetSnippetExpansionClientFactory(document).GetOrCreateSnippetExpansionClient(document, textView, subjectBuffer).TryInsertExpansion(span.Value.Start, span.Value.End, cancellationToken);
    }

    protected bool TryGetExpansionManager(out IVsExpansionManager expansionManager)
    {
        var textManager = this.ThreadingContext.JoinableTaskFactory.Run(() => _textManager.GetValueOrNullAsync(CancellationToken.None));
        if (textManager == null)
        {
            expansionManager = null;
            return false;
        }

        textManager.GetExpansionManager(out expansionManager);
        return expansionManager != null;
    }

#nullable enable
    protected bool AreSnippetsEnabled(EditorCommandArgs args)
    {
        // Don't execute in cloud environment, should be handled by LSP
        if (args.SubjectBuffer.IsInLspEditorContext())
        {
            return false;
        }

        if (!_editorOptionsService.GlobalOptions.GetOption(SnippetsOptionsStorage.Snippets))
        {
            return false;
        }

        var textContainer = args.SubjectBuffer.AsTextContainer();
        if (Workspace.TryGetWorkspace(textContainer, out var workspace) && workspace.Kind == WorkspaceKind.Interactive)
        {
            // TODO (https://github.com/dotnet/roslyn/issues/5107): enable in interactive
            return false;
        }

        return true;
    }

    protected bool AreSnippetsEnabledWithClient(EditorCommandArgs args, [NotNullWhen(true)] out SnippetExpansionClient? snippetExpansionClient)
    {
        if (!AreSnippetsEnabled(args))
        {
            snippetExpansionClient = null;
            return false;
        }

        var document = args.SubjectBuffer.AsTextContainer().GetOpenDocumentInCurrentContext();
        if (document is null)
        {
            snippetExpansionClient = null;
            return false;
        }

        var expansionClientFactory = document.Project.Services.SolutionServices.GetService<ISnippetExpansionClientFactory>();
        snippetExpansionClient = expansionClientFactory?.TryGetSnippetExpansionClient(args.TextView);
        return snippetExpansionClient is not null;
    }
}
