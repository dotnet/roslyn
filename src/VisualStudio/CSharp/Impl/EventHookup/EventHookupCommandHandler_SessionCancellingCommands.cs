// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.LanguageServices.CSharp;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.CSharp.EventHookup
{
    internal partial class EventHookupCommandHandler :
        VSCommanding.ICommandHandler<EscapeKeyCommandArgs>
    {
        public string DisplayName => CSharpVSResources.Generate_Event_Subscription;

        public bool ExecuteCommand(EscapeKeyCommandArgs args, CommandExecutionContext context)
        {
            AssertIsForeground();
            EventHookupSessionManager.CancelAndDismissExistingSessions();
            return false;
        }

        public VSCommanding.CommandState GetCommandState(EscapeKeyCommandArgs args)
        {
            AssertIsForeground();
            return VSCommanding.CommandState.Unspecified;
        }
    }
}
