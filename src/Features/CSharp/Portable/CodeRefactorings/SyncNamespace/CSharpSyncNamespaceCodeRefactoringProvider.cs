// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeRefactorings.SyncNamespace;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

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

        protected override ImmutableArray<ISymbol> GetDeclaredSymbols(SyntaxNode node, SemanticModel semanticModel, CancellationToken cancellationToken)
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
        {
            while (true)
            {
                var parent = node.Parent;                
                if (parent is QualifiedNameSyntax qualifiedName && node == qualifiedName.Left)
                {
                    // This may happen if name B is given as aprt of a qualified name A.B.C,  
                    // only A.B is the qualified name for B.
                    break;
                }
                else if (parent is NameSyntax name)
                {
                    node = name;
                }
                else
                {
                    break;
                } 
            }
            return node; 
        }        

        protected override bool TryGetReplacementSyntax(SyntaxNode reference, ImmutableArray<string> namespaceParts, out SyntaxNode old, out SyntaxNode @new)
        {
            if (reference is SimpleNameSyntax nameRef)
            {
                var outerMostNode = GetQualifiedNameSyntax(nameRef);
                old = outerMostNode;

                // If no namespace is specified, we just find and return the full NameSyntax;
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

        // TODO: Copied from src/Compilers/CSharp/Portable/Syntax/NameSyntax.cs 
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

        // TODO: Mostly copied from src/Features/CSharp/Portable/AddImport/CSharpAddImportFeatureService.cs 
        private NameSyntax CreateNameSyntax(ImmutableArray<string> namespaceParts, string aliasQualifier, int index)
        {
            var part = namespaceParts[index];
            if (SyntaxFacts.GetKeywordKind(part) != SyntaxKind.None)
            {
                part = "@" + part;
            }

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
    }
}
