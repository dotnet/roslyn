// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles;
using Microsoft.CodeAnalysis.GenerateMember.GenerateConstructor;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.GenerateConstructor
{
    [ExportLanguageService(typeof(IGenerateConstructorService), LanguageNames.CSharp), Shared]
    internal class CSharpGenerateConstructorService : AbstractGenerateConstructorService<CSharpGenerateConstructorService, ArgumentSyntax, AttributeArgumentSyntax>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpGenerateConstructorService()
        {
        }

        protected override bool ContainingTypesOrSelfHasUnsafeKeyword(INamedTypeSymbol containingType)
           => containingType.ContainingTypesOrSelfHasUnsafeKeyword();

        protected override bool IsSimpleNameGeneration(SemanticDocument document, SyntaxNode node, CancellationToken cancellationToken)
            => node is SimpleNameSyntax;

        protected override bool IsConstructorInitializerGeneration(SemanticDocument document, SyntaxNode node, CancellationToken cancellationToken)
            => node is ConstructorInitializerSyntax;

        protected override bool IsImplicitObjectCreation(SemanticDocument document, SyntaxNode node, CancellationToken cancellationToken)
            => node is ImplicitObjectCreationExpressionSyntax;

        protected override bool TryInitializeConstructorInitializerGeneration(
            SemanticDocument document, SyntaxNode node, CancellationToken cancellationToken,
            out SyntaxToken token, out ImmutableArray<ArgumentSyntax> arguments, out INamedTypeSymbol typeToGenerateIn)
        {
            var constructorInitializer = (ConstructorInitializerSyntax)node;

            if (!constructorInitializer.ArgumentList.CloseParenToken.IsMissing)
            {
                token = constructorInitializer.ThisOrBaseKeyword;
                arguments = constructorInitializer.ArgumentList.Arguments.ToImmutableArray();

                var semanticModel = document.SemanticModel;
                var currentType = semanticModel.GetEnclosingNamedType(constructorInitializer.SpanStart, cancellationToken);
                typeToGenerateIn = constructorInitializer.IsKind(SyntaxKind.ThisConstructorInitializer)
                    ? currentType
                    : currentType.BaseType.OriginalDefinition;
                return typeToGenerateIn != null;
            }

            token = default;
            arguments = default;
            typeToGenerateIn = null;
            return false;
        }

        protected override bool TryInitializeSimpleNameGenerationState(
            SemanticDocument document,
            SyntaxNode node,
            CancellationToken cancellationToken,
            out SyntaxToken token,
            out ImmutableArray<ArgumentSyntax> arguments,
            out INamedTypeSymbol typeToGenerateIn)
        {
            var simpleName = (SimpleNameSyntax)node;
            var fullName = simpleName.IsRightSideOfQualifiedName()
                ? (NameSyntax)simpleName.Parent
                : simpleName;

            if (fullName.Parent is ObjectCreationExpressionSyntax)
            {
                var objectCreationExpression = (ObjectCreationExpressionSyntax)fullName.Parent;
                if (objectCreationExpression.ArgumentList != null &&
                    !objectCreationExpression.ArgumentList.CloseParenToken.IsMissing)
                {
                    var symbolInfo = document.SemanticModel.GetSymbolInfo(objectCreationExpression.Type, cancellationToken);
                    token = simpleName.Identifier;
                    arguments = objectCreationExpression.ArgumentList.Arguments.ToImmutableArray();
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
            out ImmutableArray<ArgumentSyntax> arguments,
            out ImmutableArray<AttributeArgumentSyntax> attributeArguments,
            out INamedTypeSymbol typeToGenerateIn)
        {
            var simpleName = (SimpleNameSyntax)node;
            var fullName = simpleName.IsRightSideOfQualifiedName()
                ? (NameSyntax)simpleName.Parent
                : simpleName;

            if (fullName.Parent is AttributeSyntax)
            {
                var attribute = (AttributeSyntax)fullName.Parent;
                if (attribute.ArgumentList != null &&
                    !attribute.ArgumentList.CloseParenToken.IsMissing)
                {
                    var symbolInfo = document.SemanticModel.GetSymbolInfo(attribute, cancellationToken);
                    if (symbolInfo.CandidateReason == CandidateReason.OverloadResolutionFailure && !symbolInfo.CandidateSymbols.IsEmpty)
                    {
                        token = simpleName.Identifier;
                        attributeArguments = attribute.ArgumentList.Arguments.ToImmutableArray();
                        arguments = attributeArguments.Select(
                            x => SyntaxFactory.Argument(
                                x.NameColon ?? (x.NameEquals != null ? SyntaxFactory.NameColon(x.NameEquals.Name) : null),
                                default, x.Expression)).ToImmutableArray();

                        typeToGenerateIn = symbolInfo.CandidateSymbols.FirstOrDefault().ContainingSymbol as INamedTypeSymbol;
                        return typeToGenerateIn != null;
                    }
                }
            }

            token = default;
            arguments = default;
            attributeArguments = default;
            typeToGenerateIn = null;
            return false;
        }

        protected override bool TryInitializeImplicitObjectCreation(SemanticDocument document,
            SyntaxNode node,
            CancellationToken cancellationToken,
            out SyntaxToken token,
            out ImmutableArray<ArgumentSyntax> arguments,
            out INamedTypeSymbol typeToGenerateIn)
        {
            var implicitObjectCreation = (ImplicitObjectCreationExpressionSyntax)node;
            if (implicitObjectCreation.ArgumentList != null &&
                !implicitObjectCreation.ArgumentList.CloseParenToken.IsMissing)
            {
                var typeInfo = document.SemanticModel.GetTypeInfo(implicitObjectCreation, cancellationToken);
                if (typeInfo.Type is INamedTypeSymbol typeSymbol)
                {
                    token = implicitObjectCreation.NewKeyword;
                    arguments = implicitObjectCreation.ArgumentList.Arguments.ToImmutableArray();
                    typeToGenerateIn = typeSymbol;
                    return true;
                }
            }

            token = default;
            arguments = default;
            typeToGenerateIn = null;
            return false;
        }

        protected override ImmutableArray<ParameterName> GenerateParameterNames(
            SemanticModel semanticModel, IEnumerable<ArgumentSyntax> arguments, IList<string> reservedNames, NamingRule parameterNamingRule, CancellationToken cancellationToken)
            => semanticModel.GenerateParameterNames(arguments, reservedNames, parameterNamingRule, cancellationToken);

        protected override ImmutableArray<ParameterName> GenerateParameterNames(
            SemanticModel semanticModel, IEnumerable<AttributeArgumentSyntax> arguments, IList<string> reservedNames, NamingRule parameterNamingRule, CancellationToken cancellationToken)
            => semanticModel.GenerateParameterNames(arguments, reservedNames, parameterNamingRule, cancellationToken);

        protected override string GenerateNameForArgument(SemanticModel semanticModel, ArgumentSyntax argument, CancellationToken cancellationToken)
            => semanticModel.GenerateNameForArgument(argument, cancellationToken);

        protected override string GenerateNameForArgument(SemanticModel semanticModel, AttributeArgumentSyntax argument, CancellationToken cancellationToken)
            => semanticModel.GenerateNameForArgument(argument, cancellationToken);

        protected override RefKind GetRefKind(ArgumentSyntax argument)
            => argument.GetRefKind();

        protected override bool IsNamedArgument(ArgumentSyntax argument)
            => argument.NameColon != null;

        protected override ITypeSymbol GetArgumentType(
            SemanticModel semanticModel, ArgumentSyntax argument, CancellationToken cancellationToken)
        {
            return argument.DetermineParameterType(semanticModel, cancellationToken);
        }

        protected override ITypeSymbol GetAttributeArgumentType(
            SemanticModel semanticModel, AttributeArgumentSyntax argument, CancellationToken cancellationToken)
        {
            return semanticModel.GetType(argument.Expression, cancellationToken);
        }

        protected override bool IsConversionImplicit(Compilation compilation, ITypeSymbol sourceType, ITypeSymbol targetType)
            => compilation.ClassifyConversion(sourceType, targetType).IsImplicit;

        protected override IMethodSymbol GetCurrentConstructor(SemanticModel semanticModel, SyntaxToken token, CancellationToken cancellationToken)
            => token.GetAncestor<ConstructorDeclarationSyntax>() is { } constructor ? semanticModel.GetDeclaredSymbol(constructor, cancellationToken) : null;

        protected override IMethodSymbol GetDelegatedConstructor(SemanticModel semanticModel, IMethodSymbol constructor, CancellationToken cancellationToken)
        {
            if (constructor.DeclaringSyntaxReferences[0].GetSyntax(cancellationToken) is ConstructorDeclarationSyntax constructorDeclarationSyntax &&
                constructorDeclarationSyntax.Initializer.IsKind(SyntaxKind.ThisConstructorInitializer))
            {
                return semanticModel.GetSymbolInfo(constructorDeclarationSyntax.Initializer, cancellationToken).Symbol as IMethodSymbol;
            }

            return null;
        }
    }
}
