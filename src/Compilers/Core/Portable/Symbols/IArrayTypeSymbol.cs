// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        /// Gets the type of the elements stored in the array.
        /// </summary>
        ITypeSymbol ElementType { get; }

        /// <summary>
        /// Custom modifiers associated with the array type, or an empty array if there are none.
        /// </summary>
        ImmutableArray<CustomModifier> CustomModifiers { get; }

        bool Equals(IArrayTypeSymbol other);
    }
}
