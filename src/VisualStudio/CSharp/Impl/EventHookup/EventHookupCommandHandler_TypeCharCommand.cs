// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Editor.Shared.Options;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.CSharp.EventHookup
{
    internal partial class EventHookupCommandHandler : IChainedCommandHandler<TypeCharCommandArgs>
    {
        public void ExecuteCommand(TypeCharCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            AssertIsForeground();
            nextHandler();

            if (!args.SubjectBuffer.GetFeatureOnOffOption(InternalFeatureOnOffOptions.EventHookup))
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
            AssertIsForeground();

            var caretPoint = textView.GetCaretPoint(subjectBuffer);
            if (!caretPoint.HasValue)
            {
                return false;
            }

            var position = caretPoint.Value.Position;
            return position - 2 >= 0 && subjectBuffer.CurrentSnapshot.GetText(position - 2, 2) == "+=";
        }

        public VSCommanding.CommandState GetCommandState(TypeCharCommandArgs args, Func<VSCommanding.CommandState> nextHandler)
        {
            AssertIsForeground();
            return nextHandler();
        }
    }
}
