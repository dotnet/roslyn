// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class RenameCommandHandler : ICommandHandler<EscapeKeyCommandArgs>
    {
        public CommandState GetCommandState(EscapeKeyCommandArgs args, Func<CommandState> nextHandler)
        {
            return GetCommandState(nextHandler);
        }

        public void ExecuteCommand(EscapeKeyCommandArgs args, Action nextHandler)
        {
            if (_renameService.ActiveSession != null)
            {
                _renameService.ActiveSession.Cancel();
                (args.TextView as IWpfTextView).VisualElement.Focus();
            }
            else
            {
                nextHandler();
            }
        }
    }
}
