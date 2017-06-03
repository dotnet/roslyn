// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Experiments;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    public static partial class SymbolFinder
    {
        internal static Task<RemoteHostClient.Session> TryGetRemoteSessionAsync(
            Solution solution, CancellationToken cancellationToken)
            => TryGetRemoteSessionAsync(solution, serverCallback: null, cancellationToken: cancellationToken);

        private static Task<RemoteHostClient.Session> TryGetRemoteSessionAsync(
            Solution solution, object serverCallback, CancellationToken cancellationToken)
        {
            return solution.TryCreateCodeAnalysisServiceSessionAsync(
                RemoteFeatureOptions.SymbolFinderEnabled, serverCallback, cancellationToken);
        }
    }
}