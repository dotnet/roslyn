// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host.Mef;

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
                    progressCollector.Add(new ValueTrackedItem(location, symbol));
                }

                var references = await SymbolFinder.FindReferencesAsync(symbol, solution, cancellationToken).ConfigureAwait(false);
                foreach (var refLocation in references.SelectMany(reference => reference.Locations.Where(l => l.IsWrittenTo)))
                {
                    progressCollector.Add(new ValueTrackedItem(refLocation.Location, symbol));
                }
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
            throw new NotImplementedException();
        }
    }
}
