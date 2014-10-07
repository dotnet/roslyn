// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    /// Holds the ILocations of a symbol that should be renamed, along with the symbol and Solution
    /// for the set.
    /// </summary>
    internal sealed partial class RenameLocationSet
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
        private readonly ISymbol symbol;
        private readonly Solution solution;
        private readonly SearchResult mergedResult;

        // possibly null fields
        private readonly OptionSet optionSet;
        private readonly SearchResult originalSymbolResult;
        private readonly List<SearchResult> overloadsResult;
        private readonly IEnumerable<RenameLocation> stringsResult;
        private readonly IEnumerable<RenameLocation> commentsResult;

        public RenameLocationSet(ISet<RenameLocation> locations, ISymbol symbol, Solution solution, IEnumerable<ISymbol> referencedSymbols, IEnumerable<ReferenceLocation> implicitLocations)
        {
            this.symbol = symbol;
            this.solution = solution;
            this.mergedResult = new SearchResult(locations, implicitLocations, referencedSymbols);
        }

        private RenameLocationSet(ISymbol symbol, Solution solution, OptionSet optionSet, SearchResult originalSymbolResult, List<SearchResult> overloadsResult, IEnumerable<RenameLocation> stringsResult, IEnumerable<RenameLocation> commentsResult)
        {
            this.symbol = symbol;
            this.solution = solution;
            this.optionSet = optionSet;
            this.originalSymbolResult = originalSymbolResult;
            this.overloadsResult = overloadsResult;
            this.stringsResult = stringsResult;
            this.commentsResult = commentsResult;

            var mergedLocations = new HashSet<RenameLocation>();
            var mergedReferencedSymbols = new List<ISymbol>();
            var mergedImplicitLocations = new List<ReferenceLocation>();

            if (optionSet.GetOption(RenameOptions.RenameInStrings))
            {
                mergedLocations.AddRange(stringsResult);
            }

            if (optionSet.GetOption(RenameOptions.RenameInComments))
            {
                mergedLocations.AddRange(commentsResult);
            }

            var overloadsToMerge = (optionSet.GetOption(RenameOptions.RenameOverloads) ? overloadsResult : null) ?? SpecializedCollections.EmptyEnumerable<SearchResult>();
            foreach (var result in overloadsToMerge.Concat(originalSymbolResult))
            {
                mergedLocations.AddRange(result.Locations);
                mergedImplicitLocations.AddRange(result.ImplicitLocations);
                mergedReferencedSymbols.AddRange(result.ReferencedSymbols);
            }

            this.mergedResult = new SearchResult(mergedLocations, mergedImplicitLocations, mergedReferencedSymbols);
        }

        public ISet<RenameLocation> Locations { get { return mergedResult.Locations; } }
        public ISymbol Symbol { get { return symbol; } }
        public Solution Solution { get { return solution; } }
        public IEnumerable<ISymbol> ReferencedSymbols { get { return mergedResult.ReferencedSymbols; } }
        public IEnumerable<ReferenceLocation> ImplicitLocations { get { return mergedResult.ImplicitLocations; } }

        /// <summary>
        /// Find the locations that need to be renamed.
        /// </summary>
        public static async Task<RenameLocationSet> FindAsync(ISymbol symbol, Solution solution, OptionSet optionSet, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(symbol);
            using (Logger.LogBlock(FunctionId.Rename_AllRenameLocations, cancellationToken))
            {
                symbol = await ReferenceProcessing.FindDefinitionSymbolAsync(symbol, solution, cancellationToken).ConfigureAwait(false);
                var originalSymbolResult = await AddLocationsReferenceSymbolsAsync(symbol, solution, cancellationToken).ConfigureAwait(false);
                var intermediateResult = new RenameLocationSet(symbol, solution, optionSet, originalSymbolResult, overloadsResult: null, stringsResult: null, commentsResult: null);

                return await intermediateResult.FindWithUpdatedOptionsAsync(optionSet, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<RenameLocationSet> FindWithUpdatedOptionsAsync(OptionSet optionSet, CancellationToken cancellationToken)
        {
            Contract.ThrowIfNull(this.optionSet, "FindWithUpdatedOptionsAsync can only be called on a result of FindAsync");
            using (Logger.LogBlock(FunctionId.Rename_AllRenameLocations, cancellationToken))
            {
                var overloadsResult = this.overloadsResult ?? (optionSet.GetOption(RenameOptions.RenameOverloads)
                    ? await GetOverloadsAsync(this.symbol, this.solution, cancellationToken).ConfigureAwait(false)
                    : null);

                var stringsAndComments = await ReferenceProcessing.GetRenamableLocationsInStringsAndCommentsAsync(
                    this.symbol,
                    this.solution,
                    this.originalSymbolResult.Locations,
                    optionSet.GetOption(RenameOptions.RenameInStrings) && this.stringsResult == null,
                    optionSet.GetOption(RenameOptions.RenameInComments) && this.commentsResult == null,
                    cancellationToken).ConfigureAwait(false);

                return new RenameLocationSet(this.symbol, this.solution, optionSet, this.originalSymbolResult,
                    this.overloadsResult ?? overloadsResult,
                    this.stringsResult ?? stringsAndComments.Item1,
                    this.commentsResult ?? stringsAndComments.Item2);
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