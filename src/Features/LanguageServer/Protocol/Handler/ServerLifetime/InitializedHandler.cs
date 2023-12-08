// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.Handler;

[Method(Methods.InitializedName)]
internal class InitializedHandler : ILspServiceNotificationHandler<InitializedParams>
{
    public InitializedHandler()
    {
    }

    public bool MutatesSolutionState => true;

    public bool RequiresLSPSolution => false;

    public async Task HandleNotificationAsync(InitializedParams request, RequestContext requestContext, CancellationToken cancellationToken)
    {
        var clientCapabilities = requestContext.GetRequiredClientCapabilities();
        var onInitializeList = requestContext.GetRequiredServices<IOnInitialized>();

        foreach (var onInitialize in onInitializeList)
        {
            await onInitialize.OnInitializedAsync(clientCapabilities, requestContext, cancellationToken).ConfigureAwait(false);
        }
    }
}
