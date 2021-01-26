// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Commands;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    /// <summary>
    /// Aggregates handlers for the specified language and dispatches LSP requests
    /// to the appropriate handler for the request.
    /// </summary>
    internal class RequestDispatcher
    {
        private readonly ImmutableDictionary<string, (IRequestHandler RequestHandler, ILspMethodMetadata Metadata)> _requestHandlers;

        public RequestDispatcher(ImmutableArray<Lazy<AbstractRequestHandlerProvider, IRequestHandlerProviderMetadata>> requestHandlerProviders, string? languageName = null)
        {
            _requestHandlers = CreateMethodToHandlerMap(requestHandlerProviders.Where(rh => rh.Metadata.LanguageName == languageName));
        }

        private static ImmutableDictionary<string, (IRequestHandler, ILspMethodMetadata)> CreateMethodToHandlerMap(IEnumerable<Lazy<AbstractRequestHandlerProvider, IRequestHandlerProviderMetadata>> requestHandlerProviders)
        {
            var requestHandlerDictionary = ImmutableDictionary.CreateBuilder<string, (IRequestHandler, ILspMethodMetadata)>(StringComparer.OrdinalIgnoreCase);

            // Create the actual request handlers from the providers.
            var handlers = requestHandlerProviders.SelectMany(lazyProvider => lazyProvider.Value.CreateRequestHandlers());

            // Store the request handlers in a dicitionary from request name to handler instance.
            foreach (var handler in handlers)
            {
                var requestName = handler.Metadata.MethodName;
                if (handler.Metadata is ILspCommandMetadata commandMetadata)
                {
                    requestName = LspCommandAttribute.GetRequestNameForCommand(commandMetadata.CommandName);
                }

                requestHandlerDictionary.Add(requestName, handler);
            }

            return requestHandlerDictionary.ToImmutable();
        }

        public Task<ResponseType> ExecuteRequestAsync<RequestType, ResponseType>(RequestExecutionQueue queue, string methodName, RequestType request, LSP.ClientCapabilities clientCapabilities,
            string? clientName, CancellationToken cancellationToken) where RequestType : class
        {
            Contract.ThrowIfNull(request);
            Contract.ThrowIfTrue(string.IsNullOrEmpty(methodName), "Invalid method name");

            if (request is ExecuteCommandParams executeCommandRequest)
            {
                // If we have a workspace/executeCommand request, get the request name
                // from the command name.
                methodName = LspCommandAttribute.GetRequestNameForCommand(executeCommandRequest.Command);
            }

            var handlerEntry = _requestHandlers[methodName];
            Contract.ThrowIfNull(handlerEntry, string.Format("Request handler entry not found for method {0}", methodName));

            var mutatesSolutionState = handlerEntry.Metadata.MutatesSolutionState;

            var handler = (IRequestHandler<RequestType, ResponseType>?)handlerEntry.RequestHandler;
            Contract.ThrowIfNull(handler, string.Format("Request handler not found for method {0}", methodName));

            return ExecuteRequestAsync(queue, request, clientCapabilities, clientName, methodName, mutatesSolutionState, handler, cancellationToken);
        }

        protected virtual Task<ResponseType> ExecuteRequestAsync<RequestType, ResponseType>(RequestExecutionQueue queue, RequestType request, ClientCapabilities clientCapabilities, string? clientName, string methodName, bool mutatesSolutionState, IRequestHandler<RequestType, ResponseType> handler, CancellationToken cancellationToken) where RequestType : class
        {
            return queue.ExecuteAsync(mutatesSolutionState, handler, request, clientCapabilities, clientName, methodName, cancellationToken);
        }

        internal TestAccessor GetTestAccessor()
            => new TestAccessor(this);

        internal readonly struct TestAccessor
        {
            private readonly RequestDispatcher _requestDispatcher;

            public TestAccessor(RequestDispatcher requestDispatcher)
                => _requestDispatcher = requestDispatcher;

            public IRequestHandler<RequestType, ResponseType> GetHandler<RequestType, ResponseType>(string methodName)
                => (IRequestHandler<RequestType, ResponseType>)_requestDispatcher._requestHandlers[methodName].RequestHandler;
        }
    }
}
