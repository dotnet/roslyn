// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SynthesizedDelegateConstructor : SynthesizedInstanceConstructor
    {
        private readonly ImmutableArray<ParameterSymbol> _parameters;

        public SynthesizedDelegateConstructor(NamedTypeSymbol containingType, TypeSymbol objectType, TypeSymbol intPtrType)
            : base(containingType)
        {
            _parameters = ImmutableArray.Create<ParameterSymbol>(
               SynthesizedParameterSymbol.Create(this, TypeWithAnnotations.Create(objectType), 0, RefKind.None, "object"),
               SynthesizedParameterSymbol.Create(this, TypeWithAnnotations.Create(intPtrType), 1, RefKind.None, "method"));
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get { return _parameters; }
        }
    }

    internal sealed class SynthesizedDelegateInvokeMethod : SynthesizedInstanceMethodSymbol
    {
        internal readonly struct ParameterDescription
        {
            internal ParameterDescription(TypeWithAnnotations type, RefKind refKind, ScopedKind scope, ConstantValue? defaultValue, bool isParams, bool hasUnscopedRefAttribute)
            {
                Type = type;
                RefKind = refKind;
                Scope = scope;
                DefaultValue = defaultValue;
                IsParams = isParams;
                HasUnscopedRefAttribute = hasUnscopedRefAttribute;
            }

            internal readonly TypeWithAnnotations Type;
            internal readonly RefKind RefKind;
            internal readonly ScopedKind Scope;
            internal readonly ConstantValue? DefaultValue;
            internal readonly bool IsParams;
            internal readonly bool HasUnscopedRefAttribute;
        }

        private readonly NamedTypeSymbol _containingType;

        internal SynthesizedDelegateInvokeMethod(
            NamedTypeSymbol containingType,
            ArrayBuilder<ParameterDescription> parameterDescriptions,
            TypeWithAnnotations returnType,
            RefKind refKind)
        {
            _containingType = containingType;

            Parameters = parameterDescriptions.SelectAsArrayWithIndex(static (p, i, a) =>
                SynthesizedParameterSymbol.Create(a.Method, p.Type, i, p.RefKind, GeneratedNames.AnonymousDelegateParameterName(i, a.ParameterCount), p.Scope, p.DefaultValue, isParams: p.IsParams, hasUnscopedRefAttribute: p.HasUnscopedRefAttribute),
                (Method: this, ParameterCount: parameterDescriptions.Count));
            ReturnTypeWithAnnotations = returnType;
            RefKind = refKind;
        }

        public override string Name
        {
            get { return WellKnownMemberNames.DelegateInvokeName; }
        }

        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false)
        {
            return true;
        }

        internal override bool IsMetadataVirtual(IsMetadataVirtualOption option = IsMetadataVirtualOption.None)
        {
            return true;
        }

        internal override bool IsMetadataFinal
        {
            get
            {
                return false;
            }
        }

        public override MethodKind MethodKind
        {
            get { return MethodKind.DelegateInvoke; }
        }

        public override int Arity
        {
            get { return 0; }
        }

        public override bool IsExtensionMethod
        {
            get { return false; }
        }

        internal override bool HasSpecialName
        {
            get { return false; }
        }

        internal override System.Reflection.MethodImplAttributes ImplementationAttributes
        {
            get { return System.Reflection.MethodImplAttributes.Runtime; }
        }

        internal override bool HasDeclarativeSecurity
        {
            get { return false; }
        }

        public override DllImportData? GetDllImportData()
        {
            return null;
        }

        internal override IEnumerable<Microsoft.Cci.SecurityAttribute> GetSecurityInformation()
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override MarshalPseudoCustomAttributeData? ReturnValueMarshallingInformation
        {
            get { return null; }
        }

        internal override bool RequiresSecurityObject
        {
            get { return false; }
        }

        public override bool HidesBaseMethodsByName
        {
            get { return false; }
        }

        public override bool IsVararg
        {
            get { return false; }
        }

        public override bool ReturnsVoid
        {
            get { return ReturnType.IsVoidType(); }
        }

        public override bool IsAsync
        {
            get { return false; }
        }

        public override RefKind RefKind { get; }

        public override TypeWithAnnotations ReturnTypeWithAnnotations { get; }

        public override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull => ImmutableHashSet<string>.Empty;

        public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations
        {
            get { return ImmutableArray<TypeWithAnnotations>.Empty; }
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get { return ImmutableArray<TypeParameterSymbol>.Empty; }
        }

        public override ImmutableArray<ParameterSymbol> Parameters { get; }

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations
        {
            get { return ImmutableArray<MethodSymbol>.Empty; }
        }

        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get { return ImmutableArray<CustomModifier>.Empty; }
        }

        public override Symbol? AssociatedSymbol
        {
            get { return null; }
        }

        internal override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            return ImmutableArray<string>.Empty;
        }

        internal override Microsoft.Cci.CallingConvention CallingConvention
        {
            get { return Microsoft.Cci.CallingConvention.HasThis; }
        }

        internal override bool GenerateDebugInfo
        {
            get { return false; }
        }

        public override Symbol ContainingSymbol
        {
            get { return _containingType; }
        }

        public override ImmutableArray<Location> Locations
        {
            get { return ImmutableArray<Location>.Empty; }
        }

        public override Accessibility DeclaredAccessibility
        {
            get
            {
                // Invoke method of a delegate used in a dynamic call-site must be public 
                // since the DLR looks only for public Invoke methods:
                return Accessibility.Public;
            }
        }

        public override bool IsStatic
        {
            get { return false; }
        }

        public override bool IsVirtual
        {
            get { return true; }
        }

        public override bool IsOverride
        {
            get { return false; }
        }

        public override bool IsAbstract
        {
            get { return false; }
        }

        public override bool IsSealed
        {
            get { return false; }
        }

        public override bool IsExtern
        {
            get { return false; }
        }

        protected sealed override bool HasSetsRequiredMembersImpl => throw ExceptionUtilities.Unreachable();
    }
}
