// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Collections.Generic;
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
<<<<<<< HEAD
                var renameAction = await RenameSymbolDocumentAction.TryCreateAsync(document, newDocumentName, cancellationToken).ConfigureAwait(false);
=======
                var renameAction = await RenameSymbolDocumentAction.TryCreateAsync(document, newDocumentName, optionSet, cancellationToken).ConfigureAwait(false);
>>>>>>> d7785e81292987663a30efef90f6d988cd9bce2c

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
<<<<<<< HEAD
                document.Folders,
                optionSet);
=======
                document.Folders);
>>>>>>> d7785e81292987663a30efef90f6d988cd9bce2c
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
<<<<<<< HEAD
                var action = await SyncNamespaceDocumentAction.TryCreateAsync(document, newFolders, cancellationToken).ConfigureAwait(false);
=======
                var action = await SyncNamespaceDocumentAction.TryCreateAsync(document, newFolders, optionSet, cancellationToken).ConfigureAwait(false);
>>>>>>> d7785e81292987663a30efef90f6d988cd9bce2c

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
<<<<<<< HEAD
                newFolders,
                optionSet);
=======
                newFolders);
>>>>>>> d7785e81292987663a30efef90f6d988cd9bce2c
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
