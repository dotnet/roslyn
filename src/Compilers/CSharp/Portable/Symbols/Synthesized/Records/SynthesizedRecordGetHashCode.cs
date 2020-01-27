// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedRecordGetHashCode : SynthesizedInstanceMethodSymbol
    {
        public override NamedTypeSymbol ContainingType { get; }


        public SynthesizedRecordGetHashCode(NamedTypeSymbol containingType)
        {
            ContainingType = containingType;
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

        internal override LexicalSortKey GetLexicalSortKey() => LexicalSortKey.SynthesizedRecordObjEquals;

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

        public override bool IsVirtual => true;

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
            // PROTOYPE: We can do better :)
            F.CloseMethod(F.Return(F.Literal(0)));
        }
    }
}