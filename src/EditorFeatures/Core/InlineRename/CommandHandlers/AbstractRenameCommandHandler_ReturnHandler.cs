// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;

internal abstract partial class AbstractRenameCommandHandler : ICommandHandler<ReturnKeyCommandArgs>
{
    public CommandState GetCommandState(ReturnKeyCommandArgs args)
        => GetCommandState();

    public bool ExecuteCommand(ReturnKeyCommandArgs args, CommandExecutionContext context)
    {
        if (_renameService.ActiveSession != null)
        {
            // Prevent Editor's typing responsiveness auto canceling the rename operation.
            // InlineRenameSession will call IUIThreadOperationExecutor to sets up our own IUIThreadOperationContext
            context.OperationContext.TakeOwnership();
            _ = CommitAndSetFocusAsync(_renameService.ActiveSession, args.TextView).ReportNonFatalErrorAsync();
            return true;
        }

        return false;
    }

    protected virtual async Task CommitAndSetFocusAsync(InlineRenameSession activeSession, ITextView textView)
    {
        await activeSession.CommitAsync(previewChanges: false).ConfigureAwait(true);
        SetFocusToTextView(textView);
    }
}
