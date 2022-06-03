// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal abstract partial class AbstractRenameCommandHandler :
        ICommandHandler<LineStartCommandArgs>, ICommandHandler<LineEndCommandArgs>,
        ICommandHandler<LineStartExtendCommandArgs>, ICommandHandler<LineEndExtendCommandArgs>
    {
        public CommandState GetCommandState(LineStartCommandArgs args)
            => GetCommandState();

        public CommandState GetCommandState(LineEndCommandArgs args)
            => GetCommandState();

        public CommandState GetCommandState(LineStartExtendCommandArgs args)
            => GetCommandState();

        public CommandState GetCommandState(LineEndExtendCommandArgs args)
            => GetCommandState();

        public bool ExecuteCommand(LineStartCommandArgs args, CommandExecutionContext context)
            => HandleLineStartOrLineEndCommand(args.SubjectBuffer, args.TextView, lineStart: true, extendSelection: false);

        public bool ExecuteCommand(LineEndCommandArgs args, CommandExecutionContext context)
            => HandleLineStartOrLineEndCommand(args.SubjectBuffer, args.TextView, lineStart: false, extendSelection: false);

        public bool ExecuteCommand(LineStartExtendCommandArgs args, CommandExecutionContext context)
            => HandleLineStartOrLineEndCommand(args.SubjectBuffer, args.TextView, lineStart: true, extendSelection: true);

        public bool ExecuteCommand(LineEndExtendCommandArgs args, CommandExecutionContext context)
            => HandleLineStartOrLineEndCommand(args.SubjectBuffer, args.TextView, lineStart: false, extendSelection: true);

        private bool HandleLineStartOrLineEndCommand(ITextBuffer subjectBuffer, ITextView view, bool lineStart, bool extendSelection)
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
                    var newPoint = lineStart ? span.Start : span.End;
                    if (newPoint == caretPoint.Value && (view.Selection.IsEmpty || extendSelection))
                    {
                        // We're already at a boundary, let the editor handle the command
                        return false;
                    }

                    // The PointTrackingMode should not matter because we are not tracking between
                    // versions, and the PositionAffinity is set towards the identifier.
                    var newPointInView = view.BufferGraph.MapUpToBuffer(
                        newPoint,
                        PointTrackingMode.Negative,
                        lineStart ? PositionAffinity.Successor : PositionAffinity.Predecessor,
                        view.TextBuffer);

                    if (!newPointInView.HasValue)
                    {
                        return false;
                    }

                    if (extendSelection)
                    {
                        view.Selection.Select(view.Selection.AnchorPoint, new VirtualSnapshotPoint(newPointInView.Value));
                    }
                    else
                    {
                        view.Selection.Clear();
                    }

                    view.Caret.MoveTo(newPointInView.Value);
                    return true;
                }
            }

            return false;
        }
    }
}
