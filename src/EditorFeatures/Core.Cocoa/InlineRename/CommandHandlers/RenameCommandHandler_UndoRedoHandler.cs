// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class RenameCommandHandler :
        VSCommanding.ICommandHandler<UndoCommandArgs>, VSCommanding.ICommandHandler<RedoCommandArgs>
    {
        public VSCommanding.CommandState GetCommandState(UndoCommandArgs args)
        {
            return GetCommandState();
        }

        public VSCommanding.CommandState GetCommandState(RedoCommandArgs args)
        {
            return GetCommandState();
        }

        public bool ExecuteCommand(UndoCommandArgs args, CommandExecutionContext context)
        {
            if (_renameService.ActiveSession != null)
            {
                for (var i = 0; i < args.Count && _renameService.ActiveSession != null; i++)
                {
                    _renameService.ActiveSession.UndoManager.Undo(args.SubjectBuffer);
                }

                return true;
            }

            return false;
        }

        public bool ExecuteCommand(RedoCommandArgs args, CommandExecutionContext context)
        {
            if (_renameService.ActiveSession != null)
            {
                for (var i = 0; i < args.Count && _renameService.ActiveSession != null; i++)
                {
                    _renameService.ActiveSession.UndoManager.Redo(args.SubjectBuffer);
                }

                return true;
            }

            return false;
        }
    }
}
