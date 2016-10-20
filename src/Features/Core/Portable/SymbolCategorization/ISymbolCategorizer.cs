// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.SymbolCategorization
{
    internal interface ISymbolCategorizer
    {
        /// <summary>
        /// Returns the set of categories that this categorizer is capable of producing.
        /// </summary>
        ImmutableArray<string> SupportedCategories { get; }

        /// <summary>
        /// Returns the set of categories that apply to the given symbol.
        /// </summary>
        ImmutableArray<string> Categorize(ISymbol symbol);
    }
}
