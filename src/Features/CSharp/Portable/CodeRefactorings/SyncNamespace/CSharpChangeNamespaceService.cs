// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.ChangeNamespace;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ChangeNamespace
{
    [ExportLanguageService(typeof(IChangeNamespaceService), LanguageNames.CSharp), Shared]
    internal sealed class CSharpChangeNamespaceService :
        AbstractChangeNamespaceService<NamespaceDeclarationSyntax, CompilationUnitSyntax, MemberDeclarationSyntax>
    {
        protected override SyntaxList<MemberDeclarationSyntax> GetMemberDeclarationsInContainer(SyntaxNode compilationUnitOrNamespaceDecl)
        {
            if (compilationUnitOrNamespaceDecl is NamespaceDeclarationSyntax namespaceDecl)
            {
                return namespaceDecl.Members;
            }
            else if (compilationUnitOrNamespaceDecl is CompilationUnitSyntax compilationUnit)
            {
                return compilationUnit.Members;
            }

            throw ExceptionUtilities.Unreachable;
        }

        /// <summary>
        /// Try to get a new node to replace given node, which is a reference to a top-level type declared inside the namespce to be changed.
        /// If this reference is the right side of a qualified name, the new node returned would be the entire qualified name. Depends on 
        /// whether <paramref name="newNamespaceParts"/> is provided, the name in the new node might be qualified with this new namespace instead.
        /// </summary>
        /// <param name="reference">A reference to a type declared inside the namespce to be changed, which is calculated based on results from 
        /// `SymbolFinder.FindReferencesAsync`.</param>
        /// <param name="newNamespaceParts">If specified, and the reference is qualified with namespace, the namespace part of original reference 
        /// will be replaced with given namespace in the new node.</param>
        /// <param name="old">The node to be replaced. This might be an ancestor of original reference.</param>
        /// <param name="new">The replacement node.</param>
        public override bool TryGetReplacementReferenceSyntax(
            SyntaxNode reference,
            ImmutableArray<string> newNamespaceParts, 
            ISyntaxFactsService syntaxFacts, 
            out SyntaxNode old, 
            out SyntaxNode @new)
        {
            if (!(reference is SimpleNameSyntax nameRef))
            {
                old = @new = null;
                return false;
            }

            // A few different cases are handled here:
            //
            // 1. When the reference is not qualified (i.e. just a simple name), then there's nothing need to be done.
            //    And both old and new will point to the original reference.
            //
            // 2. When the new namespace is not specified, we don't need to change the qualified part of reference.
            //    Both old and new will point to the qualified reference.
            //
            // 3. When the new namespace is "", i.e. we are moving type referenced by name here to global namespace.
            //    As a result, we need replace qualified reference with the simple name.
            //
            // 4. When the namespace is specified and not "", i.e. we are moving referenced type to a different non-global 
            //    namespace. We need to replace the qualified reference with a new qualified reference (which is qualified 
            //    with new namespace.)
                        
            if (syntaxFacts.IsRightSideOfQualifiedName(nameRef))
            {
                old = nameRef.Parent;
                var aliasQualifier = GetAliasQualifierOpt(old);

                if (IsGlobalNamespace(newNamespaceParts))
                {
                    // If new namespace is "", then name will be declared in global namespace.
                    // We will replace qualified reference with simple name qualified with alias (global if it's not alias qualified)
                    var aliasNode = aliasQualifier?.ToIdentifierName() ?? SyntaxFactory.IdentifierName(SyntaxFactory.Token(SyntaxKind.GlobalKeyword));
                    @new = SyntaxFactory.AliasQualifiedName(aliasNode, nameRef.WithoutTrivia());
                }
                else
                {
                    var qualifiedNamespaceName = CreateNameSyntax(newNamespaceParts, aliasQualifier, newNamespaceParts.Length - 1);
                    @new = SyntaxFactory.QualifiedName(qualifiedNamespaceName, nameRef.WithoutTrivia());
                }

                // We might lose some trivia associated with children of `outerMostNode`.  
                @new = @new.WithTriviaFrom(old);
                return true;
            }

            if (nameRef.Parent is NameMemberCrefSyntax crefName && crefName.Parent is QualifiedCrefSyntax qualifiedCref)
            {
                // This is the case where the reference is the right most part of a qualified name in `cref`.
                // for example, `<see cref="Foo.Baz.Bar"/>` and `<see cref="SomeAlias::Foo.Baz.Bar"/>`. 
                // This is the form of `cref` we need to handle as a spacial case when changing namespace name or
                // changing namespace from non-global to global, other cases in these 2 scenarios can be handled in the 
                // same way we handle non cref references, for example, `<see cref="SomeAlias::Foo"/>` and `<see cref="Foo"/>`.

                var container = qualifiedCref.Container;
                var aliasQualifier = GetAliasQualifierOpt(container);

                if (IsGlobalNamespace(newNamespaceParts))
                {
                    // If new namespace is "", then name will be declared in global namespace.
                    // We will replace entire `QualifiedCrefSyntax` with a `TypeCrefSyntax`, 
                    // which is a alias qualified simple name, similar to the regular case above.

                    old = qualifiedCref;
                    var aliasNode = aliasQualifier?.ToIdentifierName() ?? SyntaxFactory.IdentifierName(SyntaxFactory.Token(SyntaxKind.GlobalKeyword));
                    var aliasQualifiedNode = SyntaxFactory.AliasQualifiedName(aliasNode, nameRef.WithoutTrivia());
                    @new = SyntaxFactory.TypeCref(aliasQualifiedNode);
                }
                else
                {
                    // if the new namespace is not global, then we just need to change the container in `QualifiedCrefSyntax`,
                    // which is just a regular namespace node, no cref node involve here.
                    old = container;
                    @new = CreateNameSyntax(newNamespaceParts, aliasQualifier, newNamespaceParts.Length - 1);
                }

                return true;
            }

            // Simple name reference, nothing to be done. 
            // The name will be resolved by adding proper import.
            old = @new = nameRef;
            return false;
        }

        /// <summary>
        /// Try to change the namespace declaration based on the refacoring rules:
        ///     - if neither declared and target namespace are "" (i.e. global namespace),
        ///     then we try to change the name of the namespace.
        ///     - if declared namespace is "", then we try to move all types declared 
        ///     in global namespace in the document into a new namespace declaration.
        ///     - if target namespace is "", then we try to move all members in declared 
        ///     namespace to global namespace (i.e. remove the namespace declaration).    
        /// </summary>
        protected override CompilationUnitSyntax ChangeNamespaceDeclaration(
            CompilationUnitSyntax compilationUnit, 
            ImmutableArray<string> declaredNamespaceParts, 
            ImmutableArray<string> targetNamespaceParts)
        {
            Debug.Assert(!declaredNamespaceParts.IsDefault && !targetNamespaceParts.IsDefault);

            // Move everything from global namespace to a namespace declaration
            if (IsGlobalNamespace(declaredNamespaceParts))
            {
                var targetNamespaceDecl = SyntaxFactory.NamespaceDeclaration(
                    name: CreateNameSyntax(targetNamespaceParts, aliasQualifier: null, targetNamespaceParts.Length - 1)
                            .WithAdditionalAnnotations(WarningAnnotation),
                    externs: default, 
                    usings: default,
                    members: compilationUnit.Members);
                return compilationUnit.WithMembers(new SyntaxList<MemberDeclarationSyntax>(targetNamespaceDecl));
            }

            // We should have a single member which is a namespace declaration in this compilation unit.
            var namespaceDeclaration = compilationUnit.DescendantNodes().OfType<NamespaceDeclarationSyntax>().Single();

            // Move everything to global namespace
            if (IsGlobalNamespace(targetNamespaceParts))
            {
                var (namespaceOpeningTrivia, namespaceClosingTrivia) = 
                    GetOpeningAndClosingTriviaOfNamespaceDeclaration(namespaceDeclaration);
                var members = namespaceDeclaration.Members;
                var eofToken = compilationUnit.EndOfFileToken
                    .WithAdditionalAnnotations(WarningAnnotation);

                // Try to preserve trivia from original namesapce declaration.
                // If there's any member inside the declaration, we attach them to the 
                // first and last member, otherwise, simply attach all to the EOF token.
                if (members.Count > 0)
                {
                    var first = members.First();
                    var firstWithTrivia = first.WithPrependedLeadingTrivia(namespaceOpeningTrivia);
                    members = members.Replace(first, firstWithTrivia);

                    var last = members.Last();
                    var lastWithTrivia = last.WithAppendedTrailingTrivia(namespaceClosingTrivia);
                    members = members.Replace(last, lastWithTrivia);
                }
                else
                {
                    eofToken = eofToken.WithPrependedLeadingTrivia(
                        namespaceOpeningTrivia.Concat(namespaceClosingTrivia));
                }

                // Moving inner imports out of the namespace declaration can lead to a break in semantics.
                // For example:
                //
                //  namespace A.B.C
                //  {
                //    using D.E.F;
                //  }
                //
                //  The using of D.E.F is looked up iwith in the context of A.B.C first. If it's moved outside,
                //  it may fail to resolve.

                return compilationUnit.Update(
                    compilationUnit.Externs.AddRange(namespaceDeclaration.Externs), 
                    compilationUnit.Usings.AddRange(namespaceDeclaration.Usings), 
                    compilationUnit.AttributeLists, 
                    members,
                    eofToken);
            }

            // Change namespace name
            return compilationUnit.ReplaceNode(namespaceDeclaration, 
                namespaceDeclaration.WithName(
                    CreateNameSyntax(targetNamespaceParts, aliasQualifier: null, targetNamespaceParts.Length - 1)
                        .WithTriviaFrom(namespaceDeclaration.Name)
                        .WithAdditionalAnnotations(WarningAnnotation)));
        }

        private static bool IsGlobalNamespace(ImmutableArray<string> parts)
            => parts.Length == 1 && parts[0].Length == 0;

        private static string GetAliasQualifierOpt(SyntaxNode name)
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
            Debug.Assert(part.Length > 0);

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

        /// <summary>
        /// return trivia attached to namespace declaration. 
        /// Leading trivia of the node and trivia around opening brace, as well as
        /// trivia around closing brace are concatenated together respectively.
        /// </summary>
        private static (ImmutableArray<SyntaxTrivia> openingTrivia, ImmutableArray<SyntaxTrivia> closingTrivia) 
            GetOpeningAndClosingTriviaOfNamespaceDeclaration(NamespaceDeclarationSyntax namespaceDeclaration)
        {
            var openingBuilder = ArrayBuilder<SyntaxTrivia>.GetInstance();
            openingBuilder.AddRange(namespaceDeclaration.GetLeadingTrivia());
            openingBuilder.AddRange(namespaceDeclaration.OpenBraceToken.LeadingTrivia);
            openingBuilder.AddRange(namespaceDeclaration.OpenBraceToken.TrailingTrivia);

            var closingBuilder = ArrayBuilder<SyntaxTrivia>.GetInstance();
            closingBuilder.AddRange(namespaceDeclaration.CloseBraceToken.LeadingTrivia);
            closingBuilder.AddRange(namespaceDeclaration.CloseBraceToken.TrailingTrivia);

            return (openingBuilder.ToImmutableAndFree(), closingBuilder.ToImmutableAndFree());
        }
    }
}
