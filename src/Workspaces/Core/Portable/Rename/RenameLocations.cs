// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeCleanup;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using static Microsoft.CodeAnalysis.Rename.RenameLocations;

namespace Microsoft.CodeAnalysis.Rename
{
    /// <summary>
    /// Equivalent to <see cref="RenameLocations"/> except that references to symbols are kept in a lightweight fashion
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

        public async Task<RenameLocations?> ToRenameLocationsAsync(CancellationToken cancellationToken)
        {
            var referencedSymbols = ReferencedSymbols.IsDefault
                ? default
                : await ReferencedSymbols.SelectAsArrayAsync(sym => sym.TryRehydrateAsync(Solution, cancellationToken)).ConfigureAwait(false);

            if (!referencedSymbols.IsDefault && referencedSymbols.Any(s => s is null))
                return null;

            return new RenameLocations(
               Symbol,
               Solution,
               Options,
               FallbackOptions,
               Locations,
               ImplicitLocations.IsDefault ? default : await ImplicitLocations.SelectAsArrayAsync(loc => loc.RehydrateAsync(Solution, cancellationToken)).ConfigureAwait(false),
               referencedSymbols);
        }

        /// <summary>
        /// Find the locations that need to be renamed.
        /// </summary>
        public static async Task<LightweightRenameLocations> FindLightweightLocationsAsync(
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
            var renameLocations = await FindLocationsInCurrentProcessAsync(
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

    /// <summary>
    /// Holds the Locations of a symbol that should be renamed, along with the symbol and Solution for the set.
    /// </summary>
    internal sealed partial class RenameLocations
    {
        public readonly Solution Solution;
        public readonly ISymbol Symbol;
        public readonly SymbolRenameOptions Options;
        public readonly CodeCleanupOptionsProvider FallbackOptions;

        public readonly ImmutableHashSet<RenameLocation> Locations;
        public readonly ImmutableArray<ReferenceLocation> ImplicitLocations;
        public readonly ImmutableArray<ISymbol> ReferencedSymbols;

        public RenameLocations(
            ISymbol symbol,
            Solution solution,
            SymbolRenameOptions options,
            CodeCleanupOptionsProvider fallbackOptions,
            ImmutableHashSet<RenameLocation> locations,
            ImmutableArray<ReferenceLocation> implicitLocations,
            ImmutableArray<ISymbol> referencedSymbols)
        {
            Solution = solution;
            Symbol = symbol;
            Options = options;
            FallbackOptions = fallbackOptions;
            Contract.ThrowIfNull(locations);
            Locations = locations;
            ReferencedSymbols = referencedSymbols;
            ImplicitLocations = implicitLocations;
        }

        public static async Task<RenameLocations> FindLocationsInCurrentProcessAsync(
            ISymbol symbol, Solution solution, SymbolRenameOptions options, CodeCleanupOptionsProvider cleanupOptions, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(symbol);
            using (Logger.LogBlock(FunctionId.Rename_AllRenameLocations, cancellationToken))
            {
                symbol = await ReferenceProcessing.FindDefinitionSymbolAsync(symbol, solution, cancellationToken).ConfigureAwait(false);

                // First, find the direct references just to the symbol being renamed.
                var originalSymbolResult = await AddLocationsReferenceSymbolsAsync(symbol, solution, cancellationToken).ConfigureAwait(false);

                // Next, find references to overloads, if the user has asked to rename those as well.
                var overloadsResult = options.RenameOverloads ? await GetOverloadsAsync(symbol, solution, cancellationToken).ConfigureAwait(false) :
                    ImmutableArray<SearchResult>.Empty;

                // Finally, include strings/comments if that's what the user wants.
                var (strings, comments) = await ReferenceProcessing.GetRenamableLocationsInStringsAndCommentsAsync(
                    symbol,
                    solution,
                    originalSymbolResult.Locations,
                    options.RenameInStrings,
                    options.RenameInComments,
                    cancellationToken).ConfigureAwait(false);

                var mergedLocations = ImmutableHashSet.CreateBuilder<RenameLocation>();

                using var _1 = ArrayBuilder<ISymbol>.GetInstance(out var mergedReferencedSymbols);
                using var _2 = ArrayBuilder<ReferenceLocation>.GetInstance(out var mergedImplicitLocations);

                var renameMethodGroupReferences = options.RenameOverloads || !GetOverloadedSymbols(symbol).Any();
                foreach (var result in overloadsResult.Concat(originalSymbolResult))
                {
                    mergedLocations.AddRange(renameMethodGroupReferences
                        ? result.Locations
                        : result.Locations.Where(x => x.CandidateReason != CandidateReason.MemberGroup));

                    mergedImplicitLocations.AddRange(result.ImplicitLocations);
                    mergedReferencedSymbols.AddRange(result.ReferencedSymbols);
                }

                // Add string and comment locations to the merged hashset 
                // after adding in reference symbols. This allows any references
                // in comments to be resolved as proper references rather than
                // comment resolutions. See https://github.com/dotnet/roslyn/issues/54294
                mergedLocations.AddRange(strings.NullToEmpty());
                mergedLocations.AddRange(comments.NullToEmpty());

                return new RenameLocations(
                    symbol, solution, options, cleanupOptions,
                    mergedLocations.ToImmutable(),
                    mergedImplicitLocations.ToImmutable(),
                    mergedReferencedSymbols.ToImmutable());
            }
        }

        private static async Task<ImmutableArray<SearchResult>> GetOverloadsAsync(
            ISymbol symbol, Solution solution, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<SearchResult>.GetInstance(out var overloadsResult);

            foreach (var overloadedSymbol in GetOverloadedSymbols(symbol))
                overloadsResult.Add(await AddLocationsReferenceSymbolsAsync(overloadedSymbol, solution, cancellationToken).ConfigureAwait(false));

            return overloadsResult.ToImmutable();
        }

        internal static IEnumerable<ISymbol> GetOverloadedSymbols(ISymbol symbol)
        {
            if (symbol is IMethodSymbol)
            {
                var containingType = symbol.ContainingType;
                if (containingType.Kind == SymbolKind.NamedType)
                {
                    foreach (var member in containingType.GetMembers())
                    {
                        if (string.Equals(member.MetadataName, symbol.MetadataName, StringComparison.Ordinal) && member is IMethodSymbol && !member.Equals(symbol))
                        {
                            yield return member;
                        }
                    }
                }
            }
        }

        private static async Task<SearchResult> AddLocationsReferenceSymbolsAsync(
            ISymbol symbol,
            Solution solution,
            CancellationToken cancellationToken)
        {
            var locations = ImmutableHashSet.CreateBuilder<RenameLocation>();
            var referenceSymbols = await SymbolFinder.FindRenamableReferencesAsync(
                ImmutableArray.Create(symbol), solution, cancellationToken).ConfigureAwait(false);

            foreach (var referencedSymbol in referenceSymbols)
            {
                locations.AddAll(
                    await ReferenceProcessing.GetRenamableDefinitionLocationsAsync(referencedSymbol.Definition, symbol, solution, cancellationToken).ConfigureAwait(false));

                locations.AddAll(
                    await referencedSymbol.Locations.SelectManyInParallelAsync(
                        (l, c) => ReferenceProcessing.GetRenamableReferenceLocationsAsync(referencedSymbol.Definition, symbol, l, solution, c),
                        cancellationToken).ConfigureAwait(false));
            }

            var implicitLocations = referenceSymbols.SelectMany(refSym => refSym.Locations).Where(loc => loc.IsImplicit).ToImmutableArray();
            var referencedSymbols = referenceSymbols.Select(r => r.Definition).Where(r => !r.Equals(symbol)).ToImmutableArray();

            return new SearchResult(locations.ToImmutable(), implicitLocations, referencedSymbols);
        }
    }
}
