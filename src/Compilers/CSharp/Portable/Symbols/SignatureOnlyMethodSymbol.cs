// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        private readonly string _name;
        private readonly TypeSymbol _containingType;
        private readonly MethodKind _methodKind;
        private readonly Cci.CallingConvention _callingConvention;
        private readonly ImmutableArray<TypeParameterSymbol> _typeParameters;
        private readonly ImmutableArray<ParameterSymbol> _parameters;
        private readonly RefKind _refKind;
        private readonly TypeWithAnnotations _returnType;
        private readonly ImmutableArray<CustomModifier> _refCustomModifiers;
        private readonly ImmutableArray<MethodSymbol> _explicitInterfaceImplementations;

        public SignatureOnlyMethodSymbol(
            string name,
            TypeSymbol containingType,
            MethodKind methodKind,
            Cci.CallingConvention callingConvention,
            ImmutableArray<TypeParameterSymbol> typeParameters,
            ImmutableArray<ParameterSymbol> parameters,
            RefKind refKind,
            TypeWithAnnotations returnType,
            ImmutableArray<CustomModifier> refCustomModifiers,
            ImmutableArray<MethodSymbol> explicitInterfaceImplementations)
        {
            _callingConvention = callingConvention;
            _typeParameters = typeParameters;
            _refKind = refKind;
            _returnType = returnType;
            _refCustomModifiers = refCustomModifiers;
            _parameters = parameters;
            _explicitInterfaceImplementations = explicitInterfaceImplementations.NullToEmpty();
            _containingType = containingType;
            _methodKind = methodKind;
            _name = name;
        }

        internal override Cci.CallingConvention CallingConvention { get { return _callingConvention; } }

        public override bool IsVararg { get { return new SignatureHeader((byte)_callingConvention).CallingConvention == SignatureCallingConvention.VarArgs; } }

        public override bool IsGenericMethod { get { return Arity > 0; } }

        public override int Arity { get { return _typeParameters.Length; } }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters { get { return _typeParameters; } }

        public override bool ReturnsVoid { get { return _returnType.IsVoidType(); } }

        public override RefKind RefKind { get { return _refKind; } }

        public override TypeWithAnnotations ReturnTypeWithAnnotations { get { return _returnType; } }

        public override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull => ImmutableHashSet<string>.Empty;

        public override FlowAnalysisAnnotations FlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public override ImmutableArray<CustomModifier> RefCustomModifiers { get { return _refCustomModifiers; } }

        public override ImmutableArray<ParameterSymbol> Parameters { get { return _parameters; } }

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations { get { return _explicitInterfaceImplementations; } }

        public override Symbol ContainingSymbol { get { return _containingType; } }

        public override MethodKind MethodKind { get { return _methodKind; } }

        public override string Name { get { return _name; } }

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

        public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations { get { throw ExceptionUtilities.Unreachable; } }

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

        public override bool AreLocalsZeroed { get { throw ExceptionUtilities.Unreachable; } }

        public override AssemblySymbol ContainingAssembly { get { throw ExceptionUtilities.Unreachable; } }

        internal override ModuleSymbol ContainingModule { get { throw ExceptionUtilities.Unreachable; } }

        internal sealed override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false) { throw ExceptionUtilities.Unreachable; }

        internal sealed override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false) { throw ExceptionUtilities.Unreachable; }

        internal override bool IsMetadataFinal
        {
            get
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        internal override bool IsDeclaredReadOnly => false;

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree) { throw ExceptionUtilities.Unreachable; }

        #endregion
    }
}
