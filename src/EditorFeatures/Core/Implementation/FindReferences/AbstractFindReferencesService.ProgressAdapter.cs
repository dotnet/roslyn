using System;
using System.Collections.Concurrent;
using Microsoft.CodeAnalysis.Editor.Navigation;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.FindReferences;
using Microsoft.CodeAnalysis.FindSymbols;

namespace Microsoft.CodeAnalysis.Editor.Implementation.FindReferences
{
    internal abstract partial class AbstractFindReferencesService
    {
        /// <summary>
        /// Forwards IFindReferencesProgress calls to a FindRefrencesContext instance.
        /// </summary>
        private class ProgressAdapter : ForegroundThreadAffinitizedObject, IFindReferencesProgress
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
            private readonly ConcurrentDictionary<ISymbol, DefinitionItem> _definitionToItem =
                new ConcurrentDictionary<ISymbol, DefinitionItem>(MetadataUnifyingEquivalenceComparer.Instance);

            private readonly Func<ISymbol, DefinitionItem> _definitionFactory;

            public ProgressAdapter(Solution solution, FindReferencesContext context)
            {
                _solution = solution;
                _context = context;
                _definitionFactory = s => s.ToDefinitionItem(solution);
            }

            // Do nothing functions.  The streaming far service doesn't care about
            // any of these.
            public void OnStarted() { }
            public void OnCompleted() { }
            public void OnFindInDocumentStarted(Document document) { }
            public void OnFindInDocumentCompleted(Document document) { }

            // Simple context forwarding functions.
            public void ReportProgress(int current, int maximum) => _context.ReportProgress(current, maximum);

            // More complicated forwarding functions.  These need to map from the symbols
            // used by the FAR engine to the INavigableItems used by the streaming FAR 
            // feature.

            private DefinitionItem GetDefinitionItem(ISymbol definition)
            {
                return _definitionToItem.GetOrAdd(definition, _definitionFactory);
            }

            public void OnDefinitionFound(ISymbol definition)
            {
                _context.OnDefinitionFound(GetDefinitionItem(definition));
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

            public void CallThirdPartyExtensions()
            {
                var factory = _solution.Workspace.Services.GetService<IDefinitionsAndReferencesFactory>();
                foreach (var definition in _definitionToItem.Keys)
                {
                    var item = factory.GetThirdPartyDefinitionItem(_solution, definition);
                    if (item != null)
                    {
                        _context.OnDefinitionFound(item);
                    }
                }
            }
        }
    }
}