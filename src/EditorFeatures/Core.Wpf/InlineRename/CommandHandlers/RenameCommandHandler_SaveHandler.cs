// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class RenameCommandHandler : ICommandHandler<SaveCommandArgs>
    {
        public CommandState GetCommandState(SaveCommandArgs args, Func<CommandState> nextHandler)
        {
            return GetCommandState(nextHandler);
        }

        public void ExecuteCommand(SaveCommandArgs args, Action nextHandler)
        {
            if (_renameService.ActiveSession != null)
            {
                _renameService.ActiveSession.Commit();
                ((IWpfTextView)args.TextView).VisualElement.Focus();
            }

            nextHandler();
        }
    }
}
