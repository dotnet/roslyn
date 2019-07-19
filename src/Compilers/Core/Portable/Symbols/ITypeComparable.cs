// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Internal interface that allows a symbol to declare that it supports comparisons involving type symbols
    /// </summary>
    /// <remarks>
    /// Because TypeSymbol equality can differ based on e.g. nullability, any symbols that contain TypeSymbols can also differ in the same way
    /// This interface allows the symbol to accept a comparison kind that should be used when comparing its contained types
    /// </remarks>
    internal interface ITypeComparable
    {
        internal bool Equals(ISymbol other, TypeCompareKind compareKind);
    }
}
