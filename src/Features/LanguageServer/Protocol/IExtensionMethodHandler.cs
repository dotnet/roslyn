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
    // Some marker interface so we can have lists of non-generic handlers
    public interface IExtensionMethodHandler
    {
    }

    // The actual generic interface that all public handlers implement.
    // Probably should actually be an abstract class.  Could also instead of implementing ILspServiceRequestHandler, we could create a Wrapper class that implements ILspServiceRequestHandler and then delegates to the actual handler.
    // This is the main thing a user implements to create a handler.

    public abstract class ExtensionMethodHandler<Request, Response> : IExtensionMethodHandler
    {
        public abstract Task<Response> HandleRequestAsync(Request request, SimpleRequestContext context, CancellationToken cancellationToken);
    }

    // May have problem with method attribute 
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
