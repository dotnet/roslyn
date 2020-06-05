// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Cci;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedAutoPropAccessorSymbol : SynthesizedInstanceMethodSymbol
    {
        public enum AccessorKind : byte
        {
            Get,
            Init
        }

        private readonly SourcePropertySymbolBase _property;
        private readonly AccessorKind _accessorKind;
        private readonly StrongBox<TypeWithAnnotations>? _voidType;

        public override string Name { get; }

        public SynthesizedAutoPropAccessorSymbol(
            SourcePropertySymbolBase property,
            string paramName,
            AccessorKind accessorKind,
            DiagnosticBag diagnostics)
        {
            _property = property;
            _accessorKind = accessorKind;
            Name = SourcePropertyAccessorSymbol.GetAccessorName(
                paramName,
                getNotSet: accessorKind == AccessorKind.Get,
                // https://github.com/dotnet/roslyn/issues/44684
                isWinMdOutput: false);

            if (accessorKind != AccessorKind.Get)
            {
                var comp = property.DeclaringCompilation;
                var type = TypeWithAnnotations.Create(comp.GetSpecialType(SpecialType.System_Void));

                if (accessorKind == AccessorKind.Init)
                {
                    var initOnlyType = Binder.GetWellKnownType(
                        comp,
                        WellKnownType.System_Runtime_CompilerServices_IsExternalInit,
                        diagnostics,
                        property.Location);

                    var modifiers = ImmutableArray.Create(CSharpCustomModifier.CreateRequired(initOnlyType));
                    type = type.WithModifiers(modifiers);
                }
                _voidType = new StrongBox<TypeWithAnnotations>(type);
            }
        }

        public override TypeWithAnnotations ReturnTypeWithAnnotations
        {
            get
            {
                if (_voidType is object)
                {
                    Debug.Assert(MethodKind == MethodKind.PropertySet);
                    return _voidType.Value;
                }

                return _property.TypeWithAnnotations;
            }
        }

        public override MethodKind MethodKind => _accessorKind == AccessorKind.Get
            ? MethodKind.PropertyGet
            : MethodKind.PropertySet;

        internal override bool IsInitOnly => _accessorKind == AccessorKind.Init;

        public override int Arity => 0;

        public override bool IsExtensionMethod => false;

        public override bool HidesBaseMethodsByName => false;

        public override bool IsVararg => false;

        public override bool ReturnsVoid => _voidType is object;

        public override bool IsAsync => false;

        public override RefKind RefKind => RefKind.None;

        public override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations => ImmutableArray<TypeWithAnnotations>.Empty;

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => ImmutableArray<TypeParameterSymbol>.Empty;

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                if (_accessorKind == AccessorKind.Get)
                {
                    return ImmutableArray<ParameterSymbol>.Empty;
                }
                return ImmutableArray.Create(SynthesizedParameterSymbol.Create(
                    this,
                    _property.TypeWithAnnotations,
                    ordinal: 0,
                    RefKind.None,
                    name: ParameterSymbol.ValueParameterName));
            }
        }

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => ImmutableArray<MethodSymbol>.Empty;

        public override ImmutableArray<CustomModifier> RefCustomModifiers => _property.RefCustomModifiers;

        public override Symbol AssociatedSymbol => _property;

        public override Symbol ContainingSymbol => _property.ContainingSymbol;

        public override ImmutableArray<Location> Locations => _property.Locations;

        public override Accessibility DeclaredAccessibility => _property.DeclaredAccessibility;

        public override bool IsStatic => _property.IsStatic;

        public override bool IsVirtual => _property.IsVirtual;

        public override bool IsOverride => _property.IsOverride;

        public override bool IsAbstract => _property.IsAbstract;

        public override bool IsSealed => _property.IsSealed;

        public override bool IsExtern => _property.IsExtern;

        public override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull => ImmutableHashSet<string>.Empty;

        internal override bool HasSpecialName => _property.HasSpecialName;

        internal override MethodImplAttributes ImplementationAttributes => MethodImplAttributes.Managed;

        internal override bool HasDeclarativeSecurity => false;

        internal override MarshalPseudoCustomAttributeData? ReturnValueMarshallingInformation => null;

        internal override bool RequiresSecurityObject => false;

        internal override CallingConvention CallingConvention
            => IsStatic ? CallingConvention.Default : CallingConvention.HasThis;

        internal override bool GenerateDebugInfo => false;

        public override DllImportData? GetDllImportData() => null;

        internal override ImmutableArray<string> GetAppliedConditionalSymbols()
            => ImmutableArray<string>.Empty;

        internal override IEnumerable<SecurityAttribute> GetSecurityInformation()
            => Array.Empty<SecurityAttribute>();

        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false) => IsVirtual && !IsOverride;

        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false) => IsVirtual;

        internal override bool SynthesizesLoweredBoundBody => true;

        internal override void GenerateMethodBody(TypeCompilationState compilationState, DiagnosticBag diagnostics)
        {
            Debug.Assert(!(_property.BackingField is null));
            var F = new SyntheticBoundNodeFactory(this, this.GetNonNullSyntaxNode(), compilationState, diagnostics);
            F.CurrentFunction = this;

            if (_accessorKind == AccessorKind.Get)
            {
                // Method body:
                //
                // {
                //      return this.<>backingField;
                // }
                F.CloseMethod(F.Block(F.Return(F.Field(F.This(), _property.BackingField))));
            }
            else
            {
                // Method body:
                //
                // {
                //      this.<>backingField = value;
                // }
                F.CloseMethod(F.Block(
                    F.Assignment(F.Field(F.This(), _property.BackingField), F.Parameter(Parameters[0])),
                    F.Return()));
            }
        }
    }
}