// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Symbols
{
    internal interface ISymbolInternal : ISymbol
    {
        Compilation DeclaringCompilation { get; }

        /// <summary>
        /// Allows a symbol to support comparisons that involve child type symbols
        /// </summary>
        /// <remarks>
        /// Because TypeSymbol equality can differ based on e.g. nullability, any symbols that contain TypeSymbols can also differ in the same way
        /// This call allows the symbol to accept a comparison kind that should be used when comparing its contained types
        /// </remarks>
        bool Equals(ISymbolInternal other, TypeCompareKind compareKind);
    }
}
