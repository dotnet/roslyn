// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename
{
    public static class Renamer
    {
        public static async Task<Solution> RenameSymbolAsync(
            Solution solution, ISymbol symbol, string newName, OptionSet optionSet, CancellationToken cancellationToken = default)
        {
            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            if (symbol == null)
                throw new ArgumentNullException(nameof(symbol));

            if (solution.GetOriginatingProjectId(symbol) == null)
                throw new ArgumentException(WorkspacesResources.Symbols_project_could_not_be_found_in_the_provided_solution, nameof(symbol));

            if (string.IsNullOrEmpty(newName))
                throw new ArgumentException(nameof(newName));

            var result = await RenameSymbolAsync(
                solution, symbol, newName,
                RenameOptionSet.From(solution, optionSet),
                nonConflictSymbols: null, cancellationToken).ConfigureAwait(false);

            if (result.ErrorMessage != null)
                throw new ArgumentException(result.ErrorMessage);

            return result.NewSolution;
        }

        internal static Task<RenameLocations> FindRenameLocationsAsync(Solution solution, ISymbol symbol, RenameOptionSet optionSet, CancellationToken cancellationToken)
            => RenameLocations.FindLocationsAsync(symbol, solution, optionSet, cancellationToken);

        internal static async Task<ConflictResolution> RenameSymbolAsync(
            Solution solution,
            ISymbol symbol,
            string newName,
            RenameOptionSet optionSet,
            ImmutableHashSet<ISymbol> nonConflictSymbols,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(solution);
            Contract.ThrowIfNull(symbol);
            Contract.ThrowIfNull(solution.GetOriginatingProjectId(symbol), WorkspacesResources.Symbols_project_could_not_be_found_in_the_provided_solution);
            Contract.ThrowIfTrue(string.IsNullOrEmpty(newName));

            cancellationToken.ThrowIfCancellationRequested();

            using (Logger.LogBlock(FunctionId.Renamer_RenameSymbolAsync, cancellationToken))
            {
                var client = await RemoteHostClient.TryGetClientAsync(solution.Workspace, cancellationToken).ConfigureAwait(false);
                if (client != null)
                {
                    var result = await client.TryRunRemoteAsync<SerializableConflictResolution>(
                        WellKnownServiceHubServices.CodeAnalysisService,
                        nameof(IRemoteRenamer.RenameSymbolAsync),
                        solution,
                        new object[]
                        {
                            SerializableSymbolAndProjectId.Dehydrate(solution, symbol, cancellationToken),
                            newName,
                            SerializableRenameOptionSet.Dehydrate(optionSet),
                            nonConflictSymbols?.Select(s => SerializableSymbolAndProjectId.Dehydrate(solution, s, cancellationToken)).ToArray(),
                        },
                        callbackTarget: null,
                        cancellationToken).ConfigureAwait(false);

                    if (result.HasValue)
                    {
                        return await result.Value.RehydrateAsync(solution, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            return await RenameSymbolInCurrentProcessAsync(
                solution, symbol, newName, optionSet,
                nonConflictSymbols, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<ConflictResolution> RenameSymbolInCurrentProcessAsync(
            Solution solution,
            ISymbol symbol,
            string newName,
            RenameOptionSet optionSet,
            ImmutableHashSet<ISymbol> nonConflictSymbols,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(solution);
            Contract.ThrowIfNull(symbol);
            Contract.ThrowIfNull(solution.GetOriginatingProjectId(symbol), WorkspacesResources.Symbols_project_could_not_be_found_in_the_provided_solution);
            Contract.ThrowIfTrue(string.IsNullOrEmpty(newName));

            cancellationToken.ThrowIfCancellationRequested();

            var renameLocations = await FindRenameLocationsAsync(solution, symbol, optionSet, cancellationToken).ConfigureAwait(false);
            return await renameLocations.ResolveConflictsAsync(newName, nonConflictSymbols, cancellationToken).ConfigureAwait(false);
        }
    }
}
