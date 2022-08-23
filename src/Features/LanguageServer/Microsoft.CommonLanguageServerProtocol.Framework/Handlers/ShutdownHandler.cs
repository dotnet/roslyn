// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.CommonLanguageServerProtocol.Framework.Handlers;

[LanguageServerEndpoint("shutdown")]
public class ShutdownHandler<RequestContextType> : INotificationHandler<RequestContextType>
{
    private readonly LifeCycleManager<RequestContextType> _lifeCycleManager;

    public ShutdownHandler(LifeCycleManager<RequestContextType> lifeCycleManager)
    {
        _lifeCycleManager = lifeCycleManager;
    }

    public bool MutatesSolutionState => true;

    public static bool RequiresLSPSolution => true;

    public async Task HandleNotificationAsync(RequestContextType requestContext, CancellationToken cancellationToken)
    {
        await _lifeCycleManager.ShutdownAsync().ConfigureAwait(false);
    }
}
