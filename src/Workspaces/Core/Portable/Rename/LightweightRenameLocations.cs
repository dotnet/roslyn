// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        public readonly Solution Solution;
        public readonly SymbolRenameOptions Options;
        public readonly CodeCleanupOptionsProvider FallbackOptions;

        public readonly ImmutableHashSet<RenameLocation> Locations;
        private readonly ImmutableArray<SerializableReferenceLocation> _implicitLocations;
        private readonly ImmutableArray<SerializableSymbolAndProjectId> _referencedSymbols;

        /// <summary>
        /// Cancellation controlling a keep-alive communication channel we have with the OOP service. We do this to ensure 
        /// that for the entirety of the inline-rename session 
        /// </summary>
        private readonly CancellationTokenSource _remoteHostKeepAliveTokenSource = new();

        private LightweightRenameLocations(
            Solution solution,
            SymbolRenameOptions options,
            CodeCleanupOptionsProvider fallbackOptions,
            ImmutableHashSet<RenameLocation> locations,
            ImmutableArray<SerializableReferenceLocation> implicitLocations,
            ImmutableArray<SerializableSymbolAndProjectId> referencedSymbols)
        {
            Contract.ThrowIfNull(locations);
            Contract.ThrowIfTrue(implicitLocations.IsDefault);
            Contract.ThrowIfTrue(referencedSymbols.IsDefault);
            Solution = solution;
            Options = options;
            FallbackOptions = fallbackOptions;
            Locations = locations;
            _implicitLocations = implicitLocations;
            _referencedSymbols = referencedSymbols;
        }

        public async Task<SymbolicRenameLocations?> ToSymbolicLocationsAsync(ISymbol symbol, CancellationToken cancellationToken)
        {
            var referencedSymbols = await _referencedSymbols.SelectAsArrayAsync(sym => sym.TryRehydrateAsync(Solution, cancellationToken)).ConfigureAwait(false);

            if (referencedSymbols.Any(s => s is null))
                return null;

            var implicitLocations = await _implicitLocations.SelectAsArrayAsync(loc => loc.RehydrateAsync(Solution, cancellationToken)).ConfigureAwait(false);

            return new SymbolicRenameLocations(
                symbol,
                Solution,
                Options,
                FallbackOptions,
                Locations,
                implicitLocations,
                referencedSymbols);
        }

        /// <summary>
        /// Find the locations that need to be renamed.  Can cross process boundaries efficiently to do this.
        /// </summary>
        public static async Task<LightweightRenameLocations> FindRenameLocationsAsync(
            ISymbol symbol, Solution solution, SymbolRenameOptions options, CodeCleanupOptionsProvider fallbackOptions, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(solution);
            Contract.ThrowIfNull(symbol);

            cancellationToken.ThrowIfCancellationRequested();

            using (Logger.LogBlock(FunctionId.Renamer_FindRenameLocationsAsync, cancellationToken))
            {
                if (SerializableSymbolAndProjectId.TryCreate(symbol, solution, cancellationToken, out var serializedSymbol))
                {
                    var client = await RemoteHostClient.TryGetClientAsync(solution.Workspace, cancellationToken).ConfigureAwait(false);
                    if (client != null)
                    {
                        var result = await client.TryInvokeAsync<IRemoteRenamerService, SerializableRenameLocations?>(
                            solution,
                            (service, solutionInfo, callbackId, cancellationToken) => service.FindRenameLocationsAsync(solutionInfo, callbackId, serializedSymbol, options, cancellationToken),
                            callbackTarget: new RemoteOptionsProvider<CodeCleanupOptions>(solution.Workspace.Services, fallbackOptions),
                            cancellationToken).ConfigureAwait(false);

                        if (result.HasValue && result.Value != null)
                        {
                            var rehydrated = await TryRehydrateAsync(
                                solution, fallbackOptions, result.Value, cancellationToken).ConfigureAwait(false);

                            if (rehydrated != null)
                                return rehydrated;
                        }

                        // TODO: do not fall back to in-proc if client is available (https://github.com/dotnet/roslyn/issues/47557)
                    }
                }
            }

            // Couldn't effectively search in OOP. Perform the search in-proc.
            var renameLocations = await SymbolicRenameLocations.FindLocationsInCurrentProcessAsync(
                symbol, solution, options, fallbackOptions, cancellationToken).ConfigureAwait(false);

            return new LightweightRenameLocations(
                solution, options, fallbackOptions, renameLocations.Locations,
                renameLocations.ImplicitLocations.SelectAsArray(loc => SerializableReferenceLocation.Dehydrate(loc, cancellationToken)),
                renameLocations.ReferencedSymbols.SelectAsArray(sym => SerializableSymbolAndProjectId.Dehydrate(solution, sym, cancellationToken)));
        }

        public Task<ConflictResolution> ResolveConflictsAsync(ISymbol symbol, string replacementText, ImmutableArray<SymbolKey> nonConflictSymbolKeys, CancellationToken cancellationToken)
            => ConflictResolver.ResolveLightweightConflictsAsync(symbol, this, replacementText, nonConflictSymbolKeys, cancellationToken);

        public LightweightRenameLocations Filter(Func<DocumentId, TextSpan, bool> filter)
            => new(
                this.Solution,
                this.Options,
                this.FallbackOptions,
                this.Locations.Where(loc => filter(loc.DocumentId, loc.Location.SourceSpan)).ToImmutableHashSet(),
                _implicitLocations.WhereAsArray(loc => filter(loc.Document, loc.Location)),
                _referencedSymbols);
    }
}
