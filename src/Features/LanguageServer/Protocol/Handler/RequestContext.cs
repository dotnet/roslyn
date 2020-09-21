// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using Microsoft.VisualStudio.LanguageServer.Protocol;

#nullable enable

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Context for requests handled by <see cref="IRequestHandler"/>
    /// </summary>
    internal readonly struct RequestContext
    {
        /// <summary>
        /// The client capabilities for the request.
        /// </summary>
        public ClientCapabilities ClientCapabilities { get; }

        /// <summary>
        /// The LSP client making the request
        /// </summary>
        public string? ClientName { get; }

        public RequestContext(ClientCapabilities clientCapabilities, string? clientName)
        {
            ClientCapabilities = clientCapabilities;
            ClientName = clientName;
        }
    }
}
