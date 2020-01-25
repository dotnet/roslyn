﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class SubstitutedParameterSymbol : WrappedParameterSymbol
    {
        // initially set to map which is only used to get the type, which is once computed is stored here.
        private object _mapOrType;

        private readonly Symbol _containingSymbol;

        internal SubstitutedParameterSymbol(MethodSymbol containingSymbol, TypeMap map, ParameterSymbol originalParameter) :
            this((Symbol)containingSymbol, map, originalParameter)
        {
        }

        internal SubstitutedParameterSymbol(PropertySymbol containingSymbol, TypeMap map, ParameterSymbol originalParameter) :
            this((Symbol)containingSymbol, map, originalParameter)
        {
        }

        private SubstitutedParameterSymbol(Symbol containingSymbol, TypeMap map, ParameterSymbol originalParameter) :
            base(originalParameter)
        {
            Debug.Assert(originalParameter.IsDefinition);
            _containingSymbol = containingSymbol;
            _mapOrType = map;
        }

        public override ParameterSymbol OriginalDefinition
        {
            get { return _underlyingParameter.OriginalDefinition; }
        }

        public override Symbol ContainingSymbol
        {
            get { return _containingSymbol; }
        }

        public override TypeWithAnnotations TypeWithAnnotations
        {
            get
            {
                var mapOrType = _mapOrType;
                if (mapOrType is TypeWithAnnotations type)
                {
                    return type;
                }

                TypeWithAnnotations substituted = ((TypeMap)mapOrType).SubstituteType(this._underlyingParameter.TypeWithAnnotations);

                if (substituted.CustomModifiers.IsEmpty &&
                    this._underlyingParameter.TypeWithAnnotations.CustomModifiers.IsEmpty &&
                    this._underlyingParameter.RefCustomModifiers.IsEmpty)
                {
                    _mapOrType = substituted;
                }

                return substituted;
            }
        }


        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get
            {
                var map = _mapOrType as TypeMap;
                return map != null ? map.SubstituteCustomModifiers(this._underlyingParameter.RefCustomModifiers) : this._underlyingParameter.RefCustomModifiers;
            }
        }

        public sealed override bool Equals(Symbol obj, TypeCompareKind compareKind)
        {
            if ((object)this == obj)
            {
                return true;
            }

            // Equality of ordinal and containing symbol is a correct
            // implementation for all ParameterSymbols, but we don't 
            // define it on the base type because most can simply use
            // ReferenceEquals.

            var other = obj as SubstitutedParameterSymbol;
            return (object)other != null &&
                this.Ordinal == other.Ordinal &&
                this.ContainingSymbol.Equals(other.ContainingSymbol, compareKind);
        }

        public sealed override int GetHashCode()
        {
            return Roslyn.Utilities.Hash.Combine(ContainingSymbol, _underlyingParameter.Ordinal);
        }
    }
}
