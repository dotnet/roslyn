// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LiveShare.LanguageServices;

namespace Microsoft.VisualStudio.LanguageServices.LiveShare
{
    /// <summary>
    /// Defines a shim between a roslyn LSP request handler and live share LSP request handlers.
    /// </summary>
    internal abstract class AbstractLiveShareHandlerShim<RequestType, ResponseType> : ILspRequestHandler<RequestType, ResponseType, Solution>
    {
        private readonly IRequestHandler<RequestType, ResponseType> _requestHandler;

        public AbstractLiveShareHandlerShim(IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, string methodName)
        {
            _requestHandler = GetRequestHandler(requestHandlers, methodName);
        }

        public virtual Task<ResponseType> HandleAsync(RequestType param, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            return _requestHandler.HandleRequestAsync(requestContext.Context, param, requestContext.ClientCapabilities, cancellationToken);
        }

        protected IRequestHandler<RequestType, ResponseType> GetRequestHandler(IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, string methodName)
        {
            return (IRequestHandler<RequestType, ResponseType>)requestHandlers.First(handler => handler.Metadata.MethodName == methodName).Value;
        }
    }
}
