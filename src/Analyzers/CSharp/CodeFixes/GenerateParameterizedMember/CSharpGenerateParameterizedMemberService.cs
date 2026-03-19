// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.GenerateMember.GenerateMethod;

internal abstract class CSharpGenerateParameterizedMemberService<TService> : AbstractGenerateParameterizedMemberService<TService, SimpleNameSyntax, ExpressionSyntax, InvocationExpressionSyntax>
    where TService : AbstractGenerateParameterizedMemberService<TService, SimpleNameSyntax, ExpressionSyntax, InvocationExpressionSyntax>
{
    internal sealed class InvocationExpressionInfo(SemanticDocument document, State state) : AbstractInvocationInfo(document, state)
    {
        private readonly InvocationExpressionSyntax _invocationExpression = state.InvocationExpressionOpt;

        protected override ImmutableArray<ParameterName> DetermineParameterNames(CancellationToken cancellationToken)
        {
            return Document.SemanticModel.GenerateParameterNames(
                _invocationExpression.ArgumentList, cancellationToken);
        }

        protected override RefKind DetermineRefKind(CancellationToken cancellationToken)
            => _invocationExpression.IsParentKind(SyntaxKind.RefExpression) ? RefKind.Ref : RefKind.None;

        protected override ITypeSymbol DetermineReturnTypeWorker(CancellationToken cancellationToken)
        {
            // Defer to the type inferrer to figure out what the return type of this new method
            // should be.
            var typeInference = Document.GetRequiredLanguageService<ITypeInferenceService>();
            var inferredType = typeInference.InferType(
                Document.SemanticModel, _invocationExpression, objectAsDefault: true,
                name: State.IdentifierToken.ValueText, cancellationToken);
            return inferredType!;
        }

        protected override ImmutableArray<ITypeParameterSymbol> GetCapturedTypeParameters(CancellationToken cancellationToken)
        {
            using var _ = ArrayBuilder<ITypeParameterSymbol>.GetInstance(out var result);
            var semanticModel = Document.SemanticModel;
            foreach (var argument in _invocationExpression.ArgumentList.Arguments)
            {
                var type = argument.DetermineParameterType(semanticModel, cancellationToken);
                type.AddReferencedTypeParameters(result);
            }

            return result.ToImmutableAndClear();
        }

        protected override ImmutableArray<ITypeParameterSymbol> GenerateTypeParameters(CancellationToken cancellationToken)
        {
            // Generate dummy type parameter names for a generic method.  If the user is inside a
            // generic method, and calls a generic method with type arguments from the outer
            // method, then use those same names for the generated type parameters.
            //
            // TODO(cyrusn): If we do capture method type variables, then we should probably
            // capture their constraints as well.
            var genericName = (GenericNameSyntax)State.SimpleNameOpt;
            var semanticModel = Document.SemanticModel;

            if (genericName.TypeArgumentList.Arguments.Count == 1)
            {
                var typeParameter = GetUniqueTypeParameter(
                    genericName.TypeArgumentList.Arguments.First(),
                    s => !State.TypeToGenerateIn.GetAllTypeParameters().Any(static (t, s) => t.Name == s, s),
                    cancellationToken);

                return [typeParameter];
            }
            else
            {
                var list = new FixedSizeArrayBuilder<ITypeParameterSymbol>(genericName.TypeArgumentList.Arguments.Count);

                using var _ = PooledHashSet<string>.GetInstance(out var usedIdentifiers);
                usedIdentifiers.Add("T");
                foreach (var type in genericName.TypeArgumentList.Arguments)
                {
                    var typeParameter = GetUniqueTypeParameter(
                        type,
                        s => !usedIdentifiers.Contains(s) && !State.TypeToGenerateIn.GetAllTypeParameters().Any(static (t, s) => t.Name == s, s),
                        cancellationToken);

                    usedIdentifiers.Add(typeParameter.Name);

                    list.Add(typeParameter);
                }

                return list.MoveToImmutable();
            }
        }

        private ITypeParameterSymbol GetUniqueTypeParameter(
            TypeSyntax type,
            Func<string, bool> isUnique,
            CancellationToken cancellationToken)
        {
            var methodTypeParameter = GetMethodTypeParameter(type, cancellationToken);
            return methodTypeParameter ?? CodeGenerationSymbolFactory.CreateTypeParameterSymbol(NameGenerator.GenerateUniqueName("T", isUnique));
        }

        private ITypeParameterSymbol? GetMethodTypeParameter(TypeSyntax type, CancellationToken cancellationToken)
        {
            if (type is IdentifierNameSyntax)
            {
                var info = Document.SemanticModel.GetTypeInfo(type, cancellationToken);
                if (info.Type is ITypeParameterSymbol { TypeParameterKind: TypeParameterKind.Method } typeParameter)
                    return typeParameter;
            }

            return null;
        }

        protected override ImmutableArray<RefKind> DetermineParameterModifiers(CancellationToken cancellationToken)
            => [.. _invocationExpression.ArgumentList.Arguments.Select(a => a.GetRefKind())];

        protected override ImmutableArray<ITypeSymbol> DetermineParameterTypes(CancellationToken cancellationToken)
            => [.. _invocationExpression.ArgumentList.Arguments.Select(a => DetermineParameterType(a, cancellationToken))];

        private ITypeSymbol DetermineParameterType(ArgumentSyntax argument, CancellationToken cancellationToken)
            => argument.DetermineParameterType(Document.SemanticModel, cancellationToken);

        protected override ImmutableArray<bool> DetermineParameterOptionality(CancellationToken cancellationToken)
            => [.. _invocationExpression.ArgumentList.Arguments.Select(a => false)];

        protected override bool IsIdentifierName()
            => State.SimpleNameOpt.Kind() == SyntaxKind.IdentifierName;

        protected override bool IsImplicitReferenceConversion(Compilation compilation, ITypeSymbol sourceType, ITypeSymbol targetType)
        {
            var conversion = compilation.ClassifyConversion(sourceType, targetType);
            return conversion.IsImplicit && conversion.IsReference;
        }

        protected override ImmutableArray<ITypeSymbol> DetermineTypeArguments(CancellationToken cancellationToken)
        {
            if (State.SimpleNameOpt is not GenericNameSyntax genericName)
                return [];

            var result = new FixedSizeArrayBuilder<ITypeSymbol>(genericName.TypeArgumentList.Arguments.Count);
            foreach (var typeArgument in genericName.TypeArgumentList.Arguments)
            {
                var typeInfo = Document.SemanticModel.GetTypeInfo(typeArgument, cancellationToken);
                result.Add(typeInfo.Type ?? Document.SemanticModel.Compilation.ObjectType);
            }

            return result.MoveToImmutable();
        }
    }
}
