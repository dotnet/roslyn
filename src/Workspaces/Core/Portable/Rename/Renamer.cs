// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename
{
    public static class Renamer
    {
        public static Task<Solution> RenameSymbolAsync(
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

            optionSet ??= solution.Options;

            return RenameSymbolAsync(solution, symbol, newName, optionSet, nonConflictSymbols: null, cancellationToken);
        }

        internal static Task<RenameLocations> FindRenameLocationsAsync(
            Solution solution, ISymbol symbol, OptionSet optionSet, CancellationToken cancellationToken)
        {
            return RenameLocations.FindLocationsAsync(
                symbol, solution, RenameOptionSet.From(solution, optionSet), cancellationToken);
        }

        internal static async Task<Solution> RenameAsync(
            RenameLocations locations,
            string newName,
            ImmutableHashSet<ISymbol> nonConflictSymbols = null,
            CancellationToken cancellationToken = default)
        {
            Contract.ThrowIfTrue(string.IsNullOrEmpty(newName));

            cancellationToken.ThrowIfCancellationRequested();

            var conflictResolution = await locations.ResolveConflictsAsync(
                newName, nonConflictSymbols, cancellationToken).ConfigureAwait(false);

            return conflictResolution.NewSolution;
        }

        internal static async Task<Solution> RenameSymbolAsync(
            Solution solution,
            ISymbol symbol,
            string newName,
            OptionSet optionSet,
            ImmutableHashSet<ISymbol> nonConflictSymbols,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(solution);
            Contract.ThrowIfNull(symbol);
            Contract.ThrowIfNull(solution.GetOriginatingProjectId(symbol), WorkspacesResources.Symbols_project_could_not_be_found_in_the_provided_solution);
            Contract.ThrowIfTrue(string.IsNullOrEmpty(newName));

            cancellationToken.ThrowIfCancellationRequested();

            var renameLocations = await FindRenameLocationsAsync(solution, symbol, optionSet, cancellationToken).ConfigureAwait(false);
            return await RenameAsync(renameLocations, newName, nonConflictSymbols, cancellationToken).ConfigureAwait(false);
        }
    }
}
