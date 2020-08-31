﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.SemanticTokens;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal abstract class AbstractRequestHandlerProvider
    {
        private readonly ImmutableDictionary<string, Lazy<IRequestHandler, IRequestHandlerMetadata>> _requestHandlers;
        private readonly RequestExecutionQueue _queue;

        public AbstractRequestHandlerProvider(IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, ILspSolutionProvider solutionProvider, string? languageName = null)
        {
            _requestHandlers = CreateMethodToHandlerMap(requestHandlers.Where(rh => rh.Metadata.LanguageName == languageName));
            _queue = new RequestExecutionQueue(solutionProvider);
        }

        private static ImmutableDictionary<string, Lazy<IRequestHandler, IRequestHandlerMetadata>> CreateMethodToHandlerMap(IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers)
        {
            var requestHandlerDictionary = ImmutableDictionary.CreateBuilder<string, Lazy<IRequestHandler, IRequestHandlerMetadata>>();
            foreach (var lazyHandler in requestHandlers)
            {
                requestHandlerDictionary.Add(lazyHandler.Metadata.MethodName, lazyHandler);
            }

            return requestHandlerDictionary.ToImmutable();
        }

        public Task<ResponseType> ExecuteRequestAsync<RequestType, ResponseType>(string methodName, RequestType request, LSP.ClientCapabilities clientCapabilities,
            string? clientName, CancellationToken cancellationToken) where RequestType : class
        {
            Contract.ThrowIfNull(request);
            Contract.ThrowIfTrue(string.IsNullOrEmpty(methodName), "Invalid method name");

            var handlerEntry = _requestHandlers[methodName];
            Contract.ThrowIfNull(handlerEntry, string.Format("Request handler entry not found for method {0}", methodName));

            var mutatesSolutionState = handlerEntry.Metadata.MutatesSolutionState;

            var handler = (IRequestHandler<RequestType, ResponseType>?)handlerEntry.Value;
            Contract.ThrowIfNull(handler, string.Format("Request handler not found for method {0}", methodName));

            return _queue.ExecuteAsync(mutatesSolutionState, handler, request, clientCapabilities, clientName, cancellationToken);
        }

        public void InitializeRequestQueue()
        {
            _queue.Initialize();
        }

        public void ShutdownRequestQueue()
        {
            _queue.Shutdown();
        }
    }
}
