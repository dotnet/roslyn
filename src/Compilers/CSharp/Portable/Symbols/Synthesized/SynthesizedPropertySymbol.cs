// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Cci;

namespace Microsoft.CodeAnalysis.CSharp.Symbols;

internal sealed class SynthesizedPropertySymbol : PropertySymbol
{
    private readonly string _name;
    private readonly SynthesizedFieldSymbol _backingField;

    public SynthesizedPropertySymbol(string name, SynthesizedFieldSymbol backingField)
    {
        _name = name;
        _backingField = backingField;
        GetMethod = new GetAccessorMethodSymbol(this);
    }

    public override string Name => _name;
    public override TypeWithAnnotations TypeWithAnnotations => _backingField.TypeWithAnnotations;
    public override RefKind RefKind => RefKind.None;
    public override ImmutableArray<CustomModifier> RefCustomModifiers => [];
    public override MethodSymbol GetMethod { get; }
    public override MethodSymbol? SetMethod => null;
    public override Symbol ContainingSymbol => _backingField.ContainingSymbol;
    public override Accessibility DeclaredAccessibility => Accessibility.Public;

    public override ImmutableArray<Location> Locations => [];
    public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => [];
    public override ImmutableArray<PropertySymbol> ExplicitInterfaceImplementations => [];
    public override ImmutableArray<ParameterSymbol> Parameters => [];
    public override bool IsIndexer => false;
    public override bool IsStatic => false;
    public override bool IsVirtual => false;
    public override bool IsOverride => false;
    public override bool IsAbstract => false;
    public override bool IsSealed => false;
    public override bool IsExtern => false;
    internal override bool IsRequired => false;
    internal override bool HasSpecialName => false;
    internal override CallingConvention CallingConvention => CallingConvention.HasThis;
    internal override bool MustCallMethodsDirectly => false;
    internal override bool HasUnscopedRefAttribute => false;
    internal override bool IsCallerUnsafe => false;
    internal override ObsoleteAttributeData? ObsoleteAttributeData => null;
    internal override int TryGetOverloadResolutionPriority() => 0;

    private sealed partial class GetAccessorMethodSymbol(SynthesizedPropertySymbol property) : SynthesizedMethodSymbol
    {
        private readonly SynthesizedPropertySymbol _property = property;

        public override string Name => $"get_{_property.Name}";
        internal override bool HasSpecialName => true;
        public override MethodKind MethodKind => MethodKind.PropertyGet;
        public override Symbol AssociatedSymbol => _property;
        public override Symbol ContainingSymbol => _property.ContainingSymbol;

        internal override bool SynthesizesLoweredBoundBody => true;

        internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
        {
            SyntheticBoundNodeFactory F = new SyntheticBoundNodeFactory(this, CSharpSyntaxTree.Dummy.GetRoot(), compilationState, diagnostics);
            F.CurrentFunction = this.OriginalDefinition;

            try
            {
                // return this._backingField;
                F.CloseMethod(F.Return(F.Field(F.This(), _property._backingField)));
            }
            catch (SyntheticBoundNodeFactory.MissingPredefinedMember ex)
            {
                F.CloseMethod(F.ThrowNull());
                diagnostics.Add(ex.Diagnostic);
            }
        }

        public override bool IsStatic => false;
        public override int Arity => 0;
        public override bool IsExtensionMethod => false;
        public override bool HidesBaseMethodsByName => false;
        public override bool IsVararg => false;
        public override bool ReturnsVoid => false;
        public override bool IsAsync => false;
        public override RefKind RefKind => RefKind.None;
        public override ImmutableArray<CustomModifier> RefCustomModifiers => [];
        public override TypeWithAnnotations ReturnTypeWithAnnotations => _property.TypeWithAnnotations;
        public override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations => FlowAnalysisAnnotations.None;
        public override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull => [];
        public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations => [];
        public override ImmutableArray<TypeParameterSymbol> TypeParameters => [];
        public override ImmutableArray<ParameterSymbol> Parameters => [];
        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => [];
        public override ImmutableArray<Location> Locations => [];
        public override Accessibility DeclaredAccessibility => _property.DeclaredAccessibility;
        public override bool IsVirtual => false;
        public override bool IsOverride => false;
        public override bool IsAbstract => false;
        public override bool IsSealed => false;
        public override bool IsExtern => false;
        protected override bool HasSetsRequiredMembersImpl => false;
        internal override MethodImplAttributes ImplementationAttributes => default;
        internal override bool HasDeclarativeSecurity => false;
        internal override MarshalPseudoCustomAttributeData? ReturnValueMarshallingInformation => null;
        internal override bool RequiresSecurityObject => false;
        internal override CallingConvention CallingConvention => CallingConvention.HasThis;
        internal override bool GenerateDebugInfo => false;

        public override DllImportData? GetDllImportData() => null;
        internal override ImmutableArray<string> GetAppliedConditionalSymbols() => [];
        internal override IEnumerable<SecurityAttribute>? GetSecurityInformation() => null;
        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false) => false;
        internal override bool IsMetadataVirtual(IsMetadataVirtualOption option = IsMetadataVirtualOption.None) => false;
    }
}
