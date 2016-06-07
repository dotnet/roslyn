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
    internal sealed class ExpandedExtensionClassPropertySymbol : PropertySymbol
    {
        private PropertySymbol _expandedFrom;
        private ImmutableArray<ParameterSymbol> _lazyParameters;

        public ExpandedExtensionClassPropertySymbol(PropertySymbol expandedFrom)
        {
            Debug.Assert((object)expandedFrom != null);
            Debug.Assert(expandedFrom.IsInExtensionClass);
            Debug.Assert(!(expandedFrom is ExpandedExtensionClassPropertySymbol));

            _expandedFrom = expandedFrom;
        }

        // PROTOTYPE: Make virtual?
        public PropertySymbol ExpandedFrom => _expandedFrom;

        public override Symbol ContainingSymbol => _expandedFrom.ContainingSymbol;

        public override Accessibility DeclaredAccessibility => _expandedFrom.DeclaredAccessibility;

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences => _expandedFrom.DeclaringSyntaxReferences;

        public override ImmutableArray<PropertySymbol> ExplicitInterfaceImplementations => ImmutableArray<PropertySymbol>.Empty;

        public override MethodSymbol GetMethod => _expandedFrom.GetMethod?.ExpandExtensionClassMethod();

        public override MethodSymbol SetMethod => _expandedFrom.SetMethod?.ExpandExtensionClassMethod();

        // PROTOTYPE: extra parameters causing problems? (find references of IsIndexer)
        public override bool IsIndexer => _expandedFrom.IsIndexer;

        public override bool IsAbstract => _expandedFrom.IsAbstract;

        public override bool IsExtern => _expandedFrom.IsExtern;

        public override bool IsOverride => _expandedFrom.IsOverride;

        public override bool IsSealed => _expandedFrom.IsSealed;

        public override bool IsStatic => true;

        public override bool IsVirtual => _expandedFrom.IsVirtual;

        public override ImmutableArray<Location> Locations => _expandedFrom.Locations;

        public override TypeSymbol Type => _expandedFrom.Type;

        public override ImmutableArray<CustomModifier> TypeCustomModifiers => _expandedFrom.TypeCustomModifiers;

        internal override Cci.CallingConvention CallingConvention => _expandedFrom.CallingConvention;

        internal override bool HasSpecialName => _expandedFrom.HasSpecialName;

        public override string Name => _expandedFrom.Name;

        internal override bool MustCallMethodsDirectly => _expandedFrom.MustCallMethodsDirectly;

        internal override ObsoleteAttributeData ObsoleteAttributeData => _expandedFrom.ObsoleteAttributeData;

        internal override RefKind RefKind => _expandedFrom.RefKind;

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
            var expandedFromParameters = _expandedFrom.Parameters;
            int count = expandedFromParameters.Length;

            var parameters = new ParameterSymbol[count + 1];
            parameters[0] = new ExpandedExtensionClassPropertyThisParameterSymbol(this);
            for (int i = 0; i < count; i++)
            {
                parameters[i + 1] = new ExpandedExtensionClassPropertyParameterSymbol(this, expandedFromParameters[i]);
            }

            return parameters.AsImmutableOrNull();
        }

        // PROTOTYPE: Merge these two classes with the ones in ExpandedExtensionClassMethodSymbol?

        private sealed class ExpandedExtensionClassPropertyThisParameterSymbol : SynthesizedParameterSymbol
        {
            public ExpandedExtensionClassPropertyThisParameterSymbol(ExpandedExtensionClassPropertySymbol containingProperty) :
                base(containingProperty, containingProperty.ContainingType.ExtensionClassType, 0, RefKind.None)
            {
            }

            // PROTOTYPE: Add overrides? (Otherwise we might want to construct SynthesizedParameterSymbol directly, since this class isn't adding much value)
        }

        private sealed class ExpandedExtensionClassPropertyParameterSymbol : WrappedParameterSymbol
        {
            private readonly ExpandedExtensionClassPropertySymbol _containingProperty;

            public ExpandedExtensionClassPropertyParameterSymbol(ExpandedExtensionClassPropertySymbol containingProperty, ParameterSymbol underlyingParameter) :
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

                var other = obj as ExpandedExtensionClassPropertyParameterSymbol;
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