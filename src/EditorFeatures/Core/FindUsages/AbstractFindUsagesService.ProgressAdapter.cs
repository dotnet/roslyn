// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.FindUsages
{
    internal abstract partial class AbstractFindUsagesService
    {
        /// <summary>
        /// Forwards <see cref="IStreamingFindLiteralReferencesProgress"/> calls to an
        /// <see cref="IFindUsagesContext"/> instance.
        /// </summary>
        private class FindLiteralsProgressAdapter : IStreamingFindLiteralReferencesProgress
        {
            private readonly IFindUsagesContext _context;
            private readonly DefinitionItem _definition;

            public FindLiteralsProgressAdapter(
                IFindUsagesContext context, DefinitionItem definition)
            {
                _context = context;
                _definition = definition;
            }

            public async Task OnReferenceFoundAsync(Document document, TextSpan span)
            {
                var documentSpan = await ClassifiedSpansAndHighlightSpanFactory.GetClassifiedDocumentSpanAsync(
                    document, span, _context.CancellationToken).ConfigureAwait(false);
                await _context.OnReferenceFoundAsync(new SourceReferenceItem(
                    _definition, documentSpan, SymbolUsageInfo.None, additionalProperties: ImmutableArray<AdditionalProperty>.Empty)).ConfigureAwait(false);
            }

            public Task ReportProgressAsync(int current, int maximum)
                => _context.ReportProgressAsync(current, maximum);
        }

        /// <summary>
        /// Forwards IFindReferencesProgress calls to an IFindUsagesContext instance.
        /// </summary>
        private class FindReferencesProgressAdapter : ForegroundThreadAffinitizedObject, IStreamingFindReferencesProgress
        {
            private readonly Solution _solution;
            private readonly IFindUsagesContext _context;
            private readonly FindReferencesSearchOptions _options;

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
            private readonly Dictionary<ISymbol, DefinitionItem> _definitionToItem =
                new Dictionary<ISymbol, DefinitionItem>(MetadataUnifyingEquivalenceComparer.Instance);

            private readonly SemaphoreSlim _gate = new SemaphoreSlim(initialCount: 1);

            public FindReferencesProgressAdapter(
                IThreadingContext threadingContext, Solution solution,
                IFindUsagesContext context, FindReferencesSearchOptions options)
                : base(threadingContext)
            {
                _solution = solution;
                _context = context;
                _options = options;
            }

            // Do nothing functions.  The streaming far service doesn't care about
            // any of these.
            public Task OnStartedAsync() => Task.CompletedTask;
            public Task OnCompletedAsync() => Task.CompletedTask;
            public Task OnFindInDocumentStartedAsync(Document document) => Task.CompletedTask;
            public Task OnFindInDocumentCompletedAsync(Document document) => Task.CompletedTask;

            // Simple context forwarding functions.
            public Task ReportProgressAsync(int current, int maximum) =>
                _context.ReportProgressAsync(current, maximum);

            // More complicated forwarding functions.  These need to map from the symbols
            // used by the FAR engine to the INavigableItems used by the streaming FAR 
            // feature.

            private async Task<DefinitionItem> GetDefinitionItemAsync(SymbolAndProjectId definition)
            {
                using (await _gate.DisposableWaitAsync(_context.CancellationToken).ConfigureAwait(false))
                {
                    if (!_definitionToItem.TryGetValue(definition.Symbol, out var definitionItem))
                    {
                        definitionItem = await definition.Symbol.ToClassifiedDefinitionItemAsync(
                            _solution.GetProject(definition.ProjectId), includeHiddenLocations: false,
                            _options, _context.CancellationToken).ConfigureAwait(false);

                        _definitionToItem[definition.Symbol] = definitionItem;
                    }

                    return definitionItem;
                }
            }

            public async Task OnDefinitionFoundAsync(SymbolAndProjectId definition)
            {
                var definitionItem = await GetDefinitionItemAsync(definition).ConfigureAwait(false);
                await _context.OnDefinitionFoundAsync(definitionItem).ConfigureAwait(false);
            }

            public async Task OnReferenceFoundAsync(SymbolAndProjectId definition, ReferenceLocation location)
            {
                // Ignore duplicate locations.  We don't want to clutter the UI with them.
                if (location.IsDuplicateReferenceLocation)
                {
                    return;
                }

                var definitionItem = await GetDefinitionItemAsync(definition).ConfigureAwait(false);
                var referenceItem = await location.TryCreateSourceReferenceItemAsync(
                    definitionItem, includeHiddenLocations: false,
                    cancellationToken: _context.CancellationToken).ConfigureAwait(false);

                if (referenceItem != null)
                {
                    await _context.OnReferenceFoundAsync(referenceItem).ConfigureAwait(false);
                }
            }
        }
    }
}
