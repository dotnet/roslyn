// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Host;
using Microsoft.CodeAnalysis.Editor.Navigation;
using Microsoft.CodeAnalysis.Editor.SymbolMapping;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.FindReferences
{
    internal abstract class AbstractFindReferencesService : IFindReferencesService
    {
        private readonly IEnumerable<IReferencedSymbolsPresenter> _presenters;

        protected AbstractFindReferencesService(IEnumerable<IReferencedSymbolsPresenter> presenters)
        {
            _presenters = presenters;
        }

        private async Task<Tuple<ISymbol, Solution>> GetRelevantSymbolAndSolutionAtPositionAsync(
            Document document, int position, CancellationToken cancellationToken)
        {
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

        private async Task<Tuple<IEnumerable<ReferencedSymbol>, Solution>> FindReferencedSymbolsAsync(Document document, int position, IWaitContext waitContext)
        {
            var cancellationToken = waitContext.CancellationToken;

            var symbolAndSolution = await GetRelevantSymbolAndSolutionAtPositionAsync(document, position, cancellationToken).ConfigureAwait(false);
            if (symbolAndSolution != null)
            {
                var symbol = symbolAndSolution.Item1;
                var solution = symbolAndSolution.Item2;

                var displayName = symbol.IsConstructor() ? symbol.ContainingType.Name : symbol.Name;

                waitContext.Message = string.Format(EditorFeaturesResources.FindingReferencesOf, displayName);

                var result = await SymbolFinder.FindReferencesAsync(symbol, solution, cancellationToken).ConfigureAwait(false);

                return Tuple.Create(result, solution);
            }

            return null;
        }

        public async Task<IEnumerable<INavigableItem>> FindReferencesAsync(Document document, int position, IWaitContext waitContext)
        {
            var cancellationToken = waitContext.CancellationToken;
            var result = await this.FindReferencedSymbolsAsync(document, position, waitContext).ConfigureAwait(false);
            if (result == null)
            {
                return SpecializedCollections.EmptyEnumerable<INavigableItem>();
            }

            var referencedSymbols = result.Item1;
            var searchSolution = result.Item2;

            var q = from r in referencedSymbols
                    from loc in r.Locations
                    select NavigableItemFactory.GetItemFromSymbolLocation(searchSolution, r.Definition, loc.Location);

            // realize the list here so that the consumer await'ing the result doesn't lazily cause
            // them to be created on an inappropriate thread.
            return q.ToList();
        }

        public bool TryFindReferences(Document document, int position, IWaitContext waitContext)
        {
            var cancellationToken = waitContext.CancellationToken;

            var result = this.FindReferencedSymbolsAsync(document, position, waitContext).WaitAndGetResult(cancellationToken);
            if (result != null && result.Item1 != null)
            {
                var searchSolution = result.Item2;
                foreach (var presenter in _presenters)
                {
                    presenter.DisplayResult(searchSolution, result.Item1);
                    return true;
                }
            }

            return false;
        }

        public async Task FindReferencesAsync(
            Document document, int position, 
            FindReferencesContext context)
        {
            var cancellationToken = context.CancellationToken;
            var symbolAndSolution = await GetRelevantSymbolAndSolutionAtPositionAsync(
                document, position, cancellationToken).ConfigureAwait(false);

            var solution = symbolAndSolution.Item2;
            var result = await SymbolFinder.FindReferencesAsync(
                symbolAndSolution.Item1,
                solution,
                new ProgressWrapper(solution, context),
                documents: null,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        private class ProgressWrapper : IFindReferencesProgress
        {
            private readonly Solution _solution;
            private readonly FindReferencesContext _context;

            private readonly ConcurrentDictionary<ISymbol, INavigableItem> _symbolToNavigableItem =
                new ConcurrentDictionary<ISymbol, INavigableItem>(SymbolEquivalenceComparer.Instance);

            private readonly Func<ISymbol, INavigableItem> _navigableItemFactory;

            public ProgressWrapper(Solution solution, FindReferencesContext context)
            {
                _solution = solution;
                _context = context;
                _navigableItemFactory = s => NavigableItemFactory.GetItemFromSymbolLocation(
                    solution, s, s.Locations.First());
            }

            public void OnStarted() => _context.OnStarted();
            public void OnCompleted() => _context.OnCompleted();

            public void ReportProgress(int current, int maximum) => _context.ReportProgress(current, maximum);

            public void OnFindInDocumentStarted(Document document) => _context.OnFindInDocumentStarted(document);
            public void OnFindInDocumentCompleted(Document document) => _context.OnFindInDocumentCompleted(document);

            private INavigableItem GetNavigableItem(ISymbol symbol)
            {
                return _symbolToNavigableItem.GetOrAdd(symbol, _navigableItemFactory);
            }

            public void OnDefinitionFound(ISymbol symbol)
            {
                _context.OnDefinitionFound(GetNavigableItem(symbol));
            }

            public void OnReferenceFound(ISymbol symbol, ReferenceLocation location)
            {
                _context.OnReferenceFound(
                    GetNavigableItem(symbol),
                    NavigableItemFactory.GetItemFromSymbolLocation(_solution, symbol, location.Location));
            }
        }
    }
}
