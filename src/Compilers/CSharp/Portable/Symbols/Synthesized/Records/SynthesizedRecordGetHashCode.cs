// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// The record type includes a synthesized override of object.GetHashCode().
    /// The method can be declared explicitly. It is an error if the explicit
    /// declaration is sealed unless the record type is sealed.
    /// </summary>
    internal sealed class SynthesizedRecordGetHashCode : SynthesizedRecordObjectMethod
    {
        private readonly PropertySymbol? _equalityContract;

        public SynthesizedRecordGetHashCode(SourceMemberContainerTypeSymbol containingType, PropertySymbol? equalityContract, int memberOffset, BindingDiagnosticBag diagnostics)
            : base(containingType, WellKnownMemberNames.ObjectGetHashCode, memberOffset, isReadOnly: containingType.IsRecordStruct, diagnostics)
        {
            Debug.Assert(containingType.IsRecordStruct == equalityContract is null);
            _equalityContract = equalityContract;
        }

        protected override SpecialMember OverriddenSpecialMember => SpecialMember.System_Object__GetHashCode;

        protected override (TypeWithAnnotations ReturnType, ImmutableArray<ParameterSymbol> Parameters, ImmutableArray<TypeParameterConstraintClause> DeclaredConstraintsForOverrideOrImplementation)
            MakeParametersAndBindReturnType(BindingDiagnosticBag diagnostics)
        {
            var compilation = DeclaringCompilation;
            var location = ReturnTypeLocation;
            return (ReturnType: TypeWithAnnotations.Create(Binder.GetSpecialType(compilation, SpecialType.System_Int32, location, diagnostics)),
                    Parameters: ImmutableArray<ParameterSymbol>.Empty,
                    DeclaredConstraintsForOverrideOrImplementation: ImmutableArray<TypeParameterConstraintClause>.Empty);
        }

        protected override int GetParameterCountFromSyntax() => 0;

        internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            var F = new SyntheticBoundNodeFactory(this, this.SyntaxNode, compilationState, diagnostics);

            try
            {
                MethodSymbol? equalityComparer_GetHashCode = null;
                MethodSymbol? equalityComparer_get_Default = null;
                BoundExpression? currentHashValue;

                if (ContainingType.IsRecordStruct)
                {
                    currentHashValue = null;
                }
                else if (ContainingType.BaseTypeNoUseSiteDiagnostics.IsObjectType())
                {
                    Debug.Assert(_equalityContract is not null);
                    if (_equalityContract.GetMethod is null)
                    {
                        // The equality contract isn't usable, an error was reported elsewhere
                        F.CloseMethod(F.ThrowNull());
                        return;
                    }

                    if (_equalityContract.IsStatic)
                    {
                        F.CloseMethod(F.ThrowNull());
                        return;
                    }

                    // There are no base record types.
                    // Get hash code of the equality contract and combine it with hash codes for field values.
                    ensureEqualityComparerHelpers(F, ref equalityComparer_GetHashCode, ref equalityComparer_get_Default);
                    currentHashValue = MethodBodySynthesizer.GenerateGetHashCode(equalityComparer_GetHashCode, equalityComparer_get_Default, F.Property(F.This(), _equalityContract), F);
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
                        if (currentHashValue is null)
                        {
                            // EqualityComparer<field type>.Default.GetHashCode(this.field)
                            currentHashValue = MethodBodySynthesizer.GenerateGetHashCode(equalityComparer_GetHashCode, equalityComparer_get_Default, F.Field(F.This(), f), F);
                        }
                        else
                        {
                            currentHashValue = MethodBodySynthesizer.GenerateHashCombine(currentHashValue, equalityComparer_GetHashCode, equalityComparer_get_Default, ref boundHashFactor,
                                                                                         F.Field(F.This(), f),
                                                                                         F);
                        }
                    }
                }

                if (currentHashValue is null)
                {
                    currentHashValue = F.Literal(0);
                }

                F.CloseMethod(F.Block(F.Return(currentHashValue)));
            }
            catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
            {
                diagnostics.Add(ex.Diagnostic);
                F.CloseMethod(F.ThrowNull());
            }

            static void ensureEqualityComparerHelpers(SyntheticBoundNodeFactory F, [NotNull] ref MethodSymbol? equalityComparer_GetHashCode, [NotNull] ref MethodSymbol? equalityComparer_get_Default)
            {
                equalityComparer_GetHashCode ??= F.WellKnownMethod(WellKnownMember.System_Collections_Generic_EqualityComparer_T__GetHashCode);
                equalityComparer_get_Default ??= F.WellKnownMethod(WellKnownMember.System_Collections_Generic_EqualityComparer_T__get_Default);
            }
        }
    }
}
