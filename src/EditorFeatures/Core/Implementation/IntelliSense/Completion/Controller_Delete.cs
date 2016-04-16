// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Editor.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        CommandState ICommandHandler<DeleteKeyCommandArgs>.GetCommandState(DeleteKeyCommandArgs args, System.Func<CommandState> nextHandler)
        {
            AssertIsForeground();
            return nextHandler();
        }

        void ICommandHandler<DeleteKeyCommandArgs>.ExecuteCommand(DeleteKeyCommandArgs args, Action nextHandler)
        {
            ExecuteBackspaceOrDelete(args.TextView, nextHandler, isDelete: true);
        }
    }
}
