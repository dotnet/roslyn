﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
    }

    /// <summary>
    /// A simple representation of a type parameter constraint clause
    /// as a set of constraint bits and a set of constraint types.
    /// </summary>
    internal sealed class TypeParameterConstraintClause
    {
        internal static readonly TypeParameterConstraintClause Empty = new TypeParameterConstraintClause(
            TypeParameterConstraintKind.None,
            ImmutableArray<TypeWithAnnotations>.Empty,
            typeConstraintsSyntax: default,
            otherPartialDeclarations: ImmutableArray<TypeParameterConstraintClause>.Empty);

        internal static readonly TypeParameterConstraintClause ObliviousNullabilityIfReferenceType = new TypeParameterConstraintClause(
            TypeParameterConstraintKind.ObliviousNullabilityIfReferenceType,
            ImmutableArray<TypeWithAnnotations>.Empty,
            typeConstraintsSyntax: default,
            otherPartialDeclarations: ImmutableArray<TypeParameterConstraintClause>.Empty);

        internal static TypeParameterConstraintClause Create(
            TypeParameterConstraintKind constraints,
            ImmutableArray<TypeWithAnnotations> constraintTypes,
            ImmutableArray<TypeConstraintSyntax> typeConstraintsSyntax = default)
        {
            Debug.Assert(!constraintTypes.IsDefault);
            if (constraintTypes.IsEmpty)
            {
                switch (constraints)
                {
                    case TypeParameterConstraintKind.None:
                        Debug.Assert(typeConstraintsSyntax.IsDefault);
                        return Empty;

                    case TypeParameterConstraintKind.ObliviousNullabilityIfReferenceType:
                        Debug.Assert(typeConstraintsSyntax.IsDefault);
                        return ObliviousNullabilityIfReferenceType;
                }
            }
            return new TypeParameterConstraintClause(constraints, constraintTypes, typeConstraintsSyntax, otherPartialDeclarations: ImmutableArray<TypeParameterConstraintClause>.Empty);
        }

        private TypeParameterConstraintClause(
            TypeParameterConstraintKind constraints,
            ImmutableArray<TypeWithAnnotations> constraintTypes,
            ImmutableArray<TypeConstraintSyntax> typeConstraintsSyntax,
            ImmutableArray<TypeParameterConstraintClause> otherPartialDeclarations)
        {
#if DEBUG
            switch (constraints & (TypeParameterConstraintKind.NullableReferenceType | TypeParameterConstraintKind.NotNullableReferenceType))
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
            this.TypeConstraintsSyntax = typeConstraintsSyntax;
            this.OtherPartialDeclarations = otherPartialDeclarations;
        }

        public readonly TypeParameterConstraintKind Constraints;
        public readonly ImmutableArray<TypeWithAnnotations> ConstraintTypes;

        /// <summary>
        /// Syntax for the constraint types. Populated from early constraint checking step only.
        /// </summary>
        internal readonly ImmutableArray<TypeConstraintSyntax> TypeConstraintsSyntax;

        /// <summary>
        /// Collection of constraint clauses for other partial declarations of the same container.
        /// Populated from early constraint checking step only.
        /// </summary>
        internal readonly ImmutableArray<TypeParameterConstraintClause> OtherPartialDeclarations;

        internal bool IsEmpty => Constraints == TypeParameterConstraintKind.None && ConstraintTypes.IsEmpty && OtherPartialDeclarations.ContainsOnlyEmptyConstraintClauses();

        internal bool IsEarly => !TypeConstraintsSyntax.IsDefault || !OtherPartialDeclarations.IsEmpty;

        internal TypeParameterConstraintClause AddPartialDeclaration(TypeParameterConstraintClause other)
        {
            return new TypeParameterConstraintClause(Constraints, ConstraintTypes, TypeConstraintsSyntax, OtherPartialDeclarations.Add(other));
        }
    }

    internal static class TypeParameterConstraintClauseExtensions
    {
        internal static bool IsEarly(this ImmutableArray<TypeParameterConstraintClause> constraintClauses)
        {
            return constraintClauses.Any(clause => clause.IsEarly);
        }

        internal static bool ContainsOnlyEmptyConstraintClauses(this ImmutableArray<TypeParameterConstraintClause> constraintClauses)
        {
            return constraintClauses.All(clause => clause.IsEmpty);
        }
    }
}
