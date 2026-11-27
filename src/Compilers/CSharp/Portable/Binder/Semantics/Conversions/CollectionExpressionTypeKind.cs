// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal enum CollectionExpressionTypeKind
    {
        None = 0,
        Array,
        Span,
        ReadOnlySpan,
        CollectionBuilder,
        ImplementsIEnumerable,

        /// <summary>
        /// One of the well-known interfaces known to be implemented by any array:
        /// <list type="bullet">
        /// <item><see cref="IReadOnlyCollection{T}"/></item>
        /// <item><see cref="IReadOnlyList{T}"/></item>
        /// <item><see cref="ICollection{T}"/></item>
        /// <item><see cref="IList{T}"/></item>
        /// </list>
        /// </summary>
        ArrayInterface,
    }
}
