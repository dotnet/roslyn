// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
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

            protected abstract IList<ITypeParameterSymbol> GetCapturedTypeParameters(CancellationToken cancellationToken);
            protected abstract IList<ITypeParameterSymbol> GenerateTypeParameters(CancellationToken cancellationToken);

            protected AbstractInvocationInfo(SemanticDocument document, State state)
                : base(document, state)
            {
            }

            public override IList<ITypeParameterSymbol> DetermineTypeParameters(
                CancellationToken cancellationToken)
            {
                var typeParameters = DetermineTypeParametersWorker(cancellationToken);
                return typeParameters.Select(tp => MassageTypeParameter(tp, cancellationToken)).ToList();
            }

            private IList<ITypeParameterSymbol> DetermineTypeParametersWorker(
                CancellationToken cancellationToken)
            {
                if (IsIdentifierName())
                {
                    // If the user wrote something like Foo(x) then we still might want to generate
                    // a generic method if the expression 'x' captured any method type variables.
                    var capturedTypeParameters = GetCapturedTypeParameters(cancellationToken);
                    var availableTypeParameters = this.State.TypeToGenerateIn.GetAllTypeParameters();
                    var result = capturedTypeParameters.Except(availableTypeParameters).ToList();
                    return result;
                }
                else
                {
                    return GenerateTypeParameters(cancellationToken);
                }
            }

            private ITypeParameterSymbol MassageTypeParameter(
                ITypeParameterSymbol typeParameter,
                CancellationToken cancellationToken)
            {
                var constraints = typeParameter.ConstraintTypes.Where(ts => !ts.IsUnexpressibleTypeParameterConstraint()).ToList();
                var classTypes = constraints.Where(ts => ts.TypeKind == TypeKind.Class).ToList();
                var nonClassTypes = constraints.Where(ts => ts.TypeKind != TypeKind.Class).ToList();

                classTypes = MergeClassTypes(classTypes, cancellationToken);
                constraints = classTypes.Concat(nonClassTypes).ToList();
                if (constraints.SequenceEqual(typeParameter.ConstraintTypes))
                {
                    return typeParameter;
                }

                return CodeGenerationSymbolFactory.CreateTypeParameter(
                    attributes: null,
                    varianceKind: typeParameter.Variance,
                    name: typeParameter.Name,
                    constraintTypes: constraints.AsImmutable<ITypeSymbol>(),
                    hasConstructorConstraint: typeParameter.HasConstructorConstraint,
                    hasReferenceConstraint: typeParameter.HasReferenceTypeConstraint,
                    hasValueConstraint: typeParameter.HasValueTypeConstraint);
            }

            private List<ITypeSymbol> MergeClassTypes(List<ITypeSymbol> classTypes, CancellationToken cancellationToken)
            {
                var compilation = this.Document.SemanticModel.Compilation;
                for (int i = classTypes.Count - 1; i >= 0; i--)
                {
                    // For example, 'Attribute'.
                    var type1 = classTypes[i];

                    for (int j = 0; j < classTypes.Count; j++)
                    {
                        if (j != i)
                        {
                            // For example 'FooAttribute'.
                            var type2 = classTypes[j];

                            if (IsImplicitReferenceConversion(compilation, type2, type1))
                            {
                                // If there's an implicit reference conversion (i.e. from
                                // FooAttribute to Attribute), then we don't need Attribute as it's
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
