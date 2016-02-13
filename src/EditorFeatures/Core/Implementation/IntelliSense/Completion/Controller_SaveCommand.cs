// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        CommandState ICommandHandler<SaveCommandArgs>.GetCommandState(SaveCommandArgs args, System.Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return nextHandler();
        }

        void ICommandHandler<SaveCommandArgs>.ExecuteCommand(SaveCommandArgs args, Action nextHandler)
        {
            AssertIsForeground();

            if (sessionOpt != null)
            {
                DismissSessionIfActive();
            }

            nextHandler();
        }
    }
}
