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

    protected abstract bool MutatesSolutionState { get; }

    protected abstract bool RequiresLSPSolution { get; }

    public abstract Task<TResponseType> HandleRequestAsync(TRequestType request, RequestContext context, CancellationToken cancellationToken);
}
