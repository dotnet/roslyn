// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        VSCommanding.CommandState IChainedCommandHandler<AutomaticLineEnderCommandArgs>.GetCommandState(AutomaticLineEnderCommandArgs args, Func<VSCommanding.CommandState> nextHandler)
        {
            AssertIsForeground();
            return nextHandler();
        }

        void IChainedCommandHandler<AutomaticLineEnderCommandArgs>.ExecuteCommand(AutomaticLineEnderCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            AssertIsForeground();

            if (sessionOpt == null)
            {
                // No computation.  Nothing to do.  Just let the editor handle this.
                nextHandler();
                return;
            }

            CommitOnEnter(out _, out var committed);

            // We did not commit based on enter.  So our computation will still be running.  Stop it now.
            if (!committed)
            {
                this.DismissSessionIfActive();
                nextHandler();
            }
        }
    }
}
