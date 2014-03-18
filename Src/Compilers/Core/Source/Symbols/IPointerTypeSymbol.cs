// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;
namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents a pointer type such as "int *". Pointer types
    /// are used only in unsafe code.
    /// </summary>
    public interface IPointerTypeSymbol : ITypeSymbol
    {
        /// <summary>
        /// Gets the type of the storage location that an instance of the pointer type points to.
        /// </summary>
        ITypeSymbol PointedAtType { get; }

        /// <summary>
        /// The list of custom modifiers, if any, associated with the pointer type.
        /// (Some managed languages may represent special information about the pointer type
        /// as a custom modifier on either the pointer type or the element type, or
        /// both.)
        /// </summary>
        ImmutableArray<CustomModifier> CustomModifiers { get; }
    }
}