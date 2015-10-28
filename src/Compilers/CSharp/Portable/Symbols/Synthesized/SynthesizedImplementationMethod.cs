// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal abstract class SynthesizedImplementationMethod : SynthesizedInstanceMethodSymbol
    {
        //inputs
        private readonly MethodSymbol _interfaceMethod;
        private readonly NamedTypeSymbol _implementingType;
        private readonly bool _generateDebugInfo;
        private readonly PropertySymbol _associatedProperty;

        //computed
        private readonly ImmutableArray<MethodSymbol> _explicitInterfaceImplementations;
        private readonly ImmutableArray<TypeParameterSymbol> _typeParameters;
        private readonly TypeSymbolWithAnnotations _returnType;
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
            _interfaceMethod = interfaceMethod;
            _implementingType = implementingType;
            _generateDebugInfo = generateDebugInfo;
            _associatedProperty = associatedProperty;
            _explicitInterfaceImplementations = ImmutableArray.Create<MethodSymbol>(interfaceMethod);

            // alpha-rename to get the implementation's type parameters
            var typeMap = interfaceMethod.ContainingType.TypeSubstitution ?? TypeMap.Empty;
            typeMap.WithAlphaRename(interfaceMethod, this, out _typeParameters);

            var substitutedInterfaceMethod = interfaceMethod.IsGenericMethod ? interfaceMethod.Construct(_typeParameters.SelectAsArray(TypeMap.AsTypeSymbolWithAnnotations)) : interfaceMethod;
            _returnType = substitutedInterfaceMethod.ReturnType;
            _parameters = SynthesizedParameterSymbol.DeriveParameters(substitutedInterfaceMethod, this);
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

        #endregion

        internal override void AddSynthesizedAttributes(ModuleCompilationState compilationState, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(compilationState, ref attributes);

            TypeSymbolWithAnnotations returnType = this.ReturnType;
            if (returnType.TypeSymbol.ContainsDynamic())
            {
                var compilation = this.DeclaringCompilation;
                AddSynthesizedAttribute(ref attributes, compilation.SynthesizeDynamicAttribute(returnType.TypeSymbol, returnType.CustomModifiers.Length));
            }
        }

        internal sealed override bool GenerateDebugInfo
        {
            get { return _generateDebugInfo; }
        }

        public sealed override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get { return _typeParameters; }
        }

        public sealed override ImmutableArray<TypeSymbolWithAnnotations> TypeArguments
        {
            get { return _typeParameters.SelectAsArray(TypeMap.AsTypeSymbolWithAnnotations); }
        }

        public sealed override TypeSymbolWithAnnotations ReturnType
        {
            get { return _returnType; }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get { return _parameters; }
        }

        public override Symbol ContainingSymbol
        {
            get { return _implementingType; }
        }

        public override NamedTypeSymbol ContainingType
        {
            get
            {
                return _implementingType;
            }
        }

        internal override bool IsExplicitInterfaceImplementation
        {
            get { return true; }
        }

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations
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

        public override Accessibility DeclaredAccessibility
        {
            get { return Accessibility.Private; }
        }

        public override Symbol AssociatedSymbol
        {
            get { return _associatedProperty; }
        }

        public override bool HidesBaseMethodsByName
        {
            get { return false; }
        }

        public override ImmutableArray<Location> Locations
        {
            get { return ImmutableArray<Location>.Empty; }
        }

        public override bool IsStatic
        {
            get { return false; }
        }

        public override bool IsAsync
        {
            get { return false; }
        }

        public override bool IsVirtual
        {
            get { return false; }
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

        public override bool IsExtensionMethod
        {
            get { return false; }
        }

        public override string Name
        {
            get { return _name; }
        }

        internal sealed override bool HasSpecialName
        {
            get { return _interfaceMethod.HasSpecialName; }
        }

        internal sealed override System.Reflection.MethodImplAttributes ImplementationAttributes
        {
            get { return default(System.Reflection.MethodImplAttributes); }
        }

        internal sealed override bool RequiresSecurityObject
        {
            get { return _interfaceMethod.RequiresSecurityObject; }
        }

        internal sealed override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false)
        {
            return true;
        }

        internal override bool IsMetadataFinal
        {
            get
            {
                return true;
            }
        }

        internal sealed override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false)
        {
            return true;
        }

        public override DllImportData GetDllImportData()
        {
            return null;
        }

        internal override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation
        {
            get { return null; }
        }

        internal override bool HasDeclarativeSecurity
        {
            get { return false; }
        }

        internal override IEnumerable<Microsoft.Cci.SecurityAttribute> GetSecurityInformation()
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            return ImmutableArray<string>.Empty;
        }
    }
}
