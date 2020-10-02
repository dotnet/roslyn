// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Rename.ConflictEngine;

namespace Microsoft.CodeAnalysis.Remote
{
    // root level service for all Roslyn services
    internal partial class CodeAnalysisService : IRemoteRenamer
    {
        public Task<SerializableConflictResolution?> RenameSymbolAsync(
            PinnedSolutionInfo solutionInfo,
            SerializableSymbolAndProjectId symbolAndProjectId,
            string newName,
            SerializableRenameOptionSet options,
            SerializableSymbolAndProjectId[] nonConflictSymbolIds,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync<SerializableConflictResolution?>(async () =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);

                    var symbol = await symbolAndProjectId.TryRehydrateAsync(
                        solution, cancellationToken).ConfigureAwait(false);

                    if (symbol == null)
                        return null;

                    var nonConflictSymbols = await GetNonConflictSymbolsAsync(solution, nonConflictSymbolIds, cancellationToken).ConfigureAwait(false);

                    var result = await Renamer.RenameSymbolAsync(
                        solution, symbol, newName, options.Rehydrate(),
                        nonConflictSymbols, cancellationToken).ConfigureAwait(false);
                    return await result.DehydrateAsync(cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        public Task<SerializableRenameLocations?> FindRenameLocationsAsync(
            PinnedSolutionInfo solutionInfo,
            SerializableSymbolAndProjectId symbolAndProjectId,
            SerializableRenameOptionSet options,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync<SerializableRenameLocations?>(async () =>
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

        public Task<SerializableConflictResolution?> ResolveConflictsAsync(
            PinnedSolutionInfo solutionInfo,
            SerializableRenameLocations renameLocationSet,
            string replacementText,
            SerializableSymbolAndProjectId[] nonConflictSymbolIds,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync<SerializableConflictResolution?>(async () =>
            {
                using (UserOperationBooster.Boost())
                {
                    var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);
                    var nonConflictSymbols = await GetNonConflictSymbolsAsync(solution, nonConflictSymbolIds, cancellationToken).ConfigureAwait(false);

                    var rehydratedSet = await RenameLocations.TryRehydrateAsync(solution, renameLocationSet, cancellationToken).ConfigureAwait(false);
                    if (rehydratedSet == null)
                        return null;

                    var result = await ConflictResolver.ResolveConflictsAsync(
                        rehydratedSet,
                        replacementText,
                        nonConflictSymbols,
                        cancellationToken).ConfigureAwait(false);
                    return await result.DehydrateAsync(cancellationToken).ConfigureAwait(false);
                }
            }, cancellationToken);
        }

        private static async Task<ImmutableHashSet<ISymbol>?> GetNonConflictSymbolsAsync(Solution solution, SerializableSymbolAndProjectId[]? nonConflictSymbolIds, CancellationToken cancellationToken)
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
