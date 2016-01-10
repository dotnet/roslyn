// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Nuget;
using Microsoft.CodeAnalysis.Shared.Extensions;
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

            public override string GetDescription(SemanticModel semanticModel, SyntaxNode node)
            {
                return provider.GetDescription(SymbolResult.Symbol, semanticModel, node);
            }

            public override async Task<ImmutableArray<CodeActionOperation>> GetOperationsAsync(
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

            private void ReplaceNameNode(
                ref SyntaxNode contextNode, ref Document document, CancellationToken cancellationToken)
            {
                var desiredName = this.SearchResult.DesiredName;
                if (!string.IsNullOrEmpty(this.SearchResult.DesiredName))
                {
                    var nameNode = this.SearchResult.NameNode;

                    if (nameNode != null)
                    {
                        var identifier = nameNode.GetFirstToken();
                        if (identifier.ValueText != desiredName)
                        {
                            var generator = SyntaxGenerator.GetGenerator(document);
                            var newIdentifier = generator.IdentifierName(desiredName).GetFirstToken().WithTriviaFrom(identifier);
                            var annotation = new SyntaxAnnotation();

                            var root = contextNode.SyntaxTree.GetRoot(cancellationToken);
                            root = root.ReplaceToken(identifier, newIdentifier.WithAdditionalAnnotations(annotation));
                            document = document.WithSyntaxRoot(root);
                            contextNode = root.GetAnnotatedTokens(annotation).First().Parent;
                        }
                    }
                }
            }

            protected abstract Solution UpdateSolution(Document newDocument);
        }
    }
}