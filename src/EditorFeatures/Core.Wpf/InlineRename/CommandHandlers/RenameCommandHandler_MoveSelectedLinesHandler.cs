// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class RenameCommandHandler :
        ICommandHandler<MoveSelectedLinesUpCommandArgs>, ICommandHandler<MoveSelectedLinesDownCommandArgs>
    {
        public CommandState GetCommandState(MoveSelectedLinesUpCommandArgs args)
        {
            return CommandState.Unspecified;
        }

        public bool ExecuteCommand(MoveSelectedLinesUpCommandArgs args, CommandExecutionContext context)
        {
            CommitIfActive(args);
            return false;
        }

        public CommandState GetCommandState(MoveSelectedLinesDownCommandArgs args)
        {
            return CommandState.Unspecified;
        }

        public bool ExecuteCommand(MoveSelectedLinesDownCommandArgs args, CommandExecutionContext context)
        {
            CommitIfActive(args);
            return false;
        }
    }
}
