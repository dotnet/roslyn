// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal record struct RequestHandlerMetadata(string MethodName, Type RequestType, Type ResponseType);

    /// <summary>
    /// Aggregates handlers for the specified languages and dispatches LSP requests
    /// to the appropriate handler for the request.
    /// </summary>
    internal class RequestDispatcher
    {
        private readonly ImmutableDictionary<RequestHandlerMetadata, Lazy<IRequestHandler>> _requestHandlers;

        public RequestDispatcher(
            // Lazily imported handler providers to avoid instantiating providers for other languages.
            ImmutableArray<Lazy<AbstractRequestHandlerProvider, RequestHandlerProviderMetadataView>> requestHandlerProviders,
            ImmutableArray<string> languageNames,
            WellKnownLspServerKinds serverKind)
        {
            _requestHandlers = CreateMethodToHandlerMap(requestHandlerProviders.Where(rh => languageNames.All(languageName => rh.Metadata.LanguageNames.Contains(languageName))), serverKind);
        }

        private static ImmutableDictionary<RequestHandlerMetadata, Lazy<IRequestHandler>> CreateMethodToHandlerMap(
            IEnumerable<Lazy<AbstractRequestHandlerProvider, RequestHandlerProviderMetadataView>> requestHandlerProviders, WellKnownLspServerKinds serverKind)
        {
            var requestHandlerDictionary = ImmutableDictionary.CreateBuilder<RequestHandlerMetadata, Lazy<IRequestHandler>>();

            // Iterate through handler providers and retrieve method and type information of the LSP methods handled.
            foreach (var lazyHandlerProvider in requestHandlerProviders)
            {
                var handlerProvider = lazyHandlerProvider.Value;

                // Get the lazily created handlers from this request handler provider.
                var lazyHandlers = handlerProvider.CreateRequestHandlers(serverKind);

                foreach (var lazyHandler in lazyHandlers)
                {
                    // Get the LSP method name from the handler's method name attribute.
                    var methodAttribute = Attribute.GetCustomAttribute(lazyHandler.RequestHandlerType, typeof(MethodAttribute)) as MethodAttribute;
                    Contract.ThrowIfNull(methodAttribute, $"{lazyHandler.RequestHandlerType} is missing Method attribute");

                    // Get the LSP type arguments that this handler type is created with.
                    var (requestType, responseType) = ConvertHandlerTypeToRequestResponseTypes(lazyHandler.RequestHandlerType);

                    // Store the handler metadata to its associated request handler.
                    requestHandlerDictionary.Add(new RequestHandlerMetadata(methodAttribute.Method, requestType, responseType), lazyHandler.RequestHandler);
                }
            }

            return requestHandlerDictionary.ToImmutable();
        }

        /// <summary>
        /// Retrieves the generic argument information from the request handler type without instantiating it.
        /// </summary>
        private static (Type requestType, Type responseType) ConvertHandlerTypeToRequestResponseTypes(Type handlerType)
        {
            var requestHandlerGenericType = handlerType.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>)).SingleOrDefault();
            Contract.ThrowIfNull(requestHandlerGenericType, $"Provided handler type {handlerType.FullName} does not implement IRequestHandler<,>");

            var genericArguments = requestHandlerGenericType.GetGenericArguments();
            Contract.ThrowIfFalse(genericArguments.Length == 2, $"Provided handler type {handlerType.FullName} does not have exactly two generic arguments");
            var requestType = genericArguments[0];
            var responseType = genericArguments[1];

            return (requestType, responseType);
        }

        public async Task<TResponseType?> ExecuteRequestAsync<TRequestType, TResponseType>(
            string methodName,
            TRequestType request,
            LSP.ClientCapabilities clientCapabilities,
            string? clientName,
            RequestExecutionQueue queue,
            CancellationToken cancellationToken) where TRequestType : class
        {
            // Get the handler matching the requested method.
            var requestHandlerMetadata = new RequestHandlerMetadata(methodName, typeof(TRequestType), typeof(TResponseType));

            var handler = _requestHandlers[requestHandlerMetadata].Value;

            var mutatesSolutionState = handler.MutatesSolutionState;
            var requiresLspSolution = handler.RequiresLSPSolution;

            var strongHandler = (IRequestHandler<TRequestType, TResponseType>?)handler;
            Contract.ThrowIfNull(strongHandler, string.Format("Request handler not found for method {0}", methodName));

            var result = await ExecuteRequestAsync(queue, mutatesSolutionState, requiresLspSolution, strongHandler, request, clientCapabilities, clientName, methodName, cancellationToken).ConfigureAwait(false);
            return result;
        }

        protected virtual Task<TResponseType?> ExecuteRequestAsync<TRequestType, TResponseType>(
            RequestExecutionQueue queue,
            bool mutatesSolutionState,
            bool requiresLSPSolution,
            IRequestHandler<TRequestType, TResponseType> handler,
            TRequestType request,
            LSP.ClientCapabilities clientCapabilities,
            string? clientName,
            string methodName,
            CancellationToken cancellationToken) where TRequestType : class
        {
            return queue.ExecuteAsync(mutatesSolutionState, requiresLSPSolution, handler, request, clientCapabilities, clientName, methodName, cancellationToken);
        }

        public ImmutableArray<RequestHandlerMetadata> GetRegisteredMethods()
        {
            return _requestHandlers.Keys.ToImmutableArray();
        }

        internal TestAccessor GetTestAccessor()
            => new TestAccessor(this);

        internal readonly struct TestAccessor
        {
            private readonly RequestDispatcher _requestDispatcher;

            public TestAccessor(RequestDispatcher requestDispatcher)
                => _requestDispatcher = requestDispatcher;

            public IRequestHandler<RequestType, ResponseType> GetHandler<RequestType, ResponseType>(string methodName)
                => (IRequestHandler<RequestType, ResponseType>)_requestDispatcher._requestHandlers.Single(handler => handler.Value.Value.Method == methodName).Value.Value;
        }
    }
}
