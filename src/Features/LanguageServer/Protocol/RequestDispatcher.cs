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
            ImmutableArray<Lazy<IRequestHandlerProvider, RequestHandlerProviderMetadataView>> requestHandlerProviders,
            ImmutableArray<string> languageNames,
            WellKnownLspServerKinds serverKind)
        {
            _requestHandlers = CreateMethodToHandlerMap(requestHandlerProviders.Where(rh => languageNames.All(languageName => rh.Metadata.LanguageNames.Contains(languageName))), serverKind);
        }

        private static ImmutableDictionary<RequestHandlerMetadata, Lazy<IRequestHandler>> CreateMethodToHandlerMap(
            IEnumerable<Lazy<IRequestHandlerProvider, RequestHandlerProviderMetadataView>> requestHandlerProviders, WellKnownLspServerKinds serverKind)
        {
            var requestHandlerDictionary = ImmutableDictionary.CreateBuilder<RequestHandlerMetadata, Lazy<IRequestHandler>>();

            // Go through all of the handler providers and lazily retrieve the handlers that they provide.
            foreach (var lazyHandlerProvider in requestHandlerProviders)
            {
                var handlerProvider = lazyHandlerProvider.Value;

                // Get the IRequestHandlerProvider<T> generic types that this provider is associated with.
                var providerGenericTypes = GetGenericProviderTypes(handlerProvider);
                foreach (var providerGenericType in providerGenericTypes)
                {
                    // Get the actual IRequestHandlerType that is provided by this IRequestHandlerProvider<T>
                    var handlerType = providerGenericType.GetGenericArguments().Single();

                    // Get the LSP type arguments that this handler type is created with.
                    var (requestType, responseType) = ConvertHandlerTypeToRequestResponseTypes(handlerType);

                    // Get the LSP method name from the handler's method name attribute.
                    var methodAttribute = Attribute.GetCustomAttribute(handlerType, typeof(MethodAttribute)) as MethodAttribute;
                    Contract.ThrowIfNull(methodAttribute, $"{handlerType} is missing Method attribute");

                    // Lazily instantiate the IRequestHandler
                    var lazyHandler = new Lazy<IRequestHandler>(() => CreateRequestHandler(handlerProvider, providerGenericType, serverKind));

                    // Store the handler metadata to its associated request handler.
                    requestHandlerDictionary.Add(new RequestHandlerMetadata(methodAttribute.Method, requestType, responseType), lazyHandler);
                }

                foreach (var (method, handlerType) in handlerMetadata)
                {
                    var (requestType, responseType) = ConvertHandlerTypeToRequestResponseTypes(handlerType);

                    // Using the lazy set of handlers, create a lazy instance that will resolve the set of handlers for the provider
                    // and then lookup the correct handler for the specified method.
                    requestHandlerDictionary.Add(new RequestHandlerMetadata(method, requestType, responseType), new Lazy<IRequestHandler>(() => lazyProviders.Value[method]));
                }
            }

            return requestHandlerDictionary.ToImmutable();
        }

        private static ImmutableArray<Type> GetGenericProviderTypes(IRequestHandlerProvider provider)
        {
            var providerType = provider.GetType();
            var providerGenericTypes = providerType.GetInterfaces().Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandlerProvider<>)).ToImmutableArray();
            Contract.ThrowIfTrue(providerGenericTypes.IsEmpty, $"Handler provider {providerType.FullName} does not implement any IRequestHandlerProvider<>");

            return providerGenericTypes;
        }

        private static IRequestHandler CreateRequestHandler(IRequestHandlerProvider providerInstance, Type providerGenericType, WellKnownLspServerKinds serverKind)
        {
            var methodInfo = providerGenericType.GetMethod("CreateRequestHandler");
            Contract.ThrowIfNull(methodInfo, $"Handler provider {providerGenericType} is missing CreateRequestHandler method");

            var result = methodInfo.Invoke(providerInstance, new object[] { serverKind }) as IRequestHandler;
            Contract.ThrowIfNull(result, $"Could not invoke {methodInfo.Name} on {providerGenericType.FullName}");

            return result;
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
