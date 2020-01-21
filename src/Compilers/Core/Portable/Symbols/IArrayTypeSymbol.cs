// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents an array.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IArrayTypeSymbol : ITypeSymbol
    {
        /// <summary>
        /// Gets the number of dimensions of this array. A regular single-dimensional array
        /// has rank 1, a two-dimensional array has rank 2, etc.
        /// </summary>
        int Rank { get; }

        /// <summary>
        /// Is this a zero-based one-dimensional array, i.e. SZArray in CLR terms.
        /// SZArray is an array type encoded in metadata with ELEMENT_TYPE_SZARRAY (always single-dim array with 0 lower bound).
        /// Non-SZArray type is encoded in metadata with ELEMENT_TYPE_ARRAY and with optional sizes and lower bounds. Even though 
        /// non-SZArray can also be a single-dim array with 0 lower bound, the encoding of these types in metadata is distinct.
        /// </summary>
        bool IsSZArray { get; }

        /// <summary>
        /// Specified lower bounds for dimensions, by position. The length can be less than <see cref="Rank"/>,
        /// meaning that some trailing dimensions don't have the lower bound specified.
        /// The most common case is all dimensions are zero bound - a default (Nothing in VB) array is returned in this case.
        /// </summary>
        ImmutableArray<int> LowerBounds { get; }

        /// <summary>
        /// Specified sizes for dimensions, by position. The length can be less than <see cref="Rank"/>,
        /// meaning that some trailing dimensions don't have the size specified.
        /// The most common case is none of the dimensions have the size specified - an empty array is returned.
        /// </summary>
        ImmutableArray<int> Sizes { get; }

        /// <summary>
        /// Gets the type of the elements stored in the array.
        /// </summary>
        ITypeSymbol ElementType { get; }

        /// <summary>
        /// Gets the top-level nullability of the elements stored in the array. 
        /// </summary>
        NullableAnnotation ElementNullableAnnotation { get; }

        /// <summary>
        /// Custom modifiers associated with the array type, or an empty array if there are none.
        /// </summary>
        ImmutableArray<CustomModifier> CustomModifiers { get; }

        bool Equals(IArrayTypeSymbol? other);
    }
}
