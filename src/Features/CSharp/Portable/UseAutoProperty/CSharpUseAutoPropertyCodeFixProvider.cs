﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
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
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.UseAutoProperty;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseAutoProperty;

using static CSharpSyntaxTokens;
using static SyntaxFactory;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.UseAutoProperty), Shared]
[method: ImportingConstructor]
[method: SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
internal sealed class CSharpUseAutoPropertyCodeFixProvider()
    : AbstractUseAutoPropertyCodeFixProvider<
        TypeDeclarationSyntax,
        PropertyDeclarationSyntax,
        VariableDeclaratorSyntax,
        ConstructorDeclarationSyntax,
        ExpressionSyntax>
{
    protected override PropertyDeclarationSyntax GetPropertyDeclaration(SyntaxNode node)
        => (PropertyDeclarationSyntax)node;

    private static bool SupportsReadOnlyProperties(Compilation compilation)
        => compilation.LanguageVersion() >= LanguageVersion.CSharp6;

    protected override SyntaxNode GetNodeToRemove(VariableDeclaratorSyntax declarator)
    {
        var fieldDeclaration = (FieldDeclarationSyntax)declarator.GetRequiredParent().GetRequiredParent();
        var nodeToRemove = fieldDeclaration.Declaration.Variables.Count > 1 ? declarator : (SyntaxNode)fieldDeclaration;
        return nodeToRemove;
    }

    private sealed class UseAutoPropertyRewriter(
        IdentifierNameSyntax propertyIdentifierName,
        ISet<IdentifierNameSyntax> identifierNames) : CSharpSyntaxRewriter
    {
        private readonly IdentifierNameSyntax _propertyIdentifierName = propertyIdentifierName;
        private readonly ISet<IdentifierNameSyntax> _identifierNames = identifierNames;

        public override SyntaxNode? VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
        {
            if (node.Name is IdentifierNameSyntax identifierName &&
                _identifierNames.Contains(identifierName))
            {
                if (node.Expression.IsKind(SyntaxKind.ThisExpression))
                {
                    // `this.fieldName` gets rewritten to `field`.
                    return FieldExpression().WithTriviaFrom(node);
                }
                else
                {
                    // `obj.fieldName` gets rewritten to `obj.PropName`
                    return node.WithName(_propertyIdentifierName.WithTriviaFrom(identifierName));
                }
            }

            return base.VisitMemberAccessExpression(node);
        }

        public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
        {
            if (_identifierNames.Contains(node))
            {
                if (node.Parent is AssignmentExpressionSyntax
                    {
                        Parent: InitializerExpressionSyntax { RawKind: (int)SyntaxKind.ObjectInitializerExpression }
                    } assignment && assignment.Left == node)
                {
                    // `new X { fieldName = ... }` gets rewritten to `new X { propName = ... }`
                    return _propertyIdentifierName.WithTriviaFrom(node);
                }

                // Any other naked reference to fieldName within the property gets updated to `field`.
                return FieldExpression().WithTriviaFrom(node);
            }

            return base.VisitIdentifierName(node);
        }
    }

    protected override PropertyDeclarationSyntax RewriteFieldReferencesInProperty(
        PropertyDeclarationSyntax property,
        LightweightRenameLocations fieldLocations,
        CancellationToken cancellationToken)
    {
        var propertyIdentifier = property.Identifier.WithoutTrivia();
        var propertyIdentifierName = IdentifierName(propertyIdentifier);

        var identifierNames = fieldLocations.Locations
            .Select(loc => loc.Location.FindNode(cancellationToken) as IdentifierNameSyntax)
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
        PropertyDeclarationSyntax propertyDeclaration,
        bool isWrittenOutsideOfConstructor,
        bool isTrivialGetAccessor,
        bool isTrivialSetAccessor,
        CancellationToken cancellationToken)
    {
        var project = propertyDocument.Project;
        var generator = SyntaxGenerator.GetGenerator(project);

        // We may need to add a setter if the field is written to outside of the constructor
        // of it's class.

        var needsSetter = NeedsSetter(compilation, propertyDeclaration, isWrittenOutsideOfConstructor);

        var fieldInitializer = GetFieldInitializer(fieldSymbol, cancellationToken);

        if (isTrivialGetAccessor || isTrivialSetAccessor || needsSetter || fieldInitializer != null)
        {
            // If we have a trivial getters/setter then we want to convert to an accessor list to have `get;set;`.  If
            // we need a setter, we have to convert to having an accessor list.  If we have a field initializer, we need
            // to convert to an accessor list to add the initializer expression after.
            var accessorList = ConvertToAccessorList(
                propertyDeclaration, isTrivialGetAccessor, isTrivialSetAccessor);

            var updatedProperty = propertyDeclaration
                .WithAccessorList(accessorList)
                .WithExpressionBody(null)
                .WithSemicolonToken(default);

            if (needsSetter)
            {
                var accessor = AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SemicolonToken);

                if (fieldSymbol.DeclaredAccessibility != propertySymbol.DeclaredAccessibility)
                    accessor = (AccessorDeclarationSyntax)generator.WithAccessibility(accessor, fieldSymbol.DeclaredAccessibility);

                var modifiers = TokenList(
                    updatedProperty.Modifiers.Where(token => !token.IsKind(SyntaxKind.ReadOnlyKeyword)));

                updatedProperty = updatedProperty.WithModifiers(modifiers)
                                                 .AddAccessorListAccessors(accessor);
            }

            if (fieldInitializer != null)
            {
                updatedProperty = updatedProperty.WithInitializer(EqualsValueClause(fieldInitializer))
                                                 .WithSemicolonToken(SemicolonToken);
            }

            var finalProperty = updatedProperty
                .WithTrailingTrivia(propertyDeclaration.GetTrailingTrivia())
                .WithAdditionalAnnotations(SpecializedFormattingAnnotation);
            return Task.FromResult<SyntaxNode>(finalProperty);

        }
        else
        {
            // Nothing to actually do.  We're not changing the accessors to `get;set;` accessors, and we didn't have to
            // add an setter.  We also had no field initializer to move over.  This can happen when we're converting to
            // using `field` and that rewrite already happened.
            return Task.FromResult<SyntaxNode>(propertyDeclaration);
        }
    }

    private static AccessorListSyntax ConvertToAccessorList(
        PropertyDeclarationSyntax propertyDeclaration, bool isTrivialGetAccessor, bool isTrivialSetAccessor)
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
                    (isTrivialSetAccessor && accessor.Kind() is SyntaxKind.SetAccessorDeclaration or SyntaxKind.InitAccessorDeclaration);

                if (convert)
                    return accessor.WithExpressionBody(null).WithBody(null).WithSemicolonToken(SemicolonToken);

                return accessor;
            })));
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

        public override AdjustNewLinesOperation? GetAdjustNewLinesOperation(in SyntaxToken previousToken, in SyntaxToken currentToken, in NextGetAdjustNewLinesOperation nextOperation)
        {
            if (ForceSingleSpace(previousToken, currentToken))
                return null;

            return base.GetAdjustNewLinesOperation(in previousToken, in currentToken, in nextOperation);
        }

        public override AdjustSpacesOperation? GetAdjustSpacesOperation(in SyntaxToken previousToken, in SyntaxToken currentToken, in NextGetAdjustSpacesOperation nextOperation)
        {
            if (ForceSingleSpace(previousToken, currentToken))
                return new AdjustSpacesOperation(1, AdjustSpacesOption.ForceSpaces);

            return base.GetAdjustSpacesOperation(in previousToken, in currentToken, in nextOperation);
        }
    }

    private static ExpressionSyntax? GetFieldInitializer(IFieldSymbol fieldSymbol, CancellationToken cancellationToken)
    {
        var variableDeclarator = (VariableDeclaratorSyntax)fieldSymbol.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken);
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
}
