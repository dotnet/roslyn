// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        CommandState ICommandHandler<ReturnKeyCommandArgs>.GetCommandState(ReturnKeyCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return nextHandler();
        }

        void ICommandHandler<ReturnKeyCommandArgs>.ExecuteCommand(ReturnKeyCommandArgs args, Action nextHandler)
        {
            AssertIsForeground();

            if (sessionOpt == null)
            {
                // No computation.  Nothing to do.  Just let the editor handle this.
                nextHandler();
                return;
            }

            // We are computing a model.  Commit it if we compute any selected item.
            bool sendThrough, committed;
            CommitOnEnter(out sendThrough, out committed);

            // We did not commit based on enter.  So our computation will still be running.  Stop it now.
            if (!committed)
            {
                this.StopModelComputation();
            }

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

            var model = sessionOpt.WaitForModel();

            // If there's no model, then there's nothing to commit.
            if (model == null)
            {
                // Make sure that the enter gets sent into the buffer.
                sendThrough = true;
                committed = false;
                return;
            }

            if (model.IsSoftSelection)
            {
                // If the completion list is soft selected, then don't commit on enter.  Instead,
                // send the enter through to the editor and dismiss the completion list.
                sendThrough = true;
                committed = false;
                return;
            }

            var selectedItem = Controller.GetExternallyUsableCompletionItem(model.SelectedItem);

            // If the selected item is the builder, dismiss
            if (selectedItem.IsBuilder)
            {
                sendThrough = false;
                committed = false;
                return;
            }

            // Get the text that the user has currently entered into the buffer
            var viewSpan = model.GetSubjectBufferFilterSpanInViewBuffer(selectedItem.FilterSpan);
            var textTypedSoFar = model.GetCurrentTextInSnapshot(
                viewSpan, this.TextView.TextSnapshot, this.GetCaretPointInViewBuffer());

            var textChange = selectedItem.CompletionProvider.GetTextChange(selectedItem);

            this.Commit(selectedItem, textChange, model, null);
            sendThrough = selectedItem.CompletionProvider.SendEnterThroughToEditor(selectedItem, textTypedSoFar);
            committed = true;
        }
    }
}
