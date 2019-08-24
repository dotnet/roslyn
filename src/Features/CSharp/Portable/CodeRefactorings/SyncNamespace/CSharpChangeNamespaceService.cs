// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ChangeNamespace;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ChangeNamespace
{
    [ExportLanguageService(typeof(IChangeNamespaceService), LanguageNames.CSharp), Shared]
    internal sealed class CSharpChangeNamespaceService :
        AbstractChangeNamespaceService<NamespaceDeclarationSyntax, CompilationUnitSyntax, MemberDeclarationSyntax>
    {
        [ImportingConstructor]
        public CSharpChangeNamespaceService()
        {
        }

        protected override async Task<ImmutableArray<(DocumentId, SyntaxNode)>> GetValidContainersFromAllLinkedDocumentsAsync(
            Document document,
            SyntaxNode container,
            CancellationToken cancellationToken)
        {
            if (document.Project.FilePath == null
                || document.Project.Solution.Workspace.Kind == WorkspaceKind.MiscellaneousFiles
                || document.IsGeneratedCode(cancellationToken))
            {
                return default;
            }

            TextSpan containerSpan;
            if (container is NamespaceDeclarationSyntax)
            {
                containerSpan = container.Span;
            }
            else if (container is CompilationUnitSyntax)
            {
                // A compilation unit as container means user want to move all its members from global to some namespace.
                // We use an empty span to indicate this case.
                containerSpan = default;
            }
            else
            {
                throw ExceptionUtilities.Unreachable;
            }

            if (!IsSupportedLinkedDocument(document, out var allDocumentIds))
            {
                return default;
            }

            return await TryGetApplicableContainersFromAllDocumentsAsync(document.Project.Solution, allDocumentIds, containerSpan, cancellationToken)
                    .ConfigureAwait(false);
        }

        protected override string GetDeclaredNamespace(SyntaxNode container)
        {
            if (container is CompilationUnitSyntax compilationUnit)
            {
                return string.Empty;
            }

            if (container is NamespaceDeclarationSyntax namespaceDecl)
            {
                return CSharpSyntaxGenerator.Instance.GetName(namespaceDecl);
            }

            throw ExceptionUtilities.Unreachable;
        }

        protected override SyntaxList<MemberDeclarationSyntax> GetMemberDeclarationsInContainer(SyntaxNode container)
        {
            if (container is NamespaceDeclarationSyntax namespaceDecl)
            {
                return namespaceDecl.Members;
            }

            if (container is CompilationUnitSyntax compilationUnit)
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
        /// <param name="oldNode">The node to be replaced. This might be an ancestor of original reference.</param>
        /// <param name="newNode">The replacement node.</param>
        public override bool TryGetReplacementReferenceSyntax(
            SyntaxNode reference,
            ImmutableArray<string> newNamespaceParts,
            ISyntaxFactsService syntaxFacts,
            [NotNullWhen(returnValue: true)] out SyntaxNode? oldNode,
            [NotNullWhen(returnValue: true)] out SyntaxNode? newNode)
        {
            if (!(reference is SimpleNameSyntax nameRef))
            {
                oldNode = newNode = null;
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
            //
            // Note that qualified type name can appear in QualifiedNameSyntax or MemberAccessSyntax, so we need to handle both cases.

            if (syntaxFacts.IsRightSideOfQualifiedName(nameRef))
            {
                oldNode = nameRef.Parent;
                var aliasQualifier = GetAliasQualifier(oldNode);

                if (!TryGetGlobalQualifiedName(newNamespaceParts, nameRef, aliasQualifier, out newNode))
                {
                    var qualifiedNamespaceName = CreateNamespaceAsQualifiedName(newNamespaceParts, aliasQualifier, newNamespaceParts.Length - 1);
                    newNode = SyntaxFactory.QualifiedName(qualifiedNamespaceName, nameRef.WithoutTrivia());
                }

                // We might lose some trivia associated with children of `oldNode`.  
                newNode = newNode.WithTriviaFrom(oldNode);
                return true;
            }
            else if (syntaxFacts.IsNameOfMemberAccessExpression(nameRef))
            {
                oldNode = nameRef.Parent;
                var aliasQualifier = GetAliasQualifier(oldNode);

                if (!TryGetGlobalQualifiedName(newNamespaceParts, nameRef, aliasQualifier, out newNode))
                {
                    var memberAccessNamespaceName = CreateNamespaceAsMemberAccess(newNamespaceParts, aliasQualifier, newNamespaceParts.Length - 1);
                    newNode = SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, memberAccessNamespaceName, nameRef.WithoutTrivia());
                }

                // We might lose some trivia associated with children of `oldNode`.  
                newNode = newNode.WithTriviaFrom(oldNode);
                return true;
            }
            else if (nameRef.Parent is NameMemberCrefSyntax crefName && crefName.Parent is QualifiedCrefSyntax qualifiedCref)
            {
                // This is the case where the reference is the right most part of a qualified name in `cref`.
                // for example, `<see cref="Foo.Baz.Bar"/>` and `<see cref="SomeAlias::Foo.Baz.Bar"/>`. 
                // This is the form of `cref` we need to handle as a spacial case when changing namespace name or
                // changing namespace from non-global to global, other cases in these 2 scenarios can be handled in the 
                // same way we handle non cref references, for example, `<see cref="SomeAlias::Foo"/>` and `<see cref="Foo"/>`.

                var container = qualifiedCref.Container;
                var aliasQualifier = GetAliasQualifier(container);

                if (TryGetGlobalQualifiedName(newNamespaceParts, nameRef, aliasQualifier, out newNode))
                {
                    // We will replace entire `QualifiedCrefSyntax` with a `TypeCrefSyntax`, 
                    // which is a alias qualified simple name, similar to the regular case above.
                    oldNode = qualifiedCref;
                    newNode = SyntaxFactory.TypeCref((AliasQualifiedNameSyntax)newNode!);
                }
                else
                {
                    // if the new namespace is not global, then we just need to change the container in `QualifiedCrefSyntax`,
                    // which is just a regular namespace node, no cref node involve here.
                    oldNode = container;
                    newNode = CreateNamespaceAsQualifiedName(newNamespaceParts, aliasQualifier, newNamespaceParts.Length - 1);
                }

                return true;
            }

            // Simple name reference, nothing to be done. 
            // The name will be resolved by adding proper import.
            oldNode = newNode = nameRef;
            return false;
        }

        private static bool TryGetGlobalQualifiedName(
            ImmutableArray<string> newNamespaceParts,
            SimpleNameSyntax nameNode,
            string? aliasQualifier,
            [NotNullWhen(returnValue: true)] out SyntaxNode? newNode)
        {
            if (IsGlobalNamespace(newNamespaceParts))
            {
                // If new namespace is "", then name will be declared in global namespace.
                // We will replace qualified reference with simple name qualified with alias (global if it's not alias qualified)
                var aliasNode = aliasQualifier?.ToIdentifierName() ?? SyntaxFactory.IdentifierName(SyntaxFactory.Token(SyntaxKind.GlobalKeyword));
                newNode = SyntaxFactory.AliasQualifiedName(aliasNode, nameNode.WithoutTrivia());
                return true;
            }

            newNode = null;
            return false;
        }

        /// <summary>
        /// Try to change the namespace declaration based on the following rules:
        ///     - if neither declared nor target namespace are "" (i.e. global namespace),
        ///     then we try to change the name of the namespace.
        ///     - if declared namespace is "", then we try to move all types declared 
        ///     in global namespace in the document into a new namespace declaration.
        ///     - if target namespace is "", then we try to move all members in declared 
        ///     namespace to global namespace (i.e. remove the namespace declaration).    
        /// </summary>
        protected override CompilationUnitSyntax ChangeNamespaceDeclaration(
            CompilationUnitSyntax root,
            ImmutableArray<string> declaredNamespaceParts,
            ImmutableArray<string> targetNamespaceParts)
        {
            Debug.Assert(!declaredNamespaceParts.IsDefault && !targetNamespaceParts.IsDefault);
            var container = root.GetAnnotatedNodes(ContainerAnnotation).Single();

            if (container is CompilationUnitSyntax compilationUnit)
            {
                // Move everything from global namespace to a namespace declaration
                Debug.Assert(IsGlobalNamespace(declaredNamespaceParts));
                return MoveMembersFromGlobalToNamespace(compilationUnit, targetNamespaceParts);
            }

            if (container is NamespaceDeclarationSyntax namespaceDecl)
            {
                // Move everything to global namespace
                if (IsGlobalNamespace(targetNamespaceParts))
                {
                    return MoveMembersFromNamespaceToGlobal(root, namespaceDecl);
                }

                // Change namespace name
                return root.ReplaceNode(
                    namespaceDecl,
                    namespaceDecl.WithName(
                        CreateNamespaceAsQualifiedName(targetNamespaceParts, aliasQualifier: null, targetNamespaceParts.Length - 1)
                        .WithTriviaFrom(namespaceDecl.Name).WithAdditionalAnnotations(WarningAnnotation))
                        .WithoutAnnotations(ContainerAnnotation));      // Make sure to remove the annotation we added
            }

            throw ExceptionUtilities.Unreachable;
        }

        private static CompilationUnitSyntax MoveMembersFromNamespaceToGlobal(CompilationUnitSyntax root, NamespaceDeclarationSyntax namespaceDecl)
        {
            var (namespaceOpeningTrivia, namespaceClosingTrivia) =
                GetOpeningAndClosingTriviaOfNamespaceDeclaration(namespaceDecl);
            var members = namespaceDecl.Members;
            var eofToken = root.EndOfFileToken
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
            //  The using of D.E.F is looked up with in the context of A.B.C first. If it's moved outside,
            //  it may fail to resolve.

            return root.Update(
                root.Externs.AddRange(namespaceDecl.Externs),
                root.Usings.AddRange(namespaceDecl.Usings),
                root.AttributeLists,
                root.Members.ReplaceRange(namespaceDecl, members),
                eofToken);
        }

        private static CompilationUnitSyntax MoveMembersFromGlobalToNamespace(CompilationUnitSyntax compilationUnit, ImmutableArray<string> targetNamespaceParts)
        {
            Debug.Assert(!compilationUnit.Members.Any(m => m is NamespaceDeclarationSyntax));

            var targetNamespaceDecl = SyntaxFactory.NamespaceDeclaration(
                name: CreateNamespaceAsQualifiedName(targetNamespaceParts, aliasQualifier: null, targetNamespaceParts.Length - 1)
                        .WithAdditionalAnnotations(WarningAnnotation),
                externs: default,
                usings: default,
                members: compilationUnit.Members);
            return compilationUnit.WithMembers(new SyntaxList<MemberDeclarationSyntax>(targetNamespaceDecl))
                .WithoutAnnotations(ContainerAnnotation);   // Make sure to remove the annotation we added
        }

        /// <summary>
        /// For the node specified by <paramref name="span"/> to be applicable container, it must be a namespace 
        /// declaration or a compilation unit, contain no partial declarations and meet the following additional
        /// requirements:
        /// 
        /// - If a namespace declaration:
        ///    1. It doesn't contain or is nested in other namespace declarations
        ///    2. The name of the namespace is valid (i.e. no errors)
        ///
        /// - If a compilation unit (i.e. <paramref name="span"/> is empty), there must be no namespace declaration
        ///   inside (i.e. all members are declared in global namespace)
        /// </summary>
        protected override async Task<SyntaxNode?> TryGetApplicableContainerFromSpanAsync(Document document, TextSpan span, CancellationToken cancellationToken)
        {
            var compilationUnit = (CompilationUnitSyntax)await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            SyntaxNode? container = null;

            // Empty span means that user wants to move all types declared in the document to a new namespace.
            // This action is only supported when everything in the document is declared in global namespace,
            // which we use the number of namespace declaration nodes to decide.
            if (span.IsEmpty)
            {
                if (ContainsNamespaceDeclaration(compilationUnit))
                {
                    return null;
                }

                container = compilationUnit;
            }
            else
            {
                // Otherwise, the span should contain a namespace declaration node, which must be the only one
                // in the entire syntax spine to enable the change namespace operation.
                var node = compilationUnit.FindNode(span, getInnermostNodeForTie: true);

                var namespaceDecl = node.AncestorsAndSelf().OfType<NamespaceDeclarationSyntax>().SingleOrDefault();
                if (namespaceDecl == null)
                {
                    return null;
                }

                if (namespaceDecl.Name.GetDiagnostics().Any(diag => diag.DefaultSeverity == DiagnosticSeverity.Error))
                {
                    return null;
                }

                if (ContainsNamespaceDeclaration(node))
                {
                    return null;
                }

                container = namespaceDecl;
            }

            var containsPartial =
                await ContainsPartialTypeWithMultipleDeclarationsAsync(document, container, cancellationToken).ConfigureAwait(false);

            if (containsPartial)
            {
                return null;
            }

            return container;

            static bool ContainsNamespaceDeclaration(SyntaxNode node)
                => node.DescendantNodes(n => n is CompilationUnitSyntax || n is NamespaceDeclarationSyntax)
                .OfType<NamespaceDeclarationSyntax>().Any();
        }

        private static string? GetAliasQualifier(SyntaxNode name)
        {
            while (true)
            {
                switch (name)
                {
                    case QualifiedNameSyntax qualifiedNameNode:
                        name = qualifiedNameNode.Left;
                        continue;
                    case MemberAccessExpressionSyntax memberAccessNode:
                        name = memberAccessNode.Expression;
                        continue;
                    case AliasQualifiedNameSyntax aliasQualifiedNameNode:
                        return aliasQualifiedNameNode.Alias.Identifier.ValueText;
                }

                return null;
            }
        }

        private static NameSyntax CreateNamespaceAsQualifiedName(ImmutableArray<string> namespaceParts, string? aliasQualifier, int index)
        {
            var part = namespaceParts[index].EscapeIdentifier();
            Debug.Assert(part.Length > 0);

            var namePiece = SyntaxFactory.IdentifierName(part);

            if (index == 0)
            {
                return aliasQualifier == null
                     ? (NameSyntax)namePiece
                     : SyntaxFactory.AliasQualifiedName(aliasQualifier, namePiece);
            }

            return SyntaxFactory.QualifiedName(CreateNamespaceAsQualifiedName(namespaceParts, aliasQualifier, index - 1), namePiece);
        }

        private static ExpressionSyntax CreateNamespaceAsMemberAccess(ImmutableArray<string> namespaceParts, string? aliasQualifier, int index)
        {
            var part = namespaceParts[index].EscapeIdentifier();
            Debug.Assert(part.Length > 0);

            var namePiece = SyntaxFactory.IdentifierName(part);

            if (index == 0)
            {
                return aliasQualifier == null
                     ? (NameSyntax)namePiece
                     : SyntaxFactory.AliasQualifiedName(aliasQualifier, namePiece);
            }

            return SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                CreateNamespaceAsMemberAccess(namespaceParts, aliasQualifier, index - 1),
                namePiece);
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
