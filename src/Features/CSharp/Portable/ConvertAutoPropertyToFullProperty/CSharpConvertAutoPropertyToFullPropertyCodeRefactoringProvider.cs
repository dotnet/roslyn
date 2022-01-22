// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.ConvertAutoPropertyToFullProperty;
using Microsoft.CodeAnalysis.CSharp.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using static Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles.SymbolSpecification;

namespace Microsoft.CodeAnalysis.CSharp.ConvertAutoPropertyToFullProperty
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertAutoPropertyToFullProperty), Shared]
    internal class CSharpConvertAutoPropertyToFullPropertyCodeRefactoringProvider : AbstractConvertAutoPropertyToFullPropertyCodeRefactoringProvider<PropertyDeclarationSyntax, TypeDeclarationSyntax, CSharpCodeGenerationPreferences>
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpConvertAutoPropertyToFullPropertyCodeRefactoringProvider()
        {
        }

        internal override async Task<string> GetFieldNameAsync(Document document, IPropertySymbol property, CancellationToken cancellationToken)
        {
            var rule = await document.GetApplicableNamingRuleAsync(
                new SymbolKindOrTypeKind(SymbolKind.Field),
                property.IsStatic ? DeclarationModifiers.Static : DeclarationModifiers.None,
                Accessibility.Private,
                cancellationToken).ConfigureAwait(false);

            var fieldName = rule.NamingStyle.MakeCompliant(property.Name).First();
            return NameGenerator.GenerateUniqueName(fieldName, n => !property.ContainingType.GetMembers(n).Any());
        }

        internal override (SyntaxNode newGetAccessor, SyntaxNode newSetAccessor) GetNewAccessors(
            CSharpCodeGenerationPreferences preferences, SyntaxNode property,
            string fieldName, SyntaxGenerator generator)
        {
            // C# might have trivia with the accessors that needs to be preserved.  
            // so we will update the existing accessors instead of creating new ones
            var accessorListSyntax = ((PropertyDeclarationSyntax)property).AccessorList;
            var (getAccessor, setAccessor) = GetExistingAccessors(accessorListSyntax);

            var getAccessorStatement = generator.ReturnStatement(generator.IdentifierName(fieldName));
            var newGetter = GetUpdatedAccessor(preferences, getAccessor, getAccessorStatement);

            SyntaxNode newSetter = null;
            if (setAccessor != null)
            {
                var setAccessorStatement = generator.ExpressionStatement(generator.AssignmentStatement(
                    generator.IdentifierName(fieldName),
                    generator.IdentifierName("value")));
                newSetter = GetUpdatedAccessor(preferences, setAccessor, setAccessorStatement);
            }

            return (newGetAccessor: newGetter, newSetAccessor: newSetter);
        }

        private static (AccessorDeclarationSyntax getAccessor, AccessorDeclarationSyntax setAccessor)
            GetExistingAccessors(AccessorListSyntax accessorListSyntax)
            => (accessorListSyntax.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.GetAccessorDeclaration)),
                accessorListSyntax.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.SetAccessorDeclaration) ||
                                                                 a.IsKind(SyntaxKind.InitAccessorDeclaration)));

        private static SyntaxNode GetUpdatedAccessor(CSharpCodeGenerationPreferences preferences,
            SyntaxNode accessor, SyntaxNode statement)
        {
            var newAccessor = AddStatement(accessor, statement);
            var accessorDeclarationSyntax = (AccessorDeclarationSyntax)newAccessor;

            var preference = preferences.PreferExpressionBodiedAccessors;
            if (preference == ExpressionBodyPreference.Never)
            {
                return accessorDeclarationSyntax.WithSemicolonToken(default);
            }

            if (!accessorDeclarationSyntax.Body.TryConvertToArrowExpressionBody(
                    accessorDeclarationSyntax.Kind(), preferences.LanguageVersion, preference,
                    out var arrowExpression, out _))
            {
                return accessorDeclarationSyntax.WithSemicolonToken(default);
            }

            return accessorDeclarationSyntax
                .WithExpressionBody(arrowExpression)
                .WithBody(null)
                .WithSemicolonToken(accessorDeclarationSyntax.SemicolonToken)
                .WithAdditionalAnnotations(Formatter.Annotation);
        }

        internal static SyntaxNode AddStatement(SyntaxNode accessor, SyntaxNode statement)
        {
            var blockSyntax = SyntaxFactory.Block(
                SyntaxFactory.Token(SyntaxKind.OpenBraceToken).WithLeadingTrivia(SyntaxFactory.ElasticCarriageReturnLineFeed),
                new SyntaxList<StatementSyntax>((StatementSyntax)statement),
                SyntaxFactory.Token(SyntaxKind.CloseBraceToken)
                    .WithTrailingTrivia(((AccessorDeclarationSyntax)accessor).SemicolonToken.TrailingTrivia));

            return ((AccessorDeclarationSyntax)accessor).WithBody(blockSyntax);
        }

        internal override SyntaxNode ConvertPropertyToExpressionBodyIfDesired(
            CSharpCodeGenerationPreferences preferences, SyntaxNode property)
        {
            var propertyDeclaration = (PropertyDeclarationSyntax)property;

            var preference = preferences.PreferExpressionBodiedProperties;
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

        internal override SyntaxNode GetTypeBlock(SyntaxNode syntaxNode)
            => syntaxNode;

        internal override SyntaxNode GetInitializerValue(SyntaxNode property)
            => ((PropertyDeclarationSyntax)property).Initializer?.Value;

        internal override SyntaxNode GetPropertyWithoutInitializer(SyntaxNode property)
            => ((PropertyDeclarationSyntax)property).WithInitializer(null);
    }
}
