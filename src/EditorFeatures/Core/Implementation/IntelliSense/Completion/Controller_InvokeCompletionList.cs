// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Editor.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        CommandState ICommandHandler<InvokeCompletionListCommandArgs>.GetCommandState(InvokeCompletionListCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return nextHandler();
        }

        void ICommandHandler<InvokeCompletionListCommandArgs>.ExecuteCommand(InvokeCompletionListCommandArgs args, Action nextHandler)
        {
            AssertIsForeground();

            // TODO(cyrusn): Should we ever call into nextHandler in this method?

            // First, always dismiss whatever session we might have already had.  We no longer need
            // it.
            if (sessionOpt != null)
            {
                this.StopModelComputation();
            }

            // Next create the session that represents that we now have a potential completion list.
            // Then tell it to start computing.
            var completionService = this.GetCompletionService();
            if (completionService == null)
            {
                Trace.WriteLine("Failed to get completion service, cannot have a completion session.");
                return;
            }

            StartNewModelComputation(completionService, filterItems: false, dismissIfEmptyAllowed: false);
        }
    }
}
