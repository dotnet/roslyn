// Licensed to the .NET Foundation under one or more agreements.
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
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer
{
    internal abstract class AbstractRequestHandlerProvider
    {
        private readonly ImmutableDictionary<string, Lazy<IRequestHandler, IRequestHandlerMetadata>> _requestHandlers;

        public AbstractRequestHandlerProvider(IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, string? languageName = null)
            => _requestHandlers = CreateMethodToHandlerMap(requestHandlers.Where(rh => rh.Metadata.LanguageName == languageName));

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

            var handler = (IRequestHandler<RequestType, ResponseType>?)_requestHandlers[methodName]?.Value;
            Contract.ThrowIfNull(handler, string.Format("Request handler not found for method {0}", methodName));

            return handler.HandleRequestAsync(request, clientCapabilities, clientName, cancellationToken);
        }
    }
}
