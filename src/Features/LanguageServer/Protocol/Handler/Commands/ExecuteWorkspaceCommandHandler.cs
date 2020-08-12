// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Commands;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    [Shared]
    [ExportLspMethod(LSP.Methods.WorkspaceExecuteCommandName)]
    internal class ExecuteWorkspaceCommandHandler : IRequestHandler<LSP.ExecuteCommandParams, object>
    {
        private readonly ImmutableDictionary<string, Lazy<IExecuteWorkspaceCommandHandler, IExecuteWorkspaceCommandHandlerMetadata>> _executeCommandHandlers;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ExecuteWorkspaceCommandHandler([ImportMany] IEnumerable<Lazy<IExecuteWorkspaceCommandHandler, IExecuteWorkspaceCommandHandlerMetadata>> executeCommandHandlers)
            => _executeCommandHandlers = CreateMap(executeCommandHandlers);

        private static ImmutableDictionary<string, Lazy<IExecuteWorkspaceCommandHandler, IExecuteWorkspaceCommandHandlerMetadata>> CreateMap(
            IEnumerable<Lazy<IExecuteWorkspaceCommandHandler, IExecuteWorkspaceCommandHandlerMetadata>> requestHandlers)
        {
            var requestHandlerDictionary = ImmutableDictionary.CreateBuilder<string, Lazy<IExecuteWorkspaceCommandHandler, IExecuteWorkspaceCommandHandlerMetadata>>();
            foreach (var lazyHandler in requestHandlers)
            {
                requestHandlerDictionary.Add(lazyHandler.Metadata.CommandName, lazyHandler);
            }

            return requestHandlerDictionary.ToImmutable();
        }

        /// <summary>
        /// Handles an <see cref="LSP.Methods.WorkspaceExecuteCommand"/>
        /// by delegating to a handler for the specific command requested.
        /// </summary>
        public Task<object> HandleRequestAsync(LSP.ExecuteCommandParams request, RequestContext context, CancellationToken cancellationToken)
        {
            var commandName = request.Command;
            if (string.IsNullOrEmpty(commandName) || !_executeCommandHandlers.TryGetValue(commandName, out var executeCommandHandler))
            {
                throw new ArgumentException(string.Format("Command name ({0}) is invalid", commandName));
            }

            return executeCommandHandler.Value.HandleRequestAsync(request, context, cancellationToken);
        }
    }
}
