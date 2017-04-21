// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        // Cut and Paste should always dismiss completion

        CommandState ICommandHandler<CutCommandArgs>.GetCommandState(CutCommandArgs args, System.Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return nextHandler();
        }

        void ICommandHandler<CutCommandArgs>.ExecuteCommand(CutCommandArgs args, Action nextHandler)
        {
            AssertIsForeground();
            DismissSessionIfActive();
            nextHandler();
        }

        CommandState ICommandHandler<PasteCommandArgs>.GetCommandState(PasteCommandArgs args, System.Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return nextHandler();
        }

        void ICommandHandler<PasteCommandArgs>.ExecuteCommand(PasteCommandArgs args, Action nextHandler)
        {
            AssertIsForeground();
            DismissSessionIfActive();
            nextHandler();
        }
    }
}
