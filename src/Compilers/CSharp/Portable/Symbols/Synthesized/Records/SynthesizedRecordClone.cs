// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.Cci;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedRecordClone : SynthesizedInstanceMethodSymbol
    {
        public override TypeWithAnnotations ReturnTypeWithAnnotations { get; }
        public override NamedTypeSymbol ContainingType { get; }
        public override bool IsOverride { get; }

        public SynthesizedRecordClone(NamedTypeSymbol containingType)
        {
            ContainingType = containingType;
            var baseType = containingType.BaseTypeNoUseSiteDiagnostics;
            if (FindValidCloneMethod(baseType) is { } baseClone)
            {
                // Use covariant returns when available
                ReturnTypeWithAnnotations = baseClone.ReturnTypeWithAnnotations;
                IsOverride = true;
            }
            else
            {
                ReturnTypeWithAnnotations = TypeWithAnnotations.Create(isNullableEnabled: true, containingType);
                IsOverride = false;
            }
        }

        public override string Name => WellKnownMemberNames.CloneMethodName;

        public override MethodKind MethodKind => MethodKind.Ordinary;

        public override int Arity => 0;

        public override bool IsExtensionMethod => false;

        public override bool HidesBaseMethodsByName => false;

        public override bool IsVararg => false;

        public override bool ReturnsVoid => false;

        public override bool IsAsync => false;

        public override RefKind RefKind => RefKind.None;

        public override ImmutableArray<ParameterSymbol> Parameters => ImmutableArray<ParameterSymbol>.Empty;

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

        public override bool IsVirtual => !IsOverride && !IsAbstract;

        public override bool IsAbstract => ContainingType.IsAbstract;

        public override bool IsSealed => false;

        public override bool IsExtern => false;

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
            var F = new SyntheticBoundNodeFactory(this, ContainingType.GetNonNullSyntaxNode(), compilationState, diagnostics);

            var members = ContainingType.GetMembers(WellKnownMemberNames.InstanceConstructorName);
            foreach (var member in members)
            {
                var ctor = (MethodSymbol)member;
                if (ctor.ParameterCount == 1 &&
                    ctor.Parameters[0].Type.Equals(ContainingType, TypeCompareKind.ConsiderEverything))
                {
                    F.CloseMethod(F.Return(F.New(ctor, F.This())));
                    return;
                }
            }

            throw ExceptionUtilities.Unreachable;
        }

        internal static MethodSymbol? FindValidCloneMethod(NamedTypeSymbol containingType)
        {
            for (; !(containingType is null); containingType = containingType.BaseTypeNoUseSiteDiagnostics)
            {
                foreach (var member in containingType.GetMembers(WellKnownMemberNames.CloneMethodName))
                {
                    if (member is MethodSymbol
                    {
                        DeclaredAccessibility: Accessibility.Public,
                        IsStatic: false,
                        ParameterCount: 0,
                        Arity: 0
                    } method && (method.IsOverride || method.IsVirtual || method.IsAbstract))
                    {
                        return method;
                    }
                }
            }
            return null;
        }
    }
}
