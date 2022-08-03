// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CommonLanguageServerProtocol.Framework.Handlers;

[LanguageServerEndpoint("shutdown")]
public class ShutdownHandler<RequestContextType> : INotificationHandler<RequestContextType>
{
    private readonly ILifeCycleManager _lifeCycleManager;

    public ShutdownHandler(ILifeCycleManager lifeCycleManager)
    {
        _lifeCycleManager = lifeCycleManager;
    }

    public bool MutatesSolutionState => true;

    public bool RequiresLSPSolution => true;

    public Task HandleNotificationAsync(RequestContextType requestContext, CancellationToken cancellationToken)
    {
        _lifeCycleManager.Shutdown();

        return Task.CompletedTask;
    }
}
