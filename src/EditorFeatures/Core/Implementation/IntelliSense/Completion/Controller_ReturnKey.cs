// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.AsyncCompletion;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        VSCommanding.CommandState IChainedCommandHandler<ReturnKeyCommandArgs>.GetCommandState(ReturnKeyCommandArgs args, Func<VSCommanding.CommandState> nextHandler)
        {
            AssertIsForeground();
            return nextHandler();
        }

        void IChainedCommandHandler<ReturnKeyCommandArgs>.ExecuteCommand(ReturnKeyCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            AssertIsForeground();

            if (sessionOpt == null)
            {
                // No computation.  Nothing to do.  Just let the editor handle this.
                nextHandler();
                return;
            }

            CommitOnEnter(out var sendThrough, out _);

            // Always stop completion after enter has been typed.
            DismissSessionIfActive();

            // Enter has different behavior amongst languages, so we need to actually defer to
            // the individual language item to determine what to do.  For example, in VB, enter
            // always commits the item and then sends the enter through so that later features can
            // handle it (i.e. indentation).  In C# enter only commits, although there is an option
            // to send the newline along if the item was completely typed.
            if (sendThrough)
            {
                nextHandler();
            }
        }

        private void CommitOnEnter(out bool sendThrough, out bool committed)
        {
            AssertIsForeground();

            var model = WaitForModel();

            // If there's no model, then there's nothing to commit.
            if (model == null)
            {
                // Make sure that the enter gets sent into the buffer.
                sendThrough = true;
                committed = false;
                return;
            }

            // If we're in a normal editor or the Immediate window, we'll send the enter through
            // to the editor.  In single-line debugger windows (Watch, etc), however, we don't
            // want to send the enter though, because those windows don't support displaying
            // more than one line of text.
            sendThrough = !_isDebugger || _isImmediateWindow;

            // If the user used completion filters to empty the list, just dismiss
            if (model.SelectedItemOpt == null)
            {
                committed = false;
                return;
            }

            if (model.IsSoftSelection)
            {
                // If the completion list is soft selected, then don't commit on enter.
                // Instead, just dismiss the completion list.
                committed = false;
                return;
            }

            // If the selected item is the builder, dismiss
            if (model.SelectedItemOpt == model.SuggestionModeItem)
            {
                sendThrough = false;
                committed = false;
                return;
            }

            if (sendThrough)
            {
                // Get the text that the user has currently entered into the buffer
                var viewSpan = model.GetViewBufferSpan(model.SelectedItemOpt.Span);
                var textTypedSoFar = model.GetCurrentTextInSnapshot(
                    viewSpan, this.TextView.TextSnapshot, this.GetCaretPointInViewBuffer());

                var service = GetCompletionService();
                sendThrough = CommitManager.SendEnterThroughToEditor(
                     service.GetRules(), model.SelectedItemOpt, textTypedSoFar);
            }

            this.CommitOnNonTypeChar(model.SelectedItemOpt, model);
            committed = true;
        }
    }
}
