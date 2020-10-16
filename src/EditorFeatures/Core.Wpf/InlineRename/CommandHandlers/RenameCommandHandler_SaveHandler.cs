﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class RenameCommandHandler : ICommandHandler<SaveCommandArgs>
    {
        public CommandState GetCommandState(SaveCommandArgs args)
            => GetCommandState();

        public bool ExecuteCommand(SaveCommandArgs args, CommandExecutionContext context)
        {
            if (_renameService.ActiveSession != null)
            {
                _renameService.ActiveSession.Commit();
                ((IWpfTextView)args.TextView).VisualElement.Focus();
            }

            return false;
        }
    }
}
