// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings.SyncNamespace;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeRefactorings.SyncNamespace
{
    [ExportLanguageService(typeof(ISyncNamespaceService), LanguageNames.CSharp), Shared]
    internal sealed class CSharpSyncNamespaceService :
        AbstractSyncNamespaceService<NamespaceDeclarationSyntax, CompilationUnitSyntax>
    {
        protected override IReadOnlyList<SyntaxNode> GetMemberDeclarationsInContainer(SyntaxNode compilationUnitOrNamespaceDecl)
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

            old = syntaxFacts.IsRightSideOfQualifiedName(nameRef) ? nameRef.Parent : nameRef;

            // If no namespace is specified, we just find and return the full NameSyntax.
            if (old == nameRef || newNamespaceParts.IsDefaultOrEmpty)
            {
                @new = old;
            }
            else
            {
                if (newNamespaceParts.Length == 1 && newNamespaceParts[0].Length == 0)
                {
                    // If new namespace is "", then name will be declared in global namespace.
                    // We will replace qualified reference with simple name + global alias.
                    @new = SyntaxFactory.AliasQualifiedName(
                        SyntaxFactory.IdentifierName(SyntaxFactory.Token(SyntaxKind.GlobalKeyword)), 
                        nameRef.WithoutTrivia());
                }
                else
                {
                    var aliasQualifier = GetAliasQualifierOpt(old);
                    var qualifiedNamespaceName = CreateNameSyntax(newNamespaceParts, aliasQualifier, newNamespaceParts.Length - 1);
                    @new = SyntaxFactory.QualifiedName(qualifiedNamespaceName, nameRef.WithoutTrivia());
                }

                // We might lose some trivia associated with children of `outerMostNode`.  
                @new = @new.WithTriviaFrom(old);
            }
            return true;
        }

        protected override string EscapeIdentifier(string identifier)
            => identifier?.EscapeIdentifier();

        protected override async Task<(bool, NamespaceDeclarationSyntax)> ShouldPositionTriggerRefactoringAsync(
            Document document, 
            int position, 
            CancellationToken cancellationToken)
        {
            var compilationUnit = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) as CompilationUnitSyntax;            
            var namespaceDecls = compilationUnit.DescendantNodes().OfType<NamespaceDeclarationSyntax>().ToImmutableArray();

            // Here's conditions that trigger the refactoring (all have to be true in each scenario):
            // 
            // - There's only one namespace declaration in the document and all types are declared in it:
            //    1. No nested namespace declaration (even it's empty).
            //    2. The cursor is on the name of the namespace declaration.
            //    3. The name of the namespace is valid (i.e. no errors).
            //    4. No partial type declared in the namespace. Otherwise its multiple declaration will
            //       end up in different namespace.
            //
            // - There's no namespace declaration and all types in the document are declared in global namespace:
            //    1. The cursor is on the name of first declared type.
            //    2. No partial type declared in the document. Otherwise its multiple declaration will
            //       end up in different namespace.

            if (namespaceDecls.Length == 1 && compilationUnit.Members.Count == 1)
            {
                var namespaceDeclaration = namespaceDecls.Single();
                Debug.Assert((object)namespaceDeclaration == (object)compilationUnit.Members.Single());

                var shouldTrigger = namespaceDeclaration.Name.Span.IntersectsWith(position) 
                    && namespaceDeclaration.Name.GetDiagnostics().All(diag => diag.DefaultSeverity != DiagnosticSeverity.Error);

                shouldTrigger = shouldTrigger && 
                    !(await ContainsPartialTypeWithMultipleDeclarationsAsync(document, namespaceDeclaration, cancellationToken)
                    .ConfigureAwait(false));

                return (shouldTrigger, shouldTrigger ? namespaceDeclaration : null);
            }

            if (namespaceDecls.Length == 0)
            {
                var firstMemberDeclaration = compilationUnit.Members.FirstOrDefault();

                var shouldTrigger = firstMemberDeclaration != null
                    && firstMemberDeclaration.GetNameToken().Span.IntersectsWith(position);

                shouldTrigger = shouldTrigger &&
                    !(await ContainsPartialTypeWithMultipleDeclarationsAsync(document, compilationUnit, cancellationToken)
                    .ConfigureAwait(false));

                return (shouldTrigger, null);
            }

            return default;
        }

        /// <summary>
        /// Try to change the namespace declaration based on the refacoring rules:
        /// 
        ///     - if neither declared and target namespace are "" (i.e. global namespace),
        ///     then we try to change the name of the namespace.
        ///     - if declared namespace is "", then we try to move all types declared 
        ///     in global namespace in the document into a new namespace declaration.
        ///     - if target namespace is "", then we try to move all members in declared 
        ///     namespace to global namespace (i.e. remove the namespace declaration).    
        /// </summary>
        protected override SyntaxNode ChangeNamespaceDeclaration(
            SyntaxNode root, 
            ImmutableArray<string> declaredNamespaceParts, 
            ImmutableArray<string> targetNamespaceParts)
        {
            if (declaredNamespaceParts.IsDefault || targetNamespaceParts.IsDefault || !(root is CompilationUnitSyntax compilationUnit))
            {
                return root;
            }

            // Move everything from global namespace to a namespace declaration
            if (declaredNamespaceParts.Length == 1 && declaredNamespaceParts[0].Length == 0)
            {
                var targetNamespaceDecl = SyntaxFactory.NamespaceDeclaration(
                    name: CreateNameSyntax(targetNamespaceParts, aliasQualifier: null, targetNamespaceParts.Length - 1)
                    .WithAdditionalAnnotations(WarningAnnotation),
                    externs: default, 
                    usings: default,
                    members: compilationUnit.Members);
                return compilationUnit.WithMembers(new SyntaxList<MemberDeclarationSyntax>(targetNamespaceDecl))
                    .WithAdditionalAnnotations(Formatter.Annotation);
            }

            // We should have a single member which is a namespace declaration in this compilation unit.
            var namespaceDeclaration = root.DescendantNodes().OfType<NamespaceDeclarationSyntax>().Single();

            // Move everything to global namespace
            if (targetNamespaceParts.Length == 1 && targetNamespaceParts[0].Length == 0)
            {
                var triviaFromNamespaceDecl = GetOpeningAndClosingTriviaOfNamespaceDeclaration(namespaceDeclaration);

                var members = namespaceDeclaration.Members;
                var eofToken = compilationUnit.EndOfFileToken
                    .WithAdditionalAnnotations(WarningAnnotation);

                // Try to preserve trivia from original namesapce declaration.
                // If there's any member inside the declaration, we attach them to the 
                // first and last member, otherwise, simply attach all to the EOF token.
                if (members.Count > 0)
                {
                    var first = members.First();
                    var firstWithTrivia = first.WithPrependedLeadingTrivia(triviaFromNamespaceDecl.openingTrivia);
                    members = members.Replace(first, firstWithTrivia);

                    var last = members.Last();
                    var lastWithTrivia = last.WithAppendedTrailingTrivia(triviaFromNamespaceDecl.closingTrivia);
                    members = members.Replace(last, lastWithTrivia);
                }
                else
                {
                    eofToken = eofToken.WithPrependedLeadingTrivia(
                        triviaFromNamespaceDecl.openingTrivia.Concat(triviaFromNamespaceDecl.closingTrivia));
                }

                return compilationUnit.Update(
                    compilationUnit.Externs.AddRange(namespaceDeclaration.Externs), 
                    compilationUnit.Usings.AddRange(namespaceDeclaration.Usings), 
                    compilationUnit.AttributeLists, 
                    members,
                    eofToken).WithAdditionalAnnotations(Formatter.Annotation);
            }

            // Change namespace name
            return root.ReplaceNode(namespaceDeclaration, 
                namespaceDeclaration.WithName(
                    CreateNameSyntax(targetNamespaceParts, aliasQualifier: null, targetNamespaceParts.Length - 1)
                    .WithTriviaFrom(namespaceDeclaration.Name)
                    .WithAdditionalAnnotations(WarningAnnotation, Formatter.Annotation)));
        }

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
        private static (List<SyntaxTrivia> openingTrivia, List<SyntaxTrivia> closingTrivia) 
            GetOpeningAndClosingTriviaOfNamespaceDeclaration(NamespaceDeclarationSyntax namespaceDeclaration)
        {
            var openingTrivia = namespaceDeclaration.GetLeadingTrivia().ToList();
            openingTrivia.AddRange(namespaceDeclaration.OpenBraceToken.LeadingTrivia);
            openingTrivia.AddRange(namespaceDeclaration.OpenBraceToken.TrailingTrivia);

            var closingTrivia = namespaceDeclaration.CloseBraceToken.LeadingTrivia.ToList();
            closingTrivia.AddRange(namespaceDeclaration.CloseBraceToken.TrailingTrivia);

            return (openingTrivia, closingTrivia);
        }
    }
}
