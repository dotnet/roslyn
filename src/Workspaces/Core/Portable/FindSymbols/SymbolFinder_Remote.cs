// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    public static partial class SymbolFinder
    {
        internal static Task<RemoteHostClient.Session> TryGetRemoteSessionAsync(
            Solution solution, CancellationToken cancellationToken)
            => TryGetRemoteSessionAsync(solution, serverCallback: null, cancellationToken: cancellationToken);

        private static async Task<RemoteHostClient.Session> TryGetRemoteSessionAsync(
            Solution solution, object serverCallback, CancellationToken cancellationToken)
        {
            var outOfProcessAllowed = solution.Workspace.Options.GetOption(SymbolFinderOptions.OutOfProcessAllowed);
            if (!outOfProcessAllowed)
            {
                return null;
            }

            var client = await solution.Workspace.TryGetRemoteHostClientAsync(cancellationToken).ConfigureAwait(false);
            if (client == null)
            {
                return null;
            }

            return await client.TryCreateCodeAnalysisServiceSessionAsync(
                solution, serverCallback, cancellationToken).ConfigureAwait(false);
        }
    }
}