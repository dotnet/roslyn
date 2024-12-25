// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using ReferenceEqualityComparer = Roslyn.Utilities.ReferenceEqualityComparer;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    [Flags]
    internal enum TypeParameterConstraintKind
    {
        None = 0x00,
        ReferenceType = 0x01,
        ValueType = 0x02,
        Constructor = 0x04,
        Unmanaged = 0x08,
        NullableReferenceType = ReferenceType | 0x10,
        NotNullableReferenceType = ReferenceType | 0x20,

        /// <summary>
        /// Type parameter has no type constraints, including `struct`, `class`, `unmanaged` and is declared in a context
        /// where nullable annotations are disabled.
        /// Cannot be combined with <see cref="ReferenceType"/>, <see cref="ValueType"/> or <see cref="Unmanaged"/>.
        /// Note, presence of this flag suppresses generation of Nullable attribute on the corresponding type parameter.
        /// This imitates the shape of metadata produced by pre-nullable compilers. Metadata import is adjusted accordingly
        /// to distinguish between the two situations.
        /// </summary>
        ObliviousNullabilityIfReferenceType = 0x40,

        NotNull = 0x80,
        Default = 0x100,

        /// <summary>
        /// <see cref="TypeParameterConstraintKind"/> mismatch is detected during merging process for partial type declarations.
        /// </summary>
        PartialMismatch = 0x200,
        ValueTypeFromConstraintTypes = 0x400, // Not set if any flag from AllValueTypeKinds is set
        ReferenceTypeFromConstraintTypes = 0x800,

        AllowByRefLike = 0x1000,

        /// <summary>
        /// All bits involved into describing various aspects of 'class' constraint.
        /// </summary>
        AllReferenceTypeKinds = NullableReferenceType | NotNullableReferenceType,

        /// <summary>
        /// Any of these bits is equivalent to presence of 'struct' constraint.
        /// </summary>
        AllValueTypeKinds = ValueType | Unmanaged,

        /// <summary>
        /// All bits except those that are involved into describing various nullability aspects.
        /// </summary>
        AllNonNullableKinds = ReferenceType | ValueType | Constructor | Unmanaged | AllowByRefLike,
    }

    /// <summary>
    /// A simple representation of a type parameter constraint clause
    /// as a set of constraint bits and a set of constraint types.
    /// </summary>
    internal sealed class TypeParameterConstraintClause
    {
        internal static readonly TypeParameterConstraintClause Empty = new TypeParameterConstraintClause(
            TypeParameterConstraintKind.None,
            ImmutableArray<TypeWithAnnotations>.Empty);

        internal static readonly TypeParameterConstraintClause ObliviousNullabilityIfReferenceType = new TypeParameterConstraintClause(
            TypeParameterConstraintKind.ObliviousNullabilityIfReferenceType,
            ImmutableArray<TypeWithAnnotations>.Empty);

        internal static TypeParameterConstraintClause Create(
            TypeParameterConstraintKind constraints,
            ImmutableArray<TypeWithAnnotations> constraintTypes)
        {
            Debug.Assert(!constraintTypes.IsDefault);
            if (constraintTypes.IsEmpty)
            {
                switch (constraints)
                {
                    case TypeParameterConstraintKind.None:
                        return Empty;

                    case TypeParameterConstraintKind.ObliviousNullabilityIfReferenceType:
                        return ObliviousNullabilityIfReferenceType;
                }
            }

            return new TypeParameterConstraintClause(constraints, constraintTypes);
        }

        private TypeParameterConstraintClause(
            TypeParameterConstraintKind constraints,
            ImmutableArray<TypeWithAnnotations> constraintTypes)
        {
#if DEBUG
            switch (constraints & TypeParameterConstraintKind.AllReferenceTypeKinds)
            {
                case TypeParameterConstraintKind.None:
                case TypeParameterConstraintKind.ReferenceType:
                case TypeParameterConstraintKind.NullableReferenceType:
                case TypeParameterConstraintKind.NotNullableReferenceType:
                    break;
                default:
                    ExceptionUtilities.UnexpectedValue(constraints); // This call asserts.
                    break;
            }

            Debug.Assert((constraints & TypeParameterConstraintKind.ObliviousNullabilityIfReferenceType) == 0 ||
                         (constraints & ~(TypeParameterConstraintKind.ObliviousNullabilityIfReferenceType | TypeParameterConstraintKind.Constructor |
                                          TypeParameterConstraintKind.Default | TypeParameterConstraintKind.PartialMismatch |
                                          TypeParameterConstraintKind.ValueTypeFromConstraintTypes | TypeParameterConstraintKind.ReferenceTypeFromConstraintTypes |
                                          TypeParameterConstraintKind.AllowByRefLike)) == 0);
#endif
            this.Constraints = constraints;
            this.ConstraintTypes = constraintTypes;
        }

        public readonly TypeParameterConstraintKind Constraints;
        public readonly ImmutableArray<TypeWithAnnotations> ConstraintTypes;

        internal static SmallDictionary<TypeParameterSymbol, bool> BuildIsValueTypeMap(
            ImmutableArray<TypeParameterSymbol> typeParameters,
            ImmutableArray<TypeParameterConstraintClause> constraintClauses)
        {
            Debug.Assert(constraintClauses.Length == typeParameters.Length);

            var isValueTypeMap = new SmallDictionary<TypeParameterSymbol, bool>(ReferenceEqualityComparer.Instance);

            foreach (TypeParameterSymbol typeParameter in typeParameters)
            {
                isValueType(typeParameter, constraintClauses, isValueTypeMap, ConsList<TypeParameterSymbol>.Empty);
            }

            return isValueTypeMap;

            static bool isValueType(TypeParameterSymbol thisTypeParameter, ImmutableArray<TypeParameterConstraintClause> constraintClauses, SmallDictionary<TypeParameterSymbol, bool> isValueTypeMap, ConsList<TypeParameterSymbol> inProgress)
            {
                if (inProgress.ContainsReference(thisTypeParameter))
                {
                    return false;
                }

                if (isValueTypeMap.TryGetValue(thisTypeParameter, out bool knownIsValueType))
                {
                    return knownIsValueType;
                }

                TypeParameterConstraintClause constraintClause = constraintClauses[thisTypeParameter.Ordinal];

                bool result = false;

                if ((constraintClause.Constraints & TypeParameterConstraintKind.AllValueTypeKinds) != 0)
                {
                    result = true;
                }
                else
                {
                    Symbol container = thisTypeParameter.ContainingSymbol;
                    inProgress = inProgress.Prepend(thisTypeParameter);

                    foreach (TypeWithAnnotations constraintType in constraintClause.ConstraintTypes)
                    {
                        TypeSymbol type = constraintType.IsResolved ? constraintType.Type : constraintType.DefaultType;

                        if (type is TypeParameterSymbol typeParameter && (object)typeParameter.ContainingSymbol == (object)container)
                        {
                            if (isValueType(typeParameter, constraintClauses, isValueTypeMap, inProgress))
                            {
                                result = true;
                                break;
                            }
                        }
                        else if (type.IsValueType)
                        {
                            result = true;
                            break;
                        }
                    }
                }

                isValueTypeMap.Add(thisTypeParameter, result);
                return result;
            }
        }

        internal static SmallDictionary<TypeParameterSymbol, bool> BuildIsReferenceTypeFromConstraintTypesMap(
            ImmutableArray<TypeParameterSymbol> typeParameters,
            ImmutableArray<TypeParameterConstraintClause> constraintClauses)
        {
            Debug.Assert(constraintClauses.Length == typeParameters.Length);

            var isReferenceTypeFromConstraintTypesMap = new SmallDictionary<TypeParameterSymbol, bool>(ReferenceEqualityComparer.Instance);

            foreach (TypeParameterSymbol typeParameter in typeParameters)
            {
                isReferenceTypeFromConstraintTypes(typeParameter, constraintClauses, isReferenceTypeFromConstraintTypesMap, ConsList<TypeParameterSymbol>.Empty);
            }

            return isReferenceTypeFromConstraintTypesMap;

            static bool isReferenceTypeFromConstraintTypes(TypeParameterSymbol thisTypeParameter, ImmutableArray<TypeParameterConstraintClause> constraintClauses,
                                                           SmallDictionary<TypeParameterSymbol, bool> isReferenceTypeFromConstraintTypesMap, ConsList<TypeParameterSymbol> inProgress)
            {
                if (inProgress.ContainsReference(thisTypeParameter))
                {
                    return false;
                }

                if (isReferenceTypeFromConstraintTypesMap.TryGetValue(thisTypeParameter, out bool knownIsReferenceTypeFromConstraintTypes))
                {
                    return knownIsReferenceTypeFromConstraintTypes;
                }

                TypeParameterConstraintClause constraintClause = constraintClauses[thisTypeParameter.Ordinal];

                bool result = false;

                Symbol container = thisTypeParameter.ContainingSymbol;
                inProgress = inProgress.Prepend(thisTypeParameter);

                foreach (TypeWithAnnotations constraintType in constraintClause.ConstraintTypes)
                {
                    TypeSymbol type = constraintType.IsResolved ? constraintType.Type : constraintType.DefaultType;

                    if (type is TypeParameterSymbol typeParameter)
                    {
                        if ((object)typeParameter.ContainingSymbol == (object)container)
                        {
                            if (isReferenceTypeFromConstraintTypes(typeParameter, constraintClauses, isReferenceTypeFromConstraintTypesMap, inProgress))
                            {
                                result = true;
                                break;
                            }
                        }
                        else if (typeParameter.IsReferenceTypeFromConstraintTypes)
                        {
                            result = true;
                            break;
                        }
                    }
                    else if (TypeParameterSymbol.NonTypeParameterConstraintImpliesReferenceType(type))
                    {
                        result = true;
                        break;
                    }
                }

                isReferenceTypeFromConstraintTypesMap.Add(thisTypeParameter, result);
                return result;
            }
        }
    }
}
