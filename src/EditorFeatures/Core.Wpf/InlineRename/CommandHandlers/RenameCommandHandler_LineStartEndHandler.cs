// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class RenameCommandHandler :
        ICommandHandler<LineStartCommandArgs>, ICommandHandler<LineEndCommandArgs>,
        ICommandHandler<LineStartExtendCommandArgs>, ICommandHandler<LineEndExtendCommandArgs>
    {
        public CommandState GetCommandState(LineStartCommandArgs args)
        {
            return GetCommandState();
        }

        public CommandState GetCommandState(LineEndCommandArgs args)
        {
            return GetCommandState();
        }

        public CommandState GetCommandState(LineStartExtendCommandArgs args)
        {
            return GetCommandState();
        }

        public CommandState GetCommandState(LineEndExtendCommandArgs args)
        {
            return GetCommandState();
        }

        public bool ExecuteCommand(LineStartCommandArgs args, CommandExecutionContext context)
        {
            return HandleLineStartOrLineEndCommand(args.SubjectBuffer, args.TextView, lineStart: true, extendSelection: false);
        }

        public bool ExecuteCommand(LineEndCommandArgs args, CommandExecutionContext context)
        {
            return HandleLineStartOrLineEndCommand(args.SubjectBuffer, args.TextView, lineStart: false, extendSelection: false);
        }

        public bool ExecuteCommand(LineStartExtendCommandArgs args, CommandExecutionContext context)
        {
            return HandleLineStartOrLineEndCommand(args.SubjectBuffer, args.TextView, lineStart: true, extendSelection: true);
        }

        public bool ExecuteCommand(LineEndExtendCommandArgs args, CommandExecutionContext context)
        {
            return HandleLineStartOrLineEndCommand(args.SubjectBuffer, args.TextView, lineStart: false, extendSelection: true);
        }

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
