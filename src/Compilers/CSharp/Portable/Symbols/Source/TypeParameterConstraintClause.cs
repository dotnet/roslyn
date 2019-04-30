// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

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

        /// <summary>
        /// All bits involved into describing various aspects of 'class' constraint. 
        /// </summary>
        AllReferenceTypeKinds = NullableReferenceType | NotNullableReferenceType,

        /// <summary>
        /// Any of these bits is equivalent to presence of 'struct' constraint. 
        /// </summary>
        AllValueTypeKinds = ValueType | Unmanaged,

        /// <summary>
        /// All bits except those that are involved into describilng various nullability aspects.
        /// </summary>
        AllNonNullableKinds = ReferenceType | ValueType | Constructor | Unmanaged,
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
                         (constraints & ~(TypeParameterConstraintKind.ObliviousNullabilityIfReferenceType | TypeParameterConstraintKind.Constructor)) == 0);
#endif 
            this.Constraints = constraints;
            this.ConstraintTypes = constraintTypes;
        }

        public readonly TypeParameterConstraintKind Constraints;
        public readonly ImmutableArray<TypeWithAnnotations> ConstraintTypes;

        internal bool IsEmpty => Constraints == TypeParameterConstraintKind.None && ConstraintTypes.IsEmpty;

        internal static void AdjustConstraintTypes(Symbol container, ImmutableArray<TypeParameterSymbol> typeParameters,
                                                   ArrayBuilder<TypeParameterConstraintClause> constraintClauses,
                                                   ref SmallDictionary<TypeParameterSymbol, bool> isValueTypeOverride)
        {
            Debug.Assert(constraintClauses.Count == typeParameters.Length);

            if (isValueTypeOverride == null)
            {
                isValueTypeOverride = new SmallDictionary<TypeParameterSymbol, bool>(ReferenceEqualityComparer.Instance);

                for (int i = 0; i < typeParameters.Length; i++)
                {
                    isValueType(typeParameters[i], constraintClauses, isValueTypeOverride, ConsList<TypeParameterSymbol>.Empty);
                }
            }

            for (int i = 0; i < typeParameters.Length; i++)
            {
                adjustConstraintTypes(container, constraintClauses[i].ConstraintTypes, isValueTypeOverride);
            }

            static bool isValueType(TypeParameterSymbol thisTypeParameter, ArrayBuilder<TypeParameterConstraintClause> constraintClauses, SmallDictionary<TypeParameterSymbol, bool> isValueTypeOverride, ConsList<TypeParameterSymbol> inProgress)
            {
                if (inProgress.ContainsReference(thisTypeParameter))
                {
                    return false;
                }

                if (isValueTypeOverride.TryGetValue(thisTypeParameter, out bool knownIsValueType))
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
                            if (isValueType(typeParameter, constraintClauses, isValueTypeOverride, inProgress))
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

                isValueTypeOverride.Add(thisTypeParameter, result);
                return result;
            }

            static void adjustConstraintTypes(Symbol container, ImmutableArray<TypeWithAnnotations> constraintTypes, SmallDictionary<TypeParameterSymbol, bool> isValueTypeOverride)
            {
                foreach (var constraintType in constraintTypes)
                {
                    constraintType.VisitType(null, (type, args, unused2) =>
                    {
                        if (type.DefaultType is TypeParameterSymbol typeParameterSymbol && typeParameterSymbol.ContainingSymbol == (object)args.container)
                        {
                            if (args.isValueTypeOverride[typeParameterSymbol])
                            {
                                type.TryForceResolveAsNullableValueType();
                            }
                            else
                            {
                                type.TryForceResolveAsNullableReferenceType();
                            }
                        }
                        return false;
                    }, typePredicateOpt: null, arg: (container, isValueTypeOverride), canDigThroughNullable: false, useDefaultType: true);
                }
            }
        }
    }

    internal static class TypeParameterConstraintClauseExtensions
    {
        internal static bool ContainsOnlyEmptyConstraintClauses(this ImmutableArray<TypeParameterConstraintClause> constraintClauses)
        {
            return constraintClauses.All(clause => clause.IsEmpty);
        }
    }
}
