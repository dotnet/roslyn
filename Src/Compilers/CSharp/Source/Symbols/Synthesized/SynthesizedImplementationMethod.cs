// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal class SynthesizedImplementationMethod : SynthesizedInstanceMethodSymbol
    {
        //inputs
        private readonly MethodSymbol interfaceMethod;
        private readonly NamedTypeSymbol implementingType;
        private readonly bool debuggerHidden;
        private readonly PropertySymbol associatedProperty;
        private readonly MethodSymbol asyncKickoffMethod;

        //computed
        private readonly ImmutableArray<MethodSymbol> explicitInterfaceImplementations;
        private readonly ImmutableArray<TypeParameterSymbol> typeParameters;
        private readonly TypeSymbol returnType;
        private readonly ImmutableArray<ParameterSymbol> parameters;
        private readonly string name;

        public SynthesizedImplementationMethod(
            MethodSymbol interfaceMethod,
            NamedTypeSymbol implementingType,
            string name = null,
            bool debuggerHidden = false,
            PropertySymbol associatedProperty = null,
            MethodSymbol asyncKickoffMethod = null)
        {
            //it does not make sense to add methods to substituted types
            Debug.Assert(implementingType.IsDefinition);

            this.name = name ?? ExplicitInterfaceHelpers.GetMemberName(interfaceMethod.Name, interfaceMethod.ContainingType, aliasQualifierOpt: null);
            this.interfaceMethod = interfaceMethod;
            this.implementingType = implementingType;
            this.debuggerHidden = debuggerHidden;
            this.associatedProperty = associatedProperty;
            this.explicitInterfaceImplementations = ImmutableArray.Create<MethodSymbol>(interfaceMethod);
            this.asyncKickoffMethod = asyncKickoffMethod;

            // alpha-rename to get the implementation's type parameters
            var typeMap = interfaceMethod.ContainingType.TypeSubstitution ?? TypeMap.Empty;
            typeMap.WithAlphaRename(interfaceMethod, this, out this.typeParameters);

            var substitutedInterfaceMethod = interfaceMethod.ConstructIfGeneric(this.typeParameters.Cast<TypeParameterSymbol, TypeSymbol>());
            this.returnType = substitutedInterfaceMethod.ReturnType;
            this.parameters = SynthesizedParameterSymbol.DeriveParameters(substitutedInterfaceMethod, this);
        }

        #region Delegate to interfaceMethod

        public sealed override bool IsVararg
        {
            get { return interfaceMethod.IsVararg; }
        }

        public sealed override int Arity
        {
            get { return interfaceMethod.Arity; }
        }

        public sealed override bool ReturnsVoid
        {
            get { return interfaceMethod.ReturnsVoid; }
        }

        internal sealed override Cci.CallingConvention CallingConvention
        {
            get { return interfaceMethod.CallingConvention; }
        }

        public sealed override ImmutableArray<CustomModifier> ReturnTypeCustomModifiers
        {
            get { return this.interfaceMethod.ReturnTypeCustomModifiers; }
        }

        #endregion

        internal sealed override void AddSynthesizedAttributes(ModuleCompilationState compilationState, ref ArrayBuilder<SynthesizedAttributeData> attributes)
        {
            base.AddSynthesizedAttributes(compilationState, ref attributes);

            if (debuggerHidden)
            {
                var compilation = this.DeclaringCompilation;
                AddSynthesizedAttribute(ref attributes, compilation.SynthesizeAttribute(WellKnownMember.System_Diagnostics_DebuggerHiddenAttribute__ctor));
            }

            if (this.ReturnType.ContainsDynamic())
            {
                var compilation = this.DeclaringCompilation;
                AddSynthesizedAttribute(ref attributes, compilation.SynthesizeDynamicAttribute(this.ReturnType, this.ReturnTypeCustomModifiers.Length));
            }
        }

        internal override bool GenerateDebugInfo
        {
            get { return !debuggerHidden; }
        }

        public sealed override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get { return this.typeParameters; }
        }

        public sealed override ImmutableArray<TypeSymbol> TypeArguments
        {
            get { return this.typeParameters.Cast<TypeParameterSymbol, TypeSymbol>(); }
        }

        public sealed override TypeSymbol ReturnType
        {
            get { return this.returnType; }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get { return this.parameters; }
        }

        public override Symbol ContainingSymbol
        {
            get { return this.implementingType; }
        }

        public override NamedTypeSymbol ContainingType
        {
            get
            {
                return this.implementingType;
            }
        }

        internal override bool IsExplicitInterfaceImplementation
        {
            get { return true; }
        }

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations
        {
            get { return this.explicitInterfaceImplementations; }
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
            get { return this.associatedProperty; }
        }

        public override bool HidesBaseMethodsByName
        {
            get { return false; }
        }

        internal override LexicalSortKey GetLexicalSortKey()
        {
            return LexicalSortKey.NotInSource;
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
            get { return this.name; }
        }

        internal sealed override bool HasSpecialName
        {
            get { return interfaceMethod.HasSpecialName; }
        }

        internal sealed override System.Reflection.MethodImplAttributes ImplementationAttributes
        {
            get { return default(System.Reflection.MethodImplAttributes); }
        }

        internal sealed override bool RequiresSecurityObject
        {
            get { return interfaceMethod.RequiresSecurityObject; }
        }

        internal sealed override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false)
        {
            return true;
        }

        internal override bool IsMetadataFinal()
        {
            return true;
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

        internal override MethodSymbol AsyncKickoffMethod
        {
            get { return this.asyncKickoffMethod; }
        }
    }
}