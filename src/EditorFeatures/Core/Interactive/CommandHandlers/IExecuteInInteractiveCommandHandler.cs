// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;

namespace Microsoft.CodeAnalysis.Interactive;

/// <summary>
/// Interface for the ExecuteInInteractiveCommand handler.
/// Ensures that the command handler can be exported via MEF
/// without actually being instantiated as all other command handlers.
/// </summary>
internal interface IExecuteInInteractiveCommandHandler
    : ICommandHandler<ExecuteInInteractiveCommandArgs>
{
}
