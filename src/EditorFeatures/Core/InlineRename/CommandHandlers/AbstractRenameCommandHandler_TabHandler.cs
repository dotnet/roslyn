// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;

internal abstract partial class AbstractRenameCommandHandler :
    IChainedCommandHandler<TabKeyCommandArgs>,
    IChainedCommandHandler<BackTabKeyCommandArgs>
{
    public CommandState GetCommandState(TabKeyCommandArgs args, Func<CommandState> nextHandler)
        => GetCommandState(nextHandler);

    public void ExecuteCommand(TabKeyCommandArgs args, Action nextHandler, CommandExecutionContext context)
    {
        // If the Dashboard is focused, just navigate through its UI.
        if (AdornmentShouldReceiveKeyboardNavigation(args.TextView))
        {
            SetAdornmentFocusToNextElement(args.TextView);
            return;
        }

        HandlePossibleTypingCommand(args, nextHandler, context.OperationContext, (activeSession, _, span) =>
        {
            var spans = new NormalizedSnapshotSpanCollection(
                activeSession.GetBufferManager(args.SubjectBuffer)
                .GetEditableSpansForSnapshot(args.SubjectBuffer.CurrentSnapshot));

            for (var i = 0; i < spans.Count; i++)
            {
                if (span == spans[i])
                {
                    var selectNext = i < spans.Count - 1 ? i + 1 : 0;
                    var newSelection = spans[selectNext];
                    args.TextView.TryMoveCaretToAndEnsureVisible(newSelection.Start);
                    args.TextView.SetSelection(newSelection);
                    break;
                }
            }
        });
    }

    public CommandState GetCommandState(BackTabKeyCommandArgs args, Func<CommandState> nextHandler)
        => GetCommandState(nextHandler);

    public void ExecuteCommand(BackTabKeyCommandArgs args, Action nextHandler, CommandExecutionContext context)
    {
        // If the Dashboard is focused, just navigate through its UI.
        if (AdornmentShouldReceiveKeyboardNavigation(args.TextView))
        {
            SetAdornmentFocusToPreviousElement(args.TextView);
            return;
        }
        else
        {
            nextHandler();
        }
    }
}
