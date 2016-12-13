// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A symbol representing a discarded value, e.g. a symbol in the result of
    /// GetSymbolInfo for `_` in `M(out _)` or `(x, _) = e`.
    /// </summary>
    public interface IDiscardSymbol : ISymbol
    {
        /// <summary>
        /// The type of the discarded value.
        /// </summary>
        ITypeSymbol Type { get; }
    }
}
