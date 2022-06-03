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
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal readonly record struct RequestHandlerMetadata(string MethodName, Type RequestType, Type ResponseType);

    /// <summary>
    /// Aggregates handlers for the specified languages and dispatches LSP requests
    /// to the appropriate handler for the request.
    /// </summary>
    internal class RequestDispatcher : ILspService
    {
        private readonly ImmutableDictionary<RequestHandlerMetadata, Lazy<IRequestHandler>> _requestHandlers;

        public RequestDispatcher(LspServices lspServices)
        {
            _requestHandlers = CreateMethodToHandlerMap(lspServices);
        }

        private static ImmutableDictionary<RequestHandlerMetadata, Lazy<IRequestHandler>> CreateMethodToHandlerMap(LspServices lspServices)
        {
            var requestHandlerDictionary = ImmutableDictionary.CreateBuilder<RequestHandlerMetadata, Lazy<IRequestHandler>>();

            var requestHandlerTypes = lspServices.GetRegisteredServices().Where(type => IsTypeRequestHandler(type));

            foreach (var handlerType in requestHandlerTypes)
            {
                var (requestType, responseType) = ConvertHandlerTypeToRequestResponseTypes(handlerType);
                var method = GetRequestHandlerMethod(handlerType);

                // Using the lazy set of handlers, create a lazy instance that will resolve the set of handlers for the provider
                // and then lookup the correct handler for the specified method.
                requestHandlerDictionary.Add(new RequestHandlerMetadata(method, requestType, responseType), new Lazy<IRequestHandler>(() =>
                {
                    Contract.ThrowIfFalse(lspServices.TryGetService(handlerType, out var lspService));
                    return (IRequestHandler)lspService;
                }));
            }

            return requestHandlerDictionary.ToImmutable();

            static string GetRequestHandlerMethod(Type handlerType)
            {
                // Get the LSP method name from the handler's method name attribute.
                var methodAttribute = Attribute.GetCustomAttribute(handlerType, typeof(MethodAttribute)) as MethodAttribute;
                Contract.ThrowIfNull(methodAttribute, $"{handlerType.FullName} is missing Method attribute");

                return methodAttribute.Method;
            }

            static bool IsTypeRequestHandler(Type type)
            {
                return type.GetInterfaces().Contains(typeof(IRequestHandler));
            }
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

            var result = await ExecuteRequestAsync(queue, mutatesSolutionState, requiresLspSolution, strongHandler, request, clientCapabilities, methodName, cancellationToken).ConfigureAwait(false);
            return result;
        }

        protected virtual Task<TResponseType?> ExecuteRequestAsync<TRequestType, TResponseType>(
            RequestExecutionQueue queue,
            bool mutatesSolutionState,
            bool requiresLSPSolution,
            IRequestHandler<TRequestType, TResponseType> handler,
            TRequestType request,
            LSP.ClientCapabilities clientCapabilities,
            string methodName,
            CancellationToken cancellationToken) where TRequestType : class
        {
            return queue.ExecuteAsync(mutatesSolutionState, requiresLSPSolution, handler, request, clientCapabilities, methodName, cancellationToken);
        }

        public ImmutableArray<RequestHandlerMetadata> GetRegisteredMethods()
        {
            return _requestHandlers.Keys.ToImmutableArray();
        }
    }
}
