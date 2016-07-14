// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
    /// An instance property originally from an extension class,
    /// but synthesized so that it's static and with an extra
    /// first parameter of the extended class's type.
    /// </summary>
    internal sealed class UnreducedExtensionPropertySymbol : PropertySymbol
    {
        private PropertySymbol _unreducedFrom;
        private ImmutableArray<ParameterSymbol> _lazyParameters;

        public UnreducedExtensionPropertySymbol(PropertySymbol unreducedFrom)
        {
            Debug.Assert((object)unreducedFrom != null);
            Debug.Assert(unreducedFrom.IsInExtensionClass);
            Debug.Assert(!unreducedFrom.IsUnreducedExtensionMember);

            _unreducedFrom = unreducedFrom;
        }

        // Only use of this is when we know we have an unreduced property and want to "reduce" it
        // - no need for it to be on PropertySymbol and be virtual
        public PropertySymbol UnreducedFrom => _unreducedFrom;

        public override Symbol ContainingSymbol => _unreducedFrom.ContainingSymbol;

        public override Accessibility DeclaredAccessibility => _unreducedFrom.DeclaredAccessibility;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => _unreducedFrom.DeclaringSyntaxReferences;

        public override ImmutableArray<PropertySymbol> ExplicitInterfaceImplementations => ImmutableArray<PropertySymbol>.Empty;

        public override MethodSymbol GetMethod => _unreducedFrom.GetMethod?.UnreduceExtensionMethod();

        public override MethodSymbol SetMethod => _unreducedFrom.SetMethod?.UnreduceExtensionMethod();

        public override bool IsIndexer => _unreducedFrom.IsIndexer;

        public override bool IsAbstract => _unreducedFrom.IsAbstract;

        public override bool IsExtern => _unreducedFrom.IsExtern;

        public override bool IsOverride => _unreducedFrom.IsOverride;

        public override bool IsSealed => _unreducedFrom.IsSealed;

        public override bool IsStatic => true;

        public override bool IsVirtual => _unreducedFrom.IsVirtual;

        public override ImmutableArray<Location> Locations => _unreducedFrom.Locations;

        public override TypeSymbol Type => _unreducedFrom.Type;

        public override ImmutableArray<CustomModifier> TypeCustomModifiers => _unreducedFrom.TypeCustomModifiers;

        internal override Cci.CallingConvention CallingConvention
        {
            get
            {
                var originalCallingConvention = _unreducedFrom.CallingConvention;
                Debug.Assert((originalCallingConvention & Cci.CallingConvention.HasThis) != 0);
                return originalCallingConvention & ~Cci.CallingConvention.HasThis;
            }
        }

        internal override bool HasSpecialName => _unreducedFrom.HasSpecialName;

        public override string Name => _unreducedFrom.Name;

        internal override bool MustCallMethodsDirectly => _unreducedFrom.MustCallMethodsDirectly;

        internal override ObsoleteAttributeData ObsoleteAttributeData => _unreducedFrom.ObsoleteAttributeData;

        internal override RefKind RefKind => _unreducedFrom.RefKind;

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

        private ImmutableArray<ParameterSymbol> MakeParameters()
        {
            var unreducedFromParameters = _unreducedFrom.Parameters;
            int count = unreducedFromParameters.Length;

            var parameters = new ParameterSymbol[count + 1];
            parameters[0] = new UnreducedExtensionPropertyThisParameterSymbol(this);
            for (int i = 0; i < count; i++)
            {
                parameters[i + 1] = new UnreducedExtensionPropertyParameterSymbol(this, unreducedFromParameters[i]);
            }

            return parameters.AsImmutableOrNull();
        }

        // PROTOTYPE: Merge these two classes with the ones in UnreducedExtensionMethodSymbol?

        private sealed class UnreducedExtensionPropertyThisParameterSymbol : SynthesizedParameterSymbol
        {
            public UnreducedExtensionPropertyThisParameterSymbol(UnreducedExtensionPropertySymbol containingProperty) :
                base(containingProperty, containingProperty.ContainingType.ExtensionClassType, 0, RefKind.None)
            {
            }

            // PROTOTYPE: Add overrides? (Otherwise we might want to construct SynthesizedParameterSymbol directly, since this class isn't adding much value)
        }

        private sealed class UnreducedExtensionPropertyParameterSymbol : WrappedParameterSymbol
        {
            private readonly UnreducedExtensionPropertySymbol _containingProperty;

            public UnreducedExtensionPropertyParameterSymbol(UnreducedExtensionPropertySymbol containingProperty, ParameterSymbol underlyingParameter) :
                base(underlyingParameter)
            {
                _containingProperty = containingProperty;
            }

            public override Symbol ContainingSymbol => _containingProperty;

            public override int Ordinal => _underlyingParameter.Ordinal - 1;

            public override TypeSymbol Type => _underlyingParameter.Type;

            public override ImmutableArray<CustomModifier> CustomModifiers => _underlyingParameter.CustomModifiers;

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

                var other = obj as UnreducedExtensionPropertyParameterSymbol;
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