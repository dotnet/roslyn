// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Classification;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindUsages;
using Microsoft.CodeAnalysis.Navigation;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindUsages;

internal abstract partial class AbstractFindUsagesService
{
    /// <summary>
    /// Forwards <see cref="IStreamingFindLiteralReferencesProgress"/> calls to an
    /// <see cref="IFindUsagesContext"/> instance.
    /// </summary>
    private sealed class FindLiteralsProgressAdapter(
        IFindUsagesContext context, OptionsProvider<ClassificationOptions> classificationOptions, DefinitionItem definition) : IStreamingFindLiteralReferencesProgress
    {
        private readonly IFindUsagesContext _context = context;
        private readonly DefinitionItem _definition = definition;

        public IStreamingProgressTracker ProgressTracker
            => _context.ProgressTracker;

        public async ValueTask OnReferenceFoundAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var options = await classificationOptions.GetOptionsAsync(document.Project.Services, cancellationToken).ConfigureAwait(false);

            var documentSpan = new DocumentSpan(document, span);
            var classifiedSpans = await ClassifiedSpansAndHighlightSpanFactory.ClassifyAsync(
                documentSpan, classifiedSpans: null, options, cancellationToken).ConfigureAwait(false);

            await _context.OnReferenceFoundAsync(
                new SourceReferenceItem(_definition, documentSpan, classifiedSpans, SymbolUsageInfo.None), cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Forwards IFindReferencesProgress calls to an IFindUsagesContext instance.
    /// </summary>
    private sealed class FindReferencesProgressAdapter(
        Solution solution, IFindUsagesContext context, FindReferencesSearchOptions searchOptions, OptionsProvider<ClassificationOptions> classificationOptions) : IStreamingFindReferencesProgress
    {
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
        private readonly Dictionary<SymbolGroup, DefinitionItem> _definitionToItem = [];

        private readonly SemaphoreSlim _gate = new(initialCount: 1);

        public IStreamingProgressTracker ProgressTracker
            => context.ProgressTracker;

        // Do nothing functions.  The streaming far service doesn't care about
        // any of these.
        public ValueTask OnStartedAsync(CancellationToken cancellationToken) => default;
        public ValueTask OnCompletedAsync(CancellationToken cancellationToken) => default;
        public ValueTask OnFindInDocumentStartedAsync(Document document, CancellationToken cancellationToken) => default;
        public ValueTask OnFindInDocumentCompletedAsync(Document document, CancellationToken cancellationToken) => default;

        // More complicated forwarding functions.  These need to map from the symbols
        // used by the FAR engine to the INavigableItems used by the streaming FAR 
        // feature.

        private async ValueTask<DefinitionItem> GetDefinitionItemAsync(SymbolGroup group, CancellationToken cancellationToken)
        {
            using (await _gate.DisposableWaitAsync(cancellationToken).ConfigureAwait(false))
            {
                if (!_definitionToItem.TryGetValue(group, out var definitionItem))
                {
                    definitionItem = await group.ToClassifiedDefinitionItemAsync(
                        classificationOptions,
                        solution,
                        searchOptions,
                        isPrimary: _definitionToItem.Count == 0,
                        includeHiddenLocations: false,
                        cancellationToken).ConfigureAwait(false);

                    _definitionToItem[group] = definitionItem;
                }

                return definitionItem;
            }
        }

        public async ValueTask OnDefinitionFoundAsync(SymbolGroup group, CancellationToken cancellationToken)
        {
            var definitionItem = await GetDefinitionItemAsync(group, cancellationToken).ConfigureAwait(false);
            await context.OnDefinitionFoundAsync(definitionItem, cancellationToken).ConfigureAwait(false);
        }

        public async ValueTask OnReferenceFoundAsync(SymbolGroup group, ISymbol definition, ReferenceLocation location, CancellationToken cancellationToken)
        {
            var definitionItem = await GetDefinitionItemAsync(group, cancellationToken).ConfigureAwait(false);
            var referenceItem = await location.TryCreateSourceReferenceItemAsync(
                classificationOptions,
                definitionItem,
                includeHiddenLocations: false,
                cancellationToken).ConfigureAwait(false);

            if (referenceItem != null)
                await context.OnReferenceFoundAsync(referenceItem, cancellationToken).ConfigureAwait(false);
        }
    }
}
