// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;

internal abstract partial class AbstractRenameCommandHandler :
    ICommandHandler<UndoCommandArgs>, ICommandHandler<RedoCommandArgs>
{
    public CommandState GetCommandState(UndoCommandArgs args)
        => GetCommandState();

    public CommandState GetCommandState(RedoCommandArgs args)
        => GetCommandState();

    public bool ExecuteCommand(UndoCommandArgs args, CommandExecutionContext context)
    {
        if (renameService.ActiveSession == null)
        {
            return false;
        }

        if (renameService.ActiveSession.IsCommitInProgress)
        {
            return true;
        }

        for (var i = 0; i < args.Count && renameService.ActiveSession != null; i++)
        {
            renameService.ActiveSession.UndoManager.Undo(args.SubjectBuffer);
        }

        return true;
    }

    public bool ExecuteCommand(RedoCommandArgs args, CommandExecutionContext context)
    {
        if (renameService.ActiveSession == null)
        {
            return false;
        }

        if (renameService.ActiveSession.IsCommitInProgress)
        {
            return true;
        }

        for (var i = 0; i < args.Count && renameService.ActiveSession != null; i++)
        {
            renameService.ActiveSession.UndoManager.Redo(args.SubjectBuffer);
        }

        return true;
    }
}
