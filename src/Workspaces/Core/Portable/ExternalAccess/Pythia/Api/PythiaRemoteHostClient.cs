// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExternalAccess.Pythia.Api
{
    internal static class PythiaRemoteHostClient
    {
        public static Task<Optional<T>> TryRunRemoteAsync<T>(Workspace workspace, string serviceName, string targetName, Solution? solution, IReadOnlyList<object?> arguments, CancellationToken cancellationToken)
        {
            Contract.ThrowIfFalse(serviceName == "pythia");
            return TryRunRemoteAsync<T>(workspace, targetName, solution, arguments, cancellationToken);
        }

        public static async Task<Optional<T>> TryRunRemoteAsync<T>(Workspace workspace, string targetName, Solution? solution, IReadOnlyList<object?> arguments, CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return default;
            }

            return await client.RunRemoteAsync<T>(WellKnownServiceHubService.IntelliCode, targetName, solution, arguments, callbackTarget: null, cancellationToken).ConfigureAwait(false);
        }
    }
}
