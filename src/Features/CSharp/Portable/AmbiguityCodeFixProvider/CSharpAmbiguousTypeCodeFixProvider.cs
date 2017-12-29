// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.AmbiguityCodeFixProvider;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.AmbiguityCodeFixProvider
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AliasType), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.FullyQualify)]
    internal class CSharpAmbiguousTypeCodeFixProvider : AbstractAmbiguousTypeCodeFixProvider
    {
        /// <summary>
        /// 'reference' is an ambiguous reference between 'identifier' and 'identifier'
        /// </summary>
        private const string CS0104 = nameof(CS0104);

        public override ImmutableArray<string> FixableDiagnosticIds
            => ImmutableArray.Create(CS0104);

        protected override SyntaxNode GetAliasDirective(string typeName, ISymbol symbol)
            => SyntaxFactory.UsingDirective(SyntaxFactory.NameEquals(typeName),
                                            SyntaxFactory.IdentifierName(symbol.ToNameDisplayString()));

        protected override async Task<Document> InsertAliasDirective(Document document, SyntaxNode nodeReferencingType, SyntaxNode aliasDirectiveToInsert, CancellationToken cancellationToken)
        {
            Debug.Assert(aliasDirectiveToInsert.IsKind(SyntaxKind.UsingDirective));
            var (parent, usings, withUsings) = GetNearestUsingBlock(nodeReferencingType);
            if (parent != null)
            {
                var newUsingList = InsertNewAliasDirectiveInUsingList((UsingDirectiveSyntax)aliasDirectiveToInsert, usings);
                var newUsingParent = withUsings(newUsingList);
                return await document.ReplaceNodeAsync(parent, newUsingParent, cancellationToken).ConfigureAwait(false);
            }

            return document;
        }

        private static SyntaxList<UsingDirectiveSyntax> InsertNewAliasDirectiveInUsingList(UsingDirectiveSyntax aliasDirectiveToInsert, SyntaxList<UsingDirectiveSyntax> usings)
            => usings.Add(aliasDirectiveToInsert);

        private (SyntaxNode parent, SyntaxList<UsingDirectiveSyntax> usings, Func<SyntaxList<SyntaxNode>, SyntaxNode> withUsingFunc) GetNearestUsingBlock(SyntaxNode nodeReferencingType)
        {
            // Look for the nearest using block that is not empty.
            var node = nodeReferencingType;
            while (node != null)
            {
                switch (node)
                {
                    case NamespaceDeclarationSyntax namespaceDeclaration when namespaceDeclaration.Usings.Count > 0:
                        return (namespaceDeclaration, namespaceDeclaration.Usings, newUsings => namespaceDeclaration.WithUsings(newUsings));
                    case CompilationUnitSyntax compilationUnit:
                        return (compilationUnit, compilationUnit.Usings, newUsings => compilationUnit.WithUsings(newUsings));
                }

                node = node.Parent;
            }

            return default;
        }
    }
}
