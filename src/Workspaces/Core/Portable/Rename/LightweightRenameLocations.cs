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
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename
{
    /// <summary>
    /// Equivalent to <see cref="HeavyweightRenameLocations"/> except that references to symbols are kept in a lightweight fashion
    /// to avoid expensive rehydration steps as a host and OOP communicate.
    /// </summary>
    internal sealed partial class LightweightRenameLocations
    {
        public readonly ISymbol Symbol;
        public readonly Solution Solution;
        public readonly SymbolRenameOptions Options;
        public readonly CodeCleanupOptionsProvider FallbackOptions;

        public readonly ImmutableHashSet<RenameLocation> Locations;
        private readonly ImmutableArray<SerializableReferenceLocation> ImplicitLocations;
        private readonly ImmutableArray<SerializableSymbolAndProjectId> ReferencedSymbols;

        //public ImmutableArray<ISymbol> ReferencedSymbols => _result.ReferencedSymbols;
        //public ImmutableArray<ReferenceLocation> ImplicitLocations => _result.ImplicitLocations;

        private LightweightRenameLocations(
            ISymbol symbol,
            Solution solution,
            SymbolRenameOptions options,
            CodeCleanupOptionsProvider fallbackOptions,
            ImmutableHashSet<RenameLocation> locations,
            ImmutableArray<SerializableReferenceLocation> implicitLocations,
            ImmutableArray<SerializableSymbolAndProjectId> referencedSymbols)
        {
            Symbol = symbol;
            Solution = solution;
            Options = options;
            FallbackOptions = fallbackOptions;
            Contract.ThrowIfNull(locations);
            this.Locations = locations;
            this.ImplicitLocations = implicitLocations;
            this.ReferencedSymbols = referencedSymbols;
        }

        public async Task<HeavyweightRenameLocations?> ToHeavyweightAsync(CancellationToken cancellationToken)
        {
            var referencedSymbols = ReferencedSymbols.IsDefault
                ? default
                : await ReferencedSymbols.SelectAsArrayAsync(sym => sym.TryRehydrateAsync(Solution, cancellationToken)).ConfigureAwait(false);

            if (!referencedSymbols.IsDefault && referencedSymbols.Any(s => s is null))
                return null;

            return new HeavyweightRenameLocations(
               Symbol,
               Solution,
               Options,
               FallbackOptions,
               Locations,
               ImplicitLocations.IsDefault ? default : await ImplicitLocations.SelectAsArrayAsync(loc => loc.RehydrateAsync(Solution, cancellationToken)).ConfigureAwait(false),
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
                                solution, symbol, fallbackOptions, result.Value, cancellationToken).ConfigureAwait(false);

                            if (rehydrated != null)
                                return rehydrated;
                        }

                        // TODO: do not fall back to in-proc if client is available (https://github.com/dotnet/roslyn/issues/47557)
                    }
                }
            }

            // Couldn't effectively search in OOP. Perform the search in-proc.
            var renameLocations = await HeavyweightRenameLocations.FindLocationsInCurrentProcessAsync(
                symbol, solution, options, fallbackOptions, cancellationToken).ConfigureAwait(false);

            return new LightweightRenameLocations(
                symbol, solution, options, fallbackOptions, renameLocations.Locations,
                renameLocations.ImplicitLocations.IsDefault ? default : renameLocations.ImplicitLocations.SelectAsArray(loc => SerializableReferenceLocation.Dehydrate(loc, cancellationToken)),
                renameLocations.ReferencedSymbols.IsDefault ? default : renameLocations.ReferencedSymbols.SelectAsArray(sym => SerializableSymbolAndProjectId.Dehydrate(solution, sym, cancellationToken)));
        }

        public LightweightRenameLocations Filter(Func<DocumentId, TextSpan, bool> filter)
            => new(
                this.Symbol,
                this.Solution,
                this.Options,
                this.FallbackOptions,
                this.Locations.Where(loc => filter(loc.DocumentId, loc.Location.SourceSpan)).ToImmutableHashSet(),
                this.ImplicitLocations.WhereAsArray(loc => filter(loc.Document, loc.Location)),
                this.ReferencedSymbols);
    }
}
