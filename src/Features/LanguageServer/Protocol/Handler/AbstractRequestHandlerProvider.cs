// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Defines a provider to create instances of <see cref="IRequestHandler"/>.
    /// New handler instances are created for each LSP server and re-created whenever the 
    /// server restarts.
    /// 
    /// Note that the instances of <see cref="AbstractRequestHandlerProvider"/> are all created
    /// upfront, so any dependencies they import will also be instantiated upfront.
    /// </summary>
    internal abstract class AbstractRequestHandlerProvider
    {
        public abstract ImmutableArray<LazyRequestHandler> CreateRequestHandlers(WellKnownLspServerKinds serverKind);

        protected static LazyRequestHandler CreateLazyRequestHandlerMetadata<T>(Func<T> creationFunc) where T : IRequestHandler
        {
            return new LazyRequestHandler(typeof(T), new Lazy<IRequestHandler>(() => creationFunc()));
        }

        protected static ImmutableArray<LazyRequestHandler> CreateSingleRequestHandler<T>(Func<T> creationFunc) where T : IRequestHandler
        {
            return ImmutableArray.Create(CreateLazyRequestHandlerMetadata(creationFunc));
        }
    }

    internal record struct LazyRequestHandler(Type RequestHandlerType, Lazy<IRequestHandler> RequestHandler);
}
