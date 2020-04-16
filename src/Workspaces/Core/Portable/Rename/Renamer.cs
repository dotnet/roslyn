// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Rename.ConflictEngine;
using Roslyn.Utilities;

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
        /// * <see cref="RenameSymbolDocumentAction"/> that will rename the type to match the document name
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

                if (renameAction is object)
                {
                    actions.Add(renameAction);
                }
            }

            return new RenameDocumentActionSet(
                actions.ToImmutable(),
                document.Project.Id,
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
        /// * <see cref="SyncNamespaceDocumentAction"/> that will sync the namespace(s) of the document to match the document folders
        /// </summary>
        public static async Task<RenameDocumentActionSet> RenameDocumentFoldersAsync(
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
                var action = await SyncNamespaceDocumentAction.TryCreateAsync(document, newFolders, cancellationToken).ConfigureAwait(false);

                if (action is object)
                {
                    actions.Add(action);
                }
            }

            return new RenameDocumentActionSet(
                actions.ToImmutable(),
                document.Project.Id,
                document.Id,
                document.Name,
                newFolders,
                optionSet);
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
