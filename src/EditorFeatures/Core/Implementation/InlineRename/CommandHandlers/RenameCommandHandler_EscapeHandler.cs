// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.UI.Commanding;
using Microsoft.VisualStudio.Text.UI.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class RenameCommandHandler : ICommandHandler<EscapeKeyCommandArgs>
    {
        public CommandState GetCommandState(EscapeKeyCommandArgs args)
        {
            return GetCommandState();
        }

        public bool ExecuteCommand(EscapeKeyCommandArgs args)
        {
            if (_renameService.ActiveSession != null)
            {
                _renameService.ActiveSession.Cancel();
                (args.TextView as IWpfTextView).VisualElement.Focus();
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
