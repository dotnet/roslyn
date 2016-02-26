// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.Commands;
using Microsoft.VisualStudio.Text;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace Microsoft.CodeAnalysis.Editor.CommandHandlers
{
    /// <summary>
    /// Implements a execute in interactive command handler.
    /// This class is separated from the <see cref="IExecuteInInteractiveCommandHandler"/>
    /// in order to ensure that the interactive command can be exposed without the necessity
    /// to load any of the interactive dll files just to get the command's status.
    /// </summary>
    [ExportCommandHandler("Interactive Command Handler", ContentTypeNames.RoslynContentType)]
    internal class ExecuteInInteractiveCommandHandler
        : ICommandHandler<ExecuteInInteractiveCommandArgs>
    {
        private readonly IEnumerable<Lazy<IExecuteInInteractiveCommandHandler, ContentTypeMetadata>> _executeInInteractiveHandlers;

        [ImportingConstructor]
        public ExecuteInInteractiveCommandHandler(
            [ImportMany]IEnumerable<Lazy<IExecuteInInteractiveCommandHandler, ContentTypeMetadata>> executeInInteractiveHandlers)
        {
            _executeInInteractiveHandlers = executeInInteractiveHandlers;
        }

        private Lazy<IExecuteInInteractiveCommandHandler> GetCommandHandler(ITextBuffer textBuffer)
        {
            return _executeInInteractiveHandlers
                .Where(handler => handler.Metadata.ContentTypes.Contains(textBuffer.ContentType.TypeName))
                .SingleOrDefault();
        }

        void ICommandHandler<ExecuteInInteractiveCommandArgs>.ExecuteCommand(ExecuteInInteractiveCommandArgs args, Action nextHandler)
        {
            GetCommandHandler(args.SubjectBuffer)?.Value.ExecuteCommand(args, nextHandler);
        }

        CommandState ICommandHandler<ExecuteInInteractiveCommandArgs>.GetCommandState(ExecuteInInteractiveCommandArgs args, Func<CommandState> nextHandler)
        {
            return GetCommandHandler(args.SubjectBuffer) == null
                ? CommandState.Unavailable
                : CommandState.Available;
        }
    }
}
