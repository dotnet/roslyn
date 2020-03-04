// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.LanguageServices.CSharp;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.CSharp.EventHookup
{
    internal partial class EventHookupCommandHandler :
        ICommandHandler<EscapeKeyCommandArgs>
    {
        public string DisplayName => CSharpVSResources.Generate_Event_Subscription;

        public bool ExecuteCommand(EscapeKeyCommandArgs args, CommandExecutionContext context)
        {
            AssertIsForeground();
            EventHookupSessionManager.CancelAndDismissExistingSessions();
            return false;
        }

        public CommandState GetCommandState(EscapeKeyCommandArgs args)
        {
            AssertIsForeground();
            return CommandState.Unspecified;
        }
    }
}
