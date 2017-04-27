// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Commands;
using VSC = Microsoft.VisualStudio.Text.UI.Commanding;
using VSInvokeCompletionListCommandArgs = Microsoft.VisualStudio.Text.UI.Commanding.Commands.InvokeCompletionListCommandArgs;
using VSCommandState = Microsoft.VisualStudio.Text.UI.Commanding.CommandState;

namespace Microsoft.CodeAnalysis.Editor.CSharp.EventHookup
{
    internal partial class EventHookupCommandHandler : VSC.ICommandHandler<VSInvokeCompletionListCommandArgs>
    {
        public bool InterestedInReadOnlyBuffer => false;

        public bool ExecuteCommand(VSInvokeCompletionListCommandArgs args)
        {
            AssertIsForeground();
            if (EventHookupSessionManager.QuickInfoSession == null || EventHookupSessionManager.QuickInfoSession.IsDismissed)
            {
                return false;
            }

            return true;
        }

        public VSCommandState GetCommandState(VSInvokeCompletionListCommandArgs args)
        {
            AssertIsForeground();
            return VSCommandState.CommandIsUnavailable;
        }
    }
}
