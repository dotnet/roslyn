// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace Microsoft.CodeAnalysis.Symbols
{
    internal interface ITypeSymbolInternal : INamespaceOrTypeSymbolInternal
    {
        /// <summary>
        /// An enumerated value that identifies whether this type is an array, pointer, enum, and so on.
        /// </summary>
        TypeKind TypeKind { get; }

        /// <summary>
        /// An enumerated value that identifies certain 'special' types such as <see cref="System.Object"/>. 
        /// Returns <see cref="Microsoft.CodeAnalysis.SpecialType.None"/> if the type is not special.
        /// </summary>
        SpecialType SpecialType { get; }

        ExtendedSpecialType ExtendedSpecialType { get; }

        /// <summary>
        /// True if this type is known to be a reference type. It is never the case that
        /// <see cref="IsReferenceType"/> and <see cref="IsValueType"/> both return true. However, for an unconstrained type
        /// parameter, <see cref="IsReferenceType"/> and <see cref="IsValueType"/> will both return false.
        /// </summary>
        bool IsReferenceType { get; }

        /// <summary>
        /// True if this type is known to be a value type. It is never the case that
        /// <see cref="IsReferenceType"/> and <see cref="IsValueType"/> both return true. However, for an unconstrained type
        /// parameter, <see cref="IsReferenceType"/> and <see cref="IsValueType"/> will both return false.
        /// </summary>
        bool IsValueType { get; }

        /// <summary>
        /// Returns an <see cref="ITypeSymbol"/> instance associated with this symbol.
        /// This API and <see cref="ISymbolInternal.GetISymbol"/> should return the same object.
        /// </summary>
        ITypeSymbol GetITypeSymbol();
    }
}
