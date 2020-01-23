// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

            return await client.TryRunRemoteAsync<T>(serviceName, targetName, solution, arguments, callbackTarget: null, cancellationToken).ConfigureAwait(false);
        }
    }
}
