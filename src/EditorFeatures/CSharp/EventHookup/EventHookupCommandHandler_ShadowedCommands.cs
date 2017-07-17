// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Commands;

namespace Microsoft.CodeAnalysis.Editor.CSharp.EventHookup
{
    internal partial class EventHookupCommandHandler : ICommandHandler<InvokeCompletionListCommandArgs>
    {
        public void ExecuteCommand(InvokeCompletionListCommandArgs args, Action nextHandler)
        {
            AssertIsForeground();
            if (EventHookupSessionManager.QuickInfoSession == null || EventHookupSessionManager.QuickInfoSession.IsDismissed)
            {
                nextHandler();
            }
        }

        public CommandState GetCommandState(InvokeCompletionListCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return nextHandler();
        }
    }
}
