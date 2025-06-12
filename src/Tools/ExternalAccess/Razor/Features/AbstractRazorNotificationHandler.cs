// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.Handler;

namespace Microsoft.CodeAnalysis.ExternalAccess.Razor.Features;

internal abstract class AbstractRazorNotificationHandler<TRequestType> : ILspServiceNotificationHandler<TRequestType>
{
    public abstract bool MutatesSolutionState { get; }
    public abstract bool RequiresLSPSolution { get; }

    public Task HandleNotificationAsync(TRequestType request, RequestContext requestContext, CancellationToken cancellationToken)
    {
        var razorRequestContext = new RazorRequestContext(requestContext);
        return HandleNotificationAsync(request, razorRequestContext, cancellationToken);
    }

    protected abstract Task HandleNotificationAsync(TRequestType request, RazorRequestContext razorRequestContext, CancellationToken cancellationToken);
}
