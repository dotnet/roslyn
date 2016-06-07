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
    internal sealed class ExpandedExtensionClassMethodSymbol : MethodSymbol
    {
        private readonly MethodSymbol _expandedFrom;
        private readonly TypeMap _typeMap;
        private readonly ImmutableArray<TypeParameterSymbol> _typeParameters;
        private readonly ImmutableArray<TypeSymbol> _typeArguments;
        private ImmutableArray<ParameterSymbol> _lazyParameters;

        /// <summary>
        /// Return the extension method in expanded form if the extension method
        /// is applicable, and satisfies type parameter constraints, based on the
        /// "this" argument type. Otherwise, returns null.
        /// </summary>
        public static MethodSymbol Create(MethodSymbol method, TypeSymbol receiverType, Compilation compilation)
        {
            Debug.Assert(method.IsInExtensionClass && method.MethodKind != MethodKind.ExpandedExtensionClass);
            Debug.Assert((object)receiverType != null);

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;

            // PROTOTYPE: fix this, left over from ReducedExtensionMethodSymbol
            method = method.InferExtensionMethodTypeArguments(receiverType, compilation, ref useSiteDiagnostics);
            if ((object)method == null)
            {
                return null;
            }

            var conversions = new TypeConversions(method.ContainingAssembly.CorLibrary);
            var conversion = conversions.ConvertExtensionMethodThisArg(method.Parameters[0].Type, receiverType, ref useSiteDiagnostics);
            if (!conversion.Exists)
            {
                return null;
            }

            if (useSiteDiagnostics != null)
            {
                foreach (var diag in useSiteDiagnostics)
                {
                    if (diag.Severity == DiagnosticSeverity.Error)
                    {
                        return null;
                    }
                }
            }

            return Create(method);
        }

        public static MethodSymbol Create(MethodSymbol method)
        {
            Debug.Assert(method.IsInExtensionClass && method.MethodKind != MethodKind.ExpandedExtensionClass);

            // The expanded form is always created from the unconstructed method symbol.
            var constructedFrom = method.ConstructedFrom;
            var expandedMethod = new ExpandedExtensionClassMethodSymbol(constructedFrom);

            if (constructedFrom == method)
            {
                return expandedMethod;
            }

            // If the given method is a constructed method, the same type arguments
            // are applied to construct the result from the expanded form.
            Debug.Assert(!method.TypeArguments.IsEmpty);
            return expandedMethod.Construct(method.TypeArguments);
        }

        private ExpandedExtensionClassMethodSymbol(MethodSymbol expandedFrom)
        {
            Debug.Assert((object)expandedFrom != null);
            Debug.Assert(expandedFrom.IsInExtensionClass);
            Debug.Assert((object)expandedFrom.ReducedFrom == null);
            Debug.Assert(expandedFrom.ConstructedFrom == expandedFrom);

            _expandedFrom = expandedFrom;
            _typeMap = TypeMap.Empty.WithAlphaRename(expandedFrom, this, out _typeParameters);
            _typeArguments = _typeMap.SubstituteTypesWithoutModifiers(expandedFrom.TypeArguments);
        }

        internal override bool TryGetThisParameter(out ParameterSymbol thisParameter)
        {
            // this is a static method, so there's no this parameter
            thisParameter = null;
            return true;
        }

        // PROTOTYPE: Do we need to return something from here? (return null is same as base virtual property)
        internal override MethodSymbol CallsiteReducedFromMethod => null;

        public override TypeSymbol ReceiverType => null;

        // PROTOTYPE: Same comment as CallsiteReducedFromMethod
        public override TypeSymbol GetTypeInferredDuringReduction(TypeParameterSymbol reducedFromTypeParameter)
        {
            if ((object)reducedFromTypeParameter == null)
            {
                throw new System.ArgumentNullException();
            }

            if (reducedFromTypeParameter.ContainingSymbol != _expandedFrom)
            {
                throw new System.ArgumentException();
            }

            return null;
        }

        // PROTOTYPE: Same comment as CallsiteReducedFromMethod
        public override MethodSymbol ReducedFrom => null;

        public override MethodSymbol ExpandedFrom => _expandedFrom;

        public override MethodSymbol ConstructedFrom
        {
            get
            {
                Debug.Assert(_expandedFrom.ConstructedFrom == _expandedFrom);
                return this;
            }
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters => _typeParameters;

        public override ImmutableArray<TypeSymbol> TypeArguments => _typeArguments;

        internal override Microsoft.Cci.CallingConvention CallingConvention => _expandedFrom.CallingConvention;

        public override int Arity => _expandedFrom.Arity;

        public override string Name => _expandedFrom.Name;

        internal override bool HasSpecialName => _expandedFrom.HasSpecialName;

        internal override System.Reflection.MethodImplAttributes ImplementationAttributes => _expandedFrom.ImplementationAttributes;

        internal override bool RequiresSecurityObject => _expandedFrom.RequiresSecurityObject;

        public override DllImportData GetDllImportData() => _expandedFrom.GetDllImportData();

        internal override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation => _expandedFrom.ReturnValueMarshallingInformation;

        internal override bool HasDeclarativeSecurity => _expandedFrom.HasDeclarativeSecurity;

        internal override IEnumerable<Microsoft.Cci.SecurityAttribute> GetSecurityInformation() => _expandedFrom.GetSecurityInformation();

        internal override ImmutableArray<string> GetAppliedConditionalSymbols() => _expandedFrom.GetAppliedConditionalSymbols();

        public override AssemblySymbol ContainingAssembly => _expandedFrom.ContainingAssembly;

        public override ImmutableArray<Location> Locations => _expandedFrom.Locations;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => _expandedFrom.DeclaringSyntaxReferences;

        public override string GetDocumentationCommentXml(CultureInfo preferredCulture = null, bool expandIncludes = false, CancellationToken cancellationToken = default(CancellationToken))
        {
            return _expandedFrom.GetDocumentationCommentXml(preferredCulture, expandIncludes, cancellationToken);
        }

        public override MethodSymbol OriginalDefinition => this;

        public override bool IsExtern => _expandedFrom.IsExtern;

        public override bool IsSealed => _expandedFrom.IsSealed;

        public override bool IsVirtual => _expandedFrom.IsVirtual;

        public override bool IsAbstract => _expandedFrom.IsAbstract;

        public override bool IsOverride => _expandedFrom.IsOverride;

        public override bool IsStatic => true;

        public override bool IsAsync => _expandedFrom.IsAsync;

        // PROTOTYPE: This probably needs to change.
        public override bool IsExtensionMethod => true;

        internal sealed override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false) => false;

        internal sealed override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false) => false;

        internal override bool IsMetadataFinal => false;

        internal sealed override ObsoleteAttributeData ObsoleteAttributeData => _expandedFrom.ObsoleteAttributeData;

        public override Accessibility DeclaredAccessibility => _expandedFrom.DeclaredAccessibility;

        public override Symbol ContainingSymbol => _expandedFrom.ContainingSymbol;

        public override ImmutableArray<CSharpAttributeData> GetAttributes() => _expandedFrom.GetAttributes();

        public override Symbol AssociatedSymbol => _expandedFrom.AssociatedSymbol;

        public override MethodKind MethodKind => MethodKind.ExpandedExtensionClass;

        public override bool ReturnsVoid => _expandedFrom.ReturnsVoid;

        public override bool IsGenericMethod => _expandedFrom.IsGenericMethod;

        public override bool IsVararg => _expandedFrom.IsVararg;

        internal override RefKind RefKind => _expandedFrom.RefKind;

        public override TypeSymbol ReturnType => _typeMap.SubstituteType(_expandedFrom.ReturnType).Type;

        public override ImmutableArray<CustomModifier> ReturnTypeCustomModifiers =>
            _typeMap.SubstituteCustomModifiers(_expandedFrom.ReturnType, _expandedFrom.ReturnTypeCustomModifiers);

        internal override int ParameterCount => _expandedFrom.ParameterCount + 1;

        internal override bool GenerateDebugInfo => _expandedFrom.GenerateDebugInfo;

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

        // PROTOTYPE: Not here, but need to test explicit interface impls in extension class (should produce error)
        internal override bool IsExplicitInterfaceImplementation => false;

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => ImmutableArray<MethodSymbol>.Empty;

        public override bool HidesBaseMethodsByName => false;

        internal override bool CallsAreOmitted(SyntaxTree syntaxTree) => _expandedFrom.CallsAreOmitted(syntaxTree);

        private ImmutableArray<ParameterSymbol> MakeParameters()
        {
            var expandedFromParameters = _expandedFrom.Parameters;
            int count = expandedFromParameters.Length;

            var parameters = new ParameterSymbol[count + 1];
            parameters[0] = new ExpandedExtensionClassMethodThisParameterSymbol(this);
            for (int i = 0; i < count; i++)
            {
                parameters[i + 1] = new ExpandedExtensionClassMethodParameterSymbol(this, expandedFromParameters[i]);
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

            ExpandedExtensionClassMethodSymbol other = obj as ExpandedExtensionClassMethodSymbol;
            return (object)other != null && _expandedFrom.Equals(other._expandedFrom);
        }

        public override int GetHashCode()
        {
            return _expandedFrom.GetHashCode();
        }

        private sealed class ExpandedExtensionClassMethodThisParameterSymbol : SynthesizedParameterSymbol
        {
            public ExpandedExtensionClassMethodThisParameterSymbol(ExpandedExtensionClassMethodSymbol containingMethod) :
                base(containingMethod, containingMethod.ContainingType.ExtensionClassType, 0, RefKind.None)
            {
            }

            // PROTOTYPE: Add overrides? (Otherwise we might want to construct SynthesizedParameterSymbol directly, since this class isn't adding much value)
        }

        private sealed class ExpandedExtensionClassMethodParameterSymbol : WrappedParameterSymbol
        {
            private readonly ExpandedExtensionClassMethodSymbol _containingMethod;

            public ExpandedExtensionClassMethodParameterSymbol(ExpandedExtensionClassMethodSymbol containingMethod, ParameterSymbol underlyingParameter) :
                base(underlyingParameter)
            {
                _containingMethod = containingMethod;
            }

            public override Symbol ContainingSymbol => _containingMethod;

            public override int Ordinal => this._underlyingParameter.Ordinal - 1;

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

                var other = obj as ExpandedExtensionClassMethodParameterSymbol;
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
