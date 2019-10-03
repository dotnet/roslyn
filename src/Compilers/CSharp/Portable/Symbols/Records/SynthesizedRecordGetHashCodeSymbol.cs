// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Cci;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal class SynthesizedRecordGetHashCodeSymbol : SynthesizedInstanceMethodSymbol
    {
        private readonly NamedTypeSymbol _containingType;
        private readonly ImmutableArray<SynthesizedRecordPropertySymbol> _properties;

        public SynthesizedRecordGetHashCodeSymbol(
            NamedTypeSymbol containingType,
            SyntaxReference syntaxRef,
            ImmutableArray<SynthesizedRecordPropertySymbol> properties)
        {
            _containingType = containingType;
            DeclaringSyntaxReferences = ImmutableArray.Create(syntaxRef);
            _properties = properties;
        }

        public override string Name => WellKnownMemberNames.ObjectGetHashCode;

        internal override LexicalSortKey GetLexicalSortKey() => LexicalSortKey.NotInSource;

        internal override bool SynthesizesLoweredBoundBody => true;

        internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            var F = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);
            F.CurrentFunction = this;

            //  Method body:
            //
            //  HASH_FACTOR = 0xa5555529;
            //  INIT_HASH = GetFNVHashCode(this.Name)
            //
            //  {
            //      return (...((INIT_HASH * HASH_FACTOR) + EqualityComparer<T_1>.Default.GetHashCode(this.prop_1)) * HASH_FACTOR
            //                                            + EqualityComparer<T_2>.Default.GetHashCode(this.prop_2)) * HASH_FACTOR
            //                                            ...
            //                                            + EqualityComparer<T_N>.Default.GetHashCode(this.prop_N)
            //  }
            //
            // Where GetFNVHashCode is the FNV-1a hash code.

            const int HASH_FACTOR = unchecked((int)0xa5555529); // (int)0xa5555529

            //  INIT_HASH
            int initHash = Hash.GetFNVHashCode(Name);

            //  Generate expression for return statement
            //      retExpression <= 'INITIAL_HASH'
            BoundExpression retExpression = F.Literal(initHash);

            //  prepare symbols
            MethodSymbol equalityComparer_GetHashCode = F.WellKnownMethod(WellKnownMember.System_Collections_Generic_EqualityComparer_T__GetHashCode);
            MethodSymbol equalityComparer_get_Default = F.WellKnownMethod(WellKnownMember.System_Collections_Generic_EqualityComparer_T__get_Default);
            NamedTypeSymbol equalityComparerType = equalityComparer_GetHashCode.ContainingType;
            var int32Type = F.SpecialType(SpecialType.System_Int32);

            //  bound HASH_FACTOR
            BoundLiteral boundHashFactor = F.Literal(HASH_FACTOR);

            // Process fields
            foreach (var prop in _properties)
            {
                // Prepare constructed symbols
                NamedTypeSymbol constructedEqualityComparer = equalityComparerType.Construct(prop.Type);

                // Generate 'retExpression' <= 'retExpression * HASH_FACTOR 
                retExpression = F.Binary(BinaryOperatorKind.IntMultiplication, int32Type, retExpression, boundHashFactor);

                // Generate 'retExpression' <= 'retExpression + EqualityComparer<T_index>.Default.GetHashCode(this.backingFld_index)'
                retExpression = F.Binary(BinaryOperatorKind.IntAddition,
                                         int32Type,
                                         retExpression,
                                         F.Call(
                                            F.StaticCall(constructedEqualityComparer,
                                                         equalityComparer_get_Default.AsMember(constructedEqualityComparer)),
                                            equalityComparer_GetHashCode.AsMember(constructedEqualityComparer),
                                            F.Property(F.This(), prop)));
            }

            // Create a bound block 
            F.CloseMethod(F.Block(F.Return(retExpression)));
        }

        public override MethodKind MethodKind => MethodKind.Ordinary;

        public override int Arity => 0;

        public override bool IsExtensionMethod => false;

        public override bool HidesBaseMethodsByName => false;

        public override bool IsVararg => false;

        public override bool ReturnsVoid => false;

        public override bool IsAsync => false;

        public override RefKind RefKind => RefKind.None;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences { get; }

        public override TypeWithAnnotations ReturnTypeWithAnnotations
            => TypeWithAnnotations.Create(DeclaringCompilation.GetSpecialType(SpecialType.System_Int32), NullableAnnotation.NotAnnotated);

        public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations => ImmutableArray<TypeWithAnnotations>.Empty;

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => ImmutableArray<TypeParameterSymbol>.Empty;

        public override ImmutableArray<ParameterSymbol> Parameters => ImmutableArray<ParameterSymbol>.Empty;

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => ImmutableArray<MethodSymbol>.Empty;

        public override ImmutableArray<CustomModifier> RefCustomModifiers => ImmutableArray<CustomModifier>.Empty;

        public override Symbol AssociatedSymbol => null;

        public override Symbol ContainingSymbol => _containingType;

        public override ImmutableArray<Location> Locations => ImmutableArray.Create(DeclaringSyntaxReferences[0].GetLocation());

        public override Accessibility DeclaredAccessibility => Accessibility.Public;

        public override bool IsStatic => false;

        public override bool IsVirtual => true;

        public override bool IsOverride => true;

        public override bool IsAbstract => false;

        public override bool IsSealed => false;

        public override bool IsExtern => false;

        internal override bool HasSpecialName => false;

        internal override MethodImplAttributes ImplementationAttributes => default;

        internal override bool HasDeclarativeSecurity => false;

        internal override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation => null;

        internal override bool RequiresSecurityObject => false;

        internal override CallingConvention CallingConvention => CallingConvention.HasThis;

        internal override bool GenerateDebugInfo => false;

        public override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull => ImmutableHashSet<string>.Empty;

        public override DllImportData GetDllImportData() => null;

        internal override ImmutableArray<string> GetAppliedConditionalSymbols() => ImmutableArray<string>.Empty;

        internal override IEnumerable<SecurityAttribute> GetSecurityInformation() => ImmutableArray<SecurityAttribute>.Empty;

        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges) => false;

        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges) => true;
    }
}
