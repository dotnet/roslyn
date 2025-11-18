// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeGeneration;
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

namespace Microsoft.CodeAnalysis.CSharp.ConvertAutoPropertyToFullProperty;

using static CSharpSyntaxTokens;
using static SyntaxFactory;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = PredefinedCodeRefactoringProviderNames.ConvertAutoPropertyToFullProperty), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class CSharpConvertAutoPropertyToFullPropertyCodeRefactoringProvider()
    : AbstractConvertAutoPropertyToFullPropertyCodeRefactoringProvider<PropertyDeclarationSyntax, TypeDeclarationSyntax, CSharpCodeGenerationContextInfo>
{
    protected override async Task<string> GetFieldNameAsync(Document document, IPropertySymbol property, CancellationToken cancellationToken)
    {
        var rule = await document.GetApplicableNamingRuleAsync(
            new SymbolSpecification.SymbolKindOrTypeKind(SymbolKind.Field),
            property.IsStatic ? Modifiers.Static : Modifiers.None,
            Accessibility.Private,
            cancellationToken).ConfigureAwait(false);

        var fieldName = rule.NamingStyle.MakeCompliant(property.Name).First();
        return NameGenerator.GenerateUniqueName(fieldName, n => !(property.ContainingType.Name == n || property.ContainingType.GetMembers(n).Any()));
    }

    protected override (SyntaxNode newGetAccessor, SyntaxNode newSetAccessor) GetNewAccessors(
        CSharpCodeGenerationContextInfo info,
        PropertyDeclarationSyntax property,
        string fieldName,
        CancellationToken cancellationToken)
    {
        // Replace the bodies with bodies that reference the new field name.
        return GetNewAccessors(info, property, fieldName.ToIdentifierName(), cancellationToken);
    }

    private static (SyntaxNode newGetAccessor, SyntaxNode newSetAccessor) GetNewAccessors(
        CSharpCodeGenerationContextInfo info,
        PropertyDeclarationSyntax property,
        ExpressionSyntax backingFieldExpression,
        CancellationToken cancellationToken)
    {
        // C# might have trivia with the accessors that needs to be preserved.  
        // so we will update the existing accessors instead of creating new ones
        var accessorListSyntax = property.AccessorList;
        var (getAccessor, setAccessor) = GetExistingAccessors(accessorListSyntax);

        var getAccessorStatement = ReturnStatement(backingFieldExpression);
        var newGetter = GetUpdatedAccessor(getAccessor, getAccessorStatement);

        var newSetter = setAccessor;
        if (newSetter != null)
        {
            var setAccessorStatement = ExpressionStatement(AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression,
                backingFieldExpression,
                IdentifierName("value")));
            newSetter = GetUpdatedAccessor(setAccessor, setAccessorStatement);
        }

        return (newGetter, newSetter);

        AccessorDeclarationSyntax GetUpdatedAccessor(AccessorDeclarationSyntax accessor, StatementSyntax statement)
        {
            if (accessor.Body != null || accessor.ExpressionBody != null)
                return ReplaceFieldExpression(accessor);

            var accessorDeclarationSyntax = accessor.WithBody(Block(
                OpenBraceToken.WithLeadingTrivia(ElasticCarriageReturnLineFeed),
                [statement],
                CloseBraceToken.WithTrailingTrivia(accessor.SemicolonToken.TrailingTrivia)));

            var preference = info.Options.PreferExpressionBodiedAccessors.Value;
            if (preference == ExpressionBodyPreference.Never)
                return accessorDeclarationSyntax.WithSemicolonToken(default);

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

        AccessorDeclarationSyntax ReplaceFieldExpression(AccessorDeclarationSyntax accessor)
        {
            return accessor.ReplaceNodes(
                accessor.DescendantNodes().OfType<FieldExpressionSyntax>(),
                (oldNode, _) => backingFieldExpression.WithTriviaFrom(oldNode));
        }
    }

    private static (AccessorDeclarationSyntax getAccessor, AccessorDeclarationSyntax setAccessor)
        GetExistingAccessors(AccessorListSyntax accessorListSyntax)
        => (accessorListSyntax.Accessors.FirstOrDefault(a => a.IsKind(SyntaxKind.GetAccessorDeclaration)),
            accessorListSyntax.Accessors.FirstOrDefault(a => a.Kind() is SyntaxKind.SetAccessorDeclaration or SyntaxKind.InitAccessorDeclaration));

    protected override SyntaxNode ConvertPropertyToExpressionBodyIfDesired(
        CSharpCodeGenerationContextInfo info, SyntaxNode property)
    {
        var propertyDeclaration = (PropertyDeclarationSyntax)property;

        var preference = info.Options.PreferExpressionBodiedProperties.Value;
        if (preference == ExpressionBodyPreference.Never)
            return propertyDeclaration;

        // if there is a get accessors only, we can move the expression body to the property
        if (propertyDeclaration is
            {
                Initializer: null,
                AccessorList.Accessors: [AccessorDeclarationSyntax(SyntaxKind.GetAccessorDeclaration)
                {
                    ExpressionBody: { } expressionBody,
                    SemicolonToken: var semicolonToken
                }],
            })
        {
            return propertyDeclaration.WithExpressionBody(expressionBody)
                .WithSemicolonToken(semicolonToken)
                .WithAccessorList(null);
        }

        return propertyDeclaration;
    }

    protected override SyntaxNode GetTypeBlock(SyntaxNode syntaxNode)
        => syntaxNode;

    protected override SyntaxNode GetInitializerValue(PropertyDeclarationSyntax property)
        => property.Initializer?.Value;

    protected override PropertyDeclarationSyntax GetPropertyWithoutInitializer(PropertyDeclarationSyntax property)
        => property.WithInitializer(null);

    protected override async Task<Document> ExpandToFieldPropertyAsync(
        Document document, PropertyDeclarationSyntax property, CancellationToken cancellationToken)
    {
        var info = (CSharpCodeGenerationContextInfo)await document.GetCodeGenerationInfoAsync(CodeGenerationContext.Default, cancellationToken).ConfigureAwait(false);

        var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        // Update the getter/setter to reference the 'field' expression instead.
        var (newGetAccessor, newSetAccessor) = GetNewAccessors(info, property, FieldExpression(), cancellationToken);

        var finalProperty = CreateFinalProperty(document, property, info, newGetAccessor, newSetAccessor);
        var finalRoot = root.ReplaceNode(property, finalProperty);

        return document.WithSyntaxRoot(finalRoot);
    }
}
