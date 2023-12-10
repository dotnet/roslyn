// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Cohost;

internal abstract class AbstractRazorCohostRequestHandler<TRequestType, TResponseType> : ILspServiceRequestHandler<TRequestType, TResponseType>
{
    bool IMethodHandler.MutatesSolutionState => MutatesSolutionState;

    bool ISolutionRequiredHandler.RequiresLSPSolution => RequiresLSPSolution;

    Task<TResponseType> IRequestHandler<TRequestType, TResponseType, RequestContext>.HandleRequestAsync(TRequestType request, RequestContext context, CancellationToken cancellationToken)
    {
        // We have to wrap the RequestContext in order to expose it to Roslyn. We could create our own (by exporting
        // and IRequestContextFactory) but that would not be possible if/when we live in the same server as Roslyn
        // so may as well deal with it now.
        // This does mean we can't nicely pass through the original Uri, which would have ProjectContext info, but
        // we get the Project so that will have to do.

        var razorRequestContext = new RazorCohostRequestContext(context);
        return HandleRequestAsync(request, razorRequestContext, cancellationToken);
    }

    protected abstract bool MutatesSolutionState { get; }

    protected abstract bool RequiresLSPSolution { get; }

    protected abstract Task<TResponseType> HandleRequestAsync(TRequestType request, RazorCohostRequestContext context, CancellationToken cancellationToken);
}
