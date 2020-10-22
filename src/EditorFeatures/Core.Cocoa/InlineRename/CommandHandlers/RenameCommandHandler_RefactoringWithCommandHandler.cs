// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class RenameCommandHandler :
        VSCommanding.ICommandHandler<ReorderParametersCommandArgs>,
        VSCommanding.ICommandHandler<RemoveParametersCommandArgs>,
        VSCommanding.ICommandHandler<ExtractInterfaceCommandArgs>,
        VSCommanding.ICommandHandler<EncapsulateFieldCommandArgs>
    {
        public VSCommanding.CommandState GetCommandState(ReorderParametersCommandArgs args)
        {
            return VSCommanding.CommandState.Unspecified;
        }

        public bool ExecuteCommand(ReorderParametersCommandArgs args, CommandExecutionContext context)
        {
            CommitIfActive(args);
            return false;
        }

        public VSCommanding.CommandState GetCommandState(RemoveParametersCommandArgs args)
        {
            return VSCommanding.CommandState.Unspecified;
        }

        public bool ExecuteCommand(RemoveParametersCommandArgs args, CommandExecutionContext context)
        {
            CommitIfActive(args);
            return false;
        }

        public VSCommanding.CommandState GetCommandState(ExtractInterfaceCommandArgs args)
        {
            return VSCommanding.CommandState.Unspecified;
        }

        public bool ExecuteCommand(ExtractInterfaceCommandArgs args, CommandExecutionContext context)
        {
            CommitIfActive(args);
            return false;
        }

        public VSCommanding.CommandState GetCommandState(EncapsulateFieldCommandArgs args)
        {
            return VSCommanding.CommandState.Unspecified;
        }

        public bool ExecuteCommand(EncapsulateFieldCommandArgs args, CommandExecutionContext context)
        {
            CommitIfActive(args);
            return false;
        }
    }
}
