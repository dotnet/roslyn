// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Text.UI.Commanding;
using Microsoft.VisualStudio.Text.UI.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        CommandState ILegacyCommandHandler<InsertSnippetCommandArgs>.GetCommandState(InsertSnippetCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return nextHandler();
        }

        void ILegacyCommandHandler<InsertSnippetCommandArgs>.ExecuteCommand(InsertSnippetCommandArgs args, Action nextHandler)
        {
            // If the completion list is showing when the snippet picker is invoked, then the 
            // editor fails to draw the text input area of the picker until a tab or enter is 
            // pressed to select a snippet folder. 

            AssertIsForeground();
            DismissCompletionForSnippetPicker(nextHandler);
        }

        CommandState ILegacyCommandHandler<SurroundWithCommandArgs>.GetCommandState(SurroundWithCommandArgs args, Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return nextHandler();
        }

        void ILegacyCommandHandler<SurroundWithCommandArgs>.ExecuteCommand(SurroundWithCommandArgs args, Action nextHandler)
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
