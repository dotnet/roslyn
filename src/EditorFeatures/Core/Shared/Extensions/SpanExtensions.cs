// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

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
        {
            return new TextSpan(span.Start, span.Length);
        }

        public static bool IntersectsWith(this Span span, int position)
        {
            return position >= span.Start && position <= span.End;
        }
    }
}
