// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
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

    internal abstract class AbstractLiveShareHandlerOnMainThreadShim<RequestType, ResponseType> : AbstractLiveShareHandlerShim<RequestType, ResponseType>
    {
        private readonly IThreadingContext _threadingContext;

        public AbstractLiveShareHandlerOnMainThreadShim(IEnumerable<Lazy<IRequestHandler, IRequestHandlerMetadata>> requestHandlers, string methodName, IThreadingContext threadingContext) : base(requestHandlers, methodName)
        {
            _threadingContext = threadingContext;
        }

        public override async Task<ResponseType> HandleAsync(RequestType param, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            await _threadingContext.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            return await HandleAsyncPreserveThreadContext(param, requestContext, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Certain implementations require that the processing be done on the UI thread.
        /// So allow the handler to specifiy that the thread context should be preserved.
        /// </summary>
        private Task<ResponseType> HandleAsyncPreserveThreadContext(RequestType param, RequestContext<Solution> requestContext, CancellationToken cancellationToken)
        {
            return ((IRequestHandler<RequestType, ResponseType>)LazyRequestHandler.Value).HandleRequestAsync(requestContext.Context, param, requestContext.ClientCapabilities?.ToObject<ClientCapabilities>(), cancellationToken, keepThreadContext: true);
        }
    }
}
