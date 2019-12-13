// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a pointer type such as "int *". Pointer types
    /// are used only in unsafe code.
    /// </summary>
    /// <remarks>
    /// This interface is reserved for implementation by its associated APIs. We reserve the right to
    /// change it in the future.
    /// </remarks>
    public interface IPointerTypeSymbol : ITypeSymbol
    {
        /// <summary>
        /// Gets the type of the storage location that an instance of the pointer type points to.
        /// </summary>
        ITypeSymbol PointedAtType { get; }

        /// <summary>
        /// Custom modifiers associated with the pointer type, or an empty array if there are none.
        /// </summary>
        /// <remarks>
        /// Some managed languages may represent special information about the pointer type
        /// as a custom modifier on either the pointer type or the element type, or
        /// both.
        /// </remarks>
        ImmutableArray<CustomModifier> CustomModifiers { get; }
    }
}
