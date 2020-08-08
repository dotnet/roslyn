// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename
{
    public static partial class Renamer
    {
        public static async Task<Solution> RenameSymbolAsync(
            Solution solution, ISymbol symbol, string newName, OptionSet optionSet, CancellationToken cancellationToken = default)
        {
            if (solution == null)
                throw new ArgumentNullException(nameof(solution));

            if (symbol == null)
                throw new ArgumentNullException(nameof(symbol));

            if (string.IsNullOrEmpty(newName))
                throw new ArgumentException(nameof(newName));

            var resolution = await RenameSymbolAsync(
                solution, symbol, newName,
                RenameOptionSet.From(solution, optionSet),
                nonConflictSymbols: null, cancellationToken).ConfigureAwait(false);

            // This is a public entry-point.  So if rename failed to resolve conflicts, we report that back to caller as
            // an exception.
            if (resolution.ErrorMessage != null)
                throw new ArgumentException(resolution.ErrorMessage);

            return resolution.NewSolution;
        }

        /// <summary>
        /// Call to perform a rename of document or change in document folders. Returns additional code changes related to the document
        /// being modified, such as renaming symbols in the file. 
        ///
        /// Each change is added as a <see cref="RenameDocumentAction"/> in the returned <see cref="RenameDocumentActionSet.ApplicableActions" />.
        /// 
        /// Each action may individually encounter errors that prevent it from behaving correctly. Those are reported in <see cref="RenameDocumentAction.GetErrors(System.Globalization.CultureInfo?)"/>.
        /// 
        /// <para />
        /// 
        /// Current supported actions that may be returned: 
        /// <list>
        ///  <item>Rename symbol action that will rename the type to match the document name.</item>
        ///  <item>Sync namespace action that will sync the namespace(s) of the document to match the document folders. </item>
        /// </list>
        /// 
        /// </summary>
        /// <param name="document">The document to be modified</param>
        /// <param name="newDocumentName">The new name for the document. Pass null or the same name to keep unchanged.</param>
        /// <param name="newDocumentFolders">The new set of folders for the <see cref="TextDocument.Folders"/> property</param>
        public static async Task<RenameDocumentActionSet> RenameDocumentAsync(
            Document document,
            string newDocumentName,
            IReadOnlyList<string>? newDocumentFolders = null,
            OptionSet? optionSet = null,
            CancellationToken cancellationToken = default)
        {
            if (document is null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            using var _ = ArrayBuilder<RenameDocumentAction>.GetInstance(out var actions);

            if (newDocumentName != null && !newDocumentName.Equals(document.Name))
            {
                var renameAction = await RenameSymbolDocumentAction.TryCreateAsync(document, newDocumentName, cancellationToken).ConfigureAwait(false);
                actions.AddIfNotNull(renameAction);
            }

            if (newDocumentFolders != null && !newDocumentFolders.SequenceEqual(document.Folders))
            {
                var action = SyncNamespaceDocumentAction.TryCreate(document, newDocumentFolders, cancellationToken);
                actions.AddIfNotNull(action);
            }

            newDocumentName ??= document.Name;
            newDocumentFolders ??= document.Folders;
            optionSet ??= document.Project.Solution.Options;

            return new RenameDocumentActionSet(
                actions.ToImmutable(),
                document.Id,
                newDocumentName,
                newDocumentFolders.ToImmutableArray(),
                optionSet);
        }

        internal static Task<RenameLocations> FindRenameLocationsAsync(Solution solution, ISymbol symbol, RenameOptionSet optionSet, CancellationToken cancellationToken)
            => RenameLocations.FindLocationsAsync(symbol, solution, optionSet, cancellationToken);

        internal static async Task<ConflictResolution> RenameSymbolAsync(
            Solution solution,
            ISymbol symbol,
            string newName,
            RenameOptionSet optionSet,
            ImmutableHashSet<ISymbol>? nonConflictSymbols,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(solution);
            Contract.ThrowIfNull(symbol);
            Contract.ThrowIfTrue(string.IsNullOrEmpty(newName));

            cancellationToken.ThrowIfCancellationRequested();

            using (Logger.LogBlock(FunctionId.Renamer_RenameSymbolAsync, cancellationToken))
            {
                if (SerializableSymbolAndProjectId.TryCreate(symbol, solution, cancellationToken, out var serializedSymbol))
                {
                    var client = await RemoteHostClient.TryGetClientAsync(solution.Workspace, cancellationToken).ConfigureAwait(false);
                    if (client != null)
                    {
                        var result = await client.RunRemoteAsync<SerializableConflictResolution?>(
                            WellKnownServiceHubService.CodeAnalysis,
                            nameof(IRemoteRenamer.RenameSymbolAsync),
                            solution,
                            new object?[]
                            {
                                serializedSymbol,
                                newName,
                                SerializableRenameOptionSet.Dehydrate(optionSet),
                                nonConflictSymbols?.Select(s => SerializableSymbolAndProjectId.Dehydrate(solution, s, cancellationToken)).ToArray(),
                            },
                            callbackTarget: null,
                            cancellationToken).ConfigureAwait(false);

                        if (result != null)
                            return await result.RehydrateAsync(solution, cancellationToken).ConfigureAwait(false);
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
            ImmutableHashSet<ISymbol>? nonConflictSymbols,
            CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(solution);
            Contract.ThrowIfNull(symbol);
            Contract.ThrowIfTrue(string.IsNullOrEmpty(newName));

            cancellationToken.ThrowIfCancellationRequested();

            var renameLocations = await FindRenameLocationsAsync(solution, symbol, optionSet, cancellationToken).ConfigureAwait(false);
            return await renameLocations.ResolveConflictsAsync(newName, nonConflictSymbols, cancellationToken).ConfigureAwait(false);
        }
    }
}
