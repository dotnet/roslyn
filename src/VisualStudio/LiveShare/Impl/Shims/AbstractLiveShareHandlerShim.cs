﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.LiveShare.LanguageServices;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    /// <summary>
    /// Defines a shim between a roslyn LSP request handler and live share LSP request handlers.
    /// </summary>
    internal abstract class AbstractLiveShareHandlerShim<RequestType, ResponseType> : ILspRequestHandler<RequestType, ResponseType, Solution>
    {
        protected readonly Lazy<IRequestHandler, IRequestHandlerMetadata> LazyRequestHandler;

        public AbstractLiveShareHandlerShim(IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, string methodName)
        {
            LazyRequestHandler = GetRequestHandler(requestHandlers, methodName);
        }

        public virtual Task<ResponseType> HandleAsync(RequestType param, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            return ((IRequestHandler<RequestType, ResponseType>)LazyRequestHandler.Value).HandleRequestAsync(requestContext.Context, param, requestContext.ClientCapabilities?.ToObject<VSClientCapabilities>(), cancellationToken);
        }

        protected Lazy<IRequestHandler, IRequestHandlerMetadata> GetRequestHandler(IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, string methodName)
        {
            return requestHandlers.First(handler => handler.Metadata.MethodName == methodName);
        }
    }
}
