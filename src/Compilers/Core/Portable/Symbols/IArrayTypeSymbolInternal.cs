// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.Symbols;

internal interface IArrayTypeSymbolInternal : ITypeSymbolInternal
{
    /// <summary>
    /// Is this a zero-based one-dimensional array, i.e. SZArray in CLR terms.
    /// </summary>
    bool IsSZArray { get; }

    /// <summary>
    /// Gets the type of the elements stored in the array.
    /// </summary>
    ITypeSymbolInternal ElementType { get; }
}
