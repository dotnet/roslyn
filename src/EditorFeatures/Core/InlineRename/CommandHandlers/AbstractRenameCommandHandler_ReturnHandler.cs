// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Roslyn.Utilities;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;

internal abstract partial class AbstractRenameCommandHandler : ICommandHandler<ReturnKeyCommandArgs>
{
    public CommandState GetCommandState(ReturnKeyCommandArgs args)
        => GetCommandState();

    public bool ExecuteCommand(ReturnKeyCommandArgs args, CommandExecutionContext context)
    {
        if (_renameService.ActiveSession != null)
        {
            var token = _listener.BeginAsyncOperation(string.Concat(nameof(ExecuteCommand), ".", nameof(ReturnKeyCommandArgs)));
            _ = CommitAndSetFocusAsync(_renameService.ActiveSession, args.TextView, context.OperationContext).ReportNonFatalErrorAsync().CompletesAsyncOperation(token);
            return true;
        }

        return false;
    }

    protected virtual async Task CommitAndSetFocusAsync(InlineRenameSession activeSession, ITextView textView, IUIThreadOperationContext operationContext)
    {
        // ConfigureAwait(true) because it needs to set focus in UI later.
        await CommitAsync(operationContext).ConfigureAwait(true);
        SetFocusToTextView(textView);
    }
}
