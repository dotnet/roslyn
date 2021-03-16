// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
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
            var referneceFinder = GetReferenceFinder(symbol);
            if (referneceFinder is null)
            {
                return ImmutableArray<ValueTrackedItem>.Empty;
            }

            var assignments = await TrackAssignmentsAsync(solution, symbol, referneceFinder, cancellationToken).ConfigureAwait(false);
            return assignments.Select(l => new ValueTrackedItem(l, symbol)).ToImmutableArray();
        }

        public Task<ImmutableArray<ValueTrackedItem>> TrackValueSourceAsync(
            Solution solution,
            ValueTrackedItem previousTrackedItem,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private static async Task<ImmutableArray<Location>> TrackAssignmentsAsync(
            Solution solution,
            ISymbol symbol,
            IReferenceFinder referenceFinder,
            CancellationToken cancellationToken)
        {
            using var _ = PooledObjects.ArrayBuilder<Location>.GetInstance(out var builder);

            // Add all the references to the symbol
            // that write to it 
            var projectsToSearch = await referenceFinder.DetermineProjectsToSearchAsync(symbol, solution, solution.Projects.ToImmutableHashSet(), cancellationToken).ConfigureAwait(false);
            foreach (var project in projectsToSearch)
            {
                var documentsToSearch = await referenceFinder.DetermineDocumentsToSearchAsync(
                    symbol,
                    project,
                    project.Documents.ToImmutableHashSet(),
                    FindReferencesSearchOptions.Default,
                    cancellationToken).ConfigureAwait(false);

                foreach (var document in documentsToSearch)
                {
                    var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
                    var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    var referencesInDocument = await referenceFinder.FindReferencesInDocumentAsync(
                        symbol,
                        document,
                        semanticModel,
                        FindReferencesSearchOptions.Default,
                        cancellationToken).ConfigureAwait(false);

                    foreach (var reference in referencesInDocument.Where(r => r.Location.IsWrittenTo))
                    {
                        AddAssignment(reference.Node, syntaxFacts);
                    }
                }
            }

            // Add all initializations of the symbol. Those are not caught in 
            // the reference finder but should still show up in the tree
            foreach (var location in symbol.Locations.Where(location => location.IsInSource))
            {
                RoslynDebug.AssertNotNull(location.SourceTree);

                var node = location.FindNode(cancellationToken);
                var document = solution.GetRequiredDocument(location.SourceTree);
                var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();

                AddAssignment(node, syntaxFacts);
            }

            return builder.AsImmutableOrEmpty();

            void AddAssignment(SyntaxNode node, ISyntaxFactsService syntaxFacts)
            {
                if (syntaxFacts.IsDeclaration(node)
                    || syntaxFacts.IsParameter(node))
                {
                    builder.Add(node.GetLocation());
                    return;
                }

                if (syntaxFacts.IsVariableDeclarator(node) && node.Parent is not null)
                {
                    builder.Add(node.Parent.GetLocation());
                    return;
                }

                var assignment = node.FirstAncestorOrSelf<SyntaxNode>(syntaxFacts.IsLeftSideOfAnyAssignment);
                if (assignment is not null && assignment.Parent is not null)
                {
                    builder.Add(assignment.Parent.GetLocation());
                    return;
                }
            }
        }

        private static IReferenceFinder? GetReferenceFinder(ISymbol? symbol)
            => symbol switch
            {
                IPropertySymbol => ReferenceFinders.Property,
                IFieldSymbol => ReferenceFinders.Field,
                ILocalSymbol => ReferenceFinders.Local,
                IParameterSymbol => ReferenceFinders.Parameter,
                _ => null
            };
    }
}
