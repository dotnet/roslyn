// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Cci;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedRecordGetHashCode : SynthesizedRecordObjectMethod
    {
        private readonly PropertySymbol _equalityContract;

        public SynthesizedRecordGetHashCode(SourceMemberContainerTypeSymbol containingType, PropertySymbol equalityContract, int memberOffset, DiagnosticBag diagnostics)
            : base(containingType, WellKnownMemberNames.ObjectGetHashCode, memberOffset, diagnostics)
        {
            _equalityContract = equalityContract;
        }

        public override TypeWithAnnotations ReturnTypeWithAnnotations => TypeWithAnnotations.Create(
            isNullableEnabled: true,
            ContainingType.DeclaringCompilation.GetSpecialType(SpecialType.System_Int32));

        protected override (TypeWithAnnotations ReturnType, ImmutableArray<ParameterSymbol> Parameters, bool IsVararg, ImmutableArray<TypeParameterConstraintClause> DeclaredConstraintsForOverrideOrImplement) MakeParametersAndBindReturnType(DiagnosticBag diagnostics)
        {
            var compilation = DeclaringCompilation;
            var location = ReturnTypeLocation;
            return (ReturnType: TypeWithAnnotations.Create(Binder.GetSpecialType(compilation, SpecialType.System_Int32, location, diagnostics)),
                    Parameters: ImmutableArray<ParameterSymbol>.Empty,
                    IsVararg: false,
                    DeclaredConstraintsForOverrideOrImplement: ImmutableArray<TypeParameterConstraintClause>.Empty);
        }

        protected override int GetParameterCountFromSyntax() => 0;

        internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            var F = new SyntheticBoundNodeFactory(this, ContainingType.GetNonNullSyntaxNode(), compilationState, diagnostics);

            try
            {
                MethodSymbol equalityComparer_GetHashCode = F.WellKnownMethod(WellKnownMember.System_Collections_Generic_EqualityComparer_T__GetHashCode, isOptional: false)!;
                MethodSymbol equalityComparer_get_Default = F.WellKnownMethod(WellKnownMember.System_Collections_Generic_EqualityComparer_T__get_Default, isOptional: false)!;

                BoundExpression currentHashValue;

                if (ContainingType.BaseTypeNoUseSiteDiagnostics.IsObjectType())
                {
                    // There are no base record types.
                    // Get hash code of the equality contract and combine it with hash codes for field values.
                    currentHashValue = MethodBodySynthesizer.GenerateGetHashCode(equalityComparer_GetHashCode, equalityComparer_get_Default, F.Property(F.This(), _equalityContract), F);
                }
                else
                {
                    // There are base record types.
                    // Get base.GetHashCode() and combine it with hash codes for field values.
                    var overridden = OverriddenMethod;
                    currentHashValue = F.Call(F.Base(overridden.ContainingType), overridden);
                }

                //  bound HASH_FACTOR
                BoundLiteral? boundHashFactor = null;

                foreach (var f in ContainingType.GetFieldsToEmit())
                {
                    if (!f.IsStatic)
                    {
                        currentHashValue = MethodBodySynthesizer.GenerateHashCombine(currentHashValue, equalityComparer_GetHashCode, equalityComparer_get_Default, ref boundHashFactor,
                                                                                     F.Field(F.This(), f),
                                                                                     F);
                    }
                }

                F.CloseMethod(F.Block(F.Return(currentHashValue)));
            }
            catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
            {
                diagnostics.Add(ex.Diagnostic);
                F.CloseMethod(F.ThrowNull());
            }
        }
    }
}
