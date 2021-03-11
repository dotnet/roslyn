// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Defines a provider to create instances of <see cref="IRequestHandler"/>.
    /// New handler instances are created for each LSP server and re-created whenever the 
    /// server restarts.
    /// 
    /// Each <see cref="AbstractRequestHandlerProvider"/> can create multiple <see cref="IRequestHandler"/>
    /// instances in order to share state between different LSP methods.
    /// E.g. completion requests can share a cache with completion resolve requests for the same LSP server.
    /// </summary>
    internal abstract class AbstractRequestHandlerProvider
    {
        /// <summary>
        /// Instantiates new handler instances and returns them.
        /// </summary>
        public abstract ImmutableArray<IRequestHandler> CreateRequestHandlers();
    }
}
