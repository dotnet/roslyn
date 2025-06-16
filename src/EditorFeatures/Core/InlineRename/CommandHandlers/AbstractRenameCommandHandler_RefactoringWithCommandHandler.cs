// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;

internal abstract partial class AbstractRenameCommandHandler :
    ICommandHandler<ReorderParametersCommandArgs>,
    ICommandHandler<RemoveParametersCommandArgs>,
    ICommandHandler<ExtractInterfaceCommandArgs>,
    ICommandHandler<EncapsulateFieldCommandArgs>
{
    public CommandState GetCommandState(ReorderParametersCommandArgs args)
        => CommandState.Unspecified;

    public bool ExecuteCommand(ReorderParametersCommandArgs args, CommandExecutionContext context)
        => HandleRefactoringCommands();

    public CommandState GetCommandState(RemoveParametersCommandArgs args)
        => CommandState.Unspecified;

    public bool ExecuteCommand(RemoveParametersCommandArgs args, CommandExecutionContext context)
        => HandleRefactoringCommands();

    public CommandState GetCommandState(ExtractInterfaceCommandArgs args)
        => CommandState.Unspecified;

    public bool ExecuteCommand(ExtractInterfaceCommandArgs args, CommandExecutionContext context)
        => HandleRefactoringCommands();

    public CommandState GetCommandState(EncapsulateFieldCommandArgs args)
        => CommandState.Unspecified;

    public bool ExecuteCommand(EncapsulateFieldCommandArgs args, CommandExecutionContext context)
        => HandleRefactoringCommands();

    private bool HandleRefactoringCommands()
    {
        if (IsRenameCommitInProgress())
            return true;

        CancelRenameSession();
        return false;
    }
}
