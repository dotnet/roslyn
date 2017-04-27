// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.UI.Commanding.Commands;
using VSC = Microsoft.VisualStudio.Text.UI.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename
{
    internal partial class RenameCommandHandler :
        VSC.ICommandHandler<TabKeyCommandArgs>,
        VSC.ICommandHandler<BackTabKeyCommandArgs>
    {
        public VSC.CommandState GetCommandState(TabKeyCommandArgs args)
        {
            return GetCommandState();
        }

        public bool ExecuteCommand(TabKeyCommandArgs args)
        {
            // If the Dashboard is focused, just navigate through its UI.
            Dashboard dashboard = GetDashboard(args.TextView);
            if (dashboard != null && dashboard.ShouldReceiveKeyboardNavigation)
            {
                dashboard.FocusNextElement();
                return false;
            }

            return HandlePossibleTypingCommand(args, span =>
            {
                var spans = new NormalizedSnapshotSpanCollection(
                    _renameService.ActiveSession.GetBufferManager(args.SubjectBuffer)
                    .GetEditableSpansForSnapshot(args.SubjectBuffer.CurrentSnapshot));

                for (int i = 0; i < spans.Count; i++)
                {
                    if (span == spans[i])
                    {
                        int selectNext = i < spans.Count - 1 ? i + 1 : 0;
                        var newSelection = spans[selectNext];
                        args.TextView.TryMoveCaretToAndEnsureVisible(newSelection.Start);
                        args.TextView.SetSelection(newSelection);
                        break;
                    }
                }

                return true;
            });
        }

        public VSC.CommandState GetCommandState(BackTabKeyCommandArgs args)
        {
            return GetCommandState();
        }

        public bool ExecuteCommand(BackTabKeyCommandArgs args)
        {
            // If the Dashboard is focused, just navigate through its UI.
            var dashboard = GetDashboard(args.TextView);
            if (dashboard != null && dashboard.ShouldReceiveKeyboardNavigation)
            {
                dashboard.FocusPreviousElement();
                return true;
            }
            else
            {
                return false;
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
