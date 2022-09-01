// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Rename.ConflictEngine;

namespace Microsoft.CodeAnalysis.Remote
{
    internal sealed class RemoteRenamerService : BrokeredServiceBase, IRemoteRenamerService
    {
        internal sealed class Factory : FactoryBase<IRemoteRenamerService>
        {
            protected override IRemoteRenamerService CreateService(in ServiceConstructionArguments arguments)
                => new RemoteRenamerService(arguments);
        }

        public RemoteRenamerService(in ServiceConstructionArguments arguments)
            : base(arguments)
        {
        }

        public ValueTask<SerializableConflictResolution?> RenameSymbolAsync(
            PinnedSolutionInfo solutionInfo,
            SerializableSymbolAndProjectId symbolAndProjectId,
            string newName,
            SymbolRenameOptions options,
            ImmutableArray<SerializableSymbolAndProjectId> nonConflictSymbolIds,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);

                var symbol = await symbolAndProjectId.TryRehydrateAsync(
                    solution, cancellationToken).ConfigureAwait(false);

                if (symbol == null)
                    return null;

                var nonConflictSymbols = await GetNonConflictSymbolsAsync(solution, nonConflictSymbolIds, cancellationToken).ConfigureAwait(false);

                var result = await Renamer.RenameSymbolAsync(
                    solution, symbol, newName, options,
                    nonConflictSymbols, cancellationToken).ConfigureAwait(false);
                return await result.DehydrateAsync(cancellationToken).ConfigureAwait(false);
            }, cancellationToken);
        }

        public ValueTask<SerializableRenameLocations?> FindRenameLocationsAsync(
            PinnedSolutionInfo solutionInfo,
            SerializableSymbolAndProjectId symbolAndProjectId,
            SymbolRenameOptions options,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
            {
                var solution = await GetSolutionAsync(solutionInfo, cancellationToken).ConfigureAwait(false);

                var symbol = await symbolAndProjectId.TryRehydrateAsync(
                    solution, cancellationToken).ConfigureAwait(false);

                if (symbol == null)
                    return null;

                var result = await RenameLocations.FindLocationsAsync(
                    symbol, solution, options, cancellationToken).ConfigureAwait(false);
                return result.Dehydrate(solution, cancellationToken);
            }, cancellationToken);
        }

        public ValueTask<SerializableConflictResolution?> ResolveConflictsAsync(
            PinnedSolutionInfo solutionInfo,
            SerializableRenameLocations renameLocationSet,
            string replacementText,
            ImmutableArray<SerializableSymbolAndProjectId> nonConflictSymbolIds,
            CancellationToken cancellationToken)
        {
            return RunServiceAsync(async cancellationToken =>
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
            }, cancellationToken);
        }

        private static async Task<ImmutableHashSet<ISymbol>?> GetNonConflictSymbolsAsync(Solution solution, ImmutableArray<SerializableSymbolAndProjectId> nonConflictSymbolIds, CancellationToken cancellationToken)
        {
            if (nonConflictSymbolIds.IsDefault)
            {
                return null;
            }

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
