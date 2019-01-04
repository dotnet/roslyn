// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
    }

    /// <summary>
    /// A simple representation of a type parameter constraint clause
    /// as a set of constraint bits and a set of constraint types.
    /// </summary>
    internal sealed class TypeParameterConstraintClause
    {
        internal static readonly TypeParameterConstraintClause Empty = new TypeParameterConstraintClause(
            TypeParameterConstraintKind.None,
            ImmutableArray<TypeSymbolWithAnnotations>.Empty,
            typeConstraintsSyntax: default,
            otherPartialDeclarations: ImmutableArray<TypeParameterConstraintClause>.Empty);

        internal static TypeParameterConstraintClause Create(
            TypeParameterConstraintKind constraints,
            ImmutableArray<TypeSymbolWithAnnotations> constraintTypes,
            ImmutableArray<TypeConstraintSyntax> typeConstraintsSyntax = default)
        {
            Debug.Assert(!constraintTypes.IsDefault);
            if (constraints == TypeParameterConstraintKind.None && constraintTypes.IsEmpty)
            {
                Debug.Assert(typeConstraintsSyntax.IsDefault);
                return Empty;
            }
            return new TypeParameterConstraintClause(constraints, constraintTypes, typeConstraintsSyntax, otherPartialDeclarations: ImmutableArray<TypeParameterConstraintClause>.Empty);
        }

        private TypeParameterConstraintClause(
            TypeParameterConstraintKind constraints,
            ImmutableArray<TypeSymbolWithAnnotations> constraintTypes,
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
#endif 
            this.Constraints = constraints;
            this.ConstraintTypes = constraintTypes;
            this.TypeConstraintsSyntax = typeConstraintsSyntax;
            this.OtherPartialDeclarations = otherPartialDeclarations;
        }

        public readonly TypeParameterConstraintKind Constraints;
        public readonly ImmutableArray<TypeSymbolWithAnnotations> ConstraintTypes;

        /// <summary>
        /// Syntax for the constraint types. Populated from early constraint checking step only.
        /// </summary>
        internal readonly ImmutableArray<TypeConstraintSyntax> TypeConstraintsSyntax;

        /// <summary>
        /// Collection of constraint clauses for other partial declarations of the same container.
        /// Populated from early constraint checking step only.
        /// </summary>
        internal readonly ImmutableArray<TypeParameterConstraintClause> OtherPartialDeclarations;

        internal bool IsEmpty => Constraints == TypeParameterConstraintKind.None && ConstraintTypes.IsEmpty;

        internal bool IsEarly => !TypeConstraintsSyntax.IsDefault;

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
    }
}
