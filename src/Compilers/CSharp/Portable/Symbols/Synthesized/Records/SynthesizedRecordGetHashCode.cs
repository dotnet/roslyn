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
    /// <summary>
    /// The record type includes a synthesized override of object.GetHashCode().
    /// The method can be declared explicitly. It is an error if the explicit
    /// declaration is sealed unless the record type is sealed.
    /// </summary>
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

        protected override (TypeWithAnnotations ReturnType, ImmutableArray<ParameterSymbol> Parameters, bool IsVararg, ImmutableArray<TypeParameterConstraintClause> DeclaredConstraintsForOverrideOrImplementation) MakeParametersAndBindReturnType(DiagnosticBag diagnostics)
        {
            var compilation = DeclaringCompilation;
            var location = ReturnTypeLocation;
            return (ReturnType: TypeWithAnnotations.Create(Binder.GetSpecialType(compilation, SpecialType.System_Int32, location, diagnostics)),
                    Parameters: ImmutableArray<ParameterSymbol>.Empty,
                    IsVararg: false,
                    DeclaredConstraintsForOverrideOrImplementation: ImmutableArray<TypeParameterConstraintClause>.Empty);
        }

        protected override int GetParameterCountFromSyntax() => 0;

        internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            var F = new SyntheticBoundNodeFactory(this, this.SyntaxNode, compilationState, diagnostics);

            try
            {
                MethodSymbol? equalityComparer_GetHashCode = null;
                MethodSymbol? equalityComparer_get_Default = null;
                BoundExpression currentHashValue;

                if (ContainingType.BaseTypeNoUseSiteDiagnostics.IsObjectType())
                {
                    if (_equalityContract.IsStatic)
                    {
                        F.CloseMethod(F.ThrowNull());
                        return;
                    }

                    // There are no base record types.
                    // Get hash code of the equality contract and combine it with hash codes for field values.
                    ensureEqualityComparerHelpers(F, ref equalityComparer_GetHashCode, ref equalityComparer_get_Default);
                    currentHashValue = MethodBodySynthesizer.GenerateGetHashCode(equalityComparer_GetHashCode!, equalityComparer_get_Default!, F.Property(F.This(), _equalityContract), F);
                }
                else
                {
                    // There are base record types.
                    // Get base.GetHashCode() and combine it with hash codes for field values.
                    var overridden = OverriddenMethod;

                    if (overridden is null || overridden.ReturnType.SpecialType != SpecialType.System_Int32)
                    {
                        // There was a problem with overriding, an error was reported elsewhere
                        F.CloseMethod(F.ThrowNull());
                        return;
                    }

                    currentHashValue = F.Call(F.Base(overridden.ContainingType), overridden);
                }

                //  bound HASH_FACTOR
                BoundLiteral? boundHashFactor = null;

                foreach (var f in ContainingType.GetFieldsToEmit())
                {
                    if (!f.IsStatic)
                    {
                        ensureEqualityComparerHelpers(F, ref equalityComparer_GetHashCode, ref equalityComparer_get_Default);
                        currentHashValue = MethodBodySynthesizer.GenerateHashCombine(currentHashValue, equalityComparer_GetHashCode!, equalityComparer_get_Default!, ref boundHashFactor,
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

            static void ensureEqualityComparerHelpers(SyntheticBoundNodeFactory F, ref MethodSymbol? equalityComparer_GetHashCode, ref MethodSymbol? equalityComparer_get_Default)
            {
                equalityComparer_GetHashCode ??= F.WellKnownMethod(WellKnownMember.System_Collections_Generic_EqualityComparer_T__GetHashCode);
                equalityComparer_get_Default ??= F.WellKnownMethod(WellKnownMember.System_Collections_Generic_EqualityComparer_T__get_Default);
            }
        }
    }
}
