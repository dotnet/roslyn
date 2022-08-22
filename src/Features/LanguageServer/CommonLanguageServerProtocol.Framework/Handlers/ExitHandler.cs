// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace CommonLanguageServerProtocol.Framework.Handlers;

[LanguageServerEndpoint("exit")]
public class ExitHandler<RequestContextType> : INotificationHandler<RequestContextType>
{
    private readonly LifeCycleManager<RequestContextType> _lifeCycleManager;

    public ExitHandler(LifeCycleManager<RequestContextType> lifeCycleManager)
    {
        _lifeCycleManager = lifeCycleManager;
    }

    public bool MutatesSolutionState => true;

    public async Task HandleNotificationAsync(RequestContextType requestContext, CancellationToken cancellationToken)
    {
        await _lifeCycleManager.ExitAsync();
    }
}
