// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.InlineRename;

internal abstract partial class AbstractRenameCommandHandler : IChainedCommandHandler<OpenLineBelowCommandArgs>
{
    public CommandState GetCommandState(OpenLineBelowCommandArgs args, Func<CommandState> nextHandler)
        => GetCommandState(nextHandler);

    public void ExecuteCommand(OpenLineBelowCommandArgs args, Action nextHandler, CommandExecutionContext context)
    {
        HandlePossibleTypingCommand(args, nextHandler, context.OperationContext, (activeSession, operationContext, span) =>
        {
            CompleteActiveSession(operationContext, invalidEditCommandInvoked: true);
            nextHandler();
        });
    }
}
