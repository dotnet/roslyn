﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A method symbol that represents a signature for arguments in a collection expression
    /// that target an interface type. The signature is intended to represent a static factory
    /// method that returns an instance of the interface type, rather than any specific
    /// implementation (such as a constructor of a concrete type).
    /// The compiler is free to emit any equivalent method call or initialization.
    /// </summary>
    internal sealed class CollectionArgumentsSignatureOnlyMethodSymbol : MethodSymbol
    {
        internal readonly MethodSymbol WellKnownConstructor;

        internal CollectionArgumentsSignatureOnlyMethodSymbol(
            MethodSymbol wellKnownConstructor,
            string name,
            Symbol containingSymbol,
            ImmutableArray<(string Name, TypeWithAnnotations Type)> parameters,
            TypeWithAnnotations returnType)
        {
            Debug.Assert(wellKnownConstructor is { IsDefinition: true });
            Debug.Assert(containingSymbol is { });
            Debug.Assert(parameters.Length == wellKnownConstructor.Parameters.Length);
            Debug.Assert(!returnType.IsVoidType());

            WellKnownConstructor = wellKnownConstructor;
            Name = name;
            ContainingSymbol = containingSymbol;
            var parameterBuilder = ArrayBuilder<ParameterSymbol>.GetInstance(parameters.Length);
            foreach ((var parameterName, var parameterType) in parameters)
            {
                parameterBuilder.Add(SynthesizedParameterSymbol.Create(this, parameterType, parameterBuilder.Count, RefKind.None, parameterName));
            }
            Parameters = parameterBuilder.ToImmutableAndFree();
            ReturnTypeWithAnnotations = returnType;
        }

        public override string Name { get; }

        public override Symbol ContainingSymbol { get; }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => [];

        public override ImmutableArray<ParameterSymbol> Parameters { get; }

        public override TypeWithAnnotations ReturnTypeWithAnnotations { get; }

        public override MethodKind MethodKind => MethodKind.Ordinary;

        public override int Arity => TypeParameters.Length;

        public override bool IsExtensionMethod => false;

        public override bool HidesBaseMethodsByName => false;

        public override bool IsVararg => false;

        public override bool ReturnsVoid => ReturnType.IsVoidType();

        public override bool IsAsync => throw ExceptionUtilities.Unreachable();

        public override RefKind RefKind => RefKind.None;

        public override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations => default;

        public override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull => ImmutableHashSet<string>.Empty;

        public override FlowAnalysisAnnotations FlowAnalysisAnnotations => default;

        public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations => GetTypeParametersAsTypeArguments();

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => [];

        public override ImmutableArray<CustomModifier> RefCustomModifiers => [];

        public override Symbol? AssociatedSymbol => null;

        public override bool AreLocalsZeroed => throw ExceptionUtilities.Unreachable();

        public override ImmutableArray<Location> Locations => [];

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => [];

        public override Accessibility DeclaredAccessibility => Accessibility.Public;

        public override bool IsStatic => true;

        public override bool IsVirtual => false;

        public override bool IsOverride => false;

        public override bool IsAbstract => false;

        public override bool IsSealed => false;

        public override bool IsExtern => throw ExceptionUtilities.Unreachable();

        protected override bool HasSetsRequiredMembersImpl => throw ExceptionUtilities.Unreachable();

        internal override bool HasSpecialName => true;

        internal override System.Reflection.MethodImplAttributes ImplementationAttributes => throw ExceptionUtilities.Unreachable();

        internal override bool HasDeclarativeSecurity => throw ExceptionUtilities.Unreachable();

        internal override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation => throw ExceptionUtilities.Unreachable();

        internal override bool RequiresSecurityObject => throw ExceptionUtilities.Unreachable();

        internal override bool IsDeclaredReadOnly => false;

        internal override bool IsInitOnly => throw ExceptionUtilities.Unreachable();

        internal override bool HasUnscopedRefAttribute => false;

        internal override bool UseUpdatedEscapeRules => true;

        internal override Microsoft.Cci.CallingConvention CallingConvention => default;

        internal override bool GenerateDebugInfo => throw ExceptionUtilities.Unreachable();

        internal override ObsoleteAttributeData? ObsoleteAttributeData => null;

        public override DllImportData? GetDllImportData() => throw ExceptionUtilities.Unreachable();

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree) => throw ExceptionUtilities.Unreachable();

        internal override ImmutableArray<string> GetAppliedConditionalSymbols() => [];

        internal override IEnumerable<Microsoft.Cci.SecurityAttribute> GetSecurityInformation() => throw ExceptionUtilities.Unreachable();

        internal override UnmanagedCallersOnlyAttributeData? GetUnmanagedCallersOnlyAttributeData(bool forceComplete) => null;

        internal override bool HasAsyncMethodBuilderAttribute(out TypeSymbol? builderArgument)
        {
            builderArgument = null;
            return false;
        }

        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false) => false;

        internal override bool IsMetadataVirtual(IsMetadataVirtualOption option = IsMetadataVirtualOption.None) => false;

        internal override bool IsNullableAnalysisEnabled() => throw ExceptionUtilities.Unreachable();

        internal override int TryGetOverloadResolutionPriority() => 0;
    }
}
