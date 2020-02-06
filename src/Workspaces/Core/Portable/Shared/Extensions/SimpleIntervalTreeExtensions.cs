// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class SimpleIntervalTreeExtensions
    {
        /// <summary>
        /// check whether the given span is intersects with the tree
        /// </summary>
        public static bool HasIntervalThatIntersectsWith(this SimpleIntervalTree<TextSpan, TextSpanIntervalIntrospector> tree, TextSpan span)
            => tree.HasIntervalThatIntersectsWith(span.Start, span.Length);
    }
}
