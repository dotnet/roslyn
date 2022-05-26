﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class TextSpanExtensions
    {
        /// <summary>
        /// merge provided spans to each distinct group of spans in ascending order
        /// </summary>
        public static IEnumerable<TextSpan> ToNormalizedSpans(this IEnumerable<TextSpan> spans)
            => new NormalizedTextSpanCollection(spans);

        public static ImmutableArray<TextSpan> ToNormalizedSpans(this ImmutableArray<TextSpan> spans)
            => new NormalizedTextSpanCollection(spans).ToImmutableArray();

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
                return default;
            }

            return TextSpan.FromBounds(start, end);
        }

        /// <summary>
        /// Returns true if the span encompasses the specified node or token and is contained within its trivia.
        /// </summary>
        public static bool IsAround(this TextSpan span, SyntaxNodeOrToken node) => IsAround(span, node, node);

        /// <summary>
        /// Returns true if the span encompasses a span between the specified nodes or tokens
        /// and is contained within trivia around them.
        /// </summary>
        public static bool IsAround(this TextSpan span, SyntaxNodeOrToken startNode, SyntaxNodeOrToken endNode)
        {
            var innerSpan = TextSpan.FromBounds(startNode.Span.Start, endNode.Span.End);
            var outerSpan = TextSpan.FromBounds(startNode.FullSpan.Start, endNode.FullSpan.End);
            return span.Contains(innerSpan) && outerSpan.Contains(span);
        }

        public static IEnumerable<TextSpan> Subtract(this TextSpan span, TextSpan except)
        {
            if (except.IsEmpty)
            {
                if (span != except)
                {
                    yield return span;
                }

                yield break;
            }

            var startSegmentEnd = Math.Min(span.End, except.Start);
            if (span.Start < startSegmentEnd)
                yield return TextSpan.FromBounds(span.Start, startSegmentEnd);

            var endSegmentStart = Math.Max(span.Start, except.End);
            if (endSegmentStart < span.End)
                yield return TextSpan.FromBounds(endSegmentStart, span.End);
        }

        public static IEnumerable<TextSpan> Subtract(this IEnumerable<TextSpan> spans, TextSpan except)
            => spans.SelectMany(span => span.Subtract(except));
    }
}
