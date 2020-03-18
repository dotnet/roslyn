// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Execution;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api
{
    internal static class PythiaRemoteHostClient
    {
        public static async Task<Optional<T>> TryRunRemoteAsync<T>(Workspace workspace, string serviceName, string targetName, Solution solution, IReadOnlyList<object> arguments, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return default;
            }

            if (client.IsRemoteHost64Bit)
            {
                serviceName += "64";
            }

            using var connection = await client.TryCreateConnectionAsync(serviceName, callbackTarget: null, cancellationToken).ConfigureAwait(false);
            if (connection == null)
            {
                return default;
            }

            var remoteDataService = workspace.Services.GetRequiredService<IRemotableDataService>();

            using var scope = await remoteDataService.CreatePinnedRemotableDataScopeAsync(solution, cancellationToken).ConfigureAwait(false);
            using var _ = ArrayBuilder<object>.GetInstance(arguments.Count + 1, out var argumentsBuilder);

            argumentsBuilder.Add(scope.SolutionInfo);
            argumentsBuilder.AddRange(arguments);

            return await connection.InvokeAsync<T>(targetName, argumentsBuilder, cancellationToken).ConfigureAwait(false);
        }
    }
}
