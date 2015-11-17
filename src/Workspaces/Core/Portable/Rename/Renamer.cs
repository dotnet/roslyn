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
            return RenameSymbolAsync(solution, symbol, newName, optionSet, callbacks: null, cancellationToken: cancellationToken);
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
            RenameCallbacks callbacks,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrEmpty(newName))
            {
                throw new ArgumentException(nameof(newName));
            }

            cancellationToken.ThrowIfCancellationRequested();

            var symbol = locations.Symbol;
            if (callbacks?.Filter != null)
            {
                locations = new RenameLocations(
                    locations.Locations.Where(loc => callbacks.Filter(loc.Location)).ToSet(),
                    symbol, locations.Solution,
                    locations.ReferencedSymbols, locations.ImplicitLocations,
                    locations.Options);
            }

            var conflictResolution = await ConflictResolver.ResolveConflictsAsync(
                locations, symbol.Name, newName, locations.Options, callbacks, cancellationToken).ConfigureAwait(false);

            return conflictResolution.NewSolution;
        }

        internal static async Task<Solution> RenameSymbolAsync(
            Solution solution,
            ISymbol symbol,
            string newName,
            OptionSet options,
            RenameCallbacks callbacks,
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
            return await RenameAsync(renameLocations, newName, callbacks, cancellationToken).ConfigureAwait(false);
        }
    }

    internal class RenameCallbacks
    {
        public readonly Func<Location, bool> Filter;
        public readonly Func<IEnumerable<ISymbol>, bool?> HasConflict;
        public readonly Func<Document, SyntaxToken, SyntaxToken, SyntaxToken> OnTokenRenamed;

        /// <param name="filter">Called on rename locations to determine if they should be renamed or not.</param>
        /// <param name="hasConflict">Called after renaming references.  Can be used by callers to 
        /// indicate if the new symbols that the reference binds to should be considered to be ok or
        /// are in conflict.  'true' means they are conflicts.  'false' means they are not conflicts.
        /// 'null' means that the default conflict check should be used.</param>
        /// <param name="onTokenRenamed">Called after a token is actually renamed.  Can be used by callers to 
        /// Further manipulate the result (for example, by adding additional annotations to the new token).</param>
        public RenameCallbacks(
            Func<Location, bool> filter = null,
            Func<IEnumerable<ISymbol>, bool?> hasConflict = null,
            Func<Document, SyntaxToken, SyntaxToken, SyntaxToken> onTokenRenamed = null)
        {
            Filter = filter;
            HasConflict = hasConflict;
            OnTokenRenamed = onTokenRenamed;
        }
    }
}