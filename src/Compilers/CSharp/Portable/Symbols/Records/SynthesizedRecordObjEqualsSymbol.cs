// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Cci;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedRecordObjEqualsSymbol : SynthesizedInstanceMethodSymbol
    {
        private readonly NamedTypeSymbol _containingType;
        private readonly MethodSymbol _overloadedEquals;

        public SynthesizedRecordObjEqualsSymbol(
            NamedTypeSymbol containingType,
            SyntaxReference syntaxRef,
            MethodSymbol overloadedEquals)
        {
            _containingType = containingType;
            DeclaringSyntaxReferences = ImmutableArray.Create(syntaxRef);
            _overloadedEquals = overloadedEquals;
            var obj = _containingType.DeclaringCompilation.GetSpecialType(SpecialType.System_Object);
            Parameters = ImmutableArray.Create(SynthesizedParameterSymbol.Create(
                this,
                TypeWithAnnotations.Create(obj),
                ordinal: 0,
                RefKind.None,
                "value"));
        }

        public override string Name => WellKnownMemberNames.ObjectEquals;

        internal override LexicalSortKey GetLexicalSortKey() => LexicalSortKey.NotInSource;

        internal override bool SynthesizesLoweredBoundBody => true;

        internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            var F = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);
            F.CurrentFunction = this;
            var temp = F.Local(F.SynthesizedLocal(_containingType));
            var sequence =
                F.Sequence(
                    ImmutableArray.Create(temp.LocalSymbol),
                    ImmutableArray<BoundExpression>.Empty,
                    F.LogicalAnd(
                        F.Is(F.Parameter(Parameters[0]), _containingType, temp),
                        F.Call(F.This(), _overloadedEquals, temp)));
            F.CloseMethod(F.Block(F.Return(sequence)));
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
            => TypeWithAnnotations.Create(DeclaringCompilation.GetSpecialType(SpecialType.System_Boolean), NullableAnnotation.NotAnnotated);

        public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations => ImmutableArray<TypeWithAnnotations>.Empty;

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => ImmutableArray<TypeParameterSymbol>.Empty;

        public override ImmutableArray<ParameterSymbol> Parameters { get; }

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
