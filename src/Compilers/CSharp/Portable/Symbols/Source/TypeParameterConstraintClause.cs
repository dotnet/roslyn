// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
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

        NotNull = 0x80,
        Default = 0x100,

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
                         (constraints & ~(TypeParameterConstraintKind.ObliviousNullabilityIfReferenceType | TypeParameterConstraintKind.Constructor | TypeParameterConstraintKind.Default)) == 0);
#endif 
            this.Constraints = constraints;
            this.ConstraintTypes = constraintTypes;
        }

        public readonly TypeParameterConstraintKind Constraints;
        public readonly ImmutableArray<TypeWithAnnotations> ConstraintTypes;

        internal bool IsEmpty => Constraints == TypeParameterConstraintKind.None && ConstraintTypes.IsEmpty;

        /// <summary>
        /// Adjust unresolved instances of <see cref="TypeWithAnnotations"/> which represent nullable type parameters
        /// of the <paramref name="container"/> within constraint types according to the IsValueType state inferred
        /// from the constraint types.
        /// </summary>
        internal static void AdjustConstraintTypes(Symbol container, ImmutableArray<TypeParameterSymbol> typeParameters,
                                                   ArrayBuilder<TypeParameterConstraintClause> constraintClauses,
                                                   ref IReadOnlyDictionary<TypeParameterSymbol, bool> isValueTypeOverride)
        {
            Debug.Assert(constraintClauses.Count == typeParameters.Length);

            if (isValueTypeOverride == null)
            {
                var isValueTypeOverrideBuilder = new Dictionary<TypeParameterSymbol, bool>(typeParameters.Length, ReferenceEqualityComparer.Instance);

                foreach (TypeParameterSymbol typeParameter in typeParameters)
                {
                    isValueType(typeParameter, constraintClauses, isValueTypeOverrideBuilder, ConsList<TypeParameterSymbol>.Empty);
                }

                isValueTypeOverride = new ReadOnlyDictionary<TypeParameterSymbol, bool>(isValueTypeOverrideBuilder);
            }

            foreach (TypeParameterConstraintClause constraintClause in constraintClauses)
            {
                adjustConstraintTypes(container, constraintClause.ConstraintTypes, isValueTypeOverride);
            }

            static bool isValueType(TypeParameterSymbol thisTypeParameter, ArrayBuilder<TypeParameterConstraintClause> constraintClauses, Dictionary<TypeParameterSymbol, bool> isValueTypeOverrideBuilder, ConsList<TypeParameterSymbol> inProgress)
            {
                if (inProgress.ContainsReference(thisTypeParameter))
                {
                    return false;
                }

                if (isValueTypeOverrideBuilder.TryGetValue(thisTypeParameter, out bool knownIsValueType))
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
                            if (isValueType(typeParameter, constraintClauses, isValueTypeOverrideBuilder, inProgress))
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

                isValueTypeOverrideBuilder.Add(thisTypeParameter, result);
                return result;
            }

            static void adjustConstraintTypes(Symbol container, ImmutableArray<TypeWithAnnotations> constraintTypes, IReadOnlyDictionary<TypeParameterSymbol, bool> isValueTypeOverride)
            {
                foreach (var constraintType in constraintTypes)
                {
                    constraintType.VisitType(null, (type, args, unused2) =>
                    {
                        if (type.DefaultType is TypeParameterSymbol typeParameterSymbol && typeParameterSymbol.ContainingSymbol == (object)args.container)
                        {
                            type.TryForceResolve(args.isValueTypeOverride[typeParameterSymbol]);
                        }
                        return false;
                    }, typePredicate: null, arg: (container, isValueTypeOverride), canDigThroughNullable: false, useDefaultType: true);
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
