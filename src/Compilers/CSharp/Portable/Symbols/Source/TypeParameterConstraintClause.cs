// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    [Flags]
    internal enum TypeParameterConstraintKind
    {
        None = 0x00,
        ReferenceType = 0x01,
        ValueType = 0x02,
        Constructor = 0x04,
    }

    /// <summary>
    /// A simple representation of a type parameter constraint clause
    /// as a set of constraint bits and a set of constraint types.
    /// </summary>
    internal sealed class TypeParameterConstraintClause
    {
        public TypeParameterConstraintClause(TypeParameterConstraintKind constraints, ImmutableArray<TypeSymbolWithAnnotations> constraintTypes)
        {
            Debug.Assert(!constraintTypes.IsDefault);
            this.Constraints = constraints;
            this.ConstraintTypes = constraintTypes;
        }

        public readonly TypeParameterConstraintKind Constraints;
        public readonly ImmutableArray<TypeSymbolWithAnnotations> ConstraintTypes;
    }
}
