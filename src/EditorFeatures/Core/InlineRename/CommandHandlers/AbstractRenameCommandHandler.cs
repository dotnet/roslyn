// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.InlineRename;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;

internal abstract partial class AbstractRenameCommandHandler(
    IThreadingContext threadingContext,
    InlineRenameService renameService,
    IGlobalOptionService globalOptionService,
    IAsynchronousOperationListener listener)
{
    public string DisplayName => EditorFeaturesResources.Rename;

    protected abstract void SetFocusToTextView(ITextView textView);

    protected abstract void SetFocusToAdornment(ITextView textView);

    private CommandState GetCommandState(Func<CommandState> nextHandler)
    {
        if (renameService.ActiveSession != null)
        {
            return CommandState.Available;
        }

        return nextHandler();
    }

    private CommandState GetCommandState()
        => renameService.ActiveSession != null ? CommandState.Available : CommandState.Unspecified;

    private void HandlePossibleTypingCommand<TArgs>(TArgs args, Action nextHandler, IUIThreadOperationContext operationContext, Action<InlineRenameSession, IUIThreadOperationContext, SnapshotSpan> actionIfInsideActiveSpan)
        where TArgs : EditorCommandArgs
    {
        if (renameService.ActiveSession == null)
        {
            nextHandler();
            return;
        }

        if (renameService.ActiveSession.IsCommitInProgress)
        {
            // When rename commit is in progress, swallow the command so it won't change the workspace
            return;
        }

        var selectedSpans = args.TextView.Selection.GetSnapshotSpansOnBuffer(args.SubjectBuffer);

        if (selectedSpans.Count > 1)
        {
            // If we have multiple spans active, then that means we have something like box
            // selection going on. In this case, we'll just forward along.
            nextHandler();
            return;
        }

        var singleSpan = selectedSpans.Single();
        if (renameService.ActiveSession.TryGetContainingEditableSpan(singleSpan.Start, out var containingSpan) &&
            containingSpan.Contains(singleSpan))
        {
            actionIfInsideActiveSpan(renameService.ActiveSession, operationContext, containingSpan);
        }
        else if (renameService.ActiveSession.IsInOpenTextBuffer(singleSpan.Start))
        {
            HandleTypingOutsideEditableSpan(args, operationContext);
            nextHandler();
        }
        else
        {
            nextHandler();
            return;
        }
    }

    private void HandleTypingOutsideEditableSpan(EditorCommandArgs args, IUIThreadOperationContext operationContext)
        => CommitIfSynchronousOrCancelIfAsynchronous(args, operationContext, placeCaretAtTheEndOfIdentifier: true);

    private void CommitIfActive(EditorCommandArgs args, IUIThreadOperationContext operationContext, bool placeCaretAtTheEndOfIdentifier = true)
    {
        if (renameService.ActiveSession != null)
        {
            var selection = args.TextView.Selection.VirtualSelectedSpans.First();
            Commit(operationContext);
            if (placeCaretAtTheEndOfIdentifier)
            {
                var translatedSelection = selection.TranslateTo(args.TextView.TextBuffer.CurrentSnapshot);
                args.TextView.Selection.Select(translatedSelection.Start, translatedSelection.End);
                args.TextView.Caret.MoveTo(translatedSelection.End);
            }
        }
    }

    /// <summary>
    /// Commit() will be called if rename commit is sync. Editor command would be executed after the rename operation complete and it is our legacy behavior.
    /// Cancel() will be called if rename is async. It also matches the other editor's behavior (like VS LSP and VSCode).
    /// </summary>
    private void CommitIfSynchronousOrCancelIfAsynchronous(EditorCommandArgs args, IUIThreadOperationContext operationContext, bool placeCaretAtTheEndOfIdentifier)
    {
        if (globalOptionService.ShouldCommitAsynchronously())
            renameService.ActiveSession?.Cancel();
        else
            CommitIfActive(args, operationContext, placeCaretAtTheEndOfIdentifier);
    }

    private void Commit(IUIThreadOperationContext operationContext)
    {
        RoslynDebug.AssertNotNull(renameService.ActiveSession);
        renameService.ActiveSession.Commit(previewChanges: false, operationContext);
    }

    private bool IsRenameCommitInProgress()
        => renameService.ActiveSession?.IsCommitInProgress is true;
}
