// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Commands;

namespace Microsoft.CodeAnalysis.Editor.CSharp.EventHookup
{
    internal partial class EventHookupCommandHandler :
        ICommandHandler2<EscapeKeyCommandArgs>
    {
        public void ExecuteCommand(EscapeKeyCommandArgs args, Action nextHandler)
        {
            AssertIsForeground();
            EventHookupSessionManager.CancelAndDismissExistingSessions();
            nextHandler();
        }

        public CommandState2 GetCommandState(EscapeKeyCommandArgs args, Func<CommandState2> nextHandler)
        {
            AssertIsForeground();
            return nextHandler();
        }
    }
}
