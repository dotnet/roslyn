// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    /// <summary>
    /// A synthesized instance method used for binding
    /// expressions outside of a method - specifically, binding
    /// DebuggerDisplayAttribute expressions.
    /// </summary>
    internal sealed class SynthesizedContextMethodSymbol : SynthesizedInstanceMethodSymbol
    {
        private readonly NamedTypeSymbol _container;

        public SynthesizedContextMethodSymbol(NamedTypeSymbol container)
        {
            _container = container;
        }

        public override int Arity
        {
            get { return 0; }
        }

        public override Symbol AssociatedSymbol
        {
            get { return null; }
        }

        public override Symbol ContainingSymbol
        {
            get { return _container; }
        }

        public override Accessibility DeclaredAccessibility
        {
            get { return Accessibility.NotApplicable; }
        }

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations
        {
            get { return ImmutableArray<MethodSymbol>.Empty; }
        }

        public override bool HidesBaseMethodsByName
        {
            get { return false; }
        }

        public override bool IsAbstract
        {
            get { return false; }
        }

        public override bool IsAsync
        {
            get { return false; }
        }

        public override bool IsExtensionMethod
        {
            get { return false; }
        }

        public override bool IsExtern
        {
            get { return false; }
        }

        public override bool IsOverride
        {
            get { return false; }
        }

        public override bool IsSealed
        {
            get { return true; }
        }

        public override bool IsStatic
        {
            get { return false; }
        }

        public override bool IsVararg
        {
            get { return false; }
        }

        public override bool IsVirtual
        {
            get { return false; }
        }

        public override ImmutableArray<Location> Locations
        {
            get { throw ExceptionUtilities.Unreachable(); }
        }

        public override MethodKind MethodKind
        {
            get { return MethodKind.Ordinary; }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get { return ImmutableArray<ParameterSymbol>.Empty; }
        }

        public override bool ReturnsVoid
        {
            get { return true; }
        }

        public override RefKind RefKind
        {
            get { return RefKind.None; }
        }

        public override TypeWithAnnotations ReturnTypeWithAnnotations
        {
            get { throw ExceptionUtilities.Unreachable(); }
        }

        public override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull => ImmutableHashSet<string>.Empty;

        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get { throw ExceptionUtilities.Unreachable(); }
        }

        public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations
        {
            get { return ImmutableArray<TypeWithAnnotations>.Empty; }
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get { return ImmutableArray<TypeParameterSymbol>.Empty; }
        }

        internal override Microsoft.Cci.CallingConvention CallingConvention
        {
            get { throw ExceptionUtilities.Unreachable(); }
        }

        internal override bool GenerateDebugInfo
        {
            get { throw ExceptionUtilities.Unreachable(); }
        }

        internal override bool HasDeclarativeSecurity
        {
            get { throw ExceptionUtilities.Unreachable(); }
        }

        internal override bool HasSpecialName
        {
            get { throw ExceptionUtilities.Unreachable(); }
        }

        internal override MethodImplAttributes ImplementationAttributes
        {
            get { throw ExceptionUtilities.Unreachable(); }
        }

        internal override bool RequiresSecurityObject
        {
            get { throw ExceptionUtilities.Unreachable(); }
        }

        internal override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation
        {
            get { throw ExceptionUtilities.Unreachable(); }
        }

        public override DllImportData GetDllImportData()
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override IEnumerable<Microsoft.Cci.SecurityAttribute> GetSecurityInformation()
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override bool IsMetadataFinal
        {
            get
            {
                return false;
            }
        }

        internal override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false)
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false)
        {
            throw ExceptionUtilities.Unreachable();
        }

        protected override bool HasSetsRequiredMembersImpl => throw ExceptionUtilities.Unreachable();
    }
}
