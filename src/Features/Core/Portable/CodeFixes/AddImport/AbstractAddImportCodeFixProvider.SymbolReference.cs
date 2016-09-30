// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.AddImport
{
    internal abstract partial class AbstractAddImportCodeFixProvider<TSimpleNameSyntax>
    {
        private abstract class SymbolReference : Reference
        {
            public readonly SymbolResult<INamespaceOrTypeSymbol> SymbolResult;

            public SymbolReference(AbstractAddImportCodeFixProvider<TSimpleNameSyntax> provider, SymbolResult<INamespaceOrTypeSymbol> symbolResult)
                : base(provider, new SearchResult(symbolResult))
            {
                this.SymbolResult = symbolResult;
            }

            protected abstract Solution UpdateSolution(Document newDocument);
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

            private async Task<ImmutableArray<CodeActionOperation>> GetOperationsAsync(
                Document document, SyntaxNode node, bool placeSystemNamespaceFirst, CancellationToken cancellationToken)
            {
                var newSolution = await UpdateSolutionAsync(document, node, placeSystemNamespaceFirst, cancellationToken).ConfigureAwait(false);
                var operation = new ApplyChangesOperation(newSolution);
                return ImmutableArray.Create<CodeActionOperation>(operation);
            }

            private async Task<Solution> UpdateSolutionAsync(
                Document document, SyntaxNode contextNode, bool placeSystemNamespaceFirst, CancellationToken cancellationToken)
            {
                ReplaceNameNode(ref contextNode, ref document, cancellationToken);

                // Defer to the language to add the actual import/using.
                var newDocument = await provider.AddImportAsync(contextNode,
                    this.SymbolResult.Symbol, document,
                    placeSystemNamespaceFirst, cancellationToken).ConfigureAwait(false);

                return this.UpdateSolution(newDocument);
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

                return new OperationBasedCodeAction(description, GetGlyph(document), GetPriority(document),
                    c => this.GetOperationsAsync(document, node, placeSystemNamespaceFirst, c),
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