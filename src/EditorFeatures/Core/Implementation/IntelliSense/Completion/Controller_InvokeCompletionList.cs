// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Commands;
using VSC = Microsoft.VisualStudio.Text.UI.Commanding;
using VSInsertSnippetCommandArgs = Microsoft.VisualStudio.Text.UI.Commanding.Commands.InsertSnippetCommandArgs;
using VSInvokeCompletionListCommandArgs = Microsoft.VisualStudio.Text.UI.Commanding.Commands.InvokeCompletionListCommandArgs;
using VSCommandState = Microsoft.VisualStudio.Text.UI.Commanding.CommandState;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        VSCommandState VSC.ICommandHandler<VSInvokeCompletionListCommandArgs>.GetCommandState(VSInvokeCompletionListCommandArgs args)
        {
            return VSCommandState.CommandIsUnavailable;
        }

        bool VSC.ICommandHandler<VSInvokeCompletionListCommandArgs>.ExecuteCommand(VSInvokeCompletionListCommandArgs args)
        {
            AssertIsForeground();

            // TODO(cyrusn): Should we ever call into nextHandler in this method?

            // First, always dismiss whatever session we might have already had.  We no longer need
            // it.
            DismissSessionIfActive();

            // Next create the session that represents that we now have a potential completion list.
            // Then tell it to start computing.
            var completionService = this.GetCompletionService();
            if (completionService == null)
            {
                return false;
            }

            var trigger = CompletionTrigger.Invoke;
            StartNewModelComputation(completionService, trigger);
            return true;
        }
    }
}
