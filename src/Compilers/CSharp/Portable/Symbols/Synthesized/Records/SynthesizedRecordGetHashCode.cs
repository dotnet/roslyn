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
    internal sealed class SynthesizedRecordGetHashCode : SynthesizedInstanceMethodSymbol
    {
        private readonly int _memberOffset;
        private readonly PropertySymbol _equalityContract;

        public override NamedTypeSymbol ContainingType { get; }

        public SynthesizedRecordGetHashCode(NamedTypeSymbol containingType, PropertySymbol equalityContract, int memberOffset)
        {
            _memberOffset = memberOffset;
            ContainingType = containingType;
            _equalityContract = equalityContract;
        }

        public override string Name => "GetHashCode";

        public override ImmutableArray<ParameterSymbol> Parameters => ImmutableArray<ParameterSymbol>.Empty;

        public override MethodKind MethodKind => MethodKind.Ordinary;

        public override int Arity => 0;

        public override bool IsExtensionMethod => false;

        public override bool HidesBaseMethodsByName => false;

        public override bool IsVararg => false;

        public override bool ReturnsVoid => false;

        public override bool IsAsync => false;

        public override RefKind RefKind => RefKind.None;

        internal override LexicalSortKey GetLexicalSortKey() => LexicalSortKey.GetSynthesizedMemberKey(_memberOffset);

        public override TypeWithAnnotations ReturnTypeWithAnnotations => TypeWithAnnotations.Create(
            isNullableEnabled: true,
            ContainingType.DeclaringCompilation.GetSpecialType(SpecialType.System_Int32));

        public override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull => ImmutableHashSet<string>.Empty;

        public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations
            => ImmutableArray<TypeWithAnnotations>.Empty;

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => ImmutableArray<TypeParameterSymbol>.Empty;

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => ImmutableArray<MethodSymbol>.Empty;

        public override ImmutableArray<CustomModifier> RefCustomModifiers => ImmutableArray<CustomModifier>.Empty;

        public override Symbol? AssociatedSymbol => null;

        public override Symbol ContainingSymbol => ContainingType;

        public override ImmutableArray<Location> Locations => ContainingType.Locations;

        public override Accessibility DeclaredAccessibility => Accessibility.Public;

        public override bool IsStatic => false;

        public override bool IsVirtual => false;

        public override bool IsOverride => true;

        public override bool IsAbstract => false;

        public override bool IsSealed => false;

        public override bool IsExtern => false;

        internal override bool HasSpecialName => false;

        internal override MethodImplAttributes ImplementationAttributes => MethodImplAttributes.Managed;

        internal override bool HasDeclarativeSecurity => false;

        internal override MarshalPseudoCustomAttributeData? ReturnValueMarshallingInformation => null;

        internal override bool RequiresSecurityObject => false;

        internal override CallingConvention CallingConvention => CallingConvention.HasThis;

        internal override bool GenerateDebugInfo => false;

        public override DllImportData? GetDllImportData() => null;

        internal override ImmutableArray<string> GetAppliedConditionalSymbols()
            => ImmutableArray<string>.Empty;

        internal override IEnumerable<SecurityAttribute> GetSecurityInformation()
            => Array.Empty<SecurityAttribute>();

        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false) => false;

        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false) => true;

        internal override bool SynthesizesLoweredBoundBody => true;

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
