// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

//#define DEBUG_ALPHA // turn on DEBUG_ALPHA to help diagnose issues around type parameter alpha-renaming

using System;
using System.Collections.Immutable;
using System.Diagnostics;
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

        internal override ImmutableArray<TypeWithAnnotations> GetConstraintTypes(ConsList<TypeParameterSymbol> inProgress)
        {
            var constraintTypes = ArrayBuilder<TypeWithAnnotations>.GetInstance();
            _map.SubstituteConstraintTypesDistinctWithoutModifiers(_underlyingTypeParameter, _underlyingTypeParameter.GetConstraintTypes(inProgress), constraintTypes, null);

            TypeWithAnnotations bestObjectConstraint = default;

            // Strip all Object constraints.
            for (int i = constraintTypes.Count - 1; i >= 0; i--)
            {
                TypeWithAnnotations type = constraintTypes[i];
                if (ConstraintsHelper.IsObjectConstraint(type, ref bestObjectConstraint))
                {
                    constraintTypes.RemoveAt(i);
                }
            }

            if (bestObjectConstraint.HasType)
            {
                // See if we need to put Object! or Object~ back in order to preserve nullability information for the type parameter.
                if (ConstraintsHelper.IsObjectConstraintSignificant(CalculateIsNotNullableFromNonTypeConstraints(), bestObjectConstraint))
                {
                    Debug.Assert(!HasNotNullConstraint && !HasValueTypeConstraint);
                    if (constraintTypes.Count == 0)
                    {
                        if (bestObjectConstraint.NullableAnnotation.IsOblivious() && !HasReferenceTypeConstraint)
                        {
                            bestObjectConstraint = default;
                        }
                    }
                    else
                    {
                        foreach (TypeWithAnnotations constraintType in constraintTypes)
                        {
                            if (!ConstraintsHelper.IsObjectConstraintSignificant(IsNotNullableFromConstraintType(constraintType, out _), bestObjectConstraint))
                            {
                                bestObjectConstraint = default;
                                break;
                            }
                        }
                    }

                    if (bestObjectConstraint.HasType)
                    {
                        constraintTypes.Insert(0, bestObjectConstraint);
                    }
                }
            }

            return constraintTypes.ToImmutableAndFree();
        }

        internal override bool? IsNotNullable
        {
            get
            {
                if (_underlyingTypeParameter.ConstraintTypesNoUseSiteDiagnostics.IsEmpty)
                {
                    return _underlyingTypeParameter.IsNotNullable;
                }
                else if (!HasNotNullConstraint && !HasValueTypeConstraint && !HasReferenceTypeConstraint)
                {
                    var constraintTypes = ArrayBuilder<TypeWithAnnotations>.GetInstance();
                    _map.SubstituteConstraintTypesDistinctWithoutModifiers(_underlyingTypeParameter, _underlyingTypeParameter.GetConstraintTypes(ConsList<TypeParameterSymbol>.Empty), constraintTypes, null);
                    return IsNotNullableFromConstraintTypes(constraintTypes.ToImmutableAndFree());
                }

                return CalculateIsNotNullable();
            }
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

        internal override CSharpCompilation DeclaringCompilation => ContainingSymbol.DeclaringCompilation;
    }
}
