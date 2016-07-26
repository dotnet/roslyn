using System;
using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.Editor.Navigation;
using Microsoft.CodeAnalysis.FindReferences;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Shared.Extensions;

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
            private readonly ConcurrentDictionary<ISymbol, DefinitionItem> _definitionToNavigableItem =
                new ConcurrentDictionary<ISymbol, DefinitionItem>(MetadataUnifyingEquivalenceComparer.Instance);

            private readonly Func<ISymbol, DefinitionItem> _definitionFactory;

            public ProgressAdapter(Solution solution, FindReferencesContext context)
            {
                _solution = solution;
                _context = context;
                _definitionFactory = s => s.ToDefinitionItem(solution);
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

            private DefinitionItem GetDefinitionItem(ISymbol definition)
            {
                return _definitionToNavigableItem.GetOrAdd(definition, _definitionFactory);
            }

            public void OnDefinitionFound(ISymbol definition)
            {
                _context.OnDefinitionFound(
                    GetDefinitionItem(definition),
                    definition.ShouldShowWithNoReferenceLocations());
            }

            public void OnReferenceFound(ISymbol definition, ReferenceLocation location)
            {
                var referenceItem = location.TryCreateSourceReferenceItem(
                    GetDefinitionItem(definition));

                if (referenceItem != null)
                {
                    _context.OnReferenceFound(referenceItem);
                }
            }
        }
    }
}