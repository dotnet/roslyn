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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Remote;
using Microsoft.CodeAnalysis.Rename.ConflictEngine;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Rename
{
    /// <summary>
    /// Holds the Locations of a symbol that should be renamed, along with the symbol and Solution
    /// for the set.
    /// </summary>
    internal sealed partial class RenameLocations
    {
        public readonly Solution Solution;
        public readonly ISymbol Symbol;
        public readonly RenameOptionSet Options;

        private readonly SearchResult _result;

        public ISet<RenameLocation> Locations => _result.Locations;
        public ImmutableArray<ISymbol> ReferencedSymbols => _result.ReferencedSymbols;
        public ImmutableArray<ReferenceLocation> ImplicitLocations => _result.ImplicitLocations;

        private RenameLocations(
            ISymbol symbol,
            Solution solution,
            RenameOptionSet options,
            SearchResult result)
        {
            Solution = solution;
            Symbol = symbol;
            Options = options;
            _result = result;
        }

        internal static RenameLocations Create(
            ImmutableHashSet<RenameLocation> locations,
            ISymbol symbol,
            Solution solution,
            ImmutableArray<ISymbol> referencedSymbols,
            ImmutableArray<ReferenceLocation> implicitLocations,
            RenameOptionSet options)
        {
            return new RenameLocations(
                symbol, solution, options,
                new SearchResult(locations, implicitLocations, referencedSymbols));
        }

        private static RenameLocations Create(
            ISymbol symbol,
            Solution solution,
            RenameOptionSet options,
            SearchResult originalSymbolResult,
            ImmutableArray<SearchResult> overloadsResult,
            ImmutableArray<RenameLocation> stringsResult,
            ImmutableArray<RenameLocation> commentsResult)
        {
            var mergedLocations = ImmutableHashSet.CreateBuilder<RenameLocation>();
            using var _1 = ArrayBuilder<ISymbol>.GetInstance(out var mergedReferencedSymbols);
            using var _2 = ArrayBuilder<ReferenceLocation>.GetInstance(out var mergedImplicitLocations);

            if (options.RenameInStrings)
                mergedLocations.AddRange(stringsResult);

            if (options.RenameInComments)
                mergedLocations.AddRange(commentsResult);

            var renameMethodGroupReferences = options.RenameOverloads || !GetOverloadedSymbols(symbol).Any();
            var overloadsToMerge = options.RenameOverloads
                ? overloadsResult.NullToEmpty()
                : ImmutableArray<SearchResult>.Empty;
            foreach (var result in overloadsToMerge.Concat(originalSymbolResult))
            {
                mergedLocations.AddRange(renameMethodGroupReferences
                    ? result.Locations
                    : result.Locations.Where(x => x.CandidateReason != CandidateReason.MemberGroup));

                mergedImplicitLocations.AddRange(result.ImplicitLocations);
                mergedReferencedSymbols.AddRange(result.ReferencedSymbols);
            }

            return new RenameLocations(
                symbol, solution, options,
                new SearchResult(
                    mergedLocations.ToImmutable(),
                    mergedImplicitLocations.ToImmutable(),
                    mergedReferencedSymbols.ToImmutable()));
        }

        /// <summary>
        /// Find the locations that need to be renamed.
        /// </summary>
        public static async Task<RenameLocations> FindLocationsAsync(
            ISymbol symbol, Solution solution, RenameOptionSet optionSet, CancellationToken cancellationToken)
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
                        var result = await client.RunRemoteAsync<SerializableRenameLocations?>(
                            WellKnownServiceHubService.CodeAnalysis,
                            nameof(IRemoteRenamer.FindRenameLocationsAsync),
                            solution,
                            new object[]
                            {
                                serializedSymbol,
                                SerializableRenameOptionSet.Dehydrate(optionSet),
                            },
                            callbackTarget: null,
                            cancellationToken).ConfigureAwait(false);

                        if (result != null)
                        {
                            var rehydrated = await RenameLocations.TryRehydrateAsync(
                                solution, result, cancellationToken).ConfigureAwait(false);

                            if (rehydrated != null)
                                return rehydrated;
                        }
                    }
                }
            }

            // Couldn't effectively search in OOP. Perform the search in-proc.
            return await FindLocationsInCurrentProcessAsync(
                symbol, solution, optionSet, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<RenameLocations> FindLocationsInCurrentProcessAsync(
            ISymbol symbol, Solution solution, RenameOptionSet optionSet, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(symbol);
            using (Logger.LogBlock(FunctionId.Rename_AllRenameLocations, cancellationToken))
            {
                symbol = await ReferenceProcessing.FindDefinitionSymbolAsync(symbol, solution, cancellationToken).ConfigureAwait(false);

                // First, find the direct references just to the symbol being renamed.
                var originalSymbolResult = await AddLocationsReferenceSymbolsAsync(symbol, solution, cancellationToken).ConfigureAwait(false);

                // Next, find references to overloads, if the user has asked to rename those as well.
                var overloadsResult = await GetOverloadsAsync(symbol, solution, optionSet, cancellationToken).ConfigureAwait(false);

                // Finally, include strings/comments if that's what the user wants.
                var (strings, comments) = await ReferenceProcessing.GetRenamableLocationsInStringsAndCommentsAsync(
                    symbol,
                    solution,
                    originalSymbolResult.Locations,
                    optionSet.RenameInStrings,
                    optionSet.RenameInComments,
                    cancellationToken).ConfigureAwait(false);

                var mergedLocations = ImmutableHashSet.CreateBuilder<RenameLocation>();

                using var _1 = ArrayBuilder<ISymbol>.GetInstance(out var mergedReferencedSymbols);
                using var _2 = ArrayBuilder<ReferenceLocation>.GetInstance(out var mergedImplicitLocations);

                mergedLocations.AddRange(strings.NullToEmpty());
                mergedLocations.AddRange(comments.NullToEmpty());

                var renameMethodGroupReferences = optionSet.RenameOverloads || !GetOverloadedSymbols(symbol).Any();
                foreach (var result in overloadsResult.Concat(originalSymbolResult))
                {
                    mergedLocations.AddRange(renameMethodGroupReferences
                        ? result.Locations
                        : result.Locations.Where(x => x.CandidateReason != CandidateReason.MemberGroup));

                    mergedImplicitLocations.AddRange(result.ImplicitLocations);
                    mergedReferencedSymbols.AddRange(result.ReferencedSymbols);
                }

                return new RenameLocations(
                    symbol, solution, optionSet,
                    new SearchResult(
                        mergedLocations.ToImmutable(),
                        mergedImplicitLocations.ToImmutable(),
                        mergedReferencedSymbols.ToImmutable()));
            }
        }

        private static async Task<ImmutableArray<SearchResult>> GetOverloadsAsync(
            ISymbol symbol, Solution solution, RenameOptionSet options, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<SearchResult>.GetInstance(out var overloadsResult);

            if (options.RenameOverloads)
            {
                foreach (var overloadedSymbol in GetOverloadedSymbols(symbol))
                    overloadsResult.Add(await AddLocationsReferenceSymbolsAsync(overloadedSymbol, solution, cancellationToken).ConfigureAwait(false));
            }

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
                symbol, solution, cancellationToken).ConfigureAwait(false);

            foreach (var referencedSymbol in referenceSymbols)
            {
                locations.AddAll(
                    await ReferenceProcessing.GetRenamableDefinitionLocationsAsync(referencedSymbol.Definition, symbol, solution, cancellationToken).ConfigureAwait(false));

                locations.AddAll(
                    await referencedSymbol.Locations.SelectManyAsync<ReferenceLocation, RenameLocation>(
                        (l, c) => ReferenceProcessing.GetRenamableReferenceLocationsAsync(referencedSymbol.Definition, symbol, l, solution, c),
                        cancellationToken).ConfigureAwait(false));
            }

            var implicitLocations = referenceSymbols.SelectMany(refSym => refSym.Locations).Where(loc => loc.IsImplicit).ToImmutableArray();
            var referencedSymbols = referenceSymbols.Select(r => r.Definition).Where(r => !r.Equals(symbol)).ToImmutableArray();

            return new SearchResult(locations.ToImmutable(), implicitLocations, referencedSymbols);
        }

        /// <summary>
        /// Performs the renaming of the symbol in the solution, identifies renaming conflicts and automatically
        /// resolves them where possible.
        /// </summary>
        /// <param name="replacementText">The new name of the identifier</param>
        /// <param name="nonConflictSymbols">Used after renaming references. References that now bind to any of these
        /// symbols are not considered to be in conflict. Useful for features that want to rename existing references to
        /// point at some existing symbol. Normally this would be a conflict, but this can be used to override that
        /// behavior.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A conflict resolution containing the new solution.</returns>
        public Task<ConflictResolution> ResolveConflictsAsync(string replacementText, ImmutableHashSet<ISymbol>? nonConflictSymbols = null, CancellationToken cancellationToken = default)
            => ConflictResolver.ResolveConflictsAsync(this, replacementText, nonConflictSymbols, cancellationToken);

        public RenameLocations Filter(Func<Location, bool> filter)
            => Create(
                this.Locations.Where(loc => filter(loc.Location)).ToImmutableHashSet(),
                this.Symbol,
                this.Solution,
                this.ReferencedSymbols,
                this.ImplicitLocations.WhereAsArray(loc => filter(loc.Location)),
                this.Options);
    }
}
