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
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ConvertAutoPropertyToFullProperty
{
    [ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertAutoPropertyToFullProperty), Shared]
    internal class CSharpConvertAutoPropertyToFullPropertyCodeRefactoringProvider : AbstractConvertAutoPropertyToFullPropertyCodeRefactoringProvider<PropertyDeclarationSyntax, TypeDeclarationSyntax, CSharpCodeGenerationContextInfo>
    {
        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public CSharpConvertAutoPropertyToFullPropertyCodeRefactoringProvider()
        {
        }

        protected override async Task<string> GetFieldNameAsync(Document document, IPropertySymbol property, NamingStylePreferencesProvider fallbackOptions, CancellationToken cancellationToken)
        {
            var rule = await document.GetApplicableNamingRuleAsync(
                new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Field),
                property.IsStatic ? DeclarationModifiers.Static : DeclarationModifiers.None,
                Accessibility.Private,
                fallbackOptions,
                cancellationToken).ConfigureAwait(false);

            var fieldName = rule.NamingStyle.MakeCompliant(property.Name).First();
            return NameGenerator.GenerateUniqueName(fieldName, n => !(property.ContainingType.Name == n || property.ContainingType.GetMembers(n).Any()));
        }

        protected override (SyntaxNode newGetAccessor, SyntaxNode newSetAccessor) GetNewAccessors(
            CSharpCodeGenerationContextInfo info, SyntaxNode property,
            string fieldName, SyntaxGenerator generator, CancellationToken cancellationToken)
        {
            // C# might have trivia with the accessors that needs to be preserved.  
            // so we will update the existing accessors instead of creating new ones
            var accessorListSyntax = ((PropertyDeclarationSyntax)property).AccessorList;
            var (getAccessor, setAccessor) = GetExistingAccessors(accessorListSyntax);

            var getAccessorStatement = generator.ReturnStatement(generator.IdentifierName(fieldName));
            var newGetter = GetUpdatedAccessor(info, getAccessor, getAccessorStatement, cancellationToken);

            SyntaxNode newSetter = null;
            if (setAccessor != null)
            {
                var setAccessorStatement = generator.ExpressionStatement(generator.AssignmentStatement(
                    generator.IdentifierName(fieldName),
                    generator.IdentifierName("value")));
                newSetter = GetUpdatedAccessor(info, setAccessor, setAccessorStatement, cancellationToken);
            }

            return (newGetAccessor: newGetter, newSetAccessor: newSetter);
        }

        private static (AccessorDeclarationSyntax getAccessor, AccessorDeclarationSyntax setAccessor)
            GetExistingAccessors(AccessorListSyntax accessorListSyntax)
            => (accessorListSyntax.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.GetAccessorDeclaration)),
                accessorListSyntax.Accessors.FirstOrDefault(a => a.Kind() is SyntaxKind.SetAccessorDeclaration or SyntaxKind.InitAccessorDeclaration));

        private static SyntaxNode GetUpdatedAccessor(CSharpCodeGenerationContextInfo info,
            SyntaxNode accessor, SyntaxNode statement, CancellationToken cancellationToken)
        {
            var newAccessor = AddStatement(accessor, statement);
            var accessorDeclarationSyntax = (AccessorDeclarationSyntax)newAccessor;

            var preference = info.Options.PreferExpressionBodiedAccessors.Value;
            if (preference == ExpressionBodyPreference.Never)
            {
                return accessorDeclarationSyntax.WithSemicolonToken(default);
            }

            if (!accessorDeclarationSyntax.Body.TryConvertToArrowExpressionBody(
                    accessorDeclarationSyntax.Kind(), info.LanguageVersion, preference, cancellationToken,
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

        protected override SyntaxNode ConvertPropertyToExpressionBodyIfDesired(
            CSharpCodeGenerationContextInfo info, SyntaxNode property)
        {
            var propertyDeclaration = (PropertyDeclarationSyntax)property;

            var preference = info.Options.PreferExpressionBodiedProperties.Value;
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

        protected override SyntaxNode GetTypeBlock(SyntaxNode syntaxNode)
            => syntaxNode;

        protected override SyntaxNode GetInitializerValue(SyntaxNode property)
            => ((PropertyDeclarationSyntax)property).Initializer?.Value;

        protected override SyntaxNode GetPropertyWithoutInitializer(SyntaxNode property)
            => ((PropertyDeclarationSyntax)property).WithInitializer(null);
    }
}
