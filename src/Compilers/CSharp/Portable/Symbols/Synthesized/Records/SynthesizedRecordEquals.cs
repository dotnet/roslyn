// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedRecordEquals : SynthesizedInstanceMethodSymbol
    {
        public override NamedTypeSymbol ContainingType { get; }

        public SynthesizedRecordEquals(NamedTypeSymbol containingType)
        {
            ContainingType = containingType;
        }

        public override string Name => "Equals";

        public override MethodKind MethodKind => MethodKind.Ordinary;

        public override int Arity => 0;

        public override bool IsExtensionMethod => false;

        public override bool HidesBaseMethodsByName => true;

        public override bool IsVararg => false;

        public override bool ReturnsVoid => false;

        public override bool IsAsync => false;

        public override RefKind RefKind => RefKind.None;

        public override ImmutableArray<ParameterSymbol> Parameters
            => ImmutableArray.Create<ParameterSymbol>(SynthesizedParameterSymbol.Create(
                this,
                TypeWithAnnotations.Create(
                    isNullableEnabled: true,
                    ContainingType,
                    isAnnotated: true),
                ordinal: 0,
                ContainingType.IsStructType() ? RefKind.In : RefKind.None));

        public override TypeWithAnnotations ReturnTypeWithAnnotations => TypeWithAnnotations.Create(
            isNullableEnabled: true,
            ContainingType.DeclaringCompilation.GetSpecialType(SpecialType.System_Boolean));

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

        public override bool IsOverride => false;

        public override bool IsAbstract => false;

        public override bool IsSealed => false;

        public override bool IsExtern => false;

        internal override bool HasSpecialName => false;

        internal override LexicalSortKey GetLexicalSortKey() => LexicalSortKey.SynthesizedRecordEquals;

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

        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false) => false;

        internal override bool SynthesizesLoweredBoundBody => true;

        internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            var F = new SyntheticBoundNodeFactory(this, ContainingType.GetNonNullSyntaxNode(), compilationState, diagnostics);

            // Compare all of the record properties in this class with the argument properties
            // Body:
            // {
            //      return other != null && `comparisons`;
            // }

            var other = F.Parameter(Parameters[0]);
            BoundExpression? retExpr = null;
            if (!ContainingType.IsStructType())
            {
                retExpr = F.ObjectNotEqual(other, F.Null(F.SpecialType(SpecialType.System_Object)));
            }

            var recordProperties = ArrayBuilder<FieldSymbol>.GetInstance();
            foreach (var member in ContainingType.GetMembersUnordered())
            {
                if (member is SynthesizedRecordPropertySymbol p)
                {
                    recordProperties.Add(p.BackingField);
                }
            }
            var comparisons = EqualityMethodBodySynthesizer.GenerateEqualsComparisons(
                ContainingType,
                other,
                recordProperties,
                F);
            recordProperties.Free();

            F.CloseMethod(F.Block(F.Return(comparisons)));
        }
    }
}