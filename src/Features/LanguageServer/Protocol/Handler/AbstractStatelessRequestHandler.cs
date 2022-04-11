// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler
{
    /// <summary>
    /// Defines a request handler that saves no state between LSP requests.
    /// This means that the handler can be shared between multiple servers
    /// and does not need to be re-instantiated on server restarts.
    /// </summary>
    internal abstract class AbstractStatelessRequestHandler<RequestType, ResponseType> : IRequestHandlerProvider, IRequestHandler<RequestType, ResponseType>
    {
        public abstract bool MutatesSolutionState { get; }
        public abstract bool RequiresLSPSolution { get; }

        public abstract TextDocumentIdentifier? GetTextDocumentIdentifier(RequestType request);
        public abstract Task<ResponseType> HandleRequestAsync(RequestType request, RequestContext context, CancellationToken cancellationToken);

        public ImmutableArray<IRequestHandler> CreateRequestHandlers(WellKnownLspServerKinds serverKind)
        {
            return ImmutableArray.Create<IRequestHandler>(this);
        }
    }
}
