// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CommonLanguageServerProtocol.Framework;

namespace Microsoft.CommonLanguageServerProtocol.Framework.Handlers;

[LanguageServerEndpoint("exit")]
public class ExitHandler<TRequestContext> : INotificationHandler<TRequestContext>
{
    private readonly ILifeCycleManager _lifeCycleManager;

    public ExitHandler(ILifeCycleManager lifeCycleManager)
    {
        _lifeCycleManager = lifeCycleManager;
    }

    public bool MutatesSolutionState => true;

    public async Task HandleNotificationAsync(TRequestContext requestContext, CancellationToken cancellationToken)
    {
        await _lifeCycleManager.ExitAsync().ConfigureAwait(false);
    }
}
