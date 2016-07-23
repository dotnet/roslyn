using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Editor.Navigation;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

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

            /// <summary>
            /// We will hear about definition symbols many times while performing FAR.  We'll
            /// here about it first when the FAR engine discovers the symbol, and then for every
            /// reference it finds to the symbol.  However, we only want to create and pass along
            /// a single instance of <see cref="INavigableItem" /> for that definition no matter
            /// how many times we see it.
            /// 
            /// This dictionary allows us to make that mapping once and then keep it around for
            /// all future callbacks.
            /// </summary>
            private readonly ConcurrentDictionary<ISymbol, IList<INavigableItem>> _definitionToNavigableItem =
                new ConcurrentDictionary<ISymbol, IList<INavigableItem>>(SymbolEquivalenceComparer.Instance);

            private readonly Func<ISymbol, IList<INavigableItem>> _navigableItemFactory;

            public ProgressAdapter(Solution solution, FindReferencesContext context)
            {
                _solution = solution;
                _context = context;
                _navigableItemFactory = s =>
                {
                    var taggedParts = s.ToDisplayParts(FindAllReferencesUtilities.DefinitionDisplayFormat)
                                       .ToTaggedText();

                    var definitionLocations = s.GetDefinitionLocationsToShow();
                    return definitionLocations.Select(loc => NavigableItemFactory.GetItemFromSymbolLocation(
                        solution, s, loc, taggedParts)).ToList();
                };
            }

            // Simple context forwarding functions.
            public void OnStarted() => _context.OnStarted();
            public void OnCompleted() => _context.OnCompleted();
            public void ReportProgress(int current, int maximum) => _context.ReportProgress(current, maximum);
            public void OnFindInDocumentStarted(Document document) => _context.OnFindInDocumentStarted(document);
            public void OnFindInDocumentCompleted(Document document) => _context.OnFindInDocumentCompleted(document);

            // More complicated forwarding functions.  These need to map from the symbols
            // used by the FAR engine to the INavigableItems used by the streaming FAR 
            // feature.

            private IList<INavigableItem> GetNavigableItems(ISymbol definition)
            {
                return _definitionToNavigableItem.GetOrAdd(definition, _navigableItemFactory);
            }

            public void OnDefinitionFound(ISymbol definition)
            {
                foreach (var item in GetNavigableItems(definition))
                {
                    _context.OnDefinitionFound(
                        item,
                        definition.ShouldShowWithNoReferenceLocations());
                }
            }

            public void OnReferenceFound(ISymbol definition, ReferenceLocation location)
            {
                _context.OnReferenceFound(
                    GetNavigableItems(definition).First(),
                    NavigableItemFactory.GetItemFromSymbolLocation(
                        _solution, definition, location.Location, displayTaggedParts: null));
            }
        }
    }
}