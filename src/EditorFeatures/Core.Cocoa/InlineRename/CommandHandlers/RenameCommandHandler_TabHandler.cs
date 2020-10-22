// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class RenameCommandHandler :
        IChainedCommandHandler<TabKeyCommandArgs>,
        IChainedCommandHandler<BackTabKeyCommandArgs>
    {
        public VSCommanding.CommandState GetCommandState(TabKeyCommandArgs args, Func<VSCommanding.CommandState> nextHandler)
        {
            return GetCommandState(nextHandler);
        }

        public void ExecuteCommand(TabKeyCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            HandlePossibleTypingCommand(args, nextHandler, span =>
            {
                var spans = new NormalizedSnapshotSpanCollection(
                    _renameService.ActiveSession.GetBufferManager(args.SubjectBuffer)
                    .GetEditableSpansForSnapshot(args.SubjectBuffer.CurrentSnapshot));

                for (var i = 0; i < spans.Count; i++)
                {
                    if (span == spans[i])
                    {
                        var selectNext = i < spans.Count - 1 ? i + 1 : 0;
                        var newSelection = spans[selectNext];
                        ITextViewExtensions.TryMoveCaretToAndEnsureVisible(args.TextView, newSelection.Start);
                        args.TextView.SetSelection(newSelection);
                        break;
                    }
                }
            });
        }

        public VSCommanding.CommandState GetCommandState(BackTabKeyCommandArgs args, Func<VSCommanding.CommandState> nextHandler)
        {
            return GetCommandState(nextHandler);
        }

        public void ExecuteCommand(BackTabKeyCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            nextHandler();
        }
    }
}
