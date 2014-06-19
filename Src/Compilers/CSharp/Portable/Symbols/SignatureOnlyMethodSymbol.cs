// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Roslyn.Utilities;
using System.Reflection.Metadata;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// A representation of a method symbol that is intended only to be used for comparison purposes
    /// (esp in MethodSignatureComparer).
    /// </summary>
    internal sealed class SignatureOnlyMethodSymbol : MethodSymbol
    {
        private readonly string name;
        private readonly TypeSymbol containingType;
        private readonly MethodKind methodKind;
        private readonly Cci.CallingConvention callingConvention;
        private readonly ImmutableArray<TypeParameterSymbol> typeParameters;
        private readonly ImmutableArray<ParameterSymbol> parameters;
        private readonly TypeSymbol returnType;
        private readonly ImmutableArray<CustomModifier> returnTypeCustomModifiers;
        private readonly ImmutableArray<MethodSymbol> explicitInterfaceImplementations;

        public SignatureOnlyMethodSymbol(
            string name,
            TypeSymbol containingType,
            MethodKind methodKind,
            Cci.CallingConvention callingConvention,
            ImmutableArray<TypeParameterSymbol> typeParameters,
            ImmutableArray<ParameterSymbol> parameters,
            TypeSymbol returnType,
            ImmutableArray<CustomModifier> returnTypeCustomModifiers,
            ImmutableArray<MethodSymbol> explicitInterfaceImplementations)
        {
            this.callingConvention = callingConvention;
            this.typeParameters = typeParameters;
            this.returnType = returnType;
            this.returnTypeCustomModifiers = returnTypeCustomModifiers;
            this.parameters = parameters;
            this.explicitInterfaceImplementations = explicitInterfaceImplementations.NullToEmpty();
            this.containingType = containingType;
            this.methodKind = methodKind;
            this.name = name;
        }

        internal override Cci.CallingConvention CallingConvention { get { return callingConvention; } }

        public override bool IsVararg { get { return SignatureHeader.IsVarArgCallSignature((byte)callingConvention); } }

        public override bool IsGenericMethod { get { return Arity > 0; } }

        public override int Arity { get { return typeParameters.Length; } }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters { get { return typeParameters; } }

        public override bool ReturnsVoid { get { return returnType.SpecialType == SpecialType.System_Void; } }

        public override TypeSymbol ReturnType { get { return returnType; } }

        public override ImmutableArray<CustomModifier> ReturnTypeCustomModifiers { get { return returnTypeCustomModifiers; } }

        public override ImmutableArray<ParameterSymbol> Parameters { get { return parameters; } }

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations { get { return explicitInterfaceImplementations; } }

        public override Symbol ContainingSymbol { get { return containingType; } }

        public override MethodKind MethodKind { get { return methodKind; } }

        public override string Name { get { return name; } }

        #region Not used by MethodSignatureComparer

        internal override bool GenerateDebugInfo { get { throw ExceptionUtilities.Unreachable; } }

        internal override bool HasSpecialName { get { throw ExceptionUtilities.Unreachable; } }

        internal override System.Reflection.MethodImplAttributes ImplementationAttributes { get { throw ExceptionUtilities.Unreachable; } }

        internal override bool RequiresSecurityObject { get { throw ExceptionUtilities.Unreachable; } }

        public override DllImportData GetDllImportData() { return null; }

        internal override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation { get { throw ExceptionUtilities.Unreachable; } }

        internal override bool HasDeclarativeSecurity { get { throw ExceptionUtilities.Unreachable; } }

        internal override IEnumerable<Microsoft.Cci.SecurityAttribute> GetSecurityInformation() { throw ExceptionUtilities.Unreachable; }

        internal override ObsoleteAttributeData ObsoleteAttributeData { get { throw ExceptionUtilities.Unreachable; } }

        internal override ImmutableArray<string> GetAppliedConditionalSymbols() { throw ExceptionUtilities.Unreachable; }

        public override ImmutableArray<TypeSymbol> TypeArguments { get { throw ExceptionUtilities.Unreachable; } }

        public override Symbol AssociatedSymbol { get { throw ExceptionUtilities.Unreachable; } }

        public override bool IsExtensionMethod { get { throw ExceptionUtilities.Unreachable; } }

        public override bool HidesBaseMethodsByName { get { throw ExceptionUtilities.Unreachable; } }

        public override ImmutableArray<Location> Locations { get { throw ExceptionUtilities.Unreachable; } }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences { get { throw ExceptionUtilities.Unreachable; } }

        public override Accessibility DeclaredAccessibility { get { throw ExceptionUtilities.Unreachable; } }

        public override bool IsStatic { get { throw ExceptionUtilities.Unreachable; } }

        public override bool IsAsync { get { throw ExceptionUtilities.Unreachable; } }

        public override bool IsVirtual { get { throw ExceptionUtilities.Unreachable; } }

        public override bool IsOverride { get { throw ExceptionUtilities.Unreachable; } }

        public override bool IsAbstract { get { throw ExceptionUtilities.Unreachable; } }

        public override bool IsSealed { get { throw ExceptionUtilities.Unreachable; } }

        public override bool IsExtern { get { throw ExceptionUtilities.Unreachable; } }

        public override AssemblySymbol ContainingAssembly { get { throw ExceptionUtilities.Unreachable; } }

        internal override ModuleSymbol ContainingModule { get { throw ExceptionUtilities.Unreachable; } }

        internal sealed override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false) { throw ExceptionUtilities.Unreachable; }

        internal sealed override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false) { throw ExceptionUtilities.Unreachable; }

        internal sealed override bool IsMetadataFinal() { throw ExceptionUtilities.Unreachable; }

        #endregion
    }
}
