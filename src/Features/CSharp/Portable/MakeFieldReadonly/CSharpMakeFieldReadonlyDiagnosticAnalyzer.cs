// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.MakeFieldReadonly;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.MakeFieldReadonly
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class CSharpMakeFieldReadonlyDiagnosticAnalyzer :
        AbstractMakeFieldReadonlyDiagnosticAnalyzer<IdentifierNameSyntax, ConstructorDeclarationSyntax>
    {
        protected override ISyntaxFactsService GetSyntaxFactsService()
            => CSharpSyntaxFactsService.Instance;

        protected override bool IsWrittenTo(IdentifierNameSyntax name, SemanticModel model, CancellationToken cancellationToken)
            => name.IsWrittenTo();

        protected override bool IsMemberOfThisInstance(SyntaxNode node)
        {
            // if it is a qualified name, make sure it is `this.name`
            if (node.Parent is MemberAccessExpressionSyntax memberAccess)
            {
                return memberAccess.Expression is ThisExpressionSyntax;
            }

            // make sure it isn't in an object initializer
            if (node.Parent.Parent is InitializerExpressionSyntax)
            {
                return false;
            }

            return true;
        }

        protected override void AddCandidateTypesInCompilationUnit(SemanticModel semanticModel, SyntaxNode compilationUnit, PooledHashSet<(ITypeSymbol, SyntaxNode)> candidateTypes, CancellationToken cancellationToken)
        {
            foreach (var node in compilationUnit.DescendantNodes(descendIntoChildren: n => IsContainerOrAnalyzableType(n)))
            {
                if (node.IsKind(SyntaxKind.ClassDeclaration, out BaseTypeDeclarationSyntax baseTypeDeclaration) ||
                    node.IsKind(SyntaxKind.StructDeclaration, out baseTypeDeclaration))
                {
                    // Walk to the root to see if the current or any containing type is non-partial
                    for (var current = baseTypeDeclaration; current != null; current = current.Parent as BaseTypeDeclarationSyntax)
                    {
                        if (!current.Modifiers.Any(SyntaxKind.PartialKeyword))
                        {
                            candidateTypes.Add((semanticModel.GetDeclaredSymbol(baseTypeDeclaration, cancellationToken), node));
                            break;
                        }
                    }
                }
            }
        }

        private static bool IsContainerOrAnalyzableType(SyntaxNode node)
        {
            return node.IsKind(SyntaxKind.CompilationUnit, SyntaxKind.NamespaceDeclaration, SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration);
        }
    }
}
