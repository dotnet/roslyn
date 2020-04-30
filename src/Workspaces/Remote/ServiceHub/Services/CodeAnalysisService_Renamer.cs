﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Rename.ConflictEngine;

namespace Microsoft.CodeAnalysis.Remote
{
    // root level service for all Roslyn services
    internal partial class CodeAnalysisService : IRemoteRenamer
    {
        public Task<SerializableRenameLocations> FindRenameLocationsAsync(
            PinnedSolutionInfo solutionInfo,
            SerializableSymbolAndProjectId symbolAndProjectId,
            SerializableRenameOptionSet options,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync<SerializableRenameLocations>(async () =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);

                    var symbol = await symbolAndProjectId.TryRehydrateAsync(
                        solution, cancellationToken).ConfigureAwait(false);

                    if (symbol == null)
                        return null;

                    var result = await RenameLocations.FindLocationsAsync(
                        symbol, solution, options.Rehydrate(), cancellationToken).ConfigureAwait(false);
                    return result.Dehydrate(solution, cancellationToken);
                }
            }, cancellationToken);
        }

        public Task<SerializableConflictResolution> ResolveConflictsAsync(
            PinnedSolutionInfo solutionInfo,
            SerializableRenameLocations renameLocationSet,
            string replacementText,
            SerializableSymbolAndProjectId[] nonConflictSymbolIds,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync<SerializableConflictResolution>(async () =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                    var nonConflictSymbols = await GetNonConflictSymbolsAsync(solution, nonConflictSymbolIds, cancellationToken).ConfigureAwait(false);

                    var result = await ConflictResolver.ResolveConflictsAsync(
                        await RenameLocations.RehydrateAsync(solution, renameLocationSet, cancellationToken).ConfigureAwait(false),
                        replacementText,
                        nonConflictSymbols,
                        cancellationToken).ConfigureAwait(false);
                    return await result.DehydrateAsync(cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        private async Task<ImmutableHashSet<ISymbol>> GetNonConflictSymbolsAsync(Solution solution, SerializableSymbolAndProjectId[] nonConflictSymbolIds, CancellationToken cancellationToken)
        {
            if (nonConflictSymbolIds == null)
                return null;

            var builder = ImmutableHashSet.CreateBuilder<ISymbol>();
            foreach (var id in nonConflictSymbolIds)
            {
                var symbol = await id.TryRehydrateAsync(solution, cancellationToken).ConfigureAwait(false);
                if (symbol != null)
                    builder.Add(symbol);
            }

            return builder.ToImmutable();
        }
    }
}
