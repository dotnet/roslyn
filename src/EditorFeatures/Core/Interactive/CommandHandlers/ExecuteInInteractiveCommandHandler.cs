// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Interactive;

/// <summary>
/// Implements a execute in interactive command handler.
/// This class is separated from the <see cref="IExecuteInInteractiveCommandHandler"/>
/// in order to ensure that the interactive command can be exposed without the necessity
/// to load any of the interactive dll files just to get the command's status.
/// </summary>
[Export(typeof(ICommandHandler))]
[ContentType(ContentTypeNames.RoslynContentType)]
[Name("Interactive Command Handler")]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal class ExecuteInInteractiveCommandHandler(
    [ImportMany] IEnumerable<Lazy<IExecuteInInteractiveCommandHandler, ContentTypeMetadata>> executeInInteractiveHandlers)
            : ICommandHandler<ExecuteInInteractiveCommandArgs>
{
    private readonly IEnumerable<Lazy<IExecuteInInteractiveCommandHandler, ContentTypeMetadata>> _executeInInteractiveHandlers = executeInInteractiveHandlers;

    public string DisplayName => EditorFeaturesResources.Execute_In_Interactive;

    private Lazy<IExecuteInInteractiveCommandHandler> GetCommandHandler(ITextBuffer textBuffer)
    {
        return _executeInInteractiveHandlers
            .Where(handler => handler.Metadata.ContentTypes.Any(textBuffer.ContentType.IsOfType))
            .SingleOrDefault();
    }

    bool ICommandHandler<ExecuteInInteractiveCommandArgs>.ExecuteCommand(ExecuteInInteractiveCommandArgs args, CommandExecutionContext context)
        => GetCommandHandler(args.SubjectBuffer)?.Value.ExecuteCommand(args, context) ?? false;

    CommandState ICommandHandler<ExecuteInInteractiveCommandArgs>.GetCommandState(ExecuteInInteractiveCommandArgs args)
    {
        return GetCommandHandler(args.SubjectBuffer) == null
            ? CommandState.Unavailable
            : CommandState.Available;
    }
}
