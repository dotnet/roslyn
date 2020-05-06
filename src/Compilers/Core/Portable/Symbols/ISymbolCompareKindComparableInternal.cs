
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.Symbols
{
    /// <summary>
    /// Interface implemented by internal components that are not symbols, but contain symbols that should
    /// respect a passed compare kind when comparing for equality.
    /// </summary>
    internal interface ISymbolCompareKindComparableInternal
    {
        /// <summary>
        /// Allows nested symbols to support comparisons that involve child type symbols
        /// </summary>
        /// <remarks>
        /// Because TypeSymbol equality can differ based on e.g. nullability, any symbols that contain TypeSymbols can also differ in the same way
        /// This call allows the component to accept a comparison kind that should be used when comparing its contained types
        /// </remarks>
        bool Equals(ISymbolCompareKindComparableInternal? other, TypeCompareKind compareKind);
    }
}
