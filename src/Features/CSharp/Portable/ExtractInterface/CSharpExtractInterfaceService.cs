// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ExtractInterface;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExtractInterface
{
    [ExportLanguageService(typeof(AbstractExtractInterfaceService), LanguageNames.CSharp), Shared]
    internal sealed class CSharpExtractInterfaceService : AbstractExtractInterfaceService
    {
        internal override async Task<SyntaxNode> GetTypeDeclarationAsync(Document document, int position, TypeDiscoveryRule typeDiscoveryRule, CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(position != tree.Length ? position : Math.Max(0, position - 1));
            var typeDeclaration = token.GetAncestor<TypeDeclarationSyntax>();

            if (typeDeclaration == null ||
                typeDiscoveryRule == TypeDiscoveryRule.TypeDeclaration)
            {
                return typeDeclaration;
            }

            var spanStart = typeDeclaration.Identifier.SpanStart;
            var spanEnd = typeDeclaration.TypeParameterList != null ? typeDeclaration.TypeParameterList.Span.End : typeDeclaration.Identifier.Span.End;
            var span = new TextSpan(spanStart, spanEnd - spanStart);

            return span.IntersectsWith(position) ? typeDeclaration : null;
        }

        internal override Solution GetSolutionWithUpdatedOriginalType(
            Solution solutionWithFormattedInterfaceDocument,
            INamedTypeSymbol extractedInterfaceSymbol,
            IEnumerable<ISymbol> includedMembers,
            Dictionary<ISymbol, SyntaxAnnotation> symbolToDeclarationAnnotationMap,
            List<DocumentId> documentIds,
            SyntaxAnnotation typeNodeAnnotation,
            DocumentId documentIdWithTypeNode,
            CancellationToken cancellationToken)
        {
            var documentWithTypeNode = solutionWithFormattedInterfaceDocument.GetDocument(documentIdWithTypeNode);
            var root = documentWithTypeNode.GetSyntaxRootAsync(cancellationToken).WaitAndGetResult(cancellationToken);
            var typeDeclaration = root.GetAnnotatedNodes<TypeDeclarationSyntax>(typeNodeAnnotation).Single();

            var docId = solutionWithFormattedInterfaceDocument.GetDocument(typeDeclaration.SyntaxTree).Id;

            var implementedInterfaceTypeSyntax = extractedInterfaceSymbol.TypeParameters.Any()
                ? SyntaxFactory.GenericName(
                    SyntaxFactory.Identifier(extractedInterfaceSymbol.Name),
                    SyntaxFactory.TypeArgumentList(SyntaxFactory.SeparatedList(extractedInterfaceSymbol.TypeParameters.Select(p => SyntaxFactory.ParseTypeName(p.Name)))))
                : SyntaxFactory.ParseTypeName(extractedInterfaceSymbol.Name);

            var baseList = typeDeclaration.BaseList ?? SyntaxFactory.BaseList();
            var updatedBaseList = baseList.WithTypes(SyntaxFactory.SeparatedList(baseList.Types.Union(new[] { SyntaxFactory.SimpleBaseType(implementedInterfaceTypeSyntax) })));

            if (!baseList.Types.Any())
            {
                // If we're adding the first element to the base list, then we need to move 
                // trivia from the type name itself to the end of the base list

                updatedBaseList = updatedBaseList.WithLeadingTrivia(SyntaxFactory.Space);

                if (typeDeclaration.TypeParameterList != null)
                {
                    updatedBaseList = updatedBaseList.WithTrailingTrivia(typeDeclaration.TypeParameterList.GetTrailingTrivia());
                    typeDeclaration = typeDeclaration.WithTypeParameterList(typeDeclaration.TypeParameterList.WithoutTrailingTrivia());
                }
                else
                {
                    updatedBaseList = updatedBaseList.WithTrailingTrivia(typeDeclaration.Identifier.TrailingTrivia);
                    typeDeclaration = typeDeclaration.WithIdentifier(typeDeclaration.Identifier.WithTrailingTrivia());
                }
            }

            var updatedTypeDeclaration = typeDeclaration.WithBaseList(updatedBaseList.WithAdditionalAnnotations(Formatter.Annotation));
            var updatedRoot = root.ReplaceNode(root.GetAnnotatedNodes<TypeDeclarationSyntax>(typeNodeAnnotation).Single(), updatedTypeDeclaration);
            var solutionWithOriginalTypeUpdated = solutionWithFormattedInterfaceDocument.WithDocumentSyntaxRoot(docId, updatedRoot, PreservationMode.PreserveIdentity);
            return solutionWithOriginalTypeUpdated;
        }

        internal override string GetGeneratedNameTypeParameterSuffix(IList<ITypeParameterSymbol> typeParameters, Workspace workspace)
        {
            if (typeParameters.IsEmpty())
            {
                return string.Empty;
            }

            var typeParameterList = SyntaxFactory.TypeParameterList(SyntaxFactory.SeparatedList(typeParameters.Select(p => SyntaxFactory.TypeParameter(p.Name))));
            return Formatter.Format(typeParameterList, workspace).ToString();
        }

        internal override string GetContainingNamespaceDisplay(INamedTypeSymbol typeSymbol, CompilationOptions compilationOptions)
        {
            return typeSymbol.ContainingNamespace.IsGlobalNamespace
                ? string.Empty
                : typeSymbol.ContainingNamespace.ToDisplayString();
        }

        internal override bool IsExtractableMember(ISymbol m)
        {
            return base.IsExtractableMember(m) && !m.ExplicitInterfaceImplementations().Any();
        }

        internal override bool ShouldIncludeAccessibilityModifier(SyntaxNode typeNode)
        {
            var typeDeclaration = typeNode as TypeDeclarationSyntax;
            return typeDeclaration.Modifiers.Any(m => SyntaxFacts.IsAccessibilityModifier(m.Kind()));
        }
    }
}
