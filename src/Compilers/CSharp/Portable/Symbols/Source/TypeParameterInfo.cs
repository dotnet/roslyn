// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    /// <summary>
    /// Wrapper around type-parameter/constraints/constraint-kind info.  We wrap this information (instead of inlining
    /// directly within type/method symbols) as most types/methods are not generic.  As such, all those non-generic
    /// types can point at the singleton sentivel <see cref="Empty"/> value, and avoid two pointers of overhead.
    /// </summary>
    internal sealed class TypeParameterInfo
    {
        public ImmutableArray<TypeParameterSymbol> LazyTypeParameters;

        /// <summary>
        /// A collection of type parameter constraint types, populated when
        /// constraint types for the first type parameter are requested.
        /// </summary>
        public ImmutableArray<ImmutableArray<TypeWithAnnotations>> LazyTypeParameterConstraintTypes;

        /// <summary>
        /// A collection of type parameter constraint kinds, populated when
        /// constraint kinds for the first type parameter are requested.
        /// </summary>
        public ImmutableArray<TypeParameterConstraintKind> LazyTypeParameterConstraintKinds;

        public static readonly TypeParameterInfo Empty = new TypeParameterInfo(
            ImmutableArray<TypeParameterSymbol>.Empty, ImmutableArray<ImmutableArray<TypeWithAnnotations>>.Empty, ImmutableArray<TypeParameterConstraintKind>.Empty);

        public TypeParameterInfo() : this(default, default, default)
        {
        }

        private TypeParameterInfo(
            ImmutableArray<TypeParameterSymbol> typeParameters,
            ImmutableArray<ImmutableArray<TypeWithAnnotations>> typeParameterConstraintTypes,
            ImmutableArray<TypeParameterConstraintKind> typeParameterConstraintKinds)
        {
            LazyTypeParameters = typeParameters;
            LazyTypeParameterConstraintTypes = typeParameterConstraintTypes;
            LazyTypeParameterConstraintKinds = typeParameterConstraintKinds;
        }

        public static TypeParameterInfo Create(ImmutableArray<TypeParameterSymbol> typeParameters)
        {
            // If we have no type parameters (common case), we can just point at the singleton empty instance.
            return typeParameters.IsEmpty ? Empty : new TypeParameterInfo(typeParameters, default, default);
        }
    }
}
