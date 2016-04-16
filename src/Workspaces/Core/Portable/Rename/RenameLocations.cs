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
            public readonly IEnumerable<ISymbol> ReferencedSymbols;

            public SearchResult(ISet<RenameLocation> locations, IEnumerable<ReferenceLocation> implicitLocations, IEnumerable<ISymbol> referencedSymbols)
            {
                this.Locations = locations;
                this.ImplicitLocations = implicitLocations;
                this.ReferencedSymbols = referencedSymbols;
            }
        }

        // never null fields
        private readonly ISymbol _symbol;
        private readonly Solution _solution;
        private readonly SearchResult _mergedResult;
        internal OptionSet Options { get; }

        // possibly null fields
        private readonly SearchResult _originalSymbolResult;
        private readonly List<SearchResult> _overloadsResult;
        private readonly IEnumerable<RenameLocation> _stringsResult;
        private readonly IEnumerable<RenameLocation> _commentsResult;

        internal RenameLocations(ISet<RenameLocation> locations, ISymbol symbol, Solution solution, IEnumerable<ISymbol> referencedSymbols, IEnumerable<ReferenceLocation> implicitLocations, OptionSet options)
        {
            _symbol = symbol;
            _solution = solution;
            _mergedResult = new SearchResult(locations, implicitLocations, referencedSymbols);
            Options = options;
        }

        private RenameLocations(ISymbol symbol, Solution solution, OptionSet options, SearchResult originalSymbolResult, List<SearchResult> overloadsResult, IEnumerable<RenameLocation> stringsResult, IEnumerable<RenameLocation> commentsResult)
        {
            _symbol = symbol;
            _solution = solution;
            Options = options;
            _originalSymbolResult = originalSymbolResult;
            _overloadsResult = overloadsResult;
            _stringsResult = stringsResult;
            _commentsResult = commentsResult;

            var mergedLocations = new HashSet<RenameLocation>();
            var mergedReferencedSymbols = new List<ISymbol>();
            var mergedImplicitLocations = new List<ReferenceLocation>();

            if (options.GetOption(RenameOptions.RenameInStrings))
            {
                mergedLocations.AddRange(stringsResult);
            }

            if (options.GetOption(RenameOptions.RenameInComments))
            {
                mergedLocations.AddRange(commentsResult);
            }

            var renameMethodGroupReferences = options.GetOption(RenameOptions.RenameOverloads) || !GetOverloadedSymbols(symbol).Any();
            var overloadsToMerge = (options.GetOption(RenameOptions.RenameOverloads) ? overloadsResult : null) ?? SpecializedCollections.EmptyEnumerable<SearchResult>();
            foreach (var result in overloadsToMerge.Concat(originalSymbolResult))
            {
                mergedLocations.AddRange(renameMethodGroupReferences
                    ? result.Locations
                    : result.Locations.Where(x => !x.IsMethodGroupReference));

                mergedImplicitLocations.AddRange(result.ImplicitLocations);
                mergedReferencedSymbols.AddRange(result.ReferencedSymbols);
            }

            _mergedResult = new SearchResult(mergedLocations, mergedImplicitLocations, mergedReferencedSymbols);
        }

        public ISet<RenameLocation> Locations { get { return _mergedResult.Locations; } }
        public ISymbol Symbol { get { return _symbol; } }
        public Solution Solution { get { return _solution; } }
        public IEnumerable<ISymbol> ReferencedSymbols { get { return _mergedResult.ReferencedSymbols; } }
        public IEnumerable<ReferenceLocation> ImplicitLocations { get { return _mergedResult.ImplicitLocations; } }

        /// <summary>
        /// Find the locations that need to be renamed.
        /// </summary>
        internal static async Task<RenameLocations> FindAsync(ISymbol symbol, Solution solution, OptionSet optionSet, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(symbol);
            using (Logger.LogBlock(FunctionId.Rename_AllRenameLocations, cancellationToken))
            {
                symbol = await ReferenceProcessing.FindDefinitionSymbolAsync(symbol, solution, cancellationToken).ConfigureAwait(false);
                var originalSymbolResult = await AddLocationsReferenceSymbolsAsync(symbol, solution, cancellationToken).ConfigureAwait(false);
                var intermediateResult = new RenameLocations(symbol, solution, optionSet, originalSymbolResult, overloadsResult: null, stringsResult: null, commentsResult: null);

                return await intermediateResult.FindWithUpdatedOptionsAsync(optionSet, cancellationToken).ConfigureAwait(false);
            }
        }

        internal async Task<RenameLocations> FindWithUpdatedOptionsAsync(OptionSet optionSet, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(Options, "FindWithUpdatedOptionsAsync can only be called on a result of FindAsync");
            using (Logger.LogBlock(FunctionId.Rename_AllRenameLocations, cancellationToken))
            {
                var overloadsResult = _overloadsResult ?? (optionSet.GetOption(RenameOptions.RenameOverloads)
                    ? await GetOverloadsAsync(_symbol, _solution, cancellationToken).ConfigureAwait(false)
                    : null);

                var stringsAndComments = await ReferenceProcessing.GetRenamableLocationsInStringsAndCommentsAsync(
                    _symbol,
                    _solution,
                    _originalSymbolResult.Locations,
                    optionSet.GetOption(RenameOptions.RenameInStrings) && _stringsResult == null,
                    optionSet.GetOption(RenameOptions.RenameInComments) && _commentsResult == null,
                    cancellationToken).ConfigureAwait(false);

                return new RenameLocations(_symbol, _solution, optionSet, _originalSymbolResult,
                    _overloadsResult ?? overloadsResult,
                    _stringsResult ?? stringsAndComments.Item1,
                    _commentsResult ?? stringsAndComments.Item2);
            }
        }

        private static async Task<List<SearchResult>> GetOverloadsAsync(ISymbol symbol, Solution solution, CancellationToken cancellationToken)
        {
            var overloadsResult = new List<SearchResult>();
            foreach (var overloadedSymbol in GetOverloadedSymbols(symbol))
            {
                overloadsResult.Add(await AddLocationsReferenceSymbolsAsync(overloadedSymbol, solution, cancellationToken).ConfigureAwait(false));
            }

            return overloadsResult;
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
            var locations = new HashSet<RenameLocation>();
            var referenceSymbols = await SymbolFinder.FindRenamableReferencesAsync(symbol, solution, cancellationToken).ConfigureAwait(false);

            foreach (var referencedSymbol in referenceSymbols)
            {
                locations.AddAll(
                    await ReferenceProcessing.GetRenamableDefinitionLocationsAsync(referencedSymbol.Definition, symbol, solution, cancellationToken).ConfigureAwait(false));

                locations.AddAll(
                    await referencedSymbol.Locations.SelectManyAsync<ReferenceLocation, RenameLocation>(
                        (l, c) => ReferenceProcessing.GetRenamableReferenceLocationsAsync(referencedSymbol.Definition, symbol, l, solution, c),
                        cancellationToken).ConfigureAwait(false));
            }

            var implicitLocations = new List<ReferenceLocation>(referenceSymbols.SelectMany(refSym => refSym.Locations).Where(loc => loc.IsImplicit));
            var referencedSymbols = new List<ISymbol>(referenceSymbols.Select(r => r.Definition).Where(r => !r.Equals(symbol)));

            return new SearchResult(locations, implicitLocations, referencedSymbols);
        }
    }
}