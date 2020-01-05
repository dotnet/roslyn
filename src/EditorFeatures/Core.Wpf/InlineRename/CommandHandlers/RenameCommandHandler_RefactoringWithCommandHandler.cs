// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class RenameCommandHandler :
        ICommandHandler<ReorderParametersCommandArgs>,
        ICommandHandler<RemoveParametersCommandArgs>,
        ICommandHandler<ExtractInterfaceCommandArgs>,
        ICommandHandler<EncapsulateFieldCommandArgs>
    {
        public CommandState GetCommandState(ReorderParametersCommandArgs args)
        {
            return CommandState.Unspecified;
        }

        public bool ExecuteCommand(ReorderParametersCommandArgs args, CommandExecutionContext context)
        {
            CommitIfActive(args);
            return false;
        }

        public CommandState GetCommandState(RemoveParametersCommandArgs args)
        {
            return CommandState.Unspecified;
        }

        public bool ExecuteCommand(RemoveParametersCommandArgs args, CommandExecutionContext context)
        {
            CommitIfActive(args);
            return false;
        }

        public CommandState GetCommandState(ExtractInterfaceCommandArgs args)
        {
            return CommandState.Unspecified;
        }

        public bool ExecuteCommand(ExtractInterfaceCommandArgs args, CommandExecutionContext context)
        {
            CommitIfActive(args);
            return false;
        }

        public CommandState GetCommandState(EncapsulateFieldCommandArgs args)
        {
            return CommandState.Unspecified;
        }

        public bool ExecuteCommand(EncapsulateFieldCommandArgs args, CommandExecutionContext context)
        {
            CommitIfActive(args);
            return false;
        }
    }
}
