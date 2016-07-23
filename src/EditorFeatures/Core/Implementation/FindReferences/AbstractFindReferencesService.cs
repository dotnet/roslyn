// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Navigation;
using Microsoft.CodeAnalysis.Editor.SymbolMapping;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.FindReferences
{
    internal abstract partial class AbstractFindReferencesService : IFindReferencesService
    {
        // private readonly IEnumerable<IReferencedSymbolsPresenter> _referenceSymbolPresenters;
        // private readonly IEnumerable<INavigableItemsPresenter> _navigableItemPresenters;
        private readonly IEnumerable<IFindReferencesResultProvider> _externalReferencesProviders;

        protected AbstractFindReferencesService(
            // IEnumerable<IReferencedSymbolsPresenter> referenceSymbolPresenters,
            // IEnumerable<INavigableItemsPresenter> navigableItemPresenters,
            IEnumerable<IFindReferencesResultProvider> externalReferencesProviders)
        {
            // _referenceSymbolPresenters = referenceSymbolPresenters;
            // _navigableItemPresenters = navigableItemPresenters;
            _externalReferencesProviders = externalReferencesProviders;
        }

        private async Task<Tuple<ISymbol, Solution>> GetRelevantSymbolAndSolutionAtPositionAsync(
            Document document, int position, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var symbol = await SymbolFinder.FindSymbolAtPositionAsync(document, position, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (symbol != null)
            {
                // If this document is not in the primary workspace, we may want to search for results
                // in a solution different from the one we started in. Use the starting workspace's
                // ISymbolMappingService to get a context for searching in the proper solution.
                var mappingService = document.Project.Solution.Workspace.Services.GetService<ISymbolMappingService>();

                var mapping = await mappingService.MapSymbolAsync(document, symbol, cancellationToken).ConfigureAwait(false);
                if (mapping != null)
                {
                    return Tuple.Create(mapping.Symbol, mapping.Solution);
                }
            }

            return null;
        }

        /// <summary>
        /// Finds references using the externally defined <see cref="IFindReferencesResultProvider"/>s.
        /// </summary>
        private async Task AddExternalReferencesAsync(Document document, int position, ArrayBuilder<INavigableItem> builder, CancellationToken cancellationToken)
        {
            // CONSIDER: Do the computation in parallel.
            foreach (var provider in _externalReferencesProviders)
            {
                var references = await provider.FindReferencesAsync(document, position, cancellationToken).ConfigureAwait(false);
                if (references != null)
                {
                    builder.AddRange(references.WhereNotNull());
                }
            }
        }

        private async Task<FindReferencesResult?> FindReferencedSymbolsAsync(
            Document document, int position, IWaitContext waitContext)
        {
            var cancellationToken = waitContext.CancellationToken;

            var symbolAndSolution = await GetRelevantSymbolAndSolutionAtPositionAsync(document, position, cancellationToken).ConfigureAwait(false);
            if (symbolAndSolution == null)
            {
                return null;
            }

            var symbol = symbolAndSolution.Item1;
            var solution = symbolAndSolution.Item2;

            var displayName = symbol.IsConstructor() ? symbol.ContainingType.Name : symbol.Name;

            waitContext.Message = string.Format(
                EditorFeaturesResources.Finding_references_of_0, displayName);

            var result = await SymbolFinder.FindReferencesAsync(symbol, solution, cancellationToken).ConfigureAwait(false);

            return new FindReferencesResult(solution, ConvertReferences(result));
        }

        private ImmutableArray<INavigableItem> ConvertReferences(IEnumerable<ReferencedSymbol> result)
        {
            throw new NotImplementedException();
        }

        public async Task<FindReferencesResult?> FindReferencesAsync(
            Document document, int position, IWaitContext waitContext)
        {
            var cancellationToken = waitContext.CancellationToken;
            var workspace = document.Project.Solution.Workspace;

            // First see if we have any external navigable item references.
            // If so, we display the results as navigable items.
            var items = await FindExternalNavigableItemsReferencesAsync(document, position, waitContext).ConfigureAwait(false);
            if (items.Length > 0)
            {
                return new FindReferencesResult(document.Project.Solution, items);
            }

            // Otherwise, fall back to displaying SymbolFinder based references.
            var result = await this.FindReferencedSymbolsAsync(document, position, waitContext).ConfigureAwait(false);
            return result;
        }

        /// <summary>
        /// Attempts to find and display navigable item references, including the references provided by external providers.
        /// </summary>
        /// <returns>False if there are no external references or display was not successful.</returns>
        private async Task<ImmutableArray<INavigableItem>> FindExternalNavigableItemsReferencesAsync(
            Document document, int position, IWaitContext waitContext)
        {
            if (_externalReferencesProviders.Any())
            {
                var cancellationToken = waitContext.CancellationToken;
                var builder = ArrayBuilder<INavigableItem>.GetInstance();
                await AddExternalReferencesAsync(document, position, builder, cancellationToken).ConfigureAwait(false);

                // TODO: Merging references from SymbolFinder and external providers might lead to duplicate or counter-intuitive results.
                // TODO: For now, we avoid merging and just display the results either from SymbolFinder or the external result providers but not both.
                //if (builder.Count > 0 && 
                //    TryDisplayReferences(document.Project.Solution, builder))
                //{
                //    foundReferences = true;
                //}

                return builder.ToImmutableAndFree();
            }

            return ImmutableArray<INavigableItem>.Empty;
        }

        //private bool TryDisplayReferences(Solution solution, IEnumerable<INavigableItem> result)
        //{
        //    if (result != null && result.Any())
        //    {
        //        var title = result.First().DisplayTaggedParts.JoinText();
        //        foreach (var presenter in _navigableItemPresenters)
        //        {
        //            presenter.DisplayResult(solution, title, result);
        //            return true;
        //        }
        //    }

        //    return false;
        //}

        //private bool TryDisplayReferences(Tuple<IEnumerable<ReferencedSymbol>, Solution> result)
        //{
        //    if (result != null && result.Item1 != null)
        //    {
        //        var navigableItems = result.Item1;
        //        var searchSolution = result.Item2;
        //        foreach (var presenter in _navigableItemPresenters)
        //        {
        //            presenter.DisplayResult(searchSolution, result.Item1);
        //            return true;
        //        }
        //    }

        //    return false;
        //}
    }
}