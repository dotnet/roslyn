// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;

namespace Microsoft.CodeAnalysis.FindSymbols
{
    internal static partial class DependentTypeFinder
    {
        public static async Task<ImmutableArray<INamedTypeSymbol>?> TryFindRemoteTypesAsync(
            INamedTypeSymbol type,
            Solution solution,
            IImmutableSet<Project> projects,
            bool transitive,
            FunctionId functionId,
            string remoteFunctionName,
            CancellationToken cancellationToken)
        {
            using (Logger.LogBlock(functionId, cancellationToken))
            {
                if (SerializableSymbolAndProjectId.TryCreate(type, solution, cancellationToken, out var serializedType))
                {
                    var client = await RemoteHostClient.TryGetClientAsync(solution.Workspace, cancellationToken).ConfigureAwait(false);
                    if (client != null)
                    {
                        var result = await client.RunRemoteAsync<ImmutableArray<SerializableSymbolAndProjectId>>(
                            WellKnownServiceHubService.CodeAnalysis,
                            remoteFunctionName,
                            solution,
                            new object?[]
                            {
                                serializedType,
                                projects?.Select(p => p.Id).ToArray(),
                                transitive,
                            },
                            null,
                            cancellationToken).ConfigureAwait(false);

                        return await RehydrateAsync(solution, result, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            return null;
        }

        private static async Task<ImmutableArray<INamedTypeSymbol>> RehydrateAsync(Solution solution, ImmutableArray<SerializableSymbolAndProjectId> values, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<INamedTypeSymbol>.GetInstance(out var builder);

            foreach (var item in values)
            {
                var rehydrated = await item.TryRehydrateAsync(solution, cancellationToken).ConfigureAwait(false);
                if (rehydrated is INamedTypeSymbol namedType)
                    builder.AddIfNotNull(namedType);
            }

            return builder.ToImmutable();
        }
    }
}
