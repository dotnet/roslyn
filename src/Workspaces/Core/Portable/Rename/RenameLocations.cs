// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.Options;
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
            public readonly IEnumerable<ReferenceLocation> ImplicitLocations;
            public readonly ISet<RenameLocation> Locations;
            public readonly IEnumerable<SymbolAndProjectId> ReferencedSymbols;

            public SearchResult(
                ISet<RenameLocation> locations,
                IEnumerable<ReferenceLocation> implicitLocations,
                IEnumerable<SymbolAndProjectId> referencedSymbols)
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

        // possibly null fields
        private readonly SearchResult _originalSymbolResult;
        private readonly List<SearchResult> _overloadsResult;
        private readonly IEnumerable<RenameLocation> _stringsResult;
        private readonly IEnumerable<RenameLocation> _commentsResult;

        internal RenameLocations(
            ISet<RenameLocation> locations,
            SymbolAndProjectId symbolAndProjectId,
            Solution solution,
            IEnumerable<SymbolAndProjectId> referencedSymbols,
            IEnumerable<ReferenceLocation> implicitLocations,
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
            List<SearchResult> overloadsResult,
            IEnumerable<RenameLocation> stringsResult,
            IEnumerable<RenameLocation> commentsResult)
        {
            _symbolAndProjectId = symbolAndProjectId;
            _solution = solution;
            Options = options;
            _originalSymbolResult = originalSymbolResult;
            _overloadsResult = overloadsResult;
            _stringsResult = stringsResult;
            _commentsResult = commentsResult;

            var mergedLocations = new HashSet<RenameLocation>();
            var mergedReferencedSymbols = new List<SymbolAndProjectId>();
            var mergedImplicitLocations = new List<ReferenceLocation>();

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
            var overloadsToMerge = (options.GetOption(RenameOptions.RenameOverloads) ? overloadsResult : null) ?? SpecializedCollections.EmptyEnumerable<SearchResult>();
            foreach (var result in overloadsToMerge.Concat(originalSymbolResult))
            {
                mergedLocations.AddRange(renameMethodGroupReferences
                    ? result.Locations
                    : result.Locations.Where(x => x.CandidateReason != CandidateReason.MemberGroup));

                mergedImplicitLocations.AddRange(result.ImplicitLocations);
                mergedReferencedSymbols.AddRange(result.ReferencedSymbols);
            }

            _mergedResult = new SearchResult(mergedLocations, mergedImplicitLocations, mergedReferencedSymbols);
        }

        public ISet<RenameLocation> Locations => _mergedResult.Locations;
        public SymbolAndProjectId SymbolAndProjectId => _symbolAndProjectId;
        public ISymbol Symbol => _symbolAndProjectId.Symbol;
        public Solution Solution => _solution;
        public IEnumerable<SymbolAndProjectId> ReferencedSymbols => _mergedResult.ReferencedSymbols;
        public IEnumerable<ReferenceLocation> ImplicitLocations => _mergedResult.ImplicitLocations;

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
                var intermediateResult = new RenameLocations(symbolAndProjectId, solution, optionSet, originalSymbolResult, overloadsResult: null, stringsResult: null, commentsResult: null);

                return await intermediateResult.FindWithUpdatedOptionsAsync(optionSet, cancellationToken).ConfigureAwait(false);
            }
        }

        internal async Task<RenameLocations> FindWithUpdatedOptionsAsync(OptionSet optionSet, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(Options, "FindWithUpdatedOptionsAsync can only be called on a result of FindAsync");
            using (Logger.LogBlock(FunctionId.Rename_AllRenameLocations, cancellationToken))
            {
                var overloadsResult = _overloadsResult ?? (optionSet.GetOption(RenameOptions.RenameOverloads)
                    ? await GetOverloadsAsync(_symbolAndProjectId, _solution, cancellationToken).ConfigureAwait(false)
                    : null);

                var stringsAndComments = await ReferenceProcessing.GetRenamableLocationsInStringsAndCommentsAsync(
                    _symbolAndProjectId.Symbol,
                    _solution,
                    _originalSymbolResult.Locations,
                    optionSet.GetOption(RenameOptions.RenameInStrings) && _stringsResult == null,
                    optionSet.GetOption(RenameOptions.RenameInComments) && _commentsResult == null,
                    cancellationToken).ConfigureAwait(false);

                return new RenameLocations(
                    _symbolAndProjectId, _solution, optionSet, _originalSymbolResult,
                    _overloadsResult ?? overloadsResult,
                    _stringsResult ?? stringsAndComments.Item1,
                    _commentsResult ?? stringsAndComments.Item2);
            }
        }

        private static async Task<List<SearchResult>> GetOverloadsAsync(
            SymbolAndProjectId symbolAndProjectId, Solution solution, CancellationToken cancellationToken)
        {
            var overloadsResult = new List<SearchResult>();
            foreach (var overloadedSymbol in GetOverloadedSymbols(symbolAndProjectId))
            {
                overloadsResult.Add(await AddLocationsReferenceSymbolsAsync(overloadedSymbol, solution, cancellationToken).ConfigureAwait(false));
            }

            return overloadsResult;
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
            var locations = new HashSet<RenameLocation>();
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

            var implicitLocations = referenceSymbols.SelectMany(refSym => refSym.Locations).Where(loc => loc.IsImplicit).ToList();
            var referencedSymbols = referenceSymbols.Select(r => r.DefinitionAndProjectId).Where(r => !r.Symbol.Equals(symbol)).ToList();

            return new SearchResult(locations, implicitLocations, referencedSymbols);
        }
    }
}
