// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.ValueTracking
{
    [ExportWorkspaceService(typeof(IValueTrackingService)), Shared]
    internal class ValueTrackingService : IValueTrackingService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public ValueTrackingService()
        {
        }

        public async Task<ImmutableArray<ValueTrackedItem>> TrackValueSourceAsync(
            Solution solution,
            ISymbol symbol,
            CancellationToken cancellationToken)
        {
            var progressTracker = new ValueTrackingProgressCollector();
            await TrackValueSourceAsync(solution, symbol, progressTracker, cancellationToken).ConfigureAwait(false);
            return progressTracker.GetItems();
        }

        public async Task TrackValueSourceAsync(
            Solution solution,
            ISymbol symbol,
            ValueTrackingProgressCollector progressCollector,
            CancellationToken cancellationToken)
        {
            if (symbol
                is IPropertySymbol
                or IFieldSymbol
                or ILocalSymbol
                or IParameterSymbol)
            {
                // Add all initializations of the symbol. Those are not caught in 
                // the reference finder but should still show up in the tree
                foreach (var syntaxRef in symbol.DeclaringSyntaxReferences)
                {
                    var location = Location.Create(syntaxRef.SyntaxTree, syntaxRef.Span);
                    var item = await ValueTrackedItem.TryCreateAsync(solution, location, symbol, parent: null, cancellationToken).ConfigureAwait(false);
                    if (item is not null)
                    {
                        progressCollector.Report(item);
                    }
                }

                var findReferenceProgressCollector = new FindReferencesProgress(progressCollector);
                await SymbolFinder.FindReferencesAsync(
                    symbol, solution, findReferenceProgressCollector,
                    documents: null, FindReferencesSearchOptions.Default, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<ImmutableArray<ValueTrackedItem>> TrackValueSourceAsync(
            Solution solution,
            ValueTrackedItem previousTrackedItem,
            CancellationToken cancellationToken)
        {
            var progressTracker = new ValueTrackingProgressCollector();
            await TrackValueSourceAsync(solution, previousTrackedItem, progressTracker, cancellationToken).ConfigureAwait(false);
            return progressTracker.GetItems();
        }

        public Task TrackValueSourceAsync(
            Solution solution,
            ValueTrackedItem previousTrackedItem,
            ValueTrackingProgressCollector progressCollector,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private class FindReferencesProgress : IStreamingFindReferencesProgress, IStreamingProgressTracker
        {
            private readonly ValueTrackingProgressCollector _valueTrackingProgressCollector;
            public FindReferencesProgress(ValueTrackingProgressCollector valueTrackingProgressCollector)
            {
                _valueTrackingProgressCollector = valueTrackingProgressCollector;
            }

            public IStreamingProgressTracker ProgressTracker => this;

            public ValueTask AddItemsAsync(int count) => new();

            public ValueTask ItemCompletedAsync() => new();

            public ValueTask OnCompletedAsync() => new();

            public ValueTask OnDefinitionFoundAsync(ISymbol symbol) => new();

            public ValueTask OnFindInDocumentCompletedAsync(Document document) => new();

            public ValueTask OnFindInDocumentStartedAsync(Document document) => new();

            public async ValueTask OnReferenceFoundAsync(ISymbol symbol, ReferenceLocation location)
            {
                if (location.IsWrittenTo)
                {
                    var solution = location.Document.Project.Solution;
                    var item = await ValueTrackedItem.TryCreateAsync(solution, location.Location, symbol, parent: null, CancellationToken.None).ConfigureAwait(false);
                    if (item is not null)
                    {
                        _valueTrackingProgressCollector.Report(item);
                    }
                }
            }

            public ValueTask OnStartedAsync() => new();
        }
    }
}
