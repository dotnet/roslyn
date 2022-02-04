// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.CodeAnalysis.Editor.Shared.Extensions
{
    /// <summary>
    /// Extension methods for the editor Span struct
    /// </summary>
    internal static class SpanExtensions
    {
        /// <summary>
        /// Convert the editor Span instance to the corresponding TextSpan instance
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        public static TextSpan ToTextSpan(this Span span)
            => new(span.Start, span.Length);

        public static bool IntersectsWith(this Span span, int position)
            => position >= span.Start && position <= span.End;
    }
}
