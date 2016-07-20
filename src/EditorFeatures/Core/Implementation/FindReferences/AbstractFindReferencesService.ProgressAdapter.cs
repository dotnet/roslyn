using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Navigation;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.FindReferences
{
    internal abstract partial class AbstractFindReferencesService
    {
        /// <summary>
        /// Forwards IFindReferencesProgress calls to a FindRefrencesContext instance.
        /// </summary>
        private class ProgressAdapter : IFindReferencesProgress
        {
            private readonly Solution _solution;
            private readonly FindReferencesContext _context;

            private readonly ConcurrentDictionary<ISymbol, INavigableItem> _symbolToNavigableItem =
                new ConcurrentDictionary<ISymbol, INavigableItem>(SymbolEquivalenceComparer.Instance);

            private readonly Func<ISymbol, INavigableItem> _navigableItemFactory;

            public ProgressAdapter(Solution solution, FindReferencesContext context)
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