// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.UseAutoProperty;

namespace Microsoft.CodeAnalysis.CSharp.UseAutoProperty;

using static CSharpSyntaxTokens;
using static SyntaxFactory;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseAutoProperty), Shared]
internal class CSharpUseAutoPropertyCodeFixProvider
    : AbstractUseAutoPropertyCodeFixProvider<TypeDeclarationSyntax, PropertyDeclarationSyntax, VariableDeclaratorSyntax, ConstructorDeclarationSyntax, ExpressionSyntax>
{
    [ImportingConstructor]
    [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
    public CSharpUseAutoPropertyCodeFixProvider()
    {
    }

    protected override PropertyDeclarationSyntax GetPropertyDeclaration(SyntaxNode node)
        => (PropertyDeclarationSyntax)node;

    protected override SyntaxNode GetNodeToRemove(VariableDeclaratorSyntax declarator)
    {
        var fieldDeclaration = (FieldDeclarationSyntax)declarator.Parent.Parent;
        var nodeToRemove = fieldDeclaration.Declaration.Variables.Count > 1 ? declarator : (SyntaxNode)fieldDeclaration;
        return nodeToRemove;
    }

    protected override async Task<SyntaxNode> UpdatePropertyAsync(
        Document propertyDocument, Compilation compilation, IFieldSymbol fieldSymbol, IPropertySymbol propertySymbol,
        PropertyDeclarationSyntax propertyDeclaration, bool isWrittenOutsideOfConstructor, CancellationToken cancellationToken)
    {
        var project = propertyDocument.Project;
        var trailingTrivia = propertyDeclaration.GetTrailingTrivia();

        var updatedProperty = propertyDeclaration.WithAccessorList(UpdateAccessorList(propertyDeclaration.AccessorList))
                                                 .WithExpressionBody(null)
                                                 .WithSemicolonToken(Token(SyntaxKind.None));

        // We may need to add a setter if the field is written to outside of the constructor
        // of it's class.
        if (NeedsSetter(compilation, propertyDeclaration, isWrittenOutsideOfConstructor))
        {
            var accessor = AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                           .WithSemicolonToken(SemicolonToken);
            var generator = SyntaxGenerator.GetGenerator(project);

            if (fieldSymbol.DeclaredAccessibility != propertySymbol.DeclaredAccessibility)
            {
                accessor = (AccessorDeclarationSyntax)generator.WithAccessibility(accessor, fieldSymbol.DeclaredAccessibility);
            }

            var modifiers = TokenList(
                updatedProperty.Modifiers.Where(token => !token.IsKind(SyntaxKind.ReadOnlyKeyword)));

            updatedProperty = updatedProperty.WithModifiers(modifiers)
                                             .AddAccessorListAccessors(accessor);
        }

        var fieldInitializer = await GetFieldInitializerAsync(fieldSymbol, cancellationToken).ConfigureAwait(false);
        if (fieldInitializer != null)
        {
            updatedProperty = updatedProperty.WithInitializer(EqualsValueClause(fieldInitializer))
                                             .WithSemicolonToken(SemicolonToken);
        }

        return updatedProperty.WithTrailingTrivia(trailingTrivia).WithAdditionalAnnotations(SpecializedFormattingAnnotation);
    }

    protected override ImmutableArray<AbstractFormattingRule> GetFormattingRules(Document document)
        => [new SingleLinePropertyFormattingRule(), .. Formatter.GetDefaultFormattingRules(document)];

    private class SingleLinePropertyFormattingRule : AbstractFormattingRule
    {
        private static bool ForceSingleSpace(SyntaxToken previousToken, SyntaxToken currentToken)
        {
            if (currentToken.IsKind(SyntaxKind.OpenBraceToken) && currentToken.Parent.IsKind(SyntaxKind.AccessorList))
            {
                return true;
            }

            if (previousToken.IsKind(SyntaxKind.OpenBraceToken) && previousToken.Parent.IsKind(SyntaxKind.AccessorList))
            {
                return true;
            }

            if (currentToken.IsKind(SyntaxKind.CloseBraceToken) && currentToken.Parent.IsKind(SyntaxKind.AccessorList))
            {
                return true;
            }

            return false;
        }

        public override AdjustNewLinesOperation GetAdjustNewLinesOperation(in SyntaxToken previousToken, in SyntaxToken currentToken, in NextGetAdjustNewLinesOperation nextOperation)
        {
            if (ForceSingleSpace(previousToken, currentToken))
            {
                return null;
            }

            return base.GetAdjustNewLinesOperation(in previousToken, in currentToken, in nextOperation);
        }

        public override AdjustSpacesOperation GetAdjustSpacesOperation(in SyntaxToken previousToken, in SyntaxToken currentToken, in NextGetAdjustSpacesOperation nextOperation)
        {
            if (ForceSingleSpace(previousToken, currentToken))
            {
                return new AdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);
            }

            return base.GetAdjustSpacesOperation(in previousToken, in currentToken, in nextOperation);
        }
    }

    private static async Task<ExpressionSyntax> GetFieldInitializerAsync(IFieldSymbol fieldSymbol, CancellationToken cancellationToken)
    {
        var variableDeclarator = (VariableDeclaratorSyntax)await fieldSymbol.DeclaringSyntaxReferences[0].GetSyntaxAsync(cancellationToken).ConfigureAwait(false);
        return variableDeclarator.Initializer?.Value;
    }

    private static bool NeedsSetter(Compilation compilation, PropertyDeclarationSyntax propertyDeclaration, bool isWrittenOutsideOfConstructor)
    {
        if (propertyDeclaration.AccessorList?.Accessors.Any(SyntaxKind.SetAccessorDeclaration) == true)
        {
            // Already has a setter.
            return false;
        }

        if (!SupportsReadOnlyProperties(compilation))
        {
            // If the language doesn't have readonly properties, then we'll need a 
            // setter here.
            return true;
        }

        // If we're written outside a constructor we need a setter.
        return isWrittenOutsideOfConstructor;
    }

    private static bool SupportsReadOnlyProperties(Compilation compilation)
        => compilation.LanguageVersion() >= LanguageVersion.CSharp6;

    private static AccessorListSyntax UpdateAccessorList(AccessorListSyntax accessorList)
    {
        if (accessorList == null)
        {
            var getter = AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                      .WithSemicolonToken(SemicolonToken);
            return AccessorList([getter]);
        }

        return accessorList.WithAccessors([.. GetAccessors(accessorList.Accessors)]);
    }

    private static IEnumerable<AccessorDeclarationSyntax> GetAccessors(SyntaxList<AccessorDeclarationSyntax> accessors)
    {
        foreach (var accessor in accessors)
        {
            yield return accessor.WithBody(null)
                                 .WithExpressionBody(null)
                                 .WithSemicolonToken(SemicolonToken);
        }
    }
}
