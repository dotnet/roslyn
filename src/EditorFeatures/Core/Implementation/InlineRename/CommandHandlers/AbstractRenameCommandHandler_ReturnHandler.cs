﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal abstract partial class AbstractRenameCommandHandler : ICommandHandler<ReturnKeyCommandArgs>
    {
        public CommandState GetCommandState(ReturnKeyCommandArgs args)
            => GetCommandState();

        public bool ExecuteCommand(ReturnKeyCommandArgs args, CommandExecutionContext context)
        {
            if (_renameService.ActiveSession != null)
            {
                _renameService.ActiveSession.Commit();
                SetFocusToTextView(args.TextView);
                return true;
            }

            return false;
        }
    }
}
