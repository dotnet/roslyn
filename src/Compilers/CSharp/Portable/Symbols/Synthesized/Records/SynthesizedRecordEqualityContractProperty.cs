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
    internal sealed class SynthesizedRecordEqualityContractProperty : PropertySymbol
    {
        internal const string PropertyName = "EqualityContract";

        public SynthesizedRecordEqualityContractProperty(NamedTypeSymbol containingType, bool isOverride)
        {
            ContainingType = containingType;
            IsOverride = isOverride;
            TypeWithAnnotations = TypeWithAnnotations.Create(containingType.DeclaringCompilation.GetWellKnownType(WellKnownType.System_Type), NullableAnnotation.NotAnnotated);
            var overriddenProperty = OverriddenProperty;
            if (overriddenProperty is object)
            {
                TypeWithAnnotations = overriddenProperty.TypeWithAnnotations;
            }
            var getAccessorName = overriddenProperty?.GetMethod?.Name ??
                SourcePropertyAccessorSymbol.GetAccessorName(PropertyName, getNotSet: true, isWinMdOutput: false /* unused for getters */);
            GetMethod = new GetAccessorSymbol(this, getAccessorName);
        }

        public override NamedTypeSymbol ContainingType { get; }

        public override MethodSymbol GetMethod { get; }

        public override MethodSymbol? SetMethod => null;

        public override RefKind RefKind => RefKind.None;

        public override TypeWithAnnotations TypeWithAnnotations { get; }

        public override ImmutableArray<CustomModifier> RefCustomModifiers => ImmutableArray<CustomModifier>.Empty;

        public override ImmutableArray<ParameterSymbol> Parameters => ImmutableArray<ParameterSymbol>.Empty;

        public override bool IsIndexer => false;

        public override bool IsImplicitlyDeclared => true;

        public override ImmutableArray<PropertySymbol> ExplicitInterfaceImplementations => ImmutableArray<PropertySymbol>.Empty;

        public override Symbol ContainingSymbol => ContainingType;

        public override ImmutableArray<Location> Locations => ContainingType.Locations;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => ImmutableArray<SyntaxReference>.Empty;

        public override Accessibility DeclaredAccessibility => Accessibility.Protected;

        public override bool IsStatic => false;

        public override bool IsVirtual => !IsOverride;

        public override bool IsOverride { get; }

        public override bool IsAbstract => false;

        public override bool IsSealed => false;

        public override bool IsExtern => false;

        internal override bool HasSpecialName => false;

        internal override CallingConvention CallingConvention => CallingConvention.HasThis;

        internal override bool MustCallMethodsDirectly => false;

        internal override ObsoleteAttributeData? ObsoleteAttributeData => null;

        public override string Name => PropertyName;

        public override ImmutableArray<CSharpAttributeData> GetAttributes() => ImmutableArray<CSharpAttributeData>.Empty;

        private sealed class GetAccessorSymbol : SynthesizedInstanceMethodSymbol
        {
            private readonly SynthesizedRecordEqualityContractProperty _property;

            public GetAccessorSymbol(SynthesizedRecordEqualityContractProperty property, string name)
            {
                _property = property;
                Name = name;
            }

            public override string Name { get; }

            public override MethodKind MethodKind => MethodKind.PropertyGet;

            public override int Arity => 0;

            public override bool IsExtensionMethod => false;

            public override bool HidesBaseMethodsByName => false;

            public override bool IsVararg => false;

            public override bool ReturnsVoid => false;

            public override bool IsAsync => false;

            public override RefKind RefKind => RefKind.None;

            public override TypeWithAnnotations ReturnTypeWithAnnotations => _property.TypeWithAnnotations;

            public override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

            public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations => ImmutableArray<TypeWithAnnotations>.Empty;

            public override ImmutableArray<TypeParameterSymbol> TypeParameters => ImmutableArray<TypeParameterSymbol>.Empty;

            public override ImmutableArray<ParameterSymbol> Parameters => ImmutableArray<ParameterSymbol>.Empty;

            public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => ImmutableArray<MethodSymbol>.Empty;

            public override ImmutableArray<CustomModifier> RefCustomModifiers => _property.RefCustomModifiers;

            public override Symbol AssociatedSymbol => _property;

            public override Symbol ContainingSymbol => _property.ContainingSymbol;

            public override ImmutableArray<Location> Locations => _property.Locations;

            public override Accessibility DeclaredAccessibility => _property.DeclaredAccessibility;

            public override bool IsStatic => false;

            public override bool IsVirtual => _property.IsVirtual;

            public override bool IsOverride => _property.IsOverride;

            public override bool IsAbstract => _property.IsAbstract;

            public override bool IsSealed => _property.IsSealed;

            public override bool IsExtern => _property.IsExtern;

            public override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull => ImmutableHashSet<string>.Empty;

            internal override bool HasSpecialName => true;

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

            internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false) => !IsOverride;

            internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false) => true;

            internal override bool SynthesizesLoweredBoundBody => true;

            internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
            {
                var F = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);

                try
                {
                    F.CurrentFunction = this;
                    F.CloseMethod(F.Block(F.Return(F.Typeof(ContainingType))));
                }
                catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
                {
                    diagnostics.Add(ex.Diagnostic);
                    F.CloseMethod(F.ThrowNull());
                }
            }
        }
    }
}
