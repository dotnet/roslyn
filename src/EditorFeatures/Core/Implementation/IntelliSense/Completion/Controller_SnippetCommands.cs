// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using VSCommanding = Microsoft.VisualStudio.Commanding;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        VSCommanding.CommandState IChainedCommandHandler<InsertSnippetCommandArgs>.GetCommandState(InsertSnippetCommandArgs args, Func<VSCommanding.CommandState> nextHandler)
        {
            AssertIsForeground();
            return nextHandler();
        }

        void IChainedCommandHandler<InsertSnippetCommandArgs>.ExecuteCommand(InsertSnippetCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            // If the completion list is showing when the snippet picker is invoked, then the 
            // editor fails to draw the text input area of the picker until a tab or enter is 
            // pressed to select a snippet folder. 

            AssertIsForeground();
            DismissCompletionForSnippetPicker(nextHandler);
        }

        VSCommanding.CommandState IChainedCommandHandler<SurroundWithCommandArgs>.GetCommandState(SurroundWithCommandArgs args, Func<VSCommanding.CommandState> nextHandler)
        {
            AssertIsForeground();
            return nextHandler();
        }

        void IChainedCommandHandler<SurroundWithCommandArgs>.ExecuteCommand(SurroundWithCommandArgs args, Action nextHandler, CommandExecutionContext context)
        {
            // If the completion list is showing when the snippet picker is invoked, then the 
            // editor fails to draw the text input area of the picker until a tab or enter is 
            // pressed to select a snippet folder. 

            AssertIsForeground();
            DismissCompletionForSnippetPicker(nextHandler);
        }

        private void DismissCompletionForSnippetPicker(Action nextHandler)
        {
            DismissSessionIfActive();
            nextHandler();
        }
    }
}
