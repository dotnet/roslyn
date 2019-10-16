// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

namespace Microsoft.CodeAnalysis.Symbols
{
    internal interface ISymbolInternal : ISymbol
    {
#nullable disable // Skipped for now https://github.com/dotnet/roslyn/issues/39166
        Compilation DeclaringCompilation { get; }
#nullable enable

        /// <summary>
        /// Allows a symbol to support comparisons that involve child type symbols
        /// </summary>
        /// <remarks>
        /// Because TypeSymbol equality can differ based on e.g. nullability, any symbols that contain TypeSymbols can also differ in the same way
        /// This call allows the symbol to accept a comparison kind that should be used when comparing its contained types
        /// </remarks>
        bool Equals(ISymbol? other, TypeCompareKind compareKind);
    }
}
