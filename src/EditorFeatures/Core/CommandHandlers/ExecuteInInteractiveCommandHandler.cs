// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Microsoft.VisualStudio.Commanding;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor.CommandHandlers
{
    /// <summary>
    /// Implements a execute in interactive command handler.
    /// This class is separated from the <see cref="IExecuteInInteractiveCommandHandler"/>
    /// in order to ensure that the interactive command can be exposed without the necessity
    /// to load any of the interactive dll files just to get the command's status.
    /// </summary>
    [Export(typeof(ICommandHandler))]
    [ContentType(ContentTypeNames.RoslynContentType)]
    [Name("Interactive Command Handler")]
    internal class ExecuteInInteractiveCommandHandler
        : ICommandHandler<ExecuteInInteractiveCommandArgs>
    {
        private readonly IEnumerable<Lazy<IExecuteInInteractiveCommandHandler, ContentTypeMetadata>> _executeInInteractiveHandlers;

        public string DisplayName => EditorFeaturesResources.Execute_In_Interactive;

        [ImportingConstructor]
        public ExecuteInInteractiveCommandHandler(
            [ImportMany]IEnumerable<Lazy<IExecuteInInteractiveCommandHandler, ContentTypeMetadata>> executeInInteractiveHandlers)
        {
            _executeInInteractiveHandlers = executeInInteractiveHandlers;
        }

        private Lazy<IExecuteInInteractiveCommandHandler> GetCommandHandler(ITextBuffer textBuffer)
        {
            return _executeInInteractiveHandlers
                .Where(handler => handler.Metadata.ContentTypes.Any(textBuffer.ContentType.IsOfType))
                .SingleOrDefault();
        }

        bool ICommandHandler<ExecuteInInteractiveCommandArgs>.ExecuteCommand(ExecuteInInteractiveCommandArgs args, CommandExecutionContext context)
        {
            return GetCommandHandler(args.SubjectBuffer)?.Value.ExecuteCommand(args, context) ?? false;
        }

        CommandState ICommandHandler<ExecuteInInteractiveCommandArgs>.GetCommandState(ExecuteInInteractiveCommandArgs args)
        {
            return GetCommandHandler(args.SubjectBuffer) == null
                ? CommandState.Unavailable
                : CommandState.Available;
        }
    }
}
