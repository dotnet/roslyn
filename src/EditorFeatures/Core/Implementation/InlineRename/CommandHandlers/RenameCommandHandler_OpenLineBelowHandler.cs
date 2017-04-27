// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Text.UI.Commanding.Commands;
using VSC = Microsoft.VisualStudio.Text.UI.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class RenameCommandHandler : VSC.ICommandHandler<OpenLineBelowCommandArgs>
    {
        public VSC.CommandState GetCommandState(OpenLineBelowCommandArgs args)
        {
            return GetCommandState();
        }

        public bool ExecuteCommand(OpenLineBelowCommandArgs args)
        {
            return HandlePossibleTypingCommand(args.TextView, args.SubjectBuffer, span =>
            {
                if (_renameService.ActiveSession != null)
                {
                    _renameService.ActiveSession.Commit();
                }

                // TODO: How?
                //nextHandler();
            });
        }
    }
}
