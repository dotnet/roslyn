// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

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

        public override TypeSymbol Type
        {
            get
            {
                var mapOrType = _mapOrType;
                var type = mapOrType as TypeSymbol;
                if (type != null)
                {
                    return type;
                }

                TypeWithModifiers substituted = ((TypeMap)mapOrType).SubstituteTypeWithTupleUnification(this._underlyingParameter.Type);

                type = substituted.Type;

                if (substituted.CustomModifiers.IsDefaultOrEmpty)
                {
                    _mapOrType = type;
                }

                return type;
            }
        }

        public override ImmutableArray<CustomModifier> CustomModifiers
        {
            get
            {
                var map = _mapOrType as TypeMap;
                return map != null ? map.SubstituteCustomModifiers(this._underlyingParameter.Type, this._underlyingParameter.CustomModifiers) : this._underlyingParameter.CustomModifiers;
            }
        }

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

            var other = obj as SubstitutedParameterSymbol;
            return (object)other != null &&
                this.Ordinal == other.Ordinal &&
                this.ContainingSymbol.Equals(other.ContainingSymbol);
        }

        public sealed override int GetHashCode()
        {
            return Roslyn.Utilities.Hash.Combine(ContainingSymbol, _underlyingParameter.Ordinal);
        }
    }
}
