// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Humanizer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Rename.ConflictEngine;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename
{
    /// <summary>
    /// Equivalent to <see cref="SymbolicRenameLocations"/> except that references to symbols are kept in a lightweight fashion
    /// to avoid expensive rehydration steps as a host and OOP communicate.
    /// </summary>
    internal sealed partial class LightweightRenameLocations : IDisposable
    {
        // Long lasting connection to oop process if we have one.
        public readonly ConnectionScope<IRemoteRenamerService>? RemoteConnectionScope;

        public readonly Solution Solution;
        public readonly SymbolRenameOptions Options;
        public readonly CodeCleanupOptionsProvider FallbackOptions;

        public readonly ImmutableHashSet<RenameLocation> Locations;

        // Note: these two are just stored as arrays so that we can pass them back/forth with a
        // SerializableRenameLocations object without having to do any work.  As they are never mutated in this type and
        // are private, this is safe and cheap.

        private readonly SerializableReferenceLocation[]? _implicitLocations;
        private readonly SerializableSymbolAndProjectId[]? _referencedSymbols;

        private LightweightRenameLocations(
            ConnectionScope<IRemoteRenamerService>? remoteConnectionScope,
            Solution solution,
            SymbolRenameOptions options,
            CodeCleanupOptionsProvider fallbackOptions,
            ImmutableHashSet<RenameLocation> locations,
            SerializableReferenceLocation[]? implicitLocations,
            SerializableSymbolAndProjectId[]? referencedSymbols)
        {
            RemoteConnectionScope = remoteConnectionScope;
            Solution = solution;
            Options = options;
            FallbackOptions = fallbackOptions;
            Contract.ThrowIfNull(locations);
            Locations = locations;
            _implicitLocations = implicitLocations;
            _referencedSymbols = referencedSymbols;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            RemoteConnectionScope?.Dispose();
        }

        ~LightweightRenameLocations()
        {

            if (!Environment.HasShutdownStarted)
            {
                Contract.Fail($@"LightweightRenameLocations should have been disposed!");
            }
        }

        public async Task<SymbolicRenameLocations?> ToHeavyweightAsync(ISymbol symbol, CancellationToken cancellationToken)
        {
            var rehydrated = await SerializableRenameLocations.TryRehydrateAsync(
                this.Solution, _implicitLocations, _referencedSymbols, cancellationToken).ConfigureAwait(false);
            if (rehydrated == null)
                return null;

            return new SymbolicRenameLocations(
                symbol,
                Solution,
                Options,
                FallbackOptions,
                Locations,
                rehydrated.Value.implicitLocations,
                rehydrated.Value.referencedSymbols);
        }

        /// <summary>
        /// Find the locations that need to be renamed.  Can cross process boundaries efficiently to do this.
        /// </summary>
        public static async Task<LightweightRenameLocations> FindRenameLocations_MustDisposeAsync(
            ISymbol symbol, Solution solution, SymbolRenameOptions options, CodeCleanupOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(solution);
            Contract.ThrowIfNull(symbol);

            cancellationToken.ThrowIfCancellationRequested();

            var renameScope = await CreateRenameScopeAsync(solution, fallbackOptions, cancellationToken).ConfigureAwait(false);
            var forwardedScopeOwnership = false;
            try
            {
                using (Logger.LogBlock(FunctionId.Renamer_FindRenameLocationsAsync, cancellationToken))
                {
                    if (SerializableSymbolAndProjectId.TryCreate(symbol, solution, cancellationToken, out var serializedSymbol))
                    {
                        if (renameScope != null)
                        {
                            var result = await renameScope.TryInvokeAsync<SerializableRenameLocations?>(
                                (service, solutionInfo, callbackId, cancellationToken) => service.FindRenameLocationsAsync(solutionInfo, callbackId, serializedSymbol, options, cancellationToken),
                                cancellationToken).ConfigureAwait(false);

                            if (result.HasValue && result.Value != null)
                            {
                                var rehydrated = await TryRehydrateAsync(
                                    renameScope, solution, fallbackOptions, result.Value, cancellationToken).ConfigureAwait(false);

                                if (rehydrated != null)
                                {
                                    forwardedScopeOwnership = true;
                                    return rehydrated;
                                }
                            }

                            // TODO: do not fall back to in-proc if client is available (https://github.com/dotnet/roslyn/issues/47557)
                        }
                    }
                }

                var renameLocations = await SymbolicRenameLocations.FindLocationsInCurrentProcessAsync(
                    symbol, solution, options, fallbackOptions, cancellationToken).ConfigureAwait(false);

                // Passing the scope to LightweightRenameLocations. It is responsible now for disposing of it.
                forwardedScopeOwnership = true;
                return new LightweightRenameLocations(
                    renameScope, solution, options, fallbackOptions, renameLocations.Locations,
                    renameLocations.ImplicitLocations.IsDefault ? null : renameLocations.ImplicitLocations.Select(loc => SerializableReferenceLocation.Dehydrate(loc, cancellationToken)).ToArray(),
                    renameLocations.ReferencedSymbols.IsDefault ? null : renameLocations.ReferencedSymbols.Select(sym => SerializableSymbolAndProjectId.Dehydrate(solution, sym, cancellationToken)).ToArray());
            }
            finally
            {
                if (!forwardedScopeOwnership)
                    renameScope?.Dispose();
            }
        }


        /// <summary>
        /// Performs the renaming of the symbol in the solution, identifies renaming conflicts and automatically
        /// resolves them where possible.
        /// </summary>
        /// <param name="replacementText">The new name of the identifier</param>
        /// <param name="nonConflictSymbolKeys">Used after renaming references. References that now bind to any of these
        /// symbols are not considered to be in conflict. Useful for features that want to rename existing references to
        /// point at some existing symbol. Normally this would be a conflict, but this can be used to override that
        /// behavior.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A conflict resolution containing the new solution.</returns>
        internal static async Task<ConflictResolution> ResolveLightweightConflictsAsync(
            ISymbol symbol,
            LightweightRenameLocations lightweightRenameLocations,
            string replacementText,
            ImmutableArray<SymbolKey> nonConflictSymbolKeys,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using (Logger.LogBlock(FunctionId.Renamer_ResolveConflictsAsync, cancellationToken))
            {
                var solution = lightweightRenameLocations.Solution;
                var client = await RemoteHostClient.TryGetClientAsync(solution.Workspace, cancellationToken).ConfigureAwait(false);
                if (client != null)
                {
                    var serializableSymbol = SerializableSymbolAndProjectId.Dehydrate(lightweightRenameLocations.Solution, symbol, cancellationToken);
                    var serializableLocationSet = lightweightRenameLocations.Dehydrate();

                    var result = await client.TryInvokeAsync<IRemoteRenamerService, SerializableConflictResolution?>(
                        solution,
                        (service, solutionInfo, callbackId, cancellationToken) => service.ResolveConflictsAsync(solutionInfo, callbackId, serializableSymbol, serializableLocationSet, replacementText, nonConflictSymbolKeys, cancellationToken),
                        callbackTarget: new RemoteOptionsProvider<CodeCleanupOptions>(solution.Workspace.Services, lightweightRenameLocations.FallbackOptions),
                        cancellationToken).ConfigureAwait(false);

                    if (result.HasValue && result.Value != null)
                        return await result.Value.RehydrateAsync(solution, cancellationToken).ConfigureAwait(false);

                    // TODO: do not fall back to in-proc if client is available (https://github.com/dotnet/roslyn/issues/47557)
                }
            }

            var heavyweightLocations = await lightweightRenameLocations.ToHeavyweightAsync(symbol, cancellationToken).ConfigureAwait(false);
            if (heavyweightLocations is null)
                return new ConflictResolution(WorkspacesResources.Failed_to_resolve_rename_conflicts);

            return await ConflictResolver.ResolveHeavyweightConflictsInCurrentProcessAsync(
                heavyweightLocations, replacementText, nonConflictSymbolKeys, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<ConnectionScope<IRemoteRenamerService>?> CreateRenameScopeAsync(
            Solution solution,
            CodeCleanupOptionsProvider fallbackOptions,
            CancellationToken cancellationToken)
        {
            var client = await RemoteHostClient.TryGetClientAsync(solution.Workspace, cancellationToken).ConfigureAwait(false);
            if (client == null)
                return null;

            var callbackTarget = new RemoteOptionsProvider<CodeCleanupOptions>(solution.Workspace.Services, fallbackOptions);
            return await client.CreateConnectionScopeAsync<IRemoteRenamerService>(
                solution, callbackTarget, cancellationToken).ConfigureAwait(false);
        }

        public LightweightRenameLocations Filter(Func<DocumentId, TextSpan, bool> filter)
            => new(
                this.RemoteConnectionScope,
                this.Solution,
                this.Options,
                this.FallbackOptions,
                this.Locations.Where(loc => filter(loc.DocumentId, loc.Location.SourceSpan)).ToImmutableHashSet(),
                _implicitLocations?.Where(loc => filter(loc.Document, loc.Location)).ToArray(),
                _referencedSymbols);

        /// <summary>
        /// Performs the renaming of the symbol in the solution, identifies renaming conflicts and automatically
        /// resolves them where possible.
        /// </summary>
        /// <param name="replacementText">The new name of the identifier</param>
        /// <param name="nonConflictSymbolKeys">Used after renaming references. References that now bind to any of these
        /// symbols are not considered to be in conflict. Useful for features that want to rename existing references to
        /// point at some existing symbol. Normally this would be a conflict, but this can be used to override that
        /// behavior.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A conflict resolution containing the new solution.</returns>
        internal async Task<ConflictResolution> ResolveConflictsAsync(
            ISymbol symbol,
            string replacementText,
            ImmutableArray<SymbolKey> nonConflictSymbolKeys,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using (Logger.LogBlock(FunctionId.Renamer_ResolveConflictsAsync, cancellationToken))
            {
                // If we made a remote connection, then piggy back off of that.
                if (this.RemoteConnectionScope != null)
                {
                    var serializableSymbol = SerializableSymbolAndProjectId.Dehydrate(this.Solution, symbol, cancellationToken);
                    var serializableLocationSet = this.Dehydrate();

                    var result = await this.RemoteConnectionScope.TryInvokeAsync<SerializableConflictResolution?>(
                        (service, solutionInfo, callbackId, cancellationToken) => service.ResolveConflictsAsync(solutionInfo, callbackId, serializableSymbol, serializableLocationSet, replacementText, nonConflictSymbolKeys, cancellationToken),
                        cancellationToken).ConfigureAwait(false);

                    if (result.HasValue && result.Value != null)
                        return await result.Value.RehydrateAsync(this.Solution, cancellationToken).ConfigureAwait(false);

                    // TODO: do not fall back to in-proc if client is available (https://github.com/dotnet/roslyn/issues/47557)
                }
            }

            // Otherwise, fallback to inproc.
            var heavyweightLocations = await this.ToHeavyweightAsync(symbol, cancellationToken).ConfigureAwait(false);
            if (heavyweightLocations is null)
                return new ConflictResolution(WorkspacesResources.Failed_to_resolve_rename_conflicts);

            return await ConflictResolver.ResolveHeavyweightConflictsInCurrentProcessAsync(
                heavyweightLocations, replacementText, nonConflictSymbolKeys, cancellationToken).ConfigureAwait(false);
        }
    }
}
