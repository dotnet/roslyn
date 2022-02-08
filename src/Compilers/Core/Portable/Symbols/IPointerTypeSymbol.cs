// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
