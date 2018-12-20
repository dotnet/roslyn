﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

//#define DEBUG_ALPHA // turn on DEBUG_ALPHA to help diagnose issues around type parameter alpha-renaming

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal class SubstitutedTypeParameterSymbol : WrappedTypeParameterSymbol
    {
        private readonly Symbol _container;
        private readonly TypeMap _map;
        private readonly int _ordinal;

#if DEBUG_ALPHA
        private static int _nextSequence = 1;
        private readonly int _mySequence;
#endif

        internal SubstitutedTypeParameterSymbol(Symbol newContainer, TypeMap map, TypeParameterSymbol substitutedFrom, int ordinal)
            : base(substitutedFrom)
        {
            _container = newContainer;
            // it is important that we don't use the map here in the constructor, as the map is still being filled
            // in by TypeMap.WithAlphaRename.  Instead, we can use the map lazily when yielding the constraints.
            _map = map;
            _ordinal = ordinal;
#if DEBUG_ALPHA
            _mySequence = _nextSequence++;
#endif
        }

        public override Symbol ContainingSymbol
        {
            get
            {
                return _container;
            }
        }

        public override TypeParameterSymbol OriginalDefinition
        {
            get
            {
                // A substituted type parameter symbol is used as a type parameter of a frame type for lambda-captured
                // variables within a generic method.  In that case the frame's own type parameter is an original.
                return
                    ContainingSymbol.OriginalDefinition != _underlyingTypeParameter.ContainingSymbol.OriginalDefinition ? this :
                    _underlyingTypeParameter.OriginalDefinition;
            }
        }

        public override TypeParameterSymbol ReducedFrom
        {
            get
            {
                if (_container.Kind == SymbolKind.Method)
                {
                    MethodSymbol reducedFrom = ((MethodSymbol)_container).ReducedFrom;

                    if ((object)reducedFrom != null)
                    {
                        return reducedFrom.TypeParameters[this.Ordinal];
                    }
                }

                return null;
            }
        }

        public override int Ordinal
        {
            get
            {
                return _ordinal;
            }
        }

        public override string Name
        {
            get
            {
                return base.Name
#if DEBUG_ALPHA
                    + "#" + _mySequence
#endif
                    ;
            }
        }

        public override ImmutableArray<CSharpAttributeData> GetAttributes()
        {
            return _underlyingTypeParameter.GetAttributes();
        }

        internal override ImmutableArray<TypeSymbolWithAnnotations> GetConstraintTypes(ConsList<TypeParameterSymbol> inProgress, bool early)
        {
            var constraintTypes = ArrayBuilder<TypeSymbolWithAnnotations>.GetInstance();
            _map.SubstituteTypesDistinctWithoutModifiers(_underlyingTypeParameter.GetConstraintTypes(inProgress, early), constraintTypes, null);
            return constraintTypes.ToImmutableAndFree().WhereAsArray(type => type.SpecialType != SpecialType.System_Object || !type.NullableAnnotation.IsAnyNullable());
        }

        internal override ImmutableArray<NamedTypeSymbol> GetInterfaces(ConsList<TypeParameterSymbol> inProgress)
        {
            return _map.SubstituteNamedTypes(_underlyingTypeParameter.GetInterfaces(inProgress));
        }

        internal override NamedTypeSymbol GetEffectiveBaseClass(ConsList<TypeParameterSymbol> inProgress)
        {
            return _map.SubstituteNamedType(_underlyingTypeParameter.GetEffectiveBaseClass(inProgress));
        }

        internal override TypeSymbol GetDeducedBaseType(ConsList<TypeParameterSymbol> inProgress)
        {
            return _map.SubstituteType(_underlyingTypeParameter.GetDeducedBaseType(inProgress)).AsTypeSymbolOnly();
        }
    }
}
