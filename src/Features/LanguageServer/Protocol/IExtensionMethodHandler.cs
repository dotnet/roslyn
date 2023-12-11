// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;

namespace Microsoft.CommonLanguageServerProtocol.Framework
{
    // Some marker interface so we can have lists of non-generic handlers
    public interface IExtensionMethodHandler : IMethodHandler
    {
    }

    // The actual generic interface that all public handlers implement.
    // Probably should actually be an abstract class.  Could also instead of implementing ILspServiceRequestHandler, we could create a Wrapper class that implements ILspServiceRequestHandler and then delegates to the actual handler.
    // This is the main thing a user implements to create a handler.
    interface IExtensionMethodHandler<Request, Response> : IExtensionMethodHandler, ILspServiceDocumentRequestHandler<Request, Response>
    {
        // TODO Simplify the context 
        Task<Response> HandleRequestAsync(Request request, SimpleRequestContext context, CancellationToken cancellationToken);
    }

    // This is the interface that we use to instantiate their request handlers.
    interface IHandlerFactory
    {
        ImmutableArray<IMethodHandler> CreateHandlers();
    }

    public struct SimpleRequestContext { }
}
