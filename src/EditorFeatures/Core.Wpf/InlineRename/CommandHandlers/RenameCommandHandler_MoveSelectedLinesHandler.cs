// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class RenameCommandHandler :
        VSCommanding.ICommandHandler<MoveSelectedLinesUpCommandArgs>, VSCommanding.ICommandHandler<MoveSelectedLinesDownCommandArgs>
    {
        public VSCommanding.CommandState GetCommandState(MoveSelectedLinesUpCommandArgs args)
        {
            return VSCommanding.CommandState.Unspecified;
        }

        public bool ExecuteCommand(MoveSelectedLinesUpCommandArgs args, CommandExecutionContext context)
        {
            CommitIfActive(args);
            return false;
        }

        public VSCommanding.CommandState GetCommandState(MoveSelectedLinesDownCommandArgs args)
        {
            return VSCommanding.CommandState.Unspecified;
        }

        public bool ExecuteCommand(MoveSelectedLinesDownCommandArgs args, CommandExecutionContext context)
        {
            CommitIfActive(args);
            return false;
        }
    }
}
