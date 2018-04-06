// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.CSharp.EventHookup
{
    internal partial class EventHookupCommandHandler : VSCommanding.ICommandHandler<InvokeCompletionListCommandArgs>
    {
        public bool ExecuteCommand(InvokeCompletionListCommandArgs args, CommandExecutionContext context)
        {
            AssertIsForeground();
            if (EventHookupSessionManager.QuickInfoSession == null || EventHookupSessionManager.QuickInfoSession.IsDismissed)
            {
                return false;
            }

            return true;
        }

        public VSCommanding.CommandState GetCommandState(InvokeCompletionListCommandArgs args)
        {
            AssertIsForeground();
            return VSCommanding.CommandState.Unspecified;
        }
    }
}
