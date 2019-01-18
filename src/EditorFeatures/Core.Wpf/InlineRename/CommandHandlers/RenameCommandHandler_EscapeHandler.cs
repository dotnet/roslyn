// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Text.Editor;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class RenameCommandHandler : VSCommanding.ICommandHandler<EscapeKeyCommandArgs>
    {
        public VSCommanding.CommandState GetCommandState(EscapeKeyCommandArgs args)
        {
            return GetCommandState();
        }

        public bool ExecuteCommand(EscapeKeyCommandArgs args, CommandExecutionContext context)
        {
            if (_renameService.ActiveSession != null)
            {
                _renameService.ActiveSession.Cancel();
                (args.TextView as IWpfTextView).VisualElement.Focus();
                return true;
            }

            return false;
        }
    }
}
