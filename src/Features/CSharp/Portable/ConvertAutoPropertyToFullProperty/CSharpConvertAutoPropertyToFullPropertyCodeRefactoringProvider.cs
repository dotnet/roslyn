// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.ConvertAutoPropertyToFullProperty;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Naming;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles.SymbolSpecification;

namespace Microsoft.CodeAnalysis.CSharp.ConvertAutoPropertyToFullProperty
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(CSharpConvertAutoPropertyToFullPropertyCodeRefactoringProvider)), Shared]
    internal class CSharpConvertAutoPropertyToFullPropertyCodeRefactoringProvider : AbstractConvertAutoPropertyToFullPropertyCodeRefactoringProvider<PropertyDeclarationSyntax, TypeDeclarationSyntax>
    {
        [ImportingConstructor]
        public CSharpConvertAutoPropertyToFullPropertyCodeRefactoringProvider()
        {
        }

        internal override async Task<string> GetFieldNameAsync(Document document, IPropertySymbol propertySymbol, CancellationToken cancellationToken)
        {
            var rules = await document.GetNamingRulesAsync(FallbackNamingRules.RefactoringMatchLookupRules, cancellationToken).ConfigureAwait(false);
            return GenerateFieldName(propertySymbol, rules);
        }

        private string GenerateFieldName(IPropertySymbol property, ImmutableArray<NamingRule> rules)
        {
            var propertyName = property.Name;
            var fieldName = "";
            foreach (var rule in rules)
            {
                if (rule.SymbolSpecification.AppliesTo(
                    new SymbolKindOrTypeKind(SymbolKind.Field),
                    property.IsStatic ? DeclarationModifiers.Static : DeclarationModifiers.None,
                    Accessibility.Private))
                {
                    fieldName = rule.NamingStyle.MakeCompliant(propertyName).First();
                    break;
                }
            }

            return NameGenerator.GenerateUniqueName(fieldName, n => !property.ContainingType.GetMembers(n).Any());
        }

        internal override (SyntaxNode newGetAccessor, SyntaxNode newSetAccessor) GetNewAccessors(
            DocumentOptionSet options, SyntaxNode property,
            string fieldName, SyntaxGenerator generator)
        {
            // C# might have trivia with the accessors that needs to be preserved.  
            // so we will update the existing accessors instead of creating new ones
            var accessorListSyntax = ((PropertyDeclarationSyntax)property).AccessorList;
            var (getAccessor, setAccessor) = GetExistingAccessors(accessorListSyntax);

            var getAccessorStatement = generator.ReturnStatement(generator.IdentifierName(fieldName));
            var newGetter = GetUpdatedAccessor(options, getAccessor, getAccessorStatement);

            SyntaxNode newSetter = null;
            if (setAccessor != null)
            {
                var setAccessorStatement = generator.ExpressionStatement(generator.AssignmentStatement(
                    generator.IdentifierName(fieldName),
                    generator.IdentifierName("value")));
                newSetter = GetUpdatedAccessor(options, setAccessor, setAccessorStatement);
            }

            return (newGetAccessor: newGetter, newSetAccessor: newSetter);
        }

        private (AccessorDeclarationSyntax getAccessor, AccessorDeclarationSyntax setAccessor)
            GetExistingAccessors(AccessorListSyntax accessorListSyntax)
            => (accessorListSyntax.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.GetAccessorDeclaration)),
                accessorListSyntax.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.SetAccessorDeclaration)));

        private SyntaxNode GetUpdatedAccessor(DocumentOptionSet options,
            SyntaxNode accessor, SyntaxNode statement)
        {
            var newAccessor = AddStatement(accessor, statement);
            var accessorDeclarationSyntax = (AccessorDeclarationSyntax)newAccessor;

            var preference = GetAccessorExpressionBodyPreference(options);
            if (preference == ExpressionBodyPreference.Never)
            {
                return accessorDeclarationSyntax.WithSemicolonToken(default);
            }

            if (!accessorDeclarationSyntax.Body.TryConvertToArrowExpressionBody(
                    accessorDeclarationSyntax.Kind(), accessor.SyntaxTree.Options, preference,
                    out var arrowExpression, out var semicolonToken))
            {
                return accessorDeclarationSyntax.WithSemicolonToken(default);
            };

            return accessorDeclarationSyntax
                .WithExpressionBody(arrowExpression)
                .WithBody(null)
                .WithSemicolonToken(accessorDeclarationSyntax.SemicolonToken)
                .WithAdditionalAnnotations(Formatter.Annotation);
        }

        internal SyntaxNode AddStatement(SyntaxNode accessor, SyntaxNode statement)
        {
            var blockSyntax = SyntaxFactory.Block(
                SyntaxFactory.Token(SyntaxKind.OpenBraceToken).WithLeadingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed),
                new SyntaxList<StatementSyntax>((StatementSyntax)statement),
                SyntaxFactory.Token(SyntaxKind.CloseBraceToken)
                    .WithTrailingTrivia(((AccessorDeclarationSyntax)accessor).SemicolonToken.TrailingTrivia));

            return ((AccessorDeclarationSyntax)accessor).WithBody(blockSyntax);
        }

        internal override SyntaxNode ConvertPropertyToExpressionBodyIfDesired(
            DocumentOptionSet options, SyntaxNode property)
        {
            var propertyDeclaration = (PropertyDeclarationSyntax)property;

            var preference = GetPropertyExpressionBodyPreference(options);
            if (preference == ExpressionBodyPreference.Never)
            {
                return propertyDeclaration.WithSemicolonToken(default);
            }

            // if there is a get accessors only, we can move the expression body to the property
            if (propertyDeclaration.AccessorList?.Accessors.Count == 1 &&
                propertyDeclaration.AccessorList.Accessors[0].Kind() == SyntaxKind.GetAccessorDeclaration)
            {
                var getAccessor = propertyDeclaration.AccessorList.Accessors[0];
                if (getAccessor.ExpressionBody != null)
                {
                    return propertyDeclaration.WithExpressionBody(getAccessor.ExpressionBody)
                        .WithSemicolonToken(getAccessor.SemicolonToken)
                        .WithAccessorList(null);
                }
            }

            return propertyDeclaration.WithSemicolonToken(default);
        }

        internal ExpressionBodyPreference GetAccessorExpressionBodyPreference(DocumentOptionSet options)
            => options.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedAccessors).Value;

        internal ExpressionBodyPreference GetPropertyExpressionBodyPreference(DocumentOptionSet options)
            => options.GetOption(CSharpCodeStyleOptions.PreferExpressionBodiedProperties).Value;


        internal override SyntaxNode GetTypeBlock(SyntaxNode syntaxNode)
            => syntaxNode;

        internal override SyntaxNode GetInitializerValue(SyntaxNode property)
            => ((PropertyDeclarationSyntax)property).Initializer?.Value;

        internal override SyntaxNode GetPropertyWithoutInitializer(SyntaxNode property)
            => ((PropertyDeclarationSyntax)property).WithInitializer(null);
    }
}
