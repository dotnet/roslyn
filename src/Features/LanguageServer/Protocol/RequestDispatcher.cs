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
    /// Aggregates handlers for the specified languages and dispatches LSP requests
    /// to the appropriate handler for the request.
    /// </summary>
    internal class RequestDispatcher
    {
        private readonly ImmutableDictionary<string, Lazy<IRequestHandler>> _requestHandlers;

        public RequestDispatcher(
            ImmutableArray<Lazy<AbstractRequestHandlerProvider, RequestHandlerProviderMetadataView>> requestHandlerProviders,
            ImmutableArray<string> languageNames,
            WellKnownLspServerKinds serverKind)
        {
            _requestHandlers = CreateMethodToHandlerMap(requestHandlerProviders.Where(rh => languageNames.All(languageName => rh.Metadata.LanguageNames.Contains(languageName))), serverKind);
        }

        private static ImmutableDictionary<string, Lazy<IRequestHandler>> CreateMethodToHandlerMap(
            IEnumerable<Lazy<AbstractRequestHandlerProvider, RequestHandlerProviderMetadataView>> requestHandlerProviders,
            WellKnownLspServerKinds serverKind)
        {
            var requestHandlerDictionary = ImmutableDictionary.CreateBuilder<string, Lazy<IRequestHandler>>(StringComparer.OrdinalIgnoreCase);

            // Store the request handlers in a dictionary from request name to handler instance.
            foreach (var handlerProvider in requestHandlerProviders)
            {
                var methods = handlerProvider.Metadata.Methods;
                // Instantiate all the providers as one lazy object and re-use it for all methods that the provider provides handlers for.
                // This ensures 2 things:
                // 1.  That the handler provider is not instantiated (and therefore its dependencies are not) until a handler it provides is needed.
                // 2.  That the handler provider's CreateRequestHandlers is only called once and always returns the same handler instances.
                var lazyProviders = new Lazy<ImmutableDictionary<string, IRequestHandler>>(() => handlerProvider.Value.CreateRequestHandlers(serverKind).ToImmutableDictionary(p => p.Method, p => p, StringComparer.OrdinalIgnoreCase));

                foreach (var method in methods)
                {
                    // Using the lazy set of handlers, create a lazy instance that will resolve the set of handlers for the provider
                    // and then lookup the correct handler for the specified method.
                    requestHandlerDictionary.Add(method, new Lazy<IRequestHandler>(() => lazyProviders.Value[method]));
                }
            }

            return requestHandlerDictionary.ToImmutable();
        }

        public Task<ResponseType?> ExecuteRequestAsync<RequestType, ResponseType>(
            RequestExecutionQueue queue,
            string methodName,
            RequestType request,
            LSP.ClientCapabilities clientCapabilities,
            string? clientName,
            CancellationToken cancellationToken) where RequestType : class
        {
            Contract.ThrowIfNull(request);
            Contract.ThrowIfTrue(string.IsNullOrEmpty(methodName), "Invalid method name");

            if (request is ExecuteCommandParams executeCommandRequest)
            {
                // If we have a workspace/executeCommand request, get the request name
                // from the command name.
                methodName = AbstractExecuteWorkspaceCommandHandler.GetRequestNameForCommandName(executeCommandRequest.Command);
            }

            var handlerEntry = _requestHandlers[methodName];
            Contract.ThrowIfNull(handlerEntry, string.Format("Request handler entry not found for method {0}", methodName));

            var mutatesSolutionState = handlerEntry.Value.MutatesSolutionState;
            var requiresLSPSolution = handlerEntry.Value.RequiresLSPSolution;

            var handler = (IRequestHandler<RequestType, ResponseType>?)handlerEntry.Value;
            Contract.ThrowIfNull(handler, string.Format("Request handler not found for method {0}", methodName));

            return ExecuteRequestAsync(queue, request, clientCapabilities, clientName, methodName, mutatesSolutionState, requiresLSPSolution, handler, cancellationToken);
        }

        protected virtual Task<ResponseType?> ExecuteRequestAsync<RequestType, ResponseType>(RequestExecutionQueue queue, RequestType request, ClientCapabilities clientCapabilities, string? clientName, string methodName, bool mutatesSolutionState, bool requiresLSPSolution, IRequestHandler<RequestType, ResponseType> handler, CancellationToken cancellationToken) where RequestType : class
        {
            return queue.ExecuteAsync(mutatesSolutionState, requiresLSPSolution, handler, request, clientCapabilities, clientName, methodName, cancellationToken);
        }

        internal TestAccessor GetTestAccessor()
            => new TestAccessor(this);

        internal readonly struct TestAccessor
        {
            private readonly RequestDispatcher _requestDispatcher;

            public TestAccessor(RequestDispatcher requestDispatcher)
                => _requestDispatcher = requestDispatcher;

            public IRequestHandler<RequestType, ResponseType> GetHandler<RequestType, ResponseType>(string methodName)
                => (IRequestHandler<RequestType, ResponseType>)_requestDispatcher._requestHandlers[methodName].Value;
        }
    }
}
