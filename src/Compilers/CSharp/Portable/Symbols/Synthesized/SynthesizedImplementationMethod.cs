﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
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
            typeMap.WithAlphaRename(interfaceMethod, this, out _typeParameters);

            _interfaceMethod = interfaceMethod.ConstructIfGeneric(_typeParameters.Cast<TypeParameterSymbol, TypeSymbol>());
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

        public sealed override ImmutableArray<CustomModifier> ReturnTypeCustomModifiers
        {
            get { return _interfaceMethod.ReturnTypeCustomModifiers; }
        }

        public sealed override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get { return _interfaceMethod.RefCustomModifiers; }
        }

        #endregion

        internal override void AddSynthesizedAttributes(ModuleCompilationState compilationState, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(compilationState, ref attributes);

            var compilation = this.DeclaringCompilation;
            if (this.ReturnType.ContainsDynamic() && compilation.HasDynamicEmitAttributes() && compilation.CanEmitBoolean())
            {
                AddSynthesizedAttribute(ref attributes, compilation.SynthesizeDynamicAttribute(this.ReturnType, this.ReturnTypeCustomModifiers.Length + this.RefCustomModifiers.Length, this.RefKind));
            }

            if (ReturnType.ContainsTupleNames() &&
                compilation.HasTupleNamesAttributes &&
                compilation.CanEmitSpecialType(SpecialType.System_String))
            {
                AddSynthesizedAttribute(ref attributes,
                    compilation.SynthesizeTupleNamesAttribute(ReturnType));
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

        public sealed override ImmutableArray<TypeSymbol> TypeArguments
        {
            get { return _typeParameters.Cast<TypeParameterSymbol, TypeSymbol>(); }
        }

        internal override RefKind RefKind
        {
            get { return _interfaceMethod.RefKind; }
        }

        public sealed override TypeSymbol ReturnType
        {
            get { return _interfaceMethod.ReturnType; }
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
