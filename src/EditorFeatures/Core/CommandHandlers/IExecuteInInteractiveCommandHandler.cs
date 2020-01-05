// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Editor.CommandHandlers
{
    /// <summary>
    /// Interface for the ExecuteInInteractiveCommand handler.
    /// Ensures that the command handler can be exported via MEF
    /// without actually being instantiated as all other command handlers.
    /// </summary>
    internal interface IExecuteInInteractiveCommandHandler
        : ICommandHandler<ExecuteInInteractiveCommandArgs>
    {
    }
}
