// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Rename.ConflictEngine;
using System.Linq;

namespace Microsoft.CodeAnalysis.Rename
{
    public static partial class Renamer
    {
        public static Task<Solution> RenameSymbolAsync(
            Solution solution, ISymbol symbol, string newName, OptionSet optionSet, CancellationToken cancellationToken = default)
        {
            return RenameSymbolAsync(
                solution,
                SymbolAndProjectId.Create(symbol, projectId: null),
                newName, optionSet, cancellationToken);
        }

        /// <summary>
        /// Similar to calling <see cref="Document.WithName(string)" /> with additional changes to the solution. 
        /// Each change is added as a <see cref="RenameDocumentAction"/> in the returned <see cref="RenameDocumentActionSet.ApplicableActions" />.
        /// 
        /// Each action may individually encounter errors that prevent it from behaving correctly. Those are reported in <see cref="RenameDocumentAction.GetErrors(System.Globalization.CultureInfo?)"/>.
        /// 
        /// Current supported actions that may be returned: 
        /// <list>
        ///  <item>Rename symbol action that will rename the type to match the document name.</item>
        /// </list>
        /// 
        /// </summary>
        public static async Task<RenameDocumentActionSet> RenameDocumentNameAsync(
            Document document,
            string newDocumentName,
            OptionSet optionSet,
            CancellationToken cancellationToken = default)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (newDocumentName == null)
            {
                throw new ArgumentNullException(nameof(newDocumentName));
            }

            using var _ = ArrayBuilder<RenameDocumentAction>.GetInstance(out var actions);

            if (!newDocumentName.Equals(document.Name))
            {
                var renameAction = await RenameSymbolDocumentAction.TryCreateAsync(document, newDocumentName, cancellationToken).ConfigureAwait(false);

                actions.AddIfNotNull(renameAction);
            }

            return new RenameDocumentActionSet(
                actions.ToImmutable(),
                document.Id,
                newDocumentName,
                document.Folders,
                optionSet);
        }

        /// <summary>
        /// Similar to calling <see cref="Document.WithFolders(IEnumerable{string})" /> with additional changes to the solution. 
        /// Each change is added as a <see cref="RenameDocumentAction"/> in the returned <see cref="RenameDocumentActionSet.ApplicableActions" />.
        /// 
        /// Each action may individually encounter errors that prevent it from behaving correctly. Those are reported in <see cref="RenameDocumentAction.GetErrors(System.Globalization.CultureInfo?)"/>.
        /// 
        /// Current supported actions that may be returned: 
        /// <list>
        ///  <item>Sync namespace action that will sync the namespace(s) of the document to match the document folders. </item>
        /// </list>
        /// 
        /// </summary>
        public static Task<RenameDocumentActionSet> RenameDocumentFoldersAsync(
            Document document,
            IReadOnlyList<string> newFolders,
            OptionSet optionSet,
            CancellationToken cancellationToken = default)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            if (newFolders == null)
            {
                throw new ArgumentNullException(nameof(newFolders));
            }

            using var _ = ArrayBuilder<RenameDocumentAction>.GetInstance(out var actions);

            if (!newFolders.SequenceEqual(document.Folders))
            {
                var action = SyncNamespaceDocumentAction.TryCreate(document, newFolders, cancellationToken);

                actions.AddIfNotNull(action);
            }

            // Analysis is not currently async, but may need to be in the future as we do more work to determine the
            // correct namespace. Keep the public API async even if it's not required for now
            return Task.FromResult(new RenameDocumentActionSet(
                actions.ToImmutable(),
                document.Id,
                document.Name,
                newFolders,
                optionSet));
        }

        internal static Task<Solution> RenameSymbolAsync(
            Solution solution, SymbolAndProjectId symbolAndProjectId, string newName, OptionSet optionSet, CancellationToken cancellationToken = default)
        {
            return RenameSymbolAsync(solution, symbolAndProjectId, newName, optionSet, nonConflictSymbols: null, cancellationToken);
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
            ImmutableHashSet<ISymbol> nonConflictSymbols = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(newName))
                throw new ArgumentException(nameof(newName));

            cancellationToken.ThrowIfCancellationRequested();

            var conflictResolution = await ConflictResolver.ResolveConflictsAsync(
                locations, newName, nonConflictSymbols, cancellationToken).ConfigureAwait(false);

            return conflictResolution.NewSolution;
        }

        internal static async Task<Solution> RenameSymbolAsync(
            Solution solution,
            SymbolAndProjectId symbolAndProjectId,
            string newName,
            OptionSet options,
            ImmutableHashSet<ISymbol> nonConflictSymbols = null,
            CancellationToken cancellationToken = default)
        {
            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            if (symbolAndProjectId.Symbol == null)
                throw new ArgumentNullException(nameof(symbolAndProjectId));

            cancellationToken.ThrowIfCancellationRequested();

            options ??= solution.Workspace.Options;
            var renameLocations = await GetRenameLocationsAsync(solution, symbolAndProjectId, options, cancellationToken).ConfigureAwait(false);
            return await RenameAsync(renameLocations, newName, nonConflictSymbols, cancellationToken).ConfigureAwait(false);
        }
    }
}
