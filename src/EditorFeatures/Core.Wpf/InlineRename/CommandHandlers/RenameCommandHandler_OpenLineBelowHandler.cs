// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class RenameCommandHandler : IChainedCommandHandler<OpenLineBelowCommandArgs>
    {
        public VSCommanding.CommandState GetCommandState(OpenLineBelowCommandArgs args, Func<VSCommanding.CommandState> nextHandler)
        {
            return GetCommandState(nextHandler);
        }

        public void ExecuteCommand(OpenLineBelowCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            HandlePossibleTypingCommand(args, nextHandler, span =>
            {
                if (_renameService.ActiveSession != null)
                {
                    _renameService.ActiveSession.Commit();
                }

                nextHandler();
            });
        }
    }
}
