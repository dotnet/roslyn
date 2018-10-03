// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.SyncNamespace;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.SyncNamespace
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.SyncNamespace), Shared]
    internal sealed class CSharpSyncNamespaceCodeRefactoringProvider :
        AbstractSyncNamespaceCodeRefactoringProvider<CSharpSyncNamespaceCodeRefactoringProvider, NamespaceDeclarationSyntax, CompilationUnitSyntax>
    {
        protected override ImmutableArray<ISymbol> GetDeclaredSymbolsInContainer(SemanticModel semanticModel, SyntaxNode node, CancellationToken cancellationToken)
        {
            var declarations = GetMemberDeclarationsInContainer(node);
            var builder = ArrayBuilder<ISymbol>.GetInstance();
            foreach (var declaration in declarations)
            {
                var symbol = semanticModel.GetDeclaredSymbol(declaration, cancellationToken);
                builder.AddIfNotNull(symbol);
            }
            return builder.ToImmutableAndFree();
        }

        private IReadOnlyList<MemberDeclarationSyntax> GetMemberDeclarationsInContainer(SyntaxNode node)
        {
            if (node is NamespaceDeclarationSyntax namespaceDecl)
            {
                return namespaceDecl.Members;
            }
            else if (node is CompilationUnitSyntax compilationUnit)
            {
                return compilationUnit.Members;
            }
            else
            {
                return default;
            }
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

        protected override async Task<(bool, NamespaceDeclarationSyntax)> ShouldPositionTriggerRefactoringAsync(Document document, int position, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            
            var namespaceDeclarationCount = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().Count();

            if (namespaceDeclarationCount == 1)
            {
                var token = root.FindToken(position);
                if (token.Kind() == SyntaxKind.EndOfFileToken)
                {
                    token = token.GetPreviousToken();
                }
                // Should trigger if cursor is on the name of only namespace declaration in this document.
                var namespaceDeclaration = token.GetAncestor<NamespaceDeclarationSyntax>();

                var shouldTrigger = namespaceDeclaration != null 
                    && namespaceDeclaration.Name.Span.IntersectsWith(position) 
                    && namespaceDeclaration.GetDiagnostics().All(diag => diag.Severity != DiagnosticSeverity.Error)
                    && !ContainsPartialDeclaration(document, namespaceDeclaration, cancellationToken);
                return (shouldTrigger, shouldTrigger ? namespaceDeclaration : null);
            }

            if (namespaceDeclarationCount == 0 && root is CompilationUnitSyntax compilationUnit)
            {
                // Should trigger if cursor is on the name of first member declaration in the 
                // compilation unit when there's no namespace decalration in the document.
                var firstMemberDeclaration = compilationUnit.Members.FirstOrDefault();
                return (firstMemberDeclaration != null 
                    && firstMemberDeclaration.GetNameToken().Span.IntersectsWith(position) 
                    && !ContainsPartialDeclaration(document, compilationUnit, cancellationToken), null);
            }

            return (false, null);
        }

        private bool ContainsPartialDeclaration(Document document, SyntaxNode node, CancellationToken cancellationToken = default)
        {
            // This is just a quick check for `partial` keyword.
            var memberDeclarations = GetMemberDeclarationsInContainer(node);
            foreach (TypeDeclarationSyntax declaration in memberDeclarations.Where(decl => decl is TypeDeclarationSyntax))
            {
                if (declaration.Modifiers.Any(token => token.IsKind(SyntaxKind.PartialKeyword)))
                {
                    return true;
                }
            }
            return false;
        }

        protected override SyntaxNode ChangeNamespaceDeclaration(SyntaxNode root, ImmutableArray<string> declaredNamespaceParts, ImmutableArray<string> targetNamespaceParts)
        {
            if (declaredNamespaceParts.IsDefault || targetNamespaceParts.IsDefault || !(root is CompilationUnitSyntax compilationUnit))
            {
                return root;
            }

            //TODO: handle trivia

            // Move everything from global namespace to a namespace declaration
            if (declaredNamespaceParts.Length == 1 && declaredNamespaceParts[0].Length == 0)
            {
                var targetNamespaceDecl = SyntaxFactory.NamespaceDeclaration(
                    name: CreateNameSyntax(targetNamespaceParts, aliasQualifier: null, targetNamespaceParts.Length - 1),
                    externs: default, usings: default,
                    members: compilationUnit.Members);
                return compilationUnit.WithMembers(new SyntaxList<MemberDeclarationSyntax>(targetNamespaceDecl))
                    .WithAdditionalAnnotations(Formatter.Annotation);
            }

            // We should have a single member which is a namespace declaration in this compilation unit.
            var namespaceDeclaration = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().Single();

            // Move everything to global namespace
            if (targetNamespaceParts.Length == 1 && targetNamespaceParts[0].Length == 0)
            {
                return compilationUnit.Update(
                    compilationUnit.Externs.AddRange(namespaceDeclaration.Externs), 
                    compilationUnit.Usings.AddRange(namespaceDeclaration.Usings), 
                    compilationUnit.AttributeLists, 
                    namespaceDeclaration.Members,
                    compilationUnit.EndOfFileToken).WithAdditionalAnnotations(Formatter.Annotation);
            }
            // Change namespace name
            else
            {
                return root.ReplaceNode(namespaceDeclaration, 
                    namespaceDeclaration.WithName(
                        CreateNameSyntax(targetNamespaceParts, aliasQualifier: null, targetNamespaceParts.Length - 1)
                        .WithTriviaFrom(namespaceDeclaration.Name)));
            }
        }
    }
}
