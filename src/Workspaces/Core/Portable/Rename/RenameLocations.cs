// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
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
        private class SearchResult
        {
            public readonly ImmutableHashSet<RenameLocation> Locations;
            public readonly ImmutableArray<ReferenceLocation> ImplicitLocations;
            public readonly ImmutableArray<SymbolAndProjectId> ReferencedSymbols;

            public SearchResult(
                ImmutableHashSet<RenameLocation> locations,
                ImmutableArray<ReferenceLocation> implicitLocations,
                ImmutableArray<SymbolAndProjectId> referencedSymbols)
            {
                this.Locations = locations;
                this.ImplicitLocations = implicitLocations;
                this.ReferencedSymbols = referencedSymbols;
            }
        }

        // never null fields
        private readonly SymbolAndProjectId _symbolAndProjectId;
        private readonly Solution _solution;
        private readonly SearchResult _mergedResult;
        internal OptionSet Options { get; }

        // can be default
        private readonly ImmutableArray<SearchResult> _overloadsResult;
        private readonly ImmutableArray<RenameLocation> _stringsResult;
        private readonly ImmutableArray<RenameLocation> _commentsResult;

        // possibly null fields
        private readonly SearchResult _originalSymbolResult;

        internal RenameLocations(
            ImmutableHashSet<RenameLocation> locations,
            SymbolAndProjectId symbolAndProjectId,
            Solution solution,
            ImmutableArray<SymbolAndProjectId> referencedSymbols,
            ImmutableArray<ReferenceLocation> implicitLocations,
            OptionSet options)
        {
            _symbolAndProjectId = symbolAndProjectId;
            _solution = solution;
            _mergedResult = new SearchResult(locations, implicitLocations, referencedSymbols);
            Options = options;
        }

        private RenameLocations(
            SymbolAndProjectId symbolAndProjectId,
            Solution solution,
            OptionSet options,
            SearchResult originalSymbolResult,
            ImmutableArray<SearchResult> overloadsResult,
            ImmutableArray<RenameLocation> stringsResult,
            ImmutableArray<RenameLocation> commentsResult)
        {
            _symbolAndProjectId = symbolAndProjectId;
            _solution = solution;
            Options = options;
            _originalSymbolResult = originalSymbolResult;
            _overloadsResult = overloadsResult;
            _stringsResult = stringsResult;
            _commentsResult = commentsResult;

            var mergedLocations = ImmutableHashSet.CreateBuilder<RenameLocation>();
            using var _1 = ArrayBuilder<SymbolAndProjectId>.GetInstance(out var mergedReferencedSymbols);
            using var _2 = ArrayBuilder<ReferenceLocation>.GetInstance(out var mergedImplicitLocations);

            if (options.GetOption(RenameOptions.RenameInStrings))
            {
                mergedLocations.AddRange(stringsResult);
            }

            if (options.GetOption(RenameOptions.RenameInComments))
            {
                mergedLocations.AddRange(commentsResult);
            }

            var renameMethodGroupReferences =
                options.GetOption(RenameOptions.RenameOverloads) || !GetOverloadedSymbols(symbolAndProjectId).Any();
            var overloadsToMerge = options.GetOption(RenameOptions.RenameOverloads)
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

            _mergedResult = new SearchResult(
                mergedLocations.ToImmutable(), mergedImplicitLocations.ToImmutable(), mergedReferencedSymbols.ToImmutable());
        }

        public ISet<RenameLocation> Locations => _mergedResult.Locations;
        public SymbolAndProjectId SymbolAndProjectId => _symbolAndProjectId;
        public ISymbol Symbol => _symbolAndProjectId.Symbol;
        public Solution Solution => _solution;
        public ImmutableArray<SymbolAndProjectId> ReferencedSymbols => _mergedResult.ReferencedSymbols;
        public ImmutableArray<ReferenceLocation> ImplicitLocations => _mergedResult.ImplicitLocations;

        /// <summary>
        /// Find the locations that need to be renamed.
        /// </summary>
        internal static async Task<RenameLocations> FindAsync(
            SymbolAndProjectId symbolAndProjectId, Solution solution, OptionSet optionSet, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(symbolAndProjectId.Symbol);
            using (Logger.LogBlock(FunctionId.Rename_AllRenameLocations, cancellationToken))
            {
                symbolAndProjectId = await ReferenceProcessing.FindDefinitionSymbolAsync(symbolAndProjectId, solution, cancellationToken).ConfigureAwait(false);
                var originalSymbolResult = await AddLocationsReferenceSymbolsAsync(symbolAndProjectId, solution, cancellationToken).ConfigureAwait(false);
                var intermediateResult = new RenameLocations(
                    symbolAndProjectId, solution, optionSet, originalSymbolResult, overloadsResult: default, stringsResult: default, commentsResult: default);

                return await intermediateResult.FindWithUpdatedOptionsAsync(optionSet, cancellationToken).ConfigureAwait(false);
            }
        }

        internal async Task<RenameLocations> FindWithUpdatedOptionsAsync(OptionSet optionSet, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(Options, "FindWithUpdatedOptionsAsync can only be called on a result of FindAsync");
            using (Logger.LogBlock(FunctionId.Rename_AllRenameLocations, cancellationToken))
            {
                var overloadsResult = !_overloadsResult.IsDefault
                    ? _overloadsResult
                    : optionSet.GetOption(RenameOptions.RenameOverloads)
                        ? await GetOverloadsAsync(_symbolAndProjectId, _solution, cancellationToken).ConfigureAwait(false)
                        : default;

                var stringsAndComments = await ReferenceProcessing.GetRenamableLocationsInStringsAndCommentsAsync(
                    _symbolAndProjectId.Symbol,
                    _solution,
                    _originalSymbolResult.Locations,
                    optionSet.GetOption(RenameOptions.RenameInStrings) && _stringsResult.IsDefault,
                    optionSet.GetOption(RenameOptions.RenameInComments) && _commentsResult.IsDefault,
                    cancellationToken).ConfigureAwait(false);

                return new RenameLocations(
                    _symbolAndProjectId, _solution, optionSet, _originalSymbolResult,
                    _overloadsResult.IsDefault ? overloadsResult : _overloadsResult,
                    _stringsResult.IsDefault ? stringsAndComments.Item1 : _stringsResult,
                    _commentsResult.IsDefault ? stringsAndComments.Item2 : _commentsResult);
            }
        }

        private static async Task<ImmutableArray<SearchResult>> GetOverloadsAsync(
            SymbolAndProjectId symbolAndProjectId, Solution solution, CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<SearchResult>.GetInstance(out var overloadsResult);
            foreach (var overloadedSymbol in GetOverloadedSymbols(symbolAndProjectId))
                overloadsResult.Add(await AddLocationsReferenceSymbolsAsync(overloadedSymbol, solution, cancellationToken).ConfigureAwait(false));

            return overloadsResult.ToImmutable();
        }

        internal static IEnumerable<SymbolAndProjectId> GetOverloadedSymbols(
            SymbolAndProjectId symbolAndProjectId)
        {
            var symbol = symbolAndProjectId.Symbol;
            if (symbol is IMethodSymbol)
            {
                var containingType = symbol.ContainingType;
                if (containingType.Kind == SymbolKind.NamedType)
                {
                    foreach (var member in containingType.GetMembers())
                    {
                        if (string.Equals(member.MetadataName, symbol.MetadataName, StringComparison.Ordinal) && member is IMethodSymbol && !member.Equals(symbol))
                        {
                            yield return symbolAndProjectId.WithSymbol(member);
                        }
                    }
                }
            }
        }

        private static async Task<SearchResult> AddLocationsReferenceSymbolsAsync(
            SymbolAndProjectId symbolAndProjectId,
            Solution solution,
            CancellationToken cancellationToken)
        {
            var symbol = symbolAndProjectId.Symbol;
            var locations = ImmutableHashSet.CreateBuilder<RenameLocation>();
            var referenceSymbols = await SymbolFinder.FindRenamableReferencesAsync(
                symbolAndProjectId, solution, cancellationToken).ConfigureAwait(false);

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
            var referencedSymbols = referenceSymbols.Select(r => r.DefinitionAndProjectId).Where(r => !r.Symbol.Equals(symbol)).ToImmutableArray();

            return new SearchResult(locations.ToImmutable(), implicitLocations, referencedSymbols);
        }

        public RenameLocations Filter(Func<Location, bool> filter)
            => new RenameLocations(
                this.Locations.Where(loc => filter(loc.Location)).ToImmutableHashSet(),
                this.SymbolAndProjectId, this.Solution,
                this.ReferencedSymbols, this.ImplicitLocations.WhereAsArray(loc => filter(loc.Location)),
                this.Options);
    }
}
