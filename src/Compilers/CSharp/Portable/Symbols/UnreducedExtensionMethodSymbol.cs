// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// An instance method originally from an extension class,
    /// but synthesized so that it's static and with an extra
    /// first parameter of the extended class's type.
    /// </summary>
    internal sealed class UnreducedExtensionMethodSymbol : MethodSymbol
    {
        private readonly MethodSymbol _unreducedFrom;
        private readonly TypeMap _typeMap;
        private readonly ImmutableArray<TypeParameterSymbol> _typeParameters;
        private readonly ImmutableArray<TypeSymbol> _typeArguments;
        private ImmutableArray<ParameterSymbol> _lazyParameters;

        public UnreducedExtensionMethodSymbol(MethodSymbol unreducedFrom)
        {
            Debug.Assert((object)unreducedFrom != null);
            Debug.Assert(unreducedFrom.IsInExtensionClass);
            // Should never try to unreduce a reduced symbol - callers of this should have short-circuited.
            Debug.Assert((object)unreducedFrom.ReducedFrom == null);
            Debug.Assert((object)unreducedFrom.UnreducedFrom == null);
            Debug.Assert(unreducedFrom.ConstructedFrom == unreducedFrom);
            Debug.Assert(unreducedFrom.MethodKind != MethodKind.UnreducedExtension);

            _unreducedFrom = unreducedFrom;
            _typeMap = TypeMap.Empty.WithAlphaRename(unreducedFrom, this, out _typeParameters);
            _typeArguments = _typeMap.SubstituteTypesWithoutModifiers(unreducedFrom.TypeArguments);
        }

        internal override bool TryGetThisParameter(out ParameterSymbol thisParameter)
        {
            // this is a static method, so there's no this parameter
            thisParameter = null;
            return true;
        }

        public override TypeSymbol ReceiverType => null;

        internal override TypeSymbol GetTypeInferredDuringUnreduction(TypeParameterSymbol unreducedFromTypeParameter)
        {
            if ((object)unreducedFromTypeParameter == null)
            {
                throw new System.ArgumentNullException();
            }

            if (unreducedFromTypeParameter.ContainingSymbol != _unreducedFrom)
            {
                throw new System.ArgumentException();
            }

            return null;
        }

        public override MethodSymbol UnreducedFrom => _unreducedFrom;

        public override MethodSymbol ConstructedFrom
        {
            get
            {
                Debug.Assert(_unreducedFrom.ConstructedFrom == _unreducedFrom);
                return this;
            }
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => _typeParameters;

        public override ImmutableArray<TypeSymbol> TypeArguments => _typeArguments;

        internal override Cci.CallingConvention CallingConvention
        {
            get
            {
                var originalCallingConvention = _unreducedFrom.CallingConvention;
                Debug.Assert((originalCallingConvention & Cci.CallingConvention.HasThis) != 0);
                return originalCallingConvention & ~Cci.CallingConvention.HasThis;
            }
        }

        public override int Arity => _unreducedFrom.Arity;

        public override string Name => _unreducedFrom.Name;

        internal override bool HasSpecialName => _unreducedFrom.HasSpecialName;

        internal override System.Reflection.MethodImplAttributes ImplementationAttributes => _unreducedFrom.ImplementationAttributes;

        internal override bool RequiresSecurityObject => _unreducedFrom.RequiresSecurityObject;

        public override DllImportData GetDllImportData() => _unreducedFrom.GetDllImportData();

        internal override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation => _unreducedFrom.ReturnValueMarshallingInformation;

        internal override bool HasDeclarativeSecurity => _unreducedFrom.HasDeclarativeSecurity;

        internal override IEnumerable<Microsoft.Cci.SecurityAttribute> GetSecurityInformation() => _unreducedFrom.GetSecurityInformation();

        internal override ImmutableArray<string> GetAppliedConditionalSymbols() => _unreducedFrom.GetAppliedConditionalSymbols();

        public override AssemblySymbol ContainingAssembly => _unreducedFrom.ContainingAssembly;

        public override ImmutableArray<Location> Locations => _unreducedFrom.Locations;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => _unreducedFrom.DeclaringSyntaxReferences;

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _unreducedFrom.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
        }

        public override MethodSymbol OriginalDefinition => this;

        public override bool IsExtern => _unreducedFrom.IsExtern;

        public override bool IsSealed => _unreducedFrom.IsSealed;

        public override bool IsVirtual => _unreducedFrom.IsVirtual;

        public override bool IsAbstract => _unreducedFrom.IsAbstract;

        public override bool IsOverride => _unreducedFrom.IsOverride;

        public override bool IsStatic => true;

        public override bool IsAsync => _unreducedFrom.IsAsync;

        public override bool IsExtensionMethod => true;

        internal sealed override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false) => false;

        internal sealed override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false) => false;

        internal override bool IsMetadataFinal => false;

        internal sealed override ObsoleteAttributeData ObsoleteAttributeData => _unreducedFrom.ObsoleteAttributeData;

        public override Accessibility DeclaredAccessibility => _unreducedFrom.DeclaredAccessibility;

        public override Symbol ContainingSymbol => _unreducedFrom.ContainingSymbol;

        public override ImmutableArray<CSharpAttributeData> GetAttributes() => _unreducedFrom.GetAttributes();

        public override Symbol AssociatedSymbol => _unreducedFrom.AssociatedSymbol;

        public override MethodKind MethodKind => MethodKind.UnreducedExtension;

        public override bool ReturnsVoid => _unreducedFrom.ReturnsVoid;

        public override bool IsGenericMethod => _unreducedFrom.IsGenericMethod;

        public override bool IsVararg => _unreducedFrom.IsVararg;

        internal override RefKind RefKind => _unreducedFrom.RefKind;

        public override TypeSymbol ReturnType => _typeMap.SubstituteType(_unreducedFrom.ReturnType).Type;

        public override ImmutableArray<CustomModifier> ReturnTypeCustomModifiers =>
            _typeMap.SubstituteCustomModifiers(_unreducedFrom.ReturnType, _unreducedFrom.ReturnTypeCustomModifiers);

        internal override int ParameterCount => _unreducedFrom.ParameterCount + 1;

        internal override bool GenerateDebugInfo => _unreducedFrom.GenerateDebugInfo;

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                if (_lazyParameters.IsDefault)
                {
                    ImmutableInterlocked.InterlockedCompareExchange(ref _lazyParameters, this.MakeParameters(), default(ImmutableArray<ParameterSymbol>));
                }
                return _lazyParameters;
            }
        }

        internal override bool IsExplicitInterfaceImplementation => false;

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => ImmutableArray<MethodSymbol>.Empty;

        public override bool HidesBaseMethodsByName => false;

        internal override bool CallsAreOmitted(SyntaxTree syntaxTree) => _unreducedFrom.CallsAreOmitted(syntaxTree);

        private ImmutableArray<ParameterSymbol> MakeParameters()
        {
            var unreducedFromParameters = _unreducedFrom.Parameters;
            int count = unreducedFromParameters.Length;

            var parameters = new ParameterSymbol[count + 1];
            parameters[0] = new UnreducedExtensionMethodThisParameterSymbol(this);
            for (int i = 0; i < count; i++)
            {
                parameters[i + 1] = new UnreducedExtensionMethodParameterSymbol(this, unreducedFromParameters[i]);
            }

            return parameters.AsImmutableOrNull();
        }

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            throw ExceptionUtilities.Unreachable;
        }

        public override bool Equals(object obj)
        {
            if ((object)this == obj) return true;

            UnreducedExtensionMethodSymbol other = obj as UnreducedExtensionMethodSymbol;
            return (object)other != null && _unreducedFrom.Equals(other._unreducedFrom);
        }

        public override int GetHashCode()
        {
            return _unreducedFrom.GetHashCode();
        }

        private sealed class UnreducedExtensionMethodThisParameterSymbol : SynthesizedParameterSymbol
        {
            public UnreducedExtensionMethodThisParameterSymbol(UnreducedExtensionMethodSymbol containingMethod) :
                base(containingMethod, containingMethod.ContainingType.ExtensionClassType, 0, RefKind.None)
            {
            }

            // PROTOTYPE: Add overrides? (Otherwise we might want to construct SynthesizedParameterSymbol directly, since this class isn't adding much value)
        }

        private sealed class UnreducedExtensionMethodParameterSymbol : WrappedParameterSymbol
        {
            private readonly UnreducedExtensionMethodSymbol _containingMethod;

            public UnreducedExtensionMethodParameterSymbol(UnreducedExtensionMethodSymbol containingMethod, ParameterSymbol underlyingParameter) :
                base(underlyingParameter)
            {
                _containingMethod = containingMethod;
            }

            public override Symbol ContainingSymbol => _containingMethod;

            public override int Ordinal => this._underlyingParameter.Ordinal + 1;

            public override TypeSymbol Type => _containingMethod._typeMap.SubstituteType(this._underlyingParameter.Type).Type;

            public override ImmutableArray<CustomModifier> CustomModifiers =>
                _containingMethod._typeMap.SubstituteCustomModifiers(this._underlyingParameter.Type, this._underlyingParameter.CustomModifiers);

            public sealed override bool Equals(object obj)
            {
                if ((object)this == obj)
                {
                    return true;
                }

                // Equality of ordinal and containing symbol is a correct
                // implementation for all ParameterSymbols, but we don't 
                // define it on the base type because most can simply use
                // ReferenceEquals.

                var other = obj as UnreducedExtensionMethodParameterSymbol;
                return (object)other != null &&
                    this.Ordinal == other.Ordinal &&
                    this.ContainingSymbol.Equals(other.ContainingSymbol);
            }

            public sealed override int GetHashCode()
            {
                return Hash.Combine(ContainingSymbol, _underlyingParameter.Ordinal);
            }
        }
    }
}
