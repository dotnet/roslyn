// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Cci;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedRecordEqualsSymbol : SynthesizedInstanceMethodSymbol
    {
        private readonly NamedTypeSymbol _containingType;
        private readonly ImmutableArray<SynthesizedRecordPropertySymbol> _properties;

        public SynthesizedRecordEqualsSymbol(
            NamedTypeSymbol containingType,
            SyntaxReference syntaxRef,
            ImmutableArray<SynthesizedRecordPropertySymbol> properties)
        {
            _containingType = containingType;
            DeclaringSyntaxReferences = ImmutableArray.Create(syntaxRef);
            _properties = properties;
            Parameters = ImmutableArray.Create(SynthesizedParameterSymbol.Create(
                this,
                TypeWithAnnotations.Create(containingType),
                ordinal: 0,
                RefKind.None,
                "value"));
        }

        public override string Name => "Equals";

        internal override LexicalSortKey GetLexicalSortKey() => LexicalSortKey.NotInSource;

        internal override bool SynthesizesLoweredBoundBody => true;

        internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            var F = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);
            F.CurrentFunction = this;
            BoundExpression expr = null;
            foreach (var prop in _properties)
            {
                var eq = F.ObjectEqual(F.Property(F.This(), prop), F.Property(F.Parameter(Parameters[0]), prop));
                expr = expr is null ? eq : F.LogicalAnd(expr, eq);
            }
            F.CloseMethod(F.Block(F.Return(expr)));
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

        public override bool IsVirtual => false;

        public override bool IsOverride => false;

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

        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges) => false;
    }
}
