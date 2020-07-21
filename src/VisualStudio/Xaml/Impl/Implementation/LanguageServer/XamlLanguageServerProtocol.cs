// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Xaml;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Roslyn.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.LanguageServer
{
    /// <summary>
    /// Implements the Language Server Protocol for XAML
    /// </summary>
    [Shared]
    [Export(typeof(XamlLanguageServerProtocol))]
    internal sealed class XamlLanguageServerProtocol : ILanguageServerProtocol
    {
        private readonly ImmutableDictionary<string, Lazy<IRequestHandler, IRequestHandlerMetadata>> _requestHandlers;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public XamlLanguageServerProtocol([ImportMany] IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers)
            => _requestHandlers = CreateMethodToHandlerMap(requestHandlers.Where(rh => rh.Metadata.LanguageName == StringConstants.XamlLanguageName));

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
