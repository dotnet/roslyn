// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedEmbeddedExtensionMarkerAttributeSymbol : SynthesizedEmbeddedAttributeSymbolBase
    {
        private readonly ImmutableArray<MethodSymbol> _constructors;
        private readonly SynthesizedFieldSymbol _nameField;
        private readonly NamePropertySymbol _nameProperty;

        private const string PropertyName = "Name";
        private const string FieldName = "<Name>k__BackingField";

        public SynthesizedEmbeddedExtensionMarkerAttributeSymbol(
            string name,
            NamespaceSymbol containingNamespace,
            ModuleSymbol containingModule,
            NamedTypeSymbol systemAttributeType,
            TypeSymbol systemStringType)
            : base(name, containingNamespace, containingModule, baseType: systemAttributeType)
        {
            Debug.Assert(FieldName == GeneratedNames.MakeBackingFieldName(PropertyName));

            _nameField = new SynthesizedFieldSymbol(this, systemStringType, FieldName, isReadOnly: true);
            _nameProperty = new NamePropertySymbol(_nameField);
            _constructors = [new SynthesizedEmbeddedAttributeConstructorWithBodySymbol(this, getConstructorParameters, getConstructorBody)];

            // Ensure we never get out of sync with the description
            Debug.Assert(_constructors.Length == AttributeDescription.ExtensionMarkerAttribute.Signatures.Length);

            ImmutableArray<ParameterSymbol> getConstructorParameters(MethodSymbol ctor)
            {
                return [SynthesizedParameterSymbol.Create(ctor, TypeWithAnnotations.Create(systemStringType), ordinal: 0, RefKind.None, name: "name")];
            }

            void getConstructorBody(SyntheticBoundNodeFactory f, ArrayBuilder<BoundStatement> statements, ImmutableArray<ParameterSymbol> parameters)
            {
                // this._namedField = name;
                statements.Add(f.Assignment(
                    f.Field(f.This(), this._nameField),
                    f.Parameter(parameters[0])));
            }
        }

        public override ImmutableArray<MethodSymbol> Constructors => _constructors;

        internal override AttributeUsageInfo GetAttributeUsageInfo()
        {
            return new AttributeUsageInfo(
                AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Method | AttributeTargets.Property |
                AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Interface | AttributeTargets.Delegate,
                allowMultiple: false, inherited: false);
        }

        internal override IEnumerable<FieldSymbol> GetFieldsToEmit()
        {
            return [_nameField];
        }

        public override ImmutableArray<Symbol> GetMembers()
            => [_nameField, _nameProperty, _nameProperty.GetMethod, _constructors[0]];

        public override ImmutableArray<Symbol> GetMembers(string name)
        {
            return name switch
            {
                FieldName => [_nameField],
                PropertyName => [_nameProperty],
                WellKnownMemberNames.InstanceConstructorName => ImmutableArray<Symbol>.CastUp(_constructors),
                _ => []
            };
        }

        public override IEnumerable<string> MemberNames
            => [FieldName, PropertyName, WellKnownMemberNames.InstanceConstructorName];

        private sealed class NamePropertySymbol : PropertySymbol
        {
            internal readonly SynthesizedFieldSymbol _backingField;

            public NamePropertySymbol(SynthesizedFieldSymbol backingField)
            {
                _backingField = backingField;
                GetMethod = new NameGetAccessorMethodSymbol(this);
            }

            public override string Name => PropertyName;
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
            internal override ObsoleteAttributeData? ObsoleteAttributeData => null;
            internal override int TryGetOverloadResolutionPriority() => 0;
        }

        private sealed partial class NameGetAccessorMethodSymbol : SynthesizedMethodSymbol
        {
            private readonly NamePropertySymbol _nameProperty;

            public NameGetAccessorMethodSymbol(NamePropertySymbol nameProperty)
            {
                _nameProperty = nameProperty;
            }

            public override string Name => "get_Name";
            internal override bool HasSpecialName => true;
            public override MethodKind MethodKind => MethodKind.PropertyGet;
            public override Symbol AssociatedSymbol => _nameProperty;
            public override Symbol ContainingSymbol => _nameProperty.ContainingSymbol;

            internal override bool SynthesizesLoweredBoundBody => true;

            internal override void GenerateMethodBody(TypeCompilationState compilationState, BindingDiagnosticBag diagnostics)
            {
                SyntheticBoundNodeFactory F = new SyntheticBoundNodeFactory(this, CSharpSyntaxTree.Dummy.GetRoot(), compilationState, diagnostics);
                F.CurrentFunction = this.OriginalDefinition;

                try
                {
                    // return this._backingField;
                    F.CloseMethod(F.Return(F.Field(F.This(), _nameProperty._backingField)));
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
            public override TypeWithAnnotations ReturnTypeWithAnnotations => _nameProperty.TypeWithAnnotations;
            public override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations => FlowAnalysisAnnotations.None;
            public override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull => [];
            public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations => [];
            public override ImmutableArray<TypeParameterSymbol> TypeParameters => [];
            public override ImmutableArray<ParameterSymbol> Parameters => [];
            public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => [];
            public override ImmutableArray<Location> Locations => [];
            public override Accessibility DeclaredAccessibility => _nameProperty.DeclaredAccessibility;
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
}
