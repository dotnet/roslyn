// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.CodeAnalysis.CSharp
{
    internal enum CollectionExpressionTypeKind
    {
        None = 0,
        Array,
        ImmutableArray,
        Span,
        ReadOnlySpan,
        List,
        CollectionBuilder,
        ImplementsIEnumerableT,
        ImplementsIEnumerable,
        ArrayInterface,
    }
}
