// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider<TSimpleNameSyntax>
    {
        private abstract partial class SymbolReference : Reference
        {
            public readonly SymbolResult<INamespaceOrTypeSymbol> SymbolResult;

            public SymbolReference(AbstractAddImportCodeFixProvider<TSimpleNameSyntax> provider, SymbolResult<INamespaceOrTypeSymbol> symbolResult)
                : base(provider, new SearchResult(symbolResult))
            {
                this.SymbolResult = symbolResult;
            }

            protected abstract Glyph? GetGlyph(Document document);
            protected abstract bool CheckForExistingImport(Project project);

            public override bool Equals(object obj)
            {
                var equals = base.Equals(obj);
                if (!equals)
                {
                    return false;
                }

                var name1 = this.SymbolResult.DesiredName;
                var name2 = (obj as SymbolReference)?.SymbolResult.DesiredName;
                return StringComparer.Ordinal.Equals(name1, name2);
            }

            public override int GetHashCode()
            {
                return Hash.Combine(this.SymbolResult.DesiredName, base.GetHashCode());
            }

            private async Task<CodeActionOperation> GetOperationAsync(
                Document document, SyntaxNode node, bool placeSystemNamespaceFirst,
                CancellationToken cancellationToken)
            {
                var newDocument = await UpdateDocumentAsync(document, node, placeSystemNamespaceFirst, cancellationToken).ConfigureAwait(false);
                var updatedSolution = GetUpdatedSolution(newDocument);

                var operation = new ApplyChangesOperation(updatedSolution);
                return operation;
            }

            protected virtual Solution GetUpdatedSolution(Document newDocument)
                => newDocument.Project.Solution;

            private async Task<Document> UpdateDocumentAsync(
                Document document, SyntaxNode contextNode, bool placeSystemNamespaceFirst, CancellationToken cancellationToken)
            {
                ReplaceNameNode(ref contextNode, ref document, cancellationToken);

                // Defer to the language to add the actual import/using.
                var newDocument = await provider.AddImportAsync(contextNode,
                    this.SymbolResult.Symbol, document,
                    placeSystemNamespaceFirst, cancellationToken).ConfigureAwait(false);

                return newDocument;
            }

            public override async Task<CodeAction> CreateCodeActionAsync(
                Document document, SyntaxNode node, bool placeSystemNamespaceFirst, CancellationToken cancellationToken)
            {
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                string description = TryGetDescription(document.Project, node, semanticModel);
                if (description == null)
                {
                    return null;
                }

                if (!this.SearchResult.DesiredNameMatchesSourceName(document))
                {
                    // The name is going to change.  Make it clear in the description that 
                    // this is going to happen.
                    description = $"{this.SearchResult.DesiredName} - {description}";
                }

                var getOperation = new AsyncLazy<CodeActionOperation>(
                    c => this.GetOperationAsync(document, node, placeSystemNamespaceFirst, c),
                    cacheResult: true);

                return new SymbolReferenceCodeAction(
                    description, GetGlyph(document), GetPriority(document),
                    getOperation,
                    this.GetIsApplicableCheck(document.Project));
            }

            protected abstract CodeActionPriority GetPriority(Document document);

            protected virtual Func<Workspace, bool> GetIsApplicableCheck(Project project)
            {
                return null;
            }

            protected virtual string TryGetDescription(
                Project project, SyntaxNode node, SemanticModel semanticModel)
            {
                return provider.TryGetDescription(SymbolResult.Symbol, semanticModel, node, this.CheckForExistingImport(project));
            }
        }
    }
}