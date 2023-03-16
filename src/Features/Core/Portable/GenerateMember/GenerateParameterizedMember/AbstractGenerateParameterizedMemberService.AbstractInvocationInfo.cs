// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGeneration;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.GenerateMember.GenerateParameterizedMember
{
    internal partial class AbstractGenerateParameterizedMemberService<TService, TSimpleNameSyntax, TExpressionSyntax, TInvocationExpressionSyntax>
    {
        internal abstract class AbstractInvocationInfo : SignatureInfo
        {
            protected abstract bool IsIdentifierName();

            protected abstract ImmutableArray<ITypeParameterSymbol> GetCapturedTypeParameters(CancellationToken cancellationToken);
            protected abstract ImmutableArray<ITypeParameterSymbol> GenerateTypeParameters(CancellationToken cancellationToken);

            protected AbstractInvocationInfo(SemanticDocument document, State state)
                : base(document, state)
            {
            }

            protected override ImmutableArray<ITypeParameterSymbol> DetermineTypeParametersWorker(
                CancellationToken cancellationToken)
            {
                var typeParameters = ComputeTypeParameters(cancellationToken);
                return typeParameters.SelectAsArray(MassageTypeParameter);
            }

            private ImmutableArray<ITypeParameterSymbol> ComputeTypeParameters(
                CancellationToken cancellationToken)
            {
                if (IsIdentifierName())
                {
                    // If the user wrote something like Goo(x) then we still might want to generate
                    // a generic method if the expression 'x' captured any method type variables.
                    var capturedTypeParameters = GetCapturedTypeParameters(cancellationToken);
                    var availableTypeParameters = State.TypeToGenerateIn.GetAllTypeParameters();
                    var result = capturedTypeParameters.Except<ITypeParameterSymbol>(availableTypeParameters, SymbolEqualityComparer.Default).ToImmutableArray();
                    return result;
                }
                else
                {
                    return GenerateTypeParameters(cancellationToken);
                }
            }

            private ITypeParameterSymbol MassageTypeParameter(
                ITypeParameterSymbol typeParameter)
            {
                var constraints = typeParameter.ConstraintTypes.Where(ts => !ts.IsUnexpressibleTypeParameterConstraint()).ToList();
                var classTypes = constraints.Where(ts => ts.TypeKind == TypeKind.Class).ToList();
                var nonClassTypes = constraints.Where(ts => ts.TypeKind != TypeKind.Class).ToList();

                classTypes = MergeClassTypes(classTypes);
                constraints = classTypes.Concat(nonClassTypes).ToList();
                if (constraints.SequenceEqual(typeParameter.ConstraintTypes))
                {
                    return typeParameter;
                }

                return CodeGenerationSymbolFactory.CreateTypeParameter(
                    attributes: default,
                    varianceKind: typeParameter.Variance,
                    name: typeParameter.Name,
                    constraintTypes: constraints.AsImmutable(),
                    hasConstructorConstraint: typeParameter.HasConstructorConstraint,
                    hasReferenceConstraint: typeParameter.HasReferenceTypeConstraint,
                    hasValueConstraint: typeParameter.HasValueTypeConstraint,
                    hasUnmanagedConstraint: typeParameter.HasUnmanagedTypeConstraint,
                    hasNotNullConstraint: typeParameter.HasNotNullConstraint);
            }

            private List<ITypeSymbol> MergeClassTypes(List<ITypeSymbol> classTypes)
            {
                var compilation = Document.SemanticModel.Compilation;
                for (var i = classTypes.Count - 1; i >= 0; i--)
                {
                    // For example, 'Attribute'.
                    var type1 = classTypes[i];

                    for (var j = 0; j < classTypes.Count; j++)
                    {
                        if (j != i)
                        {
                            // For example 'GooAttribute'.
                            var type2 = classTypes[j];

                            if (IsImplicitReferenceConversion(compilation, type2, type1))
                            {
                                // If there's an implicit reference conversion (i.e. from
                                // GooAttribute to Attribute), then we don't need Attribute as it's
                                // implied by the second attribute;
                                classTypes.RemoveAt(i);
                                break;
                            }
                        }
                    }
                }

                return classTypes;
            }

            protected abstract bool IsImplicitReferenceConversion(Compilation compilation, ITypeSymbol sourceType, ITypeSymbol targetType);
        }
    }
}
