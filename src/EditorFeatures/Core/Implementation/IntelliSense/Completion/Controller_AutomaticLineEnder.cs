// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        CommandState ICommandHandler<AutomaticLineEnderCommandArgs>.GetCommandState(AutomaticLineEnderCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return nextHandler();
        }

        void ICommandHandler<AutomaticLineEnderCommandArgs>.ExecuteCommand(AutomaticLineEnderCommandArgs args, Action nextHandler)
        {
            AssertIsForeground();

            if (sessionOpt == null)
            {
                // No computation.  Nothing to do.  Just let the editor handle this.
                nextHandler();
                return;
            }

            // We are computing a model.  Commit it if we compute any selected item.
            bool sendThrough, committed;
            CommitOnEnter(out sendThrough, out committed);

            // We did not commit based on enter.  So our computation will still be running.  Stop it now.
            if (!committed)
            {
                this.StopModelComputation();
                nextHandler();
            }
        }
    }
}
