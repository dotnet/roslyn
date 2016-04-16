// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class RenameCommandHandler :
        ICommandHandler<UndoCommandArgs>, ICommandHandler<RedoCommandArgs>
    {
        public CommandState GetCommandState(UndoCommandArgs args, Func<CommandState> nextHandler)
        {
            return GetCommandState(nextHandler);
        }

        public CommandState GetCommandState(RedoCommandArgs args, Func<CommandState> nextHandler)
        {
            return GetCommandState(nextHandler);
        }

        public void ExecuteCommand(UndoCommandArgs args, Action nextHandler)
        {
            if (_renameService.ActiveSession != null)
            {
                for (int i = 0; i < args.Count && _renameService.ActiveSession != null; i++)
                {
                    _renameService.ActiveSession.UndoManager.Undo(args.SubjectBuffer);
                }
            }
            else
            {
                nextHandler();
            }
        }

        public void ExecuteCommand(RedoCommandArgs args, Action nextHandler)
        {
            if (_renameService.ActiveSession != null)
            {
                for (int i = 0; i < args.Count && _renameService.ActiveSession != null; i++)
                {
                    _renameService.ActiveSession.UndoManager.Redo(args.SubjectBuffer);
                }
            }
            else
            {
                nextHandler();
            }
        }
    }
}
