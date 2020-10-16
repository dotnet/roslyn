﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class RenameCommandHandler :
        ICommandHandler<MoveSelectedLinesUpCommandArgs>, ICommandHandler<MoveSelectedLinesDownCommandArgs>
    {
        public CommandState GetCommandState(MoveSelectedLinesUpCommandArgs args)
            => CommandState.Unspecified;

        public bool ExecuteCommand(MoveSelectedLinesUpCommandArgs args, CommandExecutionContext context)
        {
            CommitIfActive(args);
            return false;
        }

        public CommandState GetCommandState(MoveSelectedLinesDownCommandArgs args)
            => CommandState.Unspecified;

        public bool ExecuteCommand(MoveSelectedLinesDownCommandArgs args, CommandExecutionContext context)
        {
            CommitIfActive(args);
            return false;
        }
    }
}
