// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;

internal abstract partial class AbstractRenameCommandHandler : ICommandHandler<SaveCommandArgs>
{
    public CommandState GetCommandState(SaveCommandArgs args)
        => GetCommandState();

    public bool ExecuteCommand(SaveCommandArgs args, CommandExecutionContext context)
    {
        if (_renameService.ActiveSession != null)
        {
            // Need to commit the rename session synchronously to make sure save command saves the changes from rename change.
            _ = _renameService.ActiveSession.CommitXAsync(previewChanges: false, forceCommitSynchronously: true, context.OperationContext.UserCancellationToken);
            SetFocusToTextView(args.TextView);
        }

        return false;
    }
}
