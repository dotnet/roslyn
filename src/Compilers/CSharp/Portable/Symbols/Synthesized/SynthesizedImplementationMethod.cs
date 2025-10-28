// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal abstract class SynthesizedImplementationMethod : SynthesizedMethodSymbol
    {
        //inputs
        protected readonly MethodSymbol _interfaceMethod;
        private readonly NamedTypeSymbol _implementingType;
        private readonly bool _generateDebugInfo;
        private readonly PropertySymbol _associatedProperty;

        //computed
        private readonly ImmutableArray<MethodSymbol> _explicitInterfaceImplementations;
        private readonly ImmutableArray<TypeParameterSymbol> _typeParameters;
        private readonly ImmutableArray<ParameterSymbol> _parameters;
        private readonly string _name;

        public SynthesizedImplementationMethod(
            MethodSymbol interfaceMethod,
            NamedTypeSymbol implementingType,
            string name = null,
            bool generateDebugInfo = true,
            PropertySymbol associatedProperty = null)
        {
            //it does not make sense to add methods to substituted types
            Debug.Assert(implementingType.IsDefinition);

            _name = name ?? ExplicitInterfaceHelpers.GetMemberName(interfaceMethod.Name, interfaceMethod.ContainingType, aliasQualifierOpt: null);
            _implementingType = implementingType;
            _generateDebugInfo = generateDebugInfo;
            _associatedProperty = associatedProperty;
            _explicitInterfaceImplementations = ImmutableArray.Create<MethodSymbol>(interfaceMethod);

            // alpha-rename to get the implementation's type parameters
            var typeMap = interfaceMethod.ContainingType.TypeSubstitution ?? TypeMap.Empty;
            typeMap.WithAlphaRename(interfaceMethod, this, propagateAttributes: false, out _typeParameters);

            _interfaceMethod = interfaceMethod.ConstructIfGeneric(TypeArgumentsWithAnnotations);
            _parameters = SynthesizedParameterSymbol.DeriveParameters(_interfaceMethod, this);
        }

        #region Delegate to interfaceMethod

        public sealed override bool IsVararg
        {
            get { return _interfaceMethod.IsVararg; }
        }

        public sealed override int Arity
        {
            get { return _interfaceMethod.Arity; }
        }

        public sealed override bool ReturnsVoid
        {
            get { return _interfaceMethod.ReturnsVoid; }
        }

        internal sealed override Cci.CallingConvention CallingConvention
        {
            get { return _interfaceMethod.CallingConvention; }
        }

        public sealed override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get { return _interfaceMethod.RefCustomModifiers; }
        }

        #endregion

        internal sealed override bool GenerateDebugInfo
        {
            get { return _generateDebugInfo; }
        }

        public sealed override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get { return _typeParameters; }
        }

        public sealed override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations
        {
            get { return GetTypeParametersAsTypeArguments(); }
        }

        public sealed override RefKind RefKind
        {
            get { return _interfaceMethod.RefKind; }
        }

        public sealed override TypeWithAnnotations ReturnTypeWithAnnotations
        {
            get { return _interfaceMethod.ReturnTypeWithAnnotations; }
        }

        public sealed override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public sealed override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull => ImmutableHashSet<string>.Empty;

        public sealed override ImmutableArray<ParameterSymbol> Parameters
        {
            get { return _parameters; }
        }

        public sealed override Symbol ContainingSymbol
        {
            get { return _implementingType; }
        }

        public sealed override NamedTypeSymbol ContainingType
        {
            get
            {
                return _implementingType;
            }
        }

        internal sealed override bool IsExplicitInterfaceImplementation
        {
            get { return true; }
        }

        public sealed override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations
        {
            get { return _explicitInterfaceImplementations; }
        }

        public override MethodKind MethodKind
        {
            get
            {
                return MethodKind.ExplicitInterfaceImplementation;
            }
        }

        public sealed override Accessibility DeclaredAccessibility
        {
            get { return Accessibility.Private; }
        }

        public sealed override Symbol AssociatedSymbol
        {
            get { return _associatedProperty; }
        }

        public sealed override bool HidesBaseMethodsByName
        {
            get { return false; }
        }

        public sealed override ImmutableArray<Location> Locations
        {
            get { return ImmutableArray<Location>.Empty; }
        }

        public override bool IsStatic
        {
            get { return false; }
        }

        public sealed override bool IsVirtual
        {
            get { return false; }
        }

        public sealed override bool IsOverride
        {
            get { return false; }
        }

        public sealed override bool IsAbstract
        {
            get { return false; }
        }

        public sealed override bool IsSealed
        {
            get { return false; }
        }

        public sealed override bool IsExtern
        {
            get { return false; }
        }

        public sealed override bool IsExtensionMethod
        {
            get { return false; }
        }

        public sealed override string Name
        {
            get { return _name; }
        }

        internal override bool HasSpecialName
        {
            get { return _interfaceMethod.HasSpecialName; }
        }

        internal sealed override bool RequiresSecurityObject
        {
            get { return _interfaceMethod.RequiresSecurityObject; }
        }

        internal sealed override bool IsMetadataVirtual(IsMetadataVirtualOption option = IsMetadataVirtualOption.None)
        {
            return !IsStatic;
        }

        internal sealed override bool IsMetadataFinal
        {
            get
            {
                return !IsStatic;
            }
        }

        internal sealed override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false)
        {
            return !IsStatic;
        }

        public sealed override DllImportData GetDllImportData()
        {
            return null;
        }

        internal sealed override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation
        {
            get { return null; }
        }

        internal sealed override bool HasDeclarativeSecurity
        {
            get { return false; }
        }

        internal sealed override IEnumerable<Microsoft.Cci.SecurityAttribute> GetSecurityInformation()
        {
            throw ExceptionUtilities.Unreachable();
        }

        internal sealed override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            return ImmutableArray<string>.Empty;
        }

        protected sealed override bool HasSetsRequiredMembersImpl => throw ExceptionUtilities.Unreachable();
    }
}
