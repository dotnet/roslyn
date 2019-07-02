// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Rename.ConflictEngine;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename
{
    public static class Renamer
    {
        public static Task<Solution> RenameSymbolAsync(
            Solution solution, ISymbol symbol, string newName, OptionSet optionSet, CancellationToken cancellationToken = default)
        {
            return RenameSymbolAsync(
                solution,
                SymbolAndProjectId.Create(symbol, projectId: null),
                newName, optionSet, cancellationToken);
        }

        internal static Task<Solution> RenameSymbolAsync(
            Solution solution, SymbolAndProjectId symbolAndProjectId, string newName, OptionSet optionSet, CancellationToken cancellationToken = default)
        {
            return RenameSymbolAsync(solution, symbolAndProjectId, newName, optionSet, filter: null, cancellationToken: cancellationToken);
        }

        internal static Task<RenameLocations> GetRenameLocationsAsync(
            Solution solution, SymbolAndProjectId symbolAndProjectId, OptionSet options, CancellationToken cancellationToken)
        {
            if (solution == null)
            {
                throw new ArgumentNullException(nameof(solution));
            }

            if (symbolAndProjectId.Symbol == null)
            {
                throw new ArgumentNullException(nameof(symbolAndProjectId));
            }

            cancellationToken.ThrowIfCancellationRequested();

            options ??= solution.Options;
            return RenameLocations.FindAsync(
                symbolAndProjectId, solution, options, cancellationToken);
        }

        internal static async Task<Solution> RenameAsync(
            RenameLocations locations,
            string newName,
            Func<Location, bool> filter = null,
            Func<IEnumerable<ISymbol>, bool?> hasConflict = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(newName))
            {
                throw new ArgumentException(nameof(newName));
            }

            cancellationToken.ThrowIfCancellationRequested();

            var symbolAndProjectId = locations.SymbolAndProjectId;
            if (filter != null)
            {
                locations = new RenameLocations(
                    locations.Locations.Where(loc => filter(loc.Location)).ToSet(),
                    symbolAndProjectId, locations.Solution,
                    locations.ReferencedSymbols, locations.ImplicitLocations.Where(loc => filter(loc.Location)),
                    locations.Options);
            }

            var conflictResolution = await ConflictResolver.ResolveConflictsAsync(
                locations, symbolAndProjectId.Symbol.Name, newName, locations.Options, hasConflict, cancellationToken).ConfigureAwait(false);

            return conflictResolution.NewSolution;
        }

        internal static async Task<Solution> RenameSymbolAsync(
            Solution solution,
            SymbolAndProjectId symbolAndProjectId,
            string newName,
            OptionSet options,
            Func<Location, bool> filter,
            Func<IEnumerable<ISymbol>, bool?> hasConflict = null,
            CancellationToken cancellationToken = default)
        {
            if (solution == null)
            {
                throw new ArgumentNullException(nameof(solution));
            }

            if (symbolAndProjectId.Symbol == null)
            {
                throw new ArgumentNullException(nameof(symbolAndProjectId));
            }

            cancellationToken.ThrowIfCancellationRequested();

            options ??= solution.Workspace.Options;
            var renameLocations = await GetRenameLocationsAsync(solution, symbolAndProjectId, options, cancellationToken).ConfigureAwait(false);
            return await RenameAsync(renameLocations, newName, filter, hasConflict, cancellationToken).ConfigureAwait(false);
        }
    }
}
