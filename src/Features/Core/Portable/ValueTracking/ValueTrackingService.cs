// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.FindSymbols.Finders;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

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
            Location location,
            ISymbol symbol,
            CancellationToken cancellationToken)
        {
            switch (symbol)
            {
                case IPropertySymbol:
                case ILocalSymbol:
                case IFieldSymbol:
                    {
                        var assignments = await TrackAssignmentsAsync(solution, symbol, cancellationToken).ConfigureAwait(false);
                        return assignments.Select(a => new ValueTrackedItem(a.Location.Location, symbol)).ToImmutableArray();
                    }

                default:
                    return ImmutableArray<ValueTrackedItem>.Empty;
            }
        }

        public async Task<ImmutableArray<ValueTrackedItem>> TrackValueSourceAsync(
            Solution solution,
            ValueTrackedItem previousTrackedItem,
            CancellationToken cancellationToken)
        {
            RoslynDebug.AssertNotNull(previousTrackedItem.Location.SourceTree);

            if (previousTrackedItem.PreviousTrackedItem is null)
            {
                var symbol = previousTrackedItem.Symbol;
                var assignments = await TrackAssignmentsAsync(solution, symbol, cancellationToken).ConfigureAwait(false);
                return assignments.Select(a => new ValueTrackedItem(a.Location.Location, symbol, previousTrackedItem: previousTrackedItem)).ToImmutableArray();
            }

            // There's no interesting node 
            if (previousTrackedItem.ExpressionNode is null)
            {
                return ImmutableArray<ValueTrackedItem>.Empty;
            }

            if (previousTrackedItem.Symbol is IPropertySymbol)
            {
                var document = solution.GetRequiredDocument(previousTrackedItem.Location.SourceTree);
                return await TrackFromPropertyAssignmentAsync(solution, document, previousTrackedItem, cancellationToken).ConfigureAwait(false);
            }

            throw new Exception();
        }

        private static async Task<ImmutableArray<FinderLocation>> TrackAssignmentsAsync(
            Solution solution,
            ISymbol symbol,
            CancellationToken cancellationToken)
        {
            using var _ = PooledObjects.ArrayBuilder<FinderLocation>.GetInstance(out var builder);
            var projectsToSearch = await ReferenceFinders.Property.DetermineProjectsToSearchAsync(symbol, solution, solution.Projects.ToImmutableHashSet(), cancellationToken).ConfigureAwait(false);
            foreach (var project in projectsToSearch)
            {
                var documentsToSearch = await ReferenceFinders.Property.DetermineDocumentsToSearchAsync(
                    symbol,
                    project,
                    project.Documents.ToImmutableHashSet(),
                    FindReferencesSearchOptions.Default,
                    cancellationToken).ConfigureAwait(false);

                foreach (var document in documentsToSearch)
                {
                    var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
                    var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    var referencesInDocument = await ReferenceFinders.Property.FindReferencesInDocumentAsync(
                        symbol,
                        document,
                        semanticModel,
                        FindReferencesSearchOptions.Default,
                        cancellationToken).ConfigureAwait(false);

                    builder.AddRange(referencesInDocument
                        .Where(r => r.Location.IsWrittenTo));
                }
            }

            return builder.AsImmutableOrEmpty();
        }

        private static async Task<ImmutableArray<ValueTrackedItem>> TrackFromPropertyAssignmentAsync(Solution solution, Document document, ValueTrackedItem valueTrackedItem, CancellationToken cancellationToken)
        {
            RoslynDebug.AssertNotNull(valueTrackedItem.ExpressionNode);

            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var symbolInfo = semanticModel.GetSymbolInfo(valueTrackedItem.ExpressionNode);

            if (symbolInfo.Symbol is not null)
            {
                var assignments = await TrackAssignmentsAsync(solution, symbolInfo.Symbol, cancellationToken).ConfigureAwait(false);
                return assignments
                    .Select(a => new ValueTrackedItem(a.Location.Location, symbolInfo.Symbol, previousTrackedItem: valueTrackedItem))
                    .ToImmutableArray();
            }

            return ImmutableArray<ValueTrackedItem>.Empty;
        }
    }
}
