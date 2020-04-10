// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class RenameCommandHandler :
        ICommandHandler<SelectAllCommandArgs>
    {
        public CommandState GetCommandState(SelectAllCommandArgs args)
            => GetCommandState();

        public bool ExecuteCommand(SelectAllCommandArgs args, CommandExecutionContext context)
            => ExecuteSelectAll(args.SubjectBuffer, args.TextView);

        private bool ExecuteSelectAll(ITextBuffer subjectBuffer, ITextView view)
        {
            if (_renameService.ActiveSession == null)
            {
                return false;
            }

            var caretPoint = view.GetCaretPoint(subjectBuffer);
            if (caretPoint.HasValue)
            {
                if (_renameService.ActiveSession.TryGetContainingEditableSpan(caretPoint.Value, out var span))
                {
                    if (view.Selection.Start.Position != span.Start.Position ||
                        view.Selection.End.Position != span.End.Position)
                    {
                        view.SetSelection(span);
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
