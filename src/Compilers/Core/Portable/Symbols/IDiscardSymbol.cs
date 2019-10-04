// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A symbol representing a discarded value, e.g. a symbol in the result of
    /// GetSymbolInfo for <c>_</c> in <c>M(out _)</c> or <c>(x, _) = e</c>.
    /// </summary>
    public interface IDiscardSymbol : ISymbol
    {
        /// <summary>
        /// The type of the discarded value.
        /// </summary>
        ITypeSymbol Type { get; }

        /// <summary>
        /// The top-level nullability of the discarded value.
        /// </summary>
        NullableAnnotation NullableAnnotation { get; }
    }
}
