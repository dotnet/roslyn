// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Formatting
{
    internal partial class FormatCommandHandler
    {
        public VSCommanding.CommandState GetCommandState(ReturnKeyCommandArgs args, Func<VSCommanding.CommandState> nextHandler)
        {
            return nextHandler();
        }

        public void ExecuteCommand(ReturnKeyCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            ExecuteReturnOrTypeCommand(args, nextHandler, context.OperationContext.UserCancellationToken);
        }
    }
}
