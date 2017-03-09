// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.SignatureHelp;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.SignatureHelp
{
    internal partial class Controller
    {
        CommandState ICommandHandler<TypeCharCommandArgs>.GetCommandState(TypeCharCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();

            // We just defer to the editor here.  We do not interfere with typing normal characters.
            return nextHandler();
        }

        void ICommandHandler<TypeCharCommandArgs>.ExecuteCommand(TypeCharCommandArgs args, Action nextHandler)
        {
            AssertIsForeground();

            // Note: while we're doing this, we don't want to hear about buffer changes (since we
            // know they're going to happen).  So we disconnect and reconnect to the event
            // afterwards.  That way we can hear about changes to the buffer that don't happen
            // through us.
            this.TextView.TextBuffer.PostChanged -= OnTextViewBufferPostChanged;
            try
            {
                nextHandler();
            }
            finally
            {
                this.TextView.TextBuffer.PostChanged += OnTextViewBufferPostChanged;
            }

            // We only want to process typechar if it is a normal typechar and no one else is
            // involved.  i.e. if there was a typechar, but someone processed it and moved the caret
            // somewhere else then we don't want signature help.  Also, if a character was typed but
            // something intercepted and placed different text into the editor, then we don't want
            // to proceed. 
            //
            // Note: we do not want to pass along a text version here.  It is expected that multiple
            // version changes may happen when we call 'nextHandler' and we will still want to
            // proceed.  For example, if the user types "WriteL(", then that will involve two text
            // changes as completion commits that out to "WriteLine(".  But we still want to provide
            // sig help in this case.
            if (this.TextView.TypeCharWasHandledStrangely(this.SubjectBuffer, args.TypedChar))
            {
                // If we were computing anything, we stop.  We only want to process a typechar
                // if it was a normal character.
                DismissSessionIfActive();
                return;
            }

            var service = GetSignatureHelpService();
            if (service == null)
            {
                return;
            }

            var options = GetOptions();

            if (IsSessionActive)
            {
                // We already have a session. Update our model
                sessionOpt.ComputeModel(SignatureHelpTrigger.CreateUpdateTrigger());
            }
            else
            {
                // No computation at all.  If this is not a trigger character, we just ignore it and
                // stay in this state.  Otherwise, if it's a trigger character, start up a new
                // computation and start computing the model in the background.
                if (IsTextualTriggerCharacter(service, args.TypedChar, options))
                {
                    // First create the session that represents that we now have a potential 
                    // signature help list. Then tell it to start computing.
                    StartSession(SignatureHelpTrigger.CreateInsertionTrigger(args.TypedChar));
                    return;
                }
                else
                {
                    // No need to do anything.  Just stay in the state where we have no session.
                    return;
                }
            }
        }

        private bool IsTextualTriggerCharacter(SignatureHelpService signatureHelpService, char ch, OptionSet options)
        {
            AssertIsForeground();

            // Note: When this function is called we've already guaranteed that
            // TypeCharWasHandledStrangely returned false.  That means we know that the caret is in
            // our buffer, and is after the character just typed.

            var caretPosition = this.TextView.GetCaretPoint(this.SubjectBuffer).Value;
            var previousPosition = caretPosition - 1;
            Contract.ThrowIfFalse(this.SubjectBuffer.CurrentSnapshot[previousPosition] == ch);

            var trigger = SignatureHelpTrigger.CreateInsertionTrigger(ch);
            return signatureHelpService.ShouldTriggerSignatureHelp(
                this.SubjectBuffer.CurrentSnapshot.AsText(), caretPosition, trigger, options);
        }
    }
}
