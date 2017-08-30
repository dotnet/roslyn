// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertAutoPropertyToFullProperty
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(CSharpConvertAutoPropertyToFullPropertyCodeRefactoringProvider)), Shared]
    internal class CSharpConvertAutoPropertyToFullPropertyCodeRefactoringProvider : AbstractConvertAutoPropertyToFullPropertyCodeRefactoringProvider
    {
        internal override SyntaxNode GetProperty(SyntaxToken token)
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

        internal override async Task<(SyntaxNode newGetAccessor, SyntaxNode newSetAccessor)> GetNewAccessorsAsync(Document document, SyntaxNode property, string fieldName, SyntaxGenerator generator, CancellationToken cancellationToken)
        {
            // C# might have trivia with the accessors that needs to be preserved.  
            // so we will update the existing accessors instead of creating new ones
            var accessorListSyntax = ((PropertyDeclarationSyntax)property).AccessorList;
            var existingAccessors = GetExistingAccessors(accessorListSyntax);

            var getAccessorStatement = generator.ReturnStatement(generator.IdentifierName(fieldName));
            var newGetter = await AddStatementsToAccessorAsync(
                document,
                existingAccessors.getAccessor,
                getAccessorStatement,
                generator,
                cancellationToken).ConfigureAwait(false);

            SyntaxNode newSetter = null;
            if (existingAccessors.setAccessor != null)
            {
                var setAccessorStatement = generator.ExpressionStatement(generator.AssignmentStatement(
                            generator.IdentifierName(fieldName),
                            generator.IdentifierName("value")));
                newSetter = await AddStatementsToAccessorAsync(
                    document,
                    existingAccessors.setAccessor,
                    setAccessorStatement,
                    generator,
                    cancellationToken).ConfigureAwait(false);
            }

            return (newGetAccessor: newGetter, newSetAccessor: newSetter);
        }

        private (AccessorDeclarationSyntax getAccessor, AccessorDeclarationSyntax setAccessor) GetExistingAccessors(AccessorListSyntax accessorListSyntax)
        {
            AccessorDeclarationSyntax getter = null;
            AccessorDeclarationSyntax setter = null;
            foreach (var accessor in accessorListSyntax.Accessors)
            {
                if (accessor.Kind() == SyntaxKind.GetAccessorDeclaration)
                {
                    getter = accessor;
                }
                else if (accessor.Kind() == SyntaxKind.SetAccessorDeclaration)
                {
                    setter = accessor;
                }
            }

            return (getAccessor: getter, setAccessor: setter);
        }

        private static void GetExistingAccessors(AccessorListSyntax accessorListSyntax, ref AccessorDeclarationSyntax getAccessor, ref AccessorDeclarationSyntax setAccessor)
        {
            foreach (var accessor in accessorListSyntax.Accessors)
            {
                if (accessor.Kind() == SyntaxKind.GetAccessorDeclaration)
                {
                    getAccessor = accessor;
                }
                else if (accessor.Kind() == SyntaxKind.SetAccessorDeclaration)
                {
                    setAccessor = accessor;
                }
            }
        }

        private async Task<SyntaxNode> AddStatementsToAccessorAsync(
                Document document,
                SyntaxNode accessor,
                SyntaxNode statement,
                SyntaxGenerator generator,
                CancellationToken cancellationToken)
        {
            var newAccessor = UpdateAccessor(accessor, statement);
            newAccessor = await ConvertToExpressionBodyIfDesiredAsync(
                document,
                newAccessor,
                cancellationToken).ConfigureAwait(false);

            return await Formatter.FormatAsync(newAccessor, document.Project.Solution.Workspace).ConfigureAwait(false);
        }

        internal async Task<SyntaxNode> ConvertToExpressionBodyIfDesiredAsync(
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

            var ableToConvert = accessorDeclarationSyntax.Body.TryConvertToExpressionBody(
                accessorDeclarationSyntax.Kind(),
                accessor.SyntaxTree.Options,
                preference,
                out var arrowExpression,
                out var semicolonToken);

            // Should always be able to convert to expression body since we are creating the accessor 
            // and know that it only has one statement
            Debug.Assert(ableToConvert);

            return accessorDeclarationSyntax
                .WithExpressionBody(arrowExpression)
                .WithBody(null)
                .WithSemicolonToken(semicolonToken);
        }

        internal SyntaxNode UpdateAccessor(SyntaxNode accessor, SyntaxNode statement)
        {
            var blockSyntax = SyntaxFactory.Block(
                SyntaxFactory.Token(SyntaxKind.OpenBraceToken),
                new SyntaxList<StatementSyntax>((StatementSyntax)statement),
                SyntaxFactory.Token(SyntaxKind.CloseBraceToken)
                    .WithTrailingTrivia(((AccessorDeclarationSyntax)accessor).SemicolonToken.TrailingTrivia));

            return ((AccessorDeclarationSyntax)accessor).WithBody(blockSyntax);
        }

        internal async Task<ExpressionBodyPreference> GetExpressionBodyPreferenceAsync(
            Document document,
            CancellationToken cancellationToken)
        {
            var options = await document.GetOptionsAsync(cancellationToken).ConfigureAwait(false);
            return options.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors).Value;
        }

        private bool IsEmpty(AccessorDeclarationSyntax accessor)
            => accessor.Body == null && accessor.ExpressionBody == null;

        internal override string GetUniqueName(string fieldName, IPropertySymbol property)
            => NameGenerator.GenerateUniqueName(fieldName, n => !property.ContainingType.GetMembers(n).Any());

        internal override SyntaxNode GetTypeBlock(SyntaxNode syntaxNode) 
            => syntaxNode;

        internal override SyntaxNode GetInitializerValue(SyntaxNode property)
            => ((PropertyDeclarationSyntax)property).Initializer?.Value;

        internal override SyntaxNode GetPropertyWithoutInitializer(SyntaxNode property) 
            => ((PropertyDeclarationSyntax)property).WithInitializer(null);
    }
}
