// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.ConvertAutoPropertyToFullProperty;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.CodeAnalysis.CSharp.ConvertAutoPropertyToFullProperty
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(CSharpConvertAutoPropertyToFullPropertyCodeRefactoringProvider)), Shared]
    internal class CSharpConvertAutoPropertyToFullPropertyCodeRefactoringProvider : AbstractConvertAutoPropertyToFullPropertyCodeRefactoringProvider
    {
        internal async override Task<SyntaxNode> ConvertToExpressionBodyIfDesiredAsync(
            Document document, 
            SyntaxNode accessor, 
            CancellationToken cancellationToken)
        {
            var accessorDeclarationSyntax = (AccessorDeclarationSyntax)accessor;

            var preference = await GetExpressionBodyPreferenceAsync(document, cancellationToken).ConfigureAwait(false);
            if (preference == ExpressionBodyPreference.Never)
            {
                return accessorDeclarationSyntax.WithSemicolonToken(default);
            }

            // Should always be able to convert to expression body since we are creating the accessor and know that it only has one line
            Debug.Assert(accessorDeclarationSyntax.Body.TryConvertToExpressionBody(
                accessorDeclarationSyntax.Kind(), 
                accessor.SyntaxTree.Options, 
                preference, 
                out var arrowExpression, 
                out var semicolonToken));

            return accessorDeclarationSyntax
                .WithExpressionBody(arrowExpression)
                .WithBody(null)
                .WithSemicolonToken(semicolonToken);
        }

        internal async Task<ExpressionBodyPreference> GetExpressionBodyPreferenceAsync(
            Document document, 
            CancellationToken cancellationToken)
        {
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            return options.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors).Value;
        }

        internal override SyntaxNode GetPropertyDeclaration(SyntaxToken token)
        {
            var containingProperty = token.Parent.FirstAncestorOrSelf<PropertyDeclarationSyntax>();
            if (containingProperty == null)
            {
                return null;
            }

            var start = containingProperty.AttributeLists.Count > 0
                ? containingProperty.AttributeLists.Last().GetLastToken().GetNextToken().SpanStart
                : containingProperty.SpanStart;

            // Offer this refactoring anywhere in the signature of the property
            var position = token.SpanStart;
            if (position < start || position > containingProperty.Identifier.Span.End)
            {
                return null;
            }

            return containingProperty;
        }

        internal override bool isAbstract(SyntaxNode property)
        {
            var propertyDeclarationSyntax = (PropertyDeclarationSyntax)property;
            var modifiers = propertyDeclarationSyntax.GetModifiers();
            foreach (var modifier in modifiers)
            {
                if (modifier.IsKind(SyntaxKind.AbstractKeyword))
                {
                    return true;
                }
            }

            return false;
        }

        internal override bool TryGetEmptyAccessors(
            SyntaxNode propertyDeclarationSyntax, 
            out SyntaxNode emptyGetAccessor, 
            out SyntaxNode emptySetAccessor)
        {
            emptyGetAccessor = null;
            emptySetAccessor = null;

            var accessorListSyntax = ((PropertyDeclarationSyntax)propertyDeclarationSyntax).AccessorList;
            if (accessorListSyntax == null)
            {
                return false;
            }

            foreach (var accessor in accessorListSyntax.Accessors)
            {
                if (accessor.Kind() == SyntaxKind.GetAccessorDeclaration && isEmpty(accessor))
                {
                    emptyGetAccessor = accessor;
                }
                else if (accessor.Kind() == SyntaxKind.SetAccessorDeclaration && isEmpty(accessor))
                {
                    emptySetAccessor = accessor;
                }
            }

            // both getter and setter have to be empty
            return (emptyGetAccessor != null && emptySetAccessor != null);
        }

        private bool isEmpty(AccessorDeclarationSyntax accessor) 
            => (accessor.Body == null && accessor.ExpressionBody == null);

        internal override SyntaxNode UpdateAccessor(SyntaxNode accessor, SyntaxNode[] statements)
        {
            var blockSyntax = SyntaxFactory.Block(
                SyntaxFactory.Token(SyntaxKind.OpenBraceToken),
                new SyntaxList<StatementSyntax>(statements.Cast<StatementSyntax>()),
                SyntaxFactory.Token(SyntaxKind.CloseBraceToken)
                    .WithTrailingTrivia(((AccessorDeclarationSyntax)accessor).SemicolonToken.TrailingTrivia));

            return ((AccessorDeclarationSyntax)accessor).WithBody(blockSyntax);
        }
    }
}
