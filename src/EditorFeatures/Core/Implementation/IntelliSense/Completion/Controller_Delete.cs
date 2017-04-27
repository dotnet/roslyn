// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Text.UI.Commanding;
using Microsoft.VisualStudio.Text.UI.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion
{
    internal partial class Controller
    {
        CommandState ICommandHandler<DeleteKeyCommandArgs>.GetCommandState(DeleteKeyCommandArgs args)
        {
            AssertIsForeground();
            return CommandState.CommandIsUnavailable;
        }

        bool ICommandHandler<DeleteKeyCommandArgs>.ExecuteCommand(DeleteKeyCommandArgs args)
        {
            return ExecuteBackspaceOrDelete(args.TextView, isDelete: true, args: args);
        }
    }
}
