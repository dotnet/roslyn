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

        internal static async Task<Solution> RenameSymbolAsync(
            Solution solution,
            ISymbol symbol,
            string newName,
            OptionSet optionSet,
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

            if (string.IsNullOrEmpty(newName))
            {
                throw new ArgumentException("newName");
            }

            cancellationToken.ThrowIfCancellationRequested();

            optionSet = optionSet ?? solution.Workspace.Options;
            var renameLocationSet = await RenameLocationSet.FindAsync(symbol, solution, optionSet, cancellationToken).ConfigureAwait(false);
            if (filter != null)
            {
                renameLocationSet = new RenameLocationSet(
                    renameLocationSet.Locations.Where(loc => filter(loc.Location)).ToSet(), 
                    renameLocationSet.Symbol, renameLocationSet.Solution,
                    renameLocationSet.ReferencedSymbols, renameLocationSet.ImplicitLocations);
            }

            var conflictResolution = await ConflictResolver.ResolveConflictsAsync(
                renameLocationSet, symbol.Name, newName, optionSet, hasConflict, cancellationToken).ConfigureAwait(false);

            return conflictResolution.NewSolution;
        }
    }
}
