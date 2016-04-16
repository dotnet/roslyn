// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Rename.ConflictEngine;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename
{
    public static class Renamer
    {
        public static Task<Solution> RenameSymbolAsync(Solution solution, ISymbol symbol, string newName, OptionSet optionSet, CancellationToken cancellationToken = default(CancellationToken))
        {
            return RenameSymbolAsync(solution, symbol, newName, optionSet, filter: null, cancellationToken: cancellationToken);
        }

        internal static Task<RenameLocations> GetRenameLocationsAsync(Solution solution, ISymbol symbol, OptionSet options, CancellationToken cancellationToken)
        {
            if (solution == null)
            {
                throw new ArgumentNullException(nameof(solution));
            }

            if (symbol == null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            cancellationToken.ThrowIfCancellationRequested();

            options = options ?? solution.Workspace.Options;
            return RenameLocations.FindAsync(symbol, solution, options, cancellationToken);
        }

        internal static async Task<Solution> RenameAsync(
            RenameLocations locations,
            string newName,
            Func<Location, bool> filter = null,
            Func<IEnumerable<ISymbol>, bool?> hasConflict = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(newName))
            {
                throw new ArgumentException(nameof(newName));
            }

            cancellationToken.ThrowIfCancellationRequested();

            var symbol = locations.Symbol;
            if (filter != null)
            {
                locations = new RenameLocations(
                    locations.Locations.Where(loc => filter(loc.Location)).ToSet(),
                    symbol, locations.Solution,
                    locations.ReferencedSymbols, locations.ImplicitLocations,
                    locations.Options);
            }

            var conflictResolution = await ConflictResolver.ResolveConflictsAsync(
                locations, symbol.Name, newName, locations.Options, hasConflict, cancellationToken).ConfigureAwait(false);

            return conflictResolution.NewSolution;
        }

        internal static async Task<Solution> RenameSymbolAsync(
            Solution solution,
            ISymbol symbol,
            string newName,
            OptionSet options,
            Func<Location, bool> filter,
            Func<IEnumerable<ISymbol>, bool?> hasConflict = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (solution == null)
            {
                throw new ArgumentNullException(nameof(solution));
            }

            if (symbol == null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            cancellationToken.ThrowIfCancellationRequested();

            options = options ?? solution.Workspace.Options;
            var renameLocations = await GetRenameLocationsAsync(solution, symbol, options, cancellationToken).ConfigureAwait(false);
            return await RenameAsync(renameLocations, newName, filter, hasConflict, cancellationToken).ConfigureAwait(false);
        }
    }
}