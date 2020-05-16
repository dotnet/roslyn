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
        public static async Task<ImmutableArray<INamedTypeSymbol>?> TryFindAndCacheRemoteTypesAsync(
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
                var project = solution.GetOriginatingProject(type);
                if (project != null)
                {
                    var client = await RemoteHostClient.TryGetClientAsync(solution.Workspace, cancellationToken).ConfigureAwait(false);
                    if (client != null)
                    {
                        var result = await client.TryRunRemoteAsync<ImmutableArray<SerializableSymbolAndProjectId>>(
                            WellKnownServiceHubServices.CodeAnalysisService,
                            remoteFunctionName,
                            solution,
                            new object?[]
                            {
                                SerializableSymbolAndProjectId.Create(type, project, cancellationToken),
                                projects?.Select(p => p.Id).ToArray(),
                                transitive,
                            },
                            null,
                            cancellationToken).ConfigureAwait(false);

                        if (result.HasValue)
                        {
                            return await RehydrateAsync(solution, result.Value, cancellationToken).ConfigureAwait(false);
                        }
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
