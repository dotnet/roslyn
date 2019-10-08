// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class RenameCommandHandler :
        IChainedCommandHandler<TabKeyCommandArgs>,
        IChainedCommandHandler<BackTabKeyCommandArgs>
    {
        public CommandState GetCommandState(TabKeyCommandArgs args, Func<CommandState> nextHandler)
        {
            return GetCommandState(nextHandler);
        }

        public void ExecuteCommand(TabKeyCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            // If the Dashboard is focused, just navigate through its UI.
            var dashboard = GetDashboard(args.TextView);
            if (dashboard != null && dashboard.ShouldReceiveKeyboardNavigation)
            {
                dashboard.FocusNextElement();
                return;
            }

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
                        args.TextView.TryMoveCaretToAndEnsureVisible(newSelection.Start);
                        args.TextView.SetSelection(newSelection);
                        break;
                    }
                }
            });
        }

        public CommandState GetCommandState(BackTabKeyCommandArgs args, Func<CommandState> nextHandler)
        {
            return GetCommandState(nextHandler);
        }

        public void ExecuteCommand(BackTabKeyCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            // If the Dashboard is focused, just navigate through its UI.
            var dashboard = GetDashboard(args.TextView);
            if (dashboard != null && dashboard.ShouldReceiveKeyboardNavigation)
            {
                dashboard.FocusPreviousElement();
                return;
            }
            else
            {
                nextHandler();
            }
        }

        private static Dashboard GetDashboard(ITextView textView)
        {
            // If our adornment layer somehow didn't get composed, GetAdornmentLayer will throw.
            // Don't crash if that happens.
            try
            {
                var adornment = ((IWpfTextView)textView).GetAdornmentLayer("RoslynRenameDashboard");
                return adornment.Elements.Any()
                    ? adornment.Elements[0].Adornment as Dashboard
                    : null;
            }
            catch (ArgumentOutOfRangeException)
            {
                return null;
            }
        }
    }
}
