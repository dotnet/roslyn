// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class TextSpanExtensions
    {
        /// <summary>
        /// merge provided spans to each distinct group of spans in ascending order
        /// </summary>
        public static IEnumerable<TextSpan> ToNormalizedSpans(this IEnumerable<TextSpan> spans)
        {
            return new NormalizedTextSpanCollection(spans);
        }

        public static TextSpan Collapse(this IEnumerable<TextSpan> spans)
        {
            var start = int.MaxValue;
            var end = 0;

            foreach (var span in spans)
            {
                if (span.Start < start)
                {
                    start = span.Start;
                }

                if (span.End > end)
                {
                    end = span.End;
                }
            }

            if (start > end)
            {
                // there were no changes.
                return default(TextSpan);
            }

            return TextSpan.FromBounds(start, end);
        }
    }
}
