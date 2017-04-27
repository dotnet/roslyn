// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Text.UI.Commanding;
using Microsoft.VisualStudio.Text.UI.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        // Cut and Paste should always dismiss completion

        CommandState ICommandHandler<CutCommandArgs>.GetCommandState(CutCommandArgs args)
        {
            AssertIsForeground();
            return CommandState.CommandIsUnavailable;
        }

        bool ICommandHandler<CutCommandArgs>.ExecuteCommand(CutCommandArgs args)
        {
            AssertIsForeground();
            DismissSessionIfActive();
            return false;
        }

        CommandState ICommandHandler<PasteCommandArgs>.GetCommandState(PasteCommandArgs args)
        {
            AssertIsForeground();
            return CommandState.CommandIsUnavailable;
        }

        bool ICommandHandler<PasteCommandArgs>.ExecuteCommand(PasteCommandArgs args)
        {
            AssertIsForeground();
            DismissSessionIfActive();
            return false;
        }
    }
}
