// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;

internal abstract partial class AbstractRenameCommandHandler : ICommandHandler<ReturnKeyCommandArgs>
{
    public CommandState GetCommandState(ReturnKeyCommandArgs args)
        => GetCommandState();

    public bool ExecuteCommand(ReturnKeyCommandArgs args, CommandExecutionContext context)
    {
        if (renameService.ActiveSession != null)
        {
            var token = listener.BeginAsyncOperation(nameof(CommitAndSetFocusAsync));
            _ = CommitAndSetFocusAsync(renameService.ActiveSession, args.TextView, context.OperationContext)
                .ReportNonFatalErrorAsync().CompletesAsyncOperation(token);
            return true;
        }

        return false;
    }

    protected virtual async Task CommitAndSetFocusAsync(InlineRenameSession activeSession, ITextView textView, IUIThreadOperationContext operationContext)
    {
        // ConfigureAwait(true) because UI thread is needed to change the focus of text view.
        await activeSession.CommitAsync(previewChanges: false, operationContext).ConfigureAwait(true);
        SetFocusToTextView(textView);
    }
}
