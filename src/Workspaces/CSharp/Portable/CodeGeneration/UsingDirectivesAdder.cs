// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeGeneration
{
    internal partial class UsingDirectivesAdder : AbstractImportsAdder
    {
        public UsingDirectivesAdder(Document document)
            : base(document)
        {
        }

        protected override SyntaxNode GetImportsContainer(SyntaxNode node)
        {
            return node.GetInnermostNamespaceDeclarationWithUsings() ?? (SyntaxNode)node.GetAncestorOrThis<CompilationUnitSyntax>();
        }

        protected override SyntaxNode GetInnermostNamespaceScope(SyntaxNodeOrToken nodeOrToken)
        {
            var node = nodeOrToken.IsNode ? nodeOrToken.AsNode() : nodeOrToken.Parent;
            return node.GetAncestorOrThis<NamespaceDeclarationSyntax>() ?? (SyntaxNode)node.GetAncestorOrThis<CompilationUnitSyntax>();
        }

        protected override IList<INamespaceSymbol> GetExistingNamespaces(
            SemanticModel semanticModel,
            SyntaxNode contextNode,
            CancellationToken cancellationToken)
        {
            return contextNode is NamespaceDeclarationSyntax
                ? GetExistingNamespaces(semanticModel, (NamespaceDeclarationSyntax)contextNode, cancellationToken)
                : GetExistingNamespaces(semanticModel, (CompilationUnitSyntax)contextNode, cancellationToken);
        }

        private IList<INamespaceSymbol> GetExistingNamespaces(
            SemanticModel semanticModel, NamespaceDeclarationSyntax namespaceDeclaration, CancellationToken cancellationToken)
        {
            var q = from u in namespaceDeclaration.Usings
                    let symbol = semanticModel.GetSymbolInfo(u.Name, cancellationToken).Symbol as INamespaceSymbol
                    where symbol != null && !symbol.IsGlobalNamespace
                    select symbol;

            var usingImports = q.ToList();

            var namespaceSymbol = semanticModel.GetDeclaredSymbol(namespaceDeclaration, cancellationToken) as INamespaceSymbol;
            var namespaceImports = GetContainingNamespacesAndThis(namespaceSymbol).ToList();

            var outerNamespaces = this.GetExistingNamespaces(semanticModel, namespaceDeclaration.Parent, cancellationToken);

            return outerNamespaces.Concat(namespaceImports).Concat(usingImports)
                                  .Distinct()
                                  .OrderBy(INamespaceSymbolExtensions.CompareNamespaces)
                                  .ToList();
        }

        private IList<INamespaceSymbol> GetExistingNamespaces(
            SemanticModel semanticModel, CompilationUnitSyntax compilationUnit, CancellationToken cancellationToken)
        {
            var q = from u in compilationUnit.Usings
                    let symbol = semanticModel.GetSymbolInfo(u.Name, cancellationToken).Symbol as INamespaceSymbol
                    where symbol != null && !symbol.IsGlobalNamespace
                    select symbol;

            return q.ToList();
        }

        public override async Task<Document> AddAsync(
            bool placeSystemNamespaceFirst,
            CodeGenerationOptions options,
            CancellationToken cancellationToken)
        {
            var importsContainerToMissingNamespaces = await DetermineNamespaceToImportAsync(
                options, cancellationToken).ConfigureAwait(false);
            if (importsContainerToMissingNamespaces.Count == 0)
            {
                return this.Document;
            }

            var root = await this.Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var rewriter = new Rewriter(this.Document, importsContainerToMissingNamespaces, options.PlaceSystemNamespaceFirst, cancellationToken);
            var newRoot = rewriter.Visit(root);

            return this.Document.WithSyntaxRoot(newRoot);
        }
    }
}
