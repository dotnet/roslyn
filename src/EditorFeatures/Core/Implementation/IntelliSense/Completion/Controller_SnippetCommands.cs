// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Commands;
using VSC = Microsoft.VisualStudio.Text.UI.Commanding;
using VSInsertSnippetCommandArgs = Microsoft.VisualStudio.Text.UI.Commanding.Commands.InsertSnippetCommandArgs;
using VSCommandState = Microsoft.VisualStudio.Text.UI.Commanding.CommandState;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        VSCommandState VSC.ICommandHandler<VSInsertSnippetCommandArgs>.GetCommandState(VSInsertSnippetCommandArgs arg)
        {
            return VSCommandState.CommandIsUnavailable;
        }

        bool VSC.ICommandHandler<VSInsertSnippetCommandArgs>.ExecuteCommand(VSInsertSnippetCommandArgs args)
        {
            // If the completion list is showing when the snippet picker is invoked, then the 
            // editor fails to draw the text input area of the picker until a tab or enter is 
            // pressed to select a snippet folder. 

            AssertIsForeground();
            DismissCompletionForSnippetPicker();
            return false;
        }

        VSC.CommandState VSC.ICommandHandler<VSC.Commands.SurroundWithCommandArgs>.GetCommandState(VSC.Commands.SurroundWithCommandArgs args)
        {
            AssertIsForeground();
            return VSC.CommandState.CommandIsUnavailable;
        }

        bool VSC.ICommandHandler<VSC.Commands.SurroundWithCommandArgs>.ExecuteCommand(VSC.Commands.SurroundWithCommandArgs args)
        {
            // If the completion list is showing when the snippet picker is invoked, then the 
            // editor fails to draw the text input area of the picker until a tab or enter is 
            // pressed to select a snippet folder. 

            AssertIsForeground();
            DismissCompletionForSnippetPicker();
            return false;
        }

        private void DismissCompletionForSnippetPicker()
        {
            DismissSessionIfActive();
        }
    }
}
