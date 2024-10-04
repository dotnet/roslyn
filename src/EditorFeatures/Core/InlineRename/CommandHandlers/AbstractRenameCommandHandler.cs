﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
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
    IAsynchronousOperationListener listener)
{
    public string DisplayName => EditorFeaturesResources.Rename;

    protected abstract bool AdornmentShouldReceiveKeyboardNavigation(ITextView textView);

    protected abstract void SetFocusToTextView(ITextView textView);

    protected abstract void SetFocusToAdornment(ITextView textView);

    protected abstract void SetAdornmentFocusToPreviousElement(ITextView textView);

    protected abstract void SetAdornmentFocusToNextElement(ITextView textView);

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
            // It's in a read-only area that is open, so let's commit the rename 
            // and then let the character go through
            CommitIfActive(args, operationContext);
            nextHandler();
        }
        else
        {
            nextHandler();
            return;
        }
    }

    private void CommitIfActive(EditorCommandArgs args, IUIThreadOperationContext operationContext)
    {
        if (renameService.ActiveSession != null)
        {
            var selection = args.TextView.Selection.VirtualSelectedSpans.First();

            Commit(operationContext);

            var translatedSelection = selection.TranslateTo(args.TextView.TextBuffer.CurrentSnapshot);
            args.TextView.Selection.Select(translatedSelection.Start, translatedSelection.End);
            args.TextView.Caret.MoveTo(translatedSelection.End);
        }
    }

    private void Commit(IUIThreadOperationContext operationContext)
    {
        RoslynDebug.AssertNotNull(renameService.ActiveSession);
        renameService.ActiveSession.Commit(previewChanges: false, operationContext);
    }
}
