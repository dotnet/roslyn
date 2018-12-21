// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal class LinkedFileReferenceFinder : IReferenceFinder
    {
        public async Task<ImmutableArray<SymbolAndProjectId>> DetermineCascadedSymbolsAsync(
            SymbolAndProjectId symbolAndProjectId, Solution solution, IImmutableSet<Project> projects,
            FindReferencesSearchOptions options, CancellationToken cancellationToken)
        {
            var linkedSymbols = new HashSet<SymbolAndProjectId>();

            var symbol = symbolAndProjectId.Symbol;
            foreach (var location in symbol.DeclaringSyntaxReferences)
            {
                var originalDocument = solution.GetDocument(location.SyntaxTree);

                // GetDocument will return null for locations in #load'ed trees.
                // TODO:  Remove this check and add logic to fetch the #load'ed tree's
                // Document once https://github.com/dotnet/roslyn/issues/5260 is fixed.
                if (originalDocument == null)
                {
                    Debug.Assert(solution.Workspace.Kind == WorkspaceKind.Interactive || solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles);
                    continue;
                }

                foreach (var linkedDocumentId in originalDocument.GetLinkedDocumentIds())
                {
                    var linkedDocument = solution.GetDocument(linkedDocumentId);
                    var linkedSyntaxRoot = await linkedDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

                    // Defend against constructed solutions with inconsistent linked documents
                    if (!linkedSyntaxRoot.FullSpan.Contains(location.Span))
                    {
                        continue;
                    }

                    var linkedNode = linkedSyntaxRoot.FindNode(location.Span, getInnermostNodeForTie: true);

                    var semanticModel = await linkedDocument.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    var linkedSymbol = semanticModel.GetDeclaredSymbol(linkedNode, cancellationToken);

                    if (linkedSymbol != null &&
                        linkedSymbol.Kind == symbol.Kind &&
                        linkedSymbol.Name == symbol.Name)
                    {
                        var linkedSymbolAndProjectId = SymbolAndProjectId.Create(linkedSymbol, linkedDocument.Project.Id);
                        if (!linkedSymbols.Contains(linkedSymbolAndProjectId))
                        {
                            linkedSymbols.Add(linkedSymbolAndProjectId);
                        }
                    }
                }
            }

            return linkedSymbols.ToImmutableArray();
        }

        public Task<ImmutableArray<Document>> DetermineDocumentsToSearchAsync(
            ISymbol symbol, Project project, IImmutableSet<Document> documents,
            FindReferencesSearchOptions options, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyImmutableArray<Document>();
        }

        public Task<ImmutableArray<Project>> DetermineProjectsToSearchAsync(ISymbol symbol, Solution solution, IImmutableSet<Project> projects = null, CancellationToken cancellationToken = default)
        {
            return SpecializedTasks.EmptyImmutableArray<Project>();
        }

        public Task<ImmutableArray<FinderLocation>> FindReferencesInDocumentAsync(
            SymbolAndProjectId symbolAndProjectId, Document document, SemanticModel semanticModel,
            FindReferencesSearchOptions options, CancellationToken cancellationToken)
        {
            return SpecializedTasks.EmptyImmutableArray<FinderLocation>();
        }
    }
}
