// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Text.UI.Commanding;
using Microsoft.VisualStudio.Text.UI.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class RenameCommandHandler :
        ICommandHandler<UndoCommandArgs>, ICommandHandler<RedoCommandArgs>
    {
        public CommandState GetCommandState(UndoCommandArgs args)
        {
            return GetCommandState();
        }

        public CommandState GetCommandState(RedoCommandArgs args)
        {
            return GetCommandState();
        }

        public bool ExecuteCommand(UndoCommandArgs args)
        {
            if (_renameService.ActiveSession != null)
            {
                //for (int i = 0; i < args.Count && _renameService.ActiveSession != null; i++)
                //{
                    _renameService.ActiveSession.UndoManager.Undo(args.SubjectBuffer);
                //}

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool ExecuteCommand(RedoCommandArgs args)
        {
            if (_renameService.ActiveSession != null)
            {
                //for (int i = 0; i < args.Count && _renameService.ActiveSession != null; i++)
                //{
                    _renameService.ActiveSession.UndoManager.Redo(args.SubjectBuffer);
                //}

                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
