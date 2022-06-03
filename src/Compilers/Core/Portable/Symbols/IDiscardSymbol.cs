// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
