// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.GenerateMember.GenerateMethod
{
    internal abstract class CSharpGenerateParameterizedMemberService<TService> : AbstractGenerateParameterizedMemberService<TService, SimpleNameSyntax, ExpressionSyntax, InvocationExpressionSyntax>
        where TService : AbstractGenerateParameterizedMemberService<TService, SimpleNameSyntax, ExpressionSyntax, InvocationExpressionSyntax>
    {
        internal partial class InvocationExpressionInfo : AbstractInvocationInfo
        {
            private readonly InvocationExpressionSyntax _invocationExpression;

            public InvocationExpressionInfo(SemanticDocument document, State state)
                : base(document, state)
            {
                _invocationExpression = state.InvocationExpressionOpt;
            }

            protected override ImmutableArray<ParameterName> DetermineParameterNames(CancellationToken cancellationToken)
            {
                return this.Document.SemanticModel.GenerateParameterNames(
                    _invocationExpression.ArgumentList, cancellationToken);
            }

            protected override RefKind DetermineRefKind(CancellationToken cancellationToken)
                => _invocationExpression.IsParentKind(SyntaxKind.RefExpression) ? RefKind.Ref : RefKind.None;

            protected override ITypeSymbol DetermineReturnTypeWorker(CancellationToken cancellationToken)
            {
                // Defer to the type inferrer to figure out what the return type of this new method
                // should be.
                var typeInference = this.Document.Document.GetLanguageService<ITypeInferenceService>();
                var inferredType = typeInference.InferType(
                    this.Document.SemanticModel, _invocationExpression, objectAsDefault: true,
                    nameOpt: this.State.IdentifierToken.ValueText, cancellationToken: cancellationToken);
                return inferredType;
            }

            protected override ImmutableArray<ITypeParameterSymbol> GetCapturedTypeParameters(CancellationToken cancellationToken)
            {
                var result = new List<ITypeParameterSymbol>();
                var semanticModel = this.Document.SemanticModel;
                foreach (var argument in _invocationExpression.ArgumentList.Arguments)
                {
                    var type = argument.DetermineParameterType(semanticModel, cancellationToken);
                    type.GetReferencedTypeParameters(result);
                }

                return result.ToImmutableArray();
            }

            protected override ImmutableArray<ITypeParameterSymbol> GenerateTypeParameters(CancellationToken cancellationToken)
            {
                // Generate dummy type parameter names for a generic method.  If the user is inside a
                // generic method, and calls a generic method with type arguments from the outer
                // method, then use those same names for the generated type parameters.
                //
                // TODO(cyrusn): If we do capture method type variables, then we should probably
                // capture their constraints as well.
                var genericName = (GenericNameSyntax)this.State.SimpleNameOpt;
                var semanticModel = this.Document.SemanticModel;

                if (genericName.TypeArgumentList.Arguments.Count == 1)
                {
                    var typeParameter = GetUniqueTypeParameter(
                        genericName.TypeArgumentList.Arguments.First(),
                        s => !State.TypeToGenerateIn.GetAllTypeParameters().Any(t => t.Name == s),
                        cancellationToken);

                    return ImmutableArray.Create(typeParameter);
                }
                else
                {
                    var list = ArrayBuilder<ITypeParameterSymbol>.GetInstance();

                    var usedIdentifiers = new HashSet<string> { "T" };
                    foreach (var type in genericName.TypeArgumentList.Arguments)
                    {
                        var typeParameter = GetUniqueTypeParameter(
                            type,
                            s => !usedIdentifiers.Contains(s) && !State.TypeToGenerateIn.GetAllTypeParameters().Any(t => t.Name == s),
                            cancellationToken);

                        usedIdentifiers.Add(typeParameter.Name);

                        list.Add(typeParameter);
                    }

                    return list.ToImmutableAndFree();
                }
            }

            private ITypeParameterSymbol GetUniqueTypeParameter(
                TypeSyntax type,
                Func<string, bool> isUnique,
                CancellationToken cancellationToken)
            {
                var methodTypeParameter = GetMethodTypeParameter(type, cancellationToken);
                return methodTypeParameter ?? CodeGenerationSymbolFactory.CreateTypeParameterSymbol(NameGenerator.GenerateUniqueName("T", isUnique)).WithNullability(NullableAnnotation.NotApplicable);
            }

            private ITypeParameterSymbol GetMethodTypeParameter(TypeSyntax type, CancellationToken cancellationToken)
            {
                if (type is IdentifierNameSyntax)
                {
                    var info = this.Document.SemanticModel.GetTypeInfo(type, cancellationToken);
                    if (info.Type is ITypeParameterSymbol typeParameter &&
                        typeParameter.TypeParameterKind == TypeParameterKind.Method)
                    {
                        return typeParameter;
                    }
                }

                return null;
            }

            protected override ImmutableArray<RefKind> DetermineParameterModifiers(CancellationToken cancellationToken)
            {
                return
                    _invocationExpression.ArgumentList.Arguments.Select(
                        a => a.RefOrOutKeyword.Kind() == SyntaxKind.RefKeyword ? RefKind.Ref :
                             a.RefOrOutKeyword.Kind() == SyntaxKind.OutKeyword ? RefKind.Out : RefKind.None).ToImmutableArray();
            }

            protected override ImmutableArray<ITypeSymbol> DetermineParameterTypes(CancellationToken cancellationToken)
                => _invocationExpression.ArgumentList.Arguments.Select(a => DetermineParameterType(a, cancellationToken)).ToImmutableArray();

            private ITypeSymbol DetermineParameterType(
                ArgumentSyntax argument,
                CancellationToken cancellationToken)
            {
                return argument.DetermineParameterType(this.Document.SemanticModel, cancellationToken);
            }

            protected override ImmutableArray<bool> DetermineParameterOptionality(CancellationToken cancellationToken)
                => _invocationExpression.ArgumentList.Arguments.Select(a => false).ToImmutableArray();

            protected override bool IsIdentifierName()
            {
                return this.State.SimpleNameOpt.Kind() == SyntaxKind.IdentifierName;
            }

            protected override bool IsImplicitReferenceConversion(Compilation compilation, ITypeSymbol sourceType, ITypeSymbol targetType)
            {
                var conversion = compilation.ClassifyConversion(sourceType, targetType);
                return conversion.IsImplicit && conversion.IsReference;
            }

            protected override ImmutableArray<ITypeSymbol> DetermineTypeArguments(CancellationToken cancellationToken)
            {
                var result = ArrayBuilder<ITypeSymbol>.GetInstance();

                if (State.SimpleNameOpt is GenericNameSyntax)
                {
                    foreach (var typeArgument in ((GenericNameSyntax)State.SimpleNameOpt).TypeArgumentList.Arguments)
                    {
                        var typeInfo = this.Document.SemanticModel.GetTypeInfo(typeArgument, cancellationToken);
                        result.Add(typeInfo.GetTypeWithAnnotatedNullability());
                    }
                }

                return result.ToImmutableAndFree();
            }
        }
    }
}
