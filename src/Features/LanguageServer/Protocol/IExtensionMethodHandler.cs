// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Configuration;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CommonLanguageServerProtocol.Framework
{
    // A marker interface so we can have lists of non-generic handlers
    public interface IExtensionMethodHandler
    {
    }

    // The generic interface that all public handlers implement.
    public abstract class ExtensionMethodHandler<Request, Response> : IExtensionMethodHandler
    {
        public abstract Task<Response> HandleRequestAsync(Request request, SimpleRequestContext context, CancellationToken cancellationToken);
    }

    // TODO: Additional code may be needed to handle the MethodAttribute
    internal class ExtensionMethodHandlerWrapper<Request, Response, THandler> : IMethodHandler, ILspServiceRequestHandler<Request, Response>
        where THandler : ExtensionMethodHandler<Request, Response>, new()
    {
        private readonly THandler _handler;

        public ExtensionMethodHandlerWrapper(THandler handler)
        {
            _handler = handler;
        }

        public bool RequiresLSPSolution => true;

        public bool MutatesSolutionState => false;

        public Task<Response> HandleRequestAsync(Request request, RequestContext context, CancellationToken cancellationToken)
        {
            var simpleContext = new SimpleRequestContext();
            simpleContext.SetContext(context);
            return _handler.HandleRequestAsync(request, simpleContext, cancellationToken);
        }
    }

    // This is the interface that we use to instantiate their request handlers.
    interface IHandlerFactory
    {
        ImmutableArray<IMethodHandler> CreateHandlers();
    }

    public sealed class SimpleRequestContext
    {
        private RequestContext _internalContext;

        public SimpleRequestContext()
        {

        }

        internal void SetContext(RequestContext context)
        {
            _internalContext = context;
        }

        public Solution? GetSolution()
        {
            return _internalContext.Solution;
        }
    }
}
