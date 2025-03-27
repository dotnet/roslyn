﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;

internal abstract class AbstractRazorRequestHandler<TRequestType, TResponseType> : ILspServiceRequestHandler<TRequestType, TResponseType>
{
    bool IMethodHandler.MutatesSolutionState => MutatesSolutionState;

    bool ISolutionRequiredHandler.RequiresLSPSolution => RequiresLSPSolution;

    Task<TResponseType> IRequestHandler<TRequestType, TResponseType, RequestContext>.HandleRequestAsync(TRequestType request, RequestContext context, CancellationToken cancellationToken)
    {
        var razorRequestContext = new RazorRequestContext(context);
        return HandleRequestAsync(request, razorRequestContext, cancellationToken);
    }

    protected abstract bool MutatesSolutionState { get; }

    protected abstract bool RequiresLSPSolution { get; }

    protected abstract Task<TResponseType> HandleRequestAsync(TRequestType request, RazorRequestContext context, CancellationToken cancellationToken);
}
