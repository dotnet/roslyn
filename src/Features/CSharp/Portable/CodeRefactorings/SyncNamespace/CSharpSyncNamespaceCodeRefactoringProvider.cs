// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.SyncNamespace;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.SyncNamespace
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.SyncNamespace), Shared]
    internal sealed class CSharpSyncNamespaceCodeRefactoringProvider :
        AbstractSyncNamespaceCodeRefactoringProvider<CSharpSyncNamespaceCodeRefactoringProvider, NamespaceDeclarationSyntax, CompilationUnitSyntax>
    {
        protected override NamespaceDeclarationSyntax ChangeNamespace(NamespaceDeclarationSyntax node, ImmutableArray<string> namespaceParts)
        {
            return node.WithName(CreateNameSyntax(namespaceParts, aliasQualifier: null, namespaceParts.Length - 1).WithTriviaFrom(node.Name));
        }  

        protected override ImmutableArray<ISymbol> GetDeclaredSymbols(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
        {
            SyntaxList<MemberDeclarationSyntax> declarations;

            if (node is NamespaceDeclarationSyntax namespaceDecl)
            {
                declarations = namespaceDecl.Members;
            }
            else if (node is CompilationUnitSyntax compilationUnit)
            {
                declarations = compilationUnit.Members;
            }
            else
            {
                return default;
            }

            var builder = ArrayBuilder<ISymbol>.GetInstance();
            foreach (var declaration in declarations)
            {
                var symbol = semanticModel.GetDeclaredSymbol(declaration, cancellationToken);
                builder.AddIfNotNull(symbol);
            }
            return builder.ToImmutableAndFree();
        }

        private static NameSyntax GetQualifiedNameSyntax(NameSyntax node) 
            => node.Parent is QualifiedNameSyntax qualifiedName && qualifiedName.Right == node ? qualifiedName : node;

        protected override bool TryGetReplacementSyntax(SyntaxNode reference, ImmutableArray<string> namespaceParts, out SyntaxNode old, out SyntaxNode @new)
        {
            if (reference is SimpleNameSyntax nameRef)
            {
                var outerMostNode = GetQualifiedNameSyntax(nameRef);
                old = outerMostNode;

                // If no namespace is specified, we just find and return the full NameSyntax.
                if (outerMostNode == nameRef || namespaceParts.IsDefault)
                {                        
                    @new = outerMostNode;  
                }
                else
                {
                    var aliasQualifier = GetAliasQualifierOpt(outerMostNode);
                    var qualifiedNamespaceName = CreateNameSyntax(namespaceParts, aliasQualifier, namespaceParts.Length - 1);

                    // We might lose some trivia associated with nodes within `outerMostNode`.  
                    @new = SyntaxFactory.QualifiedName(qualifiedNamespaceName, nameRef).WithTriviaFrom(outerMostNode);
                } 
                return true;
            }
            else
            {
                old = @new = null;
                return false;
            } 
        }

        protected override SyntaxNode CreateUsingDirective(ImmutableArray<string> namespaceParts)
        {
            var name = CreateNameSyntax(namespaceParts, aliasQualifier: null, namespaceParts.Length - 1);
            return SyntaxFactory.UsingDirective(alias: null, name: name);
        }

        protected override string EscapeIdentifier(string identifier)
            => identifier?.EscapeIdentifier();

        private static string GetAliasQualifierOpt(NameSyntax name)
        {                          
            while (true)
            {
                switch (name.Kind())
                {
                    case SyntaxKind.QualifiedName:
                        name = ((QualifiedNameSyntax)name).Left;
                        continue;
                    case SyntaxKind.AliasQualifiedName:
                        return ((AliasQualifiedNameSyntax)name).Alias.Identifier.ValueText;
                }

                return null;
            }
        }
        
        private NameSyntax CreateNameSyntax(ImmutableArray<string> namespaceParts, string aliasQualifier, int index)
        {
            var part = namespaceParts[index].EscapeIdentifier();
            var namePiece = SyntaxFactory.IdentifierName(part);

            if (index == 0)
            {
                return aliasQualifier == null ? (NameSyntax)namePiece : SyntaxFactory.AliasQualifiedName(aliasQualifier, namePiece);
            }
            else
            {
                return SyntaxFactory.QualifiedName(CreateNameSyntax(namespaceParts, aliasQualifier, index - 1), namePiece);
            }                                                                              
        }

        protected override bool ShouldPositionTriggerRefactoring(SyntaxNode root, int position, out NamespaceDeclarationSyntax namespaceDeclaration)
        {
            namespaceDeclaration = null;
            var namespaceDeclarationCount = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().Count();

            if (namespaceDeclarationCount == 1)
            {
                var token = root.FindToken(position);
                if (token.Kind() == SyntaxKind.EndOfFileToken)
                {
                    token = token.GetPreviousToken();
                }
                // Should trigger if cursor is on the name of only namespace declaration in this document.
                namespaceDeclaration = token.GetAncestor<NamespaceDeclarationSyntax>();
                return namespaceDeclaration != null && namespaceDeclaration.Name.Span.IntersectsWith(position);
            }

            if (namespaceDeclarationCount == 0 && root is CompilationUnitSyntax compilationUnit)
            {
                // Should trigger if cursor is on the name of first member declaration in the 
                // compilation unit when there's no namespace decalration in the document.
                var firstMemberDeclaration = compilationUnit.Members.FirstOrDefault();
                return firstMemberDeclaration != null && firstMemberDeclaration.GetNameToken().Span.IntersectsWith(position);
            }

            return false;
        }
    }
}
