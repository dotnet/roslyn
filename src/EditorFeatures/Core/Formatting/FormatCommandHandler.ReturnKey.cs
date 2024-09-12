// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Formatting;

internal partial class FormatCommandHandler
{
    public CommandState GetCommandState(ReturnKeyCommandArgs args, Func<CommandState> nextHandler)
        => nextHandler();

    public void ExecuteCommand(ReturnKeyCommandArgs args, Action nextHandler, CommandExecutionContext context)
        => ExecuteReturnOrTypeCommand(args, nextHandler, context.OperationContext.UserCancellationToken);
}
