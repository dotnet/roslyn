// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.UseAutoProperty;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseAutoProperty;

using static CSharpSyntaxTokens;
using static SyntaxFactory;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseAutoProperty), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed partial class CSharpUseAutoPropertyCodeFixProvider()
    : AbstractUseAutoPropertyCodeFixProvider<
        CSharpUseAutoPropertyCodeFixProvider,
        TypeDeclarationSyntax,
        PropertyDeclarationSyntax,
        VariableDeclaratorSyntax,
        ConstructorDeclarationSyntax,
        ExpressionSyntax>
{
    protected override ISyntaxFormatting SyntaxFormatting => CSharpSyntaxFormatting.Instance;

    protected override PropertyDeclarationSyntax GetPropertyDeclaration(SyntaxNode node)
        => (PropertyDeclarationSyntax)node;

    private static bool SupportsReadOnlyProperties(Compilation compilation)
        => compilation.LanguageVersion() >= LanguageVersion.CSharp6;

    private static bool IsSetOrInitAccessor(AccessorDeclarationSyntax accessor)
        => accessor.Kind() is SyntaxKind.SetAccessorDeclaration or SyntaxKind.InitAccessorDeclaration;

    private static FieldDeclarationSyntax GetFieldDeclaration(VariableDeclaratorSyntax declarator)
        => (FieldDeclarationSyntax)declarator.GetRequiredParent().GetRequiredParent();

    protected override SyntaxNode GetNodeToRemove(VariableDeclaratorSyntax declarator)
    {
        var fieldDeclaration = GetFieldDeclaration(declarator);
        return fieldDeclaration.Declaration.Variables.Count > 1 ? declarator : fieldDeclaration;
    }

    protected override PropertyDeclarationSyntax RewriteFieldReferencesInProperty(
        PropertyDeclarationSyntax property,
        ImmutableArray<ReferencedSymbol> fieldLocations,
        CancellationToken cancellationToken)
    {
        // We're going to walk this property body, converting most reference of the field to use the `field` keyword
        // instead.  However, not all reference can be updated.  For example, reference through another instance.  Those
        // we update to point at the property instead.  So we grab that property name here to use in the rewriter.
        var propertyIdentifierName = IdentifierName(property.Identifier.WithoutTrivia());

        var identifierNames = fieldLocations.SelectMany(loc => loc.Locations.Select(loc => loc.Location.FindNode(getInnermostNodeForTie: true, cancellationToken) as IdentifierNameSyntax))
            .WhereNotNull()
            .ToSet();

        var rewriter = new UseAutoPropertyRewriter(propertyIdentifierName, identifierNames);
        return (PropertyDeclarationSyntax)rewriter.Visit(property);
    }

    protected override Task<SyntaxNode> UpdatePropertyAsync(
        Document propertyDocument,
        Compilation compilation,
        IFieldSymbol fieldSymbol,
        IPropertySymbol propertySymbol,
        VariableDeclaratorSyntax fieldDeclarator,
        PropertyDeclarationSyntax propertyDeclaration,
        bool isWrittenOutsideOfConstructor,
        bool isTrivialGetAccessor,
        bool isTrivialSetAccessor,
        CancellationToken cancellationToken)
    {
        var project = propertyDocument.Project;
        var generator = SyntaxGenerator.GetGenerator(project);

        // Ensure that any attributes on the field are moved over to the property.
        propertyDeclaration = MoveAttributes(propertyDeclaration, GetFieldDeclaration(fieldDeclarator));

        // We may need to add a setter if the field is written to outside of the constructor
        // of it's class.
        var needsSetter = NeedsSetter(compilation, propertyDeclaration, isWrittenOutsideOfConstructor);
        var fieldInitializer = fieldDeclarator.Initializer?.Value;

        if (!isTrivialGetAccessor && !isTrivialSetAccessor && !needsSetter && fieldInitializer == null)
        {
            // Nothing to actually do.  We're not changing the accessors to `get;set;` accessors, and we didn't have to
            // add an setter.  We also had no field initializer to move over.  This can happen when we're converting to
            // using `field` and that rewrite already happened.
            return Task.FromResult<SyntaxNode>(propertyDeclaration);
        }

        // 1. If we have a trivial getters/setter then we want to convert to an accessor list to have `get;set;`
        // 2. If we need a setter, we have to convert to having an accessor list to place the setter in.
        // 3. If we have a field initializer, we need to convert to an accessor list to add the initializer expression after.
        var updatedProperty = propertyDeclaration
            .WithExpressionBody(null)
            .WithSemicolonToken(default)
            .WithAccessorList(ConvertToAccessorList(
                propertyDeclaration, isTrivialGetAccessor, isTrivialSetAccessor));

        if (needsSetter)
        {
            var accessor = AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SemicolonToken);

            if (fieldSymbol.DeclaredAccessibility != propertySymbol.DeclaredAccessibility)
                accessor = (AccessorDeclarationSyntax)generator.WithAccessibility(accessor, fieldSymbol.DeclaredAccessibility);

            updatedProperty = updatedProperty
                .AddAccessorListAccessors(accessor)
                .WithModifiers(TokenList(updatedProperty.Modifiers.Where(token => !token.IsKind(SyntaxKind.ReadOnlyKeyword))));
        }

        // Move any field initializer over to the property as well.
        if (fieldInitializer != null)
        {
            updatedProperty = updatedProperty.WithoutTrailingTrivia()
                .WithInitializer(EqualsValueClause(EqualsToken.WithLeadingTrivia(Space).WithTrailingTrivia(Space), fieldInitializer))
                .WithSemicolonToken(SemicolonToken);
        }

        var finalProperty = updatedProperty
            .WithTrailingTrivia(propertyDeclaration.GetTrailingTrivia())
            .WithAdditionalAnnotations(SpecializedFormattingAnnotation);
        return Task.FromResult<SyntaxNode>(finalProperty);

        static PropertyDeclarationSyntax MoveAttributes(
            PropertyDeclarationSyntax property,
            FieldDeclarationSyntax field)
        {
            var fieldAttributes = field.AttributeLists;
            if (fieldAttributes.Count == 0)
                return property;

            var leadingTrivia = property.GetLeadingTrivia();
            var indentation = leadingTrivia is [.., (kind: SyntaxKind.WhitespaceTrivia) whitespaceTrivia]
                ? whitespaceTrivia
                : default;

            using var _ = ArrayBuilder<AttributeListSyntax>.GetInstance(out var finalAttributes);
            foreach (var attributeList in fieldAttributes)
            {
                // Change any field attributes to be `[field: ...]` attributes. Take the property's trivia and place it
                // on the first field attribute we move over.
                var converted = ConvertAttributeList(attributeList);
                finalAttributes.Add(attributeList == fieldAttributes[0]
                    ? converted.WithLeadingTrivia(leadingTrivia)
                    : converted);
            }

            foreach (var attributeList in property.AttributeLists)
            {
                // Remove the leading trivia off of the first attribute.  We're going to move it before all the new
                // field attributes we're adding.
                finalAttributes.Add(attributeList == property.AttributeLists[0]
                    ? attributeList.WithLeadingTrivia(indentation)
                    : attributeList);
            }

            return property
                .WithAttributeLists([])
                .WithLeadingTrivia(indentation)
                .WithAttributeLists(List(finalAttributes));
        }

        static AttributeListSyntax ConvertAttributeList(AttributeListSyntax attributeList)
            => attributeList.WithTarget(AttributeTargetSpecifier(Identifier(SyntaxFacts.GetText(SyntaxKind.FieldKeyword)), ColonToken.WithTrailingTrivia(Space)));

        static AccessorListSyntax ConvertToAccessorList(
            PropertyDeclarationSyntax propertyDeclaration,
            bool isTrivialGetAccessor,
            bool isTrivialSetAccessor)
        {
            // If we don't have an accessor list at all, convert the property's expr body to a `get => ...` accessor.
            var accessorList = propertyDeclaration.AccessorList ?? AccessorList(SingletonList(
                AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithExpressionBody(propertyDeclaration.ExpressionBody)
                    .WithSemicolonToken(SemicolonToken)));

            // Now that we have an accessor list, convert the getter/setter to `get;`/`set;` form if requested.
            return accessorList.WithAccessors(List(accessorList.Accessors.Select(
                accessor =>
                {
                    var convert =
                        (isTrivialGetAccessor && accessor.Kind() is SyntaxKind.GetAccessorDeclaration) ||
                        (isTrivialSetAccessor && IsSetOrInitAccessor(accessor));

                    if (convert)
                    {
                        if (accessor.ExpressionBody != null)
                            return accessor.WithExpressionBody(null).WithKeyword(accessor.Keyword.WithoutTrailingTrivia());

                        if (accessor.Body != null)
                            return accessor.WithBody(null).WithSemicolonToken(SemicolonToken.WithTrailingTrivia(accessor.Body.CloseBraceToken.TrailingTrivia));
                    }

                    return accessor;
                })));
        }
    }

    protected override ImmutableArray<AbstractFormattingRule> GetFormattingRules(
        Document document,
        SyntaxNode propertyDeclaration)
    {
        // If the final property is only simple `get;set;` accessors, then reformat the property to be on a single line.
        if (propertyDeclaration is PropertyDeclarationSyntax { AccessorList.Accessors: var accessors } &&
            accessors.All(a => a is { ExpressionBody: null, Body: null, AttributeLists.Count: 0 }))
        {
            return [new SingleLinePropertyFormattingRule(), .. this.SyntaxFormatting.GetDefaultFormattingRules()];
        }

        return default;
    }

    private static bool NeedsSetter(Compilation compilation, PropertyDeclarationSyntax propertyDeclaration, bool isWrittenOutsideOfConstructor)
    {
        // Don't need to add if we already have a setter.
        if (propertyDeclaration.AccessorList != null &&
            propertyDeclaration.AccessorList.Accessors.Any(IsSetOrInitAccessor))
        {
            return false;
        }

        // If the language doesn't have readonly properties, then we'll need a setter here.
        if (!SupportsReadOnlyProperties(compilation))
            return true;

        // If we're written outside a constructor we need a setter.
        return isWrittenOutsideOfConstructor;
    }
}
