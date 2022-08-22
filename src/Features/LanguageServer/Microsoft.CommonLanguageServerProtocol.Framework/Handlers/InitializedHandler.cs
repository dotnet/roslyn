// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CommonLanguageServerProtocol.Framework.Handlers;

[LanguageServerEndpoint("initialized")]
public class InitializedHandler<RequestType, RequestContextType> : INotificationHandler<RequestType, RequestContextType>
{
    private bool HasBeenInitialized = false;

    public bool MutatesSolutionState => true;

    public bool RequiresLSPSolution => true;

    public Task HandleNotificationAsync(RequestType request, RequestContextType requestContext, CancellationToken cancellationToken)
    {
        if (HasBeenInitialized)
            throw new InvalidOperationException("initialized was called twice");

        HasBeenInitialized = true;

        return Task.CompletedTask;
    }
}
