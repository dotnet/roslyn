// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.GenerateMember.GenerateMethod;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.GenerateMember.GenerateParameterizedMember
{
    [ExportLanguageService(typeof(IGenerateConversionService), LanguageNames.CSharp), Shared]
    internal partial class CSharpGenerateConversionService :
        AbstractGenerateConversionService<CSharpGenerateConversionService, SimpleNameSyntax, ExpressionSyntax, InvocationExpressionSyntax>
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpGenerateConversionService()
        {
        }

        protected override bool IsImplicitConversionGeneration(SyntaxNode node)
        {
            return node is ExpressionSyntax &&
                    (node.Parent is AssignmentExpressionSyntax || node.Parent is EqualsValueClauseSyntax) &&
                    !(node is CastExpressionSyntax) &&
                    !(node is MemberAccessExpressionSyntax);
        }

        protected override bool IsExplicitConversionGeneration(SyntaxNode node)
            => node is CastExpressionSyntax;

        protected override bool ContainingTypesOrSelfHasUnsafeKeyword(INamedTypeSymbol containingType)
            => containingType.ContainingTypesOrSelfHasUnsafeKeyword();

        protected override AbstractInvocationInfo CreateInvocationMethodInfo(
            SemanticDocument document, AbstractGenerateParameterizedMemberService<CSharpGenerateConversionService, SimpleNameSyntax, ExpressionSyntax, InvocationExpressionSyntax>.State state)
        {
            return new CSharpGenerateParameterizedMemberService<CSharpGenerateConversionService>.InvocationExpressionInfo(document, state);
        }

        protected override bool AreSpecialOptionsActive(SemanticModel semanticModel)
            => CSharpCommonGenerationServiceMethods.AreSpecialOptionsActive();

        protected override bool IsValidSymbol(ISymbol symbol, SemanticModel semanticModel)
            => CSharpCommonGenerationServiceMethods.IsValidSymbol();

        protected override bool TryInitializeImplicitConversionState(
           SemanticDocument document,
           SyntaxNode expression,
           ISet<TypeKind> classInterfaceModuleStructTypes,
           CancellationToken cancellationToken,
           out SyntaxToken identifierToken,
           out IMethodSymbol methodSymbol,
           out INamedTypeSymbol typeToGenerateIn)
        {
            if (TryGetConversionMethodAndTypeToGenerateIn(document, expression, classInterfaceModuleStructTypes, cancellationToken, out methodSymbol, out typeToGenerateIn))
            {
                identifierToken = SyntaxFactory.Token(
                    default,
                    SyntaxKind.ImplicitKeyword,
                    WellKnownMemberNames.ImplicitConversionName,
                    WellKnownMemberNames.ImplicitConversionName,
                    default);
                return true;
            }

            identifierToken = default;
            methodSymbol = null;
            typeToGenerateIn = null;
            return false;
        }

        protected override bool TryInitializeExplicitConversionState(
            SemanticDocument document,
            SyntaxNode expression,
            ISet<TypeKind> classInterfaceModuleStructTypes,
            CancellationToken cancellationToken,
            out SyntaxToken identifierToken,
            out IMethodSymbol methodSymbol,
            out INamedTypeSymbol typeToGenerateIn)
        {
            if (TryGetConversionMethodAndTypeToGenerateIn(document, expression, classInterfaceModuleStructTypes, cancellationToken, out methodSymbol, out typeToGenerateIn))
            {
                identifierToken = SyntaxFactory.Token(
                    default,
                    SyntaxKind.ImplicitKeyword,
                    WellKnownMemberNames.ExplicitConversionName,
                    WellKnownMemberNames.ExplicitConversionName,
                    default);
                return true;
            }

            identifierToken = default;
            methodSymbol = null;
            typeToGenerateIn = null;
            return false;
        }

        private static bool TryGetConversionMethodAndTypeToGenerateIn(
            SemanticDocument document,
            SyntaxNode expression,
            ISet<TypeKind> classInterfaceModuleStructTypes,
            CancellationToken cancellationToken,
            out IMethodSymbol methodSymbol,
            out INamedTypeSymbol typeToGenerateIn)
        {
            if (expression is CastExpressionSyntax castExpression)
            {
                return TryGetExplicitConversionMethodAndTypeToGenerateIn(
                    document,
                    castExpression,
                    classInterfaceModuleStructTypes,
                    cancellationToken,
                    out methodSymbol,
                    out typeToGenerateIn);
            }

            return TryGetImplicitConversionMethodAndTypeToGenerateIn(
                    document,
                    expression,
                    classInterfaceModuleStructTypes,
                cancellationToken,
                    out methodSymbol,
                    out typeToGenerateIn);
        }

        private static bool TryGetExplicitConversionMethodAndTypeToGenerateIn(
            SemanticDocument document,
            CastExpressionSyntax castExpression,
            ISet<TypeKind> classInterfaceModuleStructTypes,
            CancellationToken cancellationToken,
            out IMethodSymbol methodSymbol,
            out INamedTypeSymbol typeToGenerateIn)
        {
            methodSymbol = null;
            typeToGenerateIn = document.SemanticModel.GetTypeInfo(castExpression.Type, cancellationToken).Type as INamedTypeSymbol;
            if (typeToGenerateIn == null
                || document.SemanticModel.GetTypeInfo(castExpression.Expression, cancellationToken).Type is not INamedTypeSymbol parameterSymbol
                || typeToGenerateIn.IsErrorType()
                || parameterSymbol.IsErrorType())
            {
                return false;
            }

            methodSymbol = GenerateMethodSymbol(typeToGenerateIn, parameterSymbol);

            if (!ValidateTypeToGenerateIn(
                    typeToGenerateIn,
                    true,
                    classInterfaceModuleStructTypes))
            {
                typeToGenerateIn = parameterSymbol;
            }

            return true;
        }

        private static bool TryGetImplicitConversionMethodAndTypeToGenerateIn(
            SemanticDocument document,
            SyntaxNode expression,
            ISet<TypeKind> classInterfaceModuleStructTypes,
            CancellationToken cancellationToken,
            out IMethodSymbol methodSymbol,
            out INamedTypeSymbol typeToGenerateIn)
        {
            methodSymbol = null;
            typeToGenerateIn = document.SemanticModel.GetTypeInfo(expression, cancellationToken).ConvertedType as INamedTypeSymbol;
            if (typeToGenerateIn == null
                || document.SemanticModel.GetTypeInfo(expression, cancellationToken).Type is not INamedTypeSymbol parameterSymbol
                || typeToGenerateIn.IsErrorType()
                || parameterSymbol.IsErrorType())
            {
                return false;
            }

            methodSymbol = GenerateMethodSymbol(typeToGenerateIn, parameterSymbol);

            if (!ValidateTypeToGenerateIn(
                    typeToGenerateIn,
                    true,
                    classInterfaceModuleStructTypes))
            {
                typeToGenerateIn = parameterSymbol;
            }

            return true;
        }

        private static IMethodSymbol GenerateMethodSymbol(
            INamedTypeSymbol typeToGenerateIn, INamedTypeSymbol parameterSymbol)
        {
            // Remove any generic parameters
            if (typeToGenerateIn.IsGenericType)
            {
                typeToGenerateIn = typeToGenerateIn.ConstructUnboundGenericType().ConstructedFrom;
            }

            return CodeGenerationSymbolFactory.CreateMethodSymbol(
                attributes: ImmutableArray<AttributeData>.Empty,
                accessibility: default,
                modifiers: default,
                returnType: typeToGenerateIn,
                refKind: RefKind.None,
                explicitInterfaceImplementations: default,
                name: null,
                typeParameters: ImmutableArray<ITypeParameterSymbol>.Empty,
                parameters: ImmutableArray.Create(CodeGenerationSymbolFactory.CreateParameterSymbol(parameterSymbol, "v")),
                methodKind: MethodKind.Conversion);
        }

        protected override string GetImplicitConversionDisplayText(AbstractGenerateParameterizedMemberService<CSharpGenerateConversionService, SimpleNameSyntax, ExpressionSyntax, InvocationExpressionSyntax>.State state)
            => string.Format(CSharpFeaturesResources.Generate_implicit_conversion_operator_in_0, state.TypeToGenerateIn.Name);

        protected override string GetExplicitConversionDisplayText(AbstractGenerateParameterizedMemberService<CSharpGenerateConversionService, SimpleNameSyntax, ExpressionSyntax, InvocationExpressionSyntax>.State state)
            => string.Format(CSharpFeaturesResources.Generate_explicit_conversion_operator_in_0, state.TypeToGenerateIn.Name);
    }
}
