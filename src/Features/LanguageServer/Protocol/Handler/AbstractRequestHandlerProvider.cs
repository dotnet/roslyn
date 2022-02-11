// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Defines a provider to create instances of <see cref="IRequestHandler"/>.
    /// New handler instances are created for each LSP server and re-created whenever the 
    /// server restarts.
    /// 
    /// Implement multiple <see cref="IRequestHandlerProvider{T}"/> on the same provider instance
    /// in order to share state between different LSP methods.
    /// E.g. completion requests can share a cache with completion resolve requests for the same LSP server.
    /// </summary>
    internal interface IRequestHandlerProvider<T> : IRequestHandlerProvider where T : IRequestHandler
    {
        /// <summary>
        /// Instantiates new handler instances and returns them.
        /// </summary>
        public T CreateRequestHandler(WellKnownLspServerKinds serverKind);
    }

    /// <summary>
    /// Marker interface for <see cref="IRequestHandlerProvider{T}"/> to allow exporting.
    /// </summary>
    internal interface IRequestHandlerProvider
    {
    }
}
