// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Rename.ConflictEngine;

namespace Microsoft.CodeAnalysis.Rename
{
    public static class Renamer
    {
        /// <summary>
        /// Renames the provided <paramref name="symbol"/> to the name <paramref name="newName"/>. This method is less
        /// efficient than <see cref="RenameSymbolAsync(Project, ISymbol, string, OptionSet, CancellationToken)"/> and
        /// should be avoided.
        /// </summary>
        [Obsolete("Use the overload of RenameSymbolAsync that takes a Project", error: false)]
        public static Task<Solution> RenameSymbolAsync(
            Solution solution, ISymbol symbol, string newName, OptionSet optionSet, CancellationToken cancellationToken = default)
        {
            return RenameSymbolAsync(
                solution,
                SymbolAndProjectId.Create(symbol, projectId: null),
                newName, optionSet, cancellationToken);
        }

        /// <summary>
        /// Renames the provided <paramref name="symbol"/> to the name <paramref name="newName"/>. <paramref
        /// name="symbol"/> must either be a source symbol from <paramref name="project"/> or a metadata symbol from one
        /// of <paramref name="project"/>'s <see cref="Project.MetadataReferences"/>.
        /// </summary>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
        public static Task<Solution> RenameSymbolAsync(
            Project project, ISymbol symbol, string newName, OptionSet optionSet, CancellationToken cancellationToken = default)
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
        {
            return RenameSymbolAsync(
                project.Solution,
                SymbolAndProjectId.Create(symbol, project.Id),
                newName, optionSet, cancellationToken);
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
