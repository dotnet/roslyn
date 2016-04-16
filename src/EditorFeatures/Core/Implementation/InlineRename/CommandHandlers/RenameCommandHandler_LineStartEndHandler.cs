// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class RenameCommandHandler :
        ICommandHandler<LineStartCommandArgs>, ICommandHandler<LineEndCommandArgs>,
        ICommandHandler<LineStartExtendCommandArgs>, ICommandHandler<LineEndExtendCommandArgs>
    {
        public CommandState GetCommandState(LineStartCommandArgs args, Func<CommandState> nextHandler)
        {
            return GetCommandState(nextHandler);
        }

        public CommandState GetCommandState(LineEndCommandArgs args, Func<CommandState> nextHandler)
        {
            return GetCommandState(nextHandler);
        }

        public CommandState GetCommandState(LineStartExtendCommandArgs args, Func<CommandState> nextHandler)
        {
            return GetCommandState(nextHandler);
        }

        public CommandState GetCommandState(LineEndExtendCommandArgs args, Func<CommandState> nextHandler)
        {
            return GetCommandState(nextHandler);
        }

        public void ExecuteCommand(LineStartCommandArgs args, Action nextHandler)
        {
            if (HandleLineStartOrLineEndCommand(args.SubjectBuffer, args.TextView, lineStart: true, extendSelection: false))
            {
                return;
            }

            nextHandler();
        }

        public void ExecuteCommand(LineEndCommandArgs args, Action nextHandler)
        {
            if (HandleLineStartOrLineEndCommand(args.SubjectBuffer, args.TextView, lineStart: false, extendSelection: false))
            {
                return;
            }

            nextHandler();
        }

        public void ExecuteCommand(LineStartExtendCommandArgs args, Action nextHandler)
        {
            if (HandleLineStartOrLineEndCommand(args.SubjectBuffer, args.TextView, lineStart: true, extendSelection: true))
            {
                return;
            }

            nextHandler();
        }

        public void ExecuteCommand(LineEndExtendCommandArgs args, Action nextHandler)
        {
            if (HandleLineStartOrLineEndCommand(args.SubjectBuffer, args.TextView, lineStart: false, extendSelection: true))
            {
                return;
            }

            nextHandler();
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
                SnapshotSpan span;
                if (_renameService.ActiveSession.TryGetContainingEditableSpan(caretPoint.Value, out span))
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
