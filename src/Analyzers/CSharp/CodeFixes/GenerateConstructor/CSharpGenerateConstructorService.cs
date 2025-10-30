// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.InitializeParameter;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.GenerateMember.GenerateConstructor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.GenerateConstructor;

[ExportLanguageService(typeof(IGenerateConstructorService), LanguageNames.CSharp), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class CSharpGenerateConstructorService()
    : AbstractGenerateConstructorService<CSharpGenerateConstructorService, ExpressionSyntax>
{
    protected override bool ContainingTypesOrSelfHasUnsafeKeyword(INamedTypeSymbol containingType)
       => containingType.ContainingTypesOrSelfHasUnsafeKeyword();

    protected override bool IsSimpleNameGeneration(SemanticDocument document, SyntaxNode node, CancellationToken cancellationToken)
        => node is SimpleNameSyntax;

    protected override bool IsConstructorInitializerGeneration(SemanticDocument document, SyntaxNode node, CancellationToken cancellationToken)
        => node is ConstructorInitializerSyntax;

    protected override bool IsImplicitObjectCreation(SemanticDocument document, SyntaxNode node, CancellationToken cancellationToken)
        => node is ImplicitObjectCreationExpressionSyntax;

    protected override bool TryInitializeConstructorInitializerGeneration(
        SemanticDocument document,
        SyntaxNode node,
        CancellationToken cancellationToken,
        out SyntaxToken token,
        out ImmutableArray<Argument<ExpressionSyntax>> arguments,
        [NotNullWhen(true)] out INamedTypeSymbol? typeToGenerateIn)
    {
        var constructorInitializer = (ConstructorInitializerSyntax)node;

        if (!constructorInitializer.ArgumentList.CloseParenToken.IsMissing)
        {
            token = constructorInitializer.ThisOrBaseKeyword;
            arguments = GetArguments(constructorInitializer.ArgumentList.Arguments);

            var semanticModel = document.SemanticModel;
            var currentType = semanticModel.GetEnclosingNamedType(constructorInitializer.SpanStart, cancellationToken);
            typeToGenerateIn = constructorInitializer.IsKind(SyntaxKind.ThisConstructorInitializer)
                ? currentType
                : currentType?.BaseType?.OriginalDefinition;
            return typeToGenerateIn != null;
        }

        token = default;
        arguments = default;
        typeToGenerateIn = null;
        return false;
    }

    private static ImmutableArray<Argument<ExpressionSyntax>> GetArguments(SeparatedSyntaxList<ArgumentSyntax> arguments)
        => arguments.SelectAsArray(InitializeParameterHelpers.GetArgument);

    private static ImmutableArray<Argument<ExpressionSyntax>> GetArguments(SeparatedSyntaxList<AttributeArgumentSyntax> arguments)
        => arguments.SelectAsArray(a => new Argument<ExpressionSyntax>(
            refKind: RefKind.None,
            a.NameEquals?.Name.Identifier.ValueText ?? a.NameColon?.Name.Identifier.ValueText,
            a.Expression));

    protected override bool TryInitializeSimpleNameGenerationState(
        SemanticDocument document,
        SyntaxNode node,
        CancellationToken cancellationToken,
        out SyntaxToken token,
        out ImmutableArray<Argument<ExpressionSyntax>> arguments,
        [NotNullWhen(true)] out INamedTypeSymbol? typeToGenerateIn)
    {
        var simpleName = (SimpleNameSyntax)node;
        var fullName = simpleName.IsRightSideOfQualifiedName()
            ? (NameSyntax)simpleName.GetRequiredParent()
            : simpleName;

        if (fullName.Parent is ObjectCreationExpressionSyntax objectCreationExpression)
        {
            if (objectCreationExpression.ArgumentList != null &&
                !objectCreationExpression.ArgumentList.CloseParenToken.IsMissing)
            {
                var symbolInfo = document.SemanticModel.GetSymbolInfo(objectCreationExpression.Type, cancellationToken);
                token = simpleName.Identifier;
                arguments = GetArguments(objectCreationExpression.ArgumentList.Arguments);
                typeToGenerateIn = symbolInfo.GetAnySymbol() as INamedTypeSymbol;
                return typeToGenerateIn != null;
            }
        }

        token = default;
        arguments = default;
        typeToGenerateIn = null;
        return false;
    }

    protected override bool TryInitializeSimpleAttributeNameGenerationState(
        SemanticDocument document,
        SyntaxNode node,
        CancellationToken cancellationToken,
        out SyntaxToken token,
        out ImmutableArray<Argument<ExpressionSyntax>> arguments,
        [NotNullWhen(true)] out INamedTypeSymbol? typeToGenerateIn)
    {
        var simpleName = (SimpleNameSyntax)node;
        var fullName = simpleName.IsRightSideOfQualifiedName()
            ? (NameSyntax)simpleName.GetRequiredParent()
            : simpleName;

        if (fullName.Parent is AttributeSyntax attribute)
        {
            if (attribute.ArgumentList != null &&
                !attribute.ArgumentList.CloseParenToken.IsMissing)
            {
                var symbolInfo = document.SemanticModel.GetSymbolInfo(attribute, cancellationToken);
                if (symbolInfo.CandidateReason == CandidateReason.OverloadResolutionFailure && !symbolInfo.CandidateSymbols.IsEmpty)
                {
                    token = simpleName.Identifier;
                    arguments = GetArguments(attribute.ArgumentList.Arguments);

                    typeToGenerateIn = symbolInfo.CandidateSymbols.FirstOrDefault()?.ContainingSymbol as INamedTypeSymbol;
                    return typeToGenerateIn != null;
                }
            }
        }

        token = default;
        arguments = default;
        typeToGenerateIn = null;
        return false;
    }

    protected override bool TryInitializeImplicitObjectCreation(SemanticDocument document,
        SyntaxNode node,
        CancellationToken cancellationToken,
        out SyntaxToken token,
        out ImmutableArray<Argument<ExpressionSyntax>> arguments,
        [NotNullWhen(true)] out INamedTypeSymbol? typeToGenerateIn)
    {
        var implicitObjectCreation = (ImplicitObjectCreationExpressionSyntax)node;
        if (implicitObjectCreation.ArgumentList != null &&
            !implicitObjectCreation.ArgumentList.CloseParenToken.IsMissing)
        {
            var typeInfo = document.SemanticModel.GetTypeInfo(implicitObjectCreation, cancellationToken);
            if (typeInfo.Type is INamedTypeSymbol typeSymbol)
            {
                token = implicitObjectCreation.NewKeyword;
                arguments = GetArguments(implicitObjectCreation.ArgumentList.Arguments);
                typeToGenerateIn = typeSymbol;
                return true;
            }
        }

        token = default;
        arguments = default;
        typeToGenerateIn = null;
        return false;
    }

    protected override string GenerateNameForExpression(SemanticModel semanticModel, ExpressionSyntax expression, CancellationToken cancellationToken)
        => semanticModel.GenerateNameForExpression(expression, capitalize: false, cancellationToken: cancellationToken);

    protected override ITypeSymbol GetArgumentType(SemanticModel semanticModel, Argument<ExpressionSyntax> argument, CancellationToken cancellationToken)
        => InternalExtensions.DetermineParameterType(argument.Expression!, semanticModel, cancellationToken);

    protected override bool IsConversionImplicit(Compilation compilation, ITypeSymbol sourceType, ITypeSymbol targetType)
        => compilation.ClassifyConversion(sourceType, targetType).IsImplicit;

    protected override IMethodSymbol? GetCurrentConstructor(SemanticModel semanticModel, SyntaxToken token, CancellationToken cancellationToken)
        => token.GetAncestor<ConstructorDeclarationSyntax>() is { } constructor ? semanticModel.GetDeclaredSymbol(constructor, cancellationToken) : null;

    protected override IMethodSymbol? GetDelegatedConstructor(SemanticModel semanticModel, IMethodSymbol constructor, CancellationToken cancellationToken)
    {
        if (constructor.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken) is ConstructorDeclarationSyntax constructorDeclarationSyntax &&
            constructorDeclarationSyntax.Initializer.IsKind(SyntaxKind.ThisConstructorInitializer))
        {
            return semanticModel.GetSymbolInfo(constructorDeclarationSyntax.Initializer, cancellationToken).Symbol as IMethodSymbol;
        }

        return null;
    }
}
