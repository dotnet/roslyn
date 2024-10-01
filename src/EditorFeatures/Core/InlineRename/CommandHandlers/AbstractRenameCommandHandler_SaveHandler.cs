// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.InlineRename;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;

internal abstract partial class AbstractRenameCommandHandler : ICommandHandler<SaveCommandArgs>
{
    public CommandState GetCommandState(SaveCommandArgs args)
        => GetCommandState();

    public bool ExecuteCommand(SaveCommandArgs args, CommandExecutionContext context)
    {
        if (renameService.ActiveSession != null)
        {
            if (globalOptionService.ShouldCommitAsynchronously())
            {
                // No-op if commit is async, because when commit is async,
                // it might complete after the save command finish, workspace could still be dirty.
                return false;
            }
            else
            {
                // Commit the session if commit is sync,
                // because we can make sure rename complete before the save command finish.
                renameService.ActiveSession.Commit(previewChanges: false, context.OperationContext);
                SetFocusToTextView(args.TextView);
            }
        }

        return false;
    }
}
