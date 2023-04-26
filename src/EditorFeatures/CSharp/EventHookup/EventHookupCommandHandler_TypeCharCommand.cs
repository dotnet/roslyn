// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.EventHookup;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.CSharp.EventHookup
{
    internal partial class EventHookupCommandHandler : IChainedCommandHandler<TypeCharCommandArgs>
    {
        public void ExecuteCommand(TypeCharCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            _threadingContext.ThrowIfNotOnUIThread();
            nextHandler();

            if (!_globalOptions.GetOption(EventHookupOptionsStorage.EventHookup))
            {
                EventHookupSessionManager.CancelAndDismissExistingSessions();
                return;
            }

            // Event hookup is current uncancellable.
            var cancellationToken = CancellationToken.None;
            using (Logger.LogBlock(FunctionId.EventHookup_Type_Char, cancellationToken))
            {
                if (args.TypedChar == '=')
                {
                    // They've typed an equals. Cancel existing sessions and potentially start a 
                    // new session.

                    EventHookupSessionManager.CancelAndDismissExistingSessions();

                    if (IsTextualPlusEquals(args.TextView, args.SubjectBuffer))
                    {
                        EventHookupSessionManager.BeginSession(this, args.TextView, args.SubjectBuffer, _asyncListener, TESTSessionHookupMutex);
                    }
                }
                else
                {
                    // Spaces are the only non-'=' character that allow the session to continue
                    if (args.TypedChar != ' ')
                    {
                        EventHookupSessionManager.CancelAndDismissExistingSessions();
                    }
                }
            }
        }

        private bool IsTextualPlusEquals(ITextView textView, ITextBuffer subjectBuffer)
        {
            _threadingContext.ThrowIfNotOnUIThread();

            var caretPoint = textView.GetCaretPoint(subjectBuffer);
            if (!caretPoint.HasValue)
            {
                return false;
            }

            var position = caretPoint.Value.Position;
            return position - 2 >= 0 && subjectBuffer.CurrentSnapshot.GetText(position - 2, 2) == "+=";
        }

        public CommandState GetCommandState(TypeCharCommandArgs args, Func<CommandState> nextHandler)
        {
            _threadingContext.ThrowIfNotOnUIThread();
            return nextHandler();
        }
    }
}
