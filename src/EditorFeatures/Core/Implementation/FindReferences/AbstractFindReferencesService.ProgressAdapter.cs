// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.FindReferences;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Navigation;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.FindReferences
{
    internal abstract partial class AbstractFindReferencesService
    {
        /// <summary>
        /// Forwards IFindReferencesProgress calls to a FindRefrencesContext instance.
        /// </summary>
        private class ProgressAdapter : ForegroundThreadAffinitizedObject, IStreamingFindReferencesProgress
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
            public Task OnStartedAsync() => SpecializedTasks.EmptyTask;
            public Task OnCompletedAsync() => SpecializedTasks.EmptyTask;
            public Task OnFindInDocumentStartedAsync(Document document) => SpecializedTasks.EmptyTask;
            public Task OnFindInDocumentCompletedAsync(Document document) => SpecializedTasks.EmptyTask;

            // Simple context forwarding functions.
            public Task ReportProgressAsync(int current, int maximum) => 
                _context.ReportProgressAsync(current, maximum);

            // More complicated forwarding functions.  These need to map from the symbols
            // used by the FAR engine to the INavigableItems used by the streaming FAR 
            // feature.

            private DefinitionItem GetDefinitionItem(SymbolAndProjectId definition)
            {
                return _definitionToItem.GetOrAdd(definition.Symbol, _definitionFactory);
            }

            public Task OnDefinitionFoundAsync(SymbolAndProjectId definition)
            {
                return _context.OnDefinitionFoundAsync(GetDefinitionItem(definition));
            }

            public async Task OnReferenceFoundAsync(SymbolAndProjectId definition, ReferenceLocation location)
            {
                // Ignore duplicate locations.  We don't want to clutter the UI with them.
                if (location.IsDuplicateReferenceLocation)
                {
                    return;
                }

                var referenceItem = location.TryCreateSourceReferenceItem(
                    GetDefinitionItem(definition));

                if (referenceItem != null)
                {
                    await _context.OnReferenceFoundAsync(referenceItem).ConfigureAwait(false);
                }
            }

            public async Task CallThirdPartyExtensionsAsync()
            {
                var factory = _solution.Workspace.Services.GetService<IDefinitionsAndReferencesFactory>();
                foreach (var definition in _definitionToItem.Keys)
                {
                    var item = factory.GetThirdPartyDefinitionItem(_solution, definition);
                    if (item != null)
                    {
                        // ConfigureAwait(true) because we want to come back on the 
                        // same thread after calling into extensions.
                        await _context.OnDefinitionFoundAsync(item).ConfigureAwait(true);
                    }
                }
            }
        }
    }
}