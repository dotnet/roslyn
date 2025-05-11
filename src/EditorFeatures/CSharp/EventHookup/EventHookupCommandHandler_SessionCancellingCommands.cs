// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.CSharp.EventHookup;

internal sealed partial class EventHookupCommandHandler :
    ICommandHandler<EscapeKeyCommandArgs>
{
    public string DisplayName => CSharpEditorResources.Generate_Event_Subscription;

    public bool ExecuteCommand(EscapeKeyCommandArgs args, CommandExecutionContext context)
    {
        _threadingContext.ThrowIfNotOnUIThread();
        EventHookupSessionManager.DismissExistingSessions();
        return false;
    }

    public CommandState GetCommandState(EscapeKeyCommandArgs args)
    {
        _threadingContext.ThrowIfNotOnUIThread();
        return CommandState.Unspecified;
    }
}
