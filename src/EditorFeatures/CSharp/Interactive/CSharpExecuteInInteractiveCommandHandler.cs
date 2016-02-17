// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Editor.CommandHandlers;
using Microsoft.CodeAnalysis.Editor.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Interactive
{
    /// <summary>
    /// Implements a execute in interactive command handler.
    /// </summary>
    [ExportCommandHandler("Interactive Command Handler", ContentTypeNames.CSharpContentType)]
    internal class CSharpExecuteInInteractiveCommandHandler
        : ICommandHandler<ExecuteInInteractiveCommandArgs>
    {
        private readonly IEnumerable<Lazy<IExecuteInInteractiveCommandHandler, ContentTypeMetadata>> _executeInInteractiveHandlers;

        [ImportingConstructor]
        public CSharpExecuteInInteractiveCommandHandler(
            [ImportMany]IEnumerable<Lazy<IExecuteInInteractiveCommandHandler, ContentTypeMetadata>> executeInInteractiveHandlers)
        {
            _executeInInteractiveHandlers = executeInInteractiveHandlers;
        }

        private IExecuteInInteractiveCommandHandler CommandHandler
        {
            get
            {
                return _executeInInteractiveHandlers
                    .Where(handler => handler.Metadata.ContentTypes.Contains(ContentTypeNames.CSharpContentType))
                    .SingleOrDefault().Value;
            }
        }

        void ICommandHandler<ExecuteInInteractiveCommandArgs>.ExecuteCommand(ExecuteInInteractiveCommandArgs args, Action nextHandler)
        {
            CommandHandler.ExecuteCommand(args, nextHandler);
        }

        CommandState ICommandHandler<ExecuteInInteractiveCommandArgs>.GetCommandState(ExecuteInInteractiveCommandArgs args, Func<CommandState> nextHandler)
        {
            return CommandState.Available;
        }
    }
}
