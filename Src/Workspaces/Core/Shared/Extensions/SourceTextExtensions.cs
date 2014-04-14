// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static class SourceTextExtensions
    {
        /// <summary>
        /// Returns the leading whitespace of the line located at the specified position in the given snapshot.
        /// </summary>
        public static string GetLeadingWhitespaceOfLineAtPosition(this SourceText text, int position)
        {
            Contract.ThrowIfNull(text);

            var line = text.Lines.GetLineFromPosition(position);
            var linePosition = line.GetFirstNonWhitespacePosition();
            if (!linePosition.HasValue)
            {
                return line.ToString();
            }

            var lineText = line.ToString();
            return lineText.Substring(0, linePosition.Value - line.Start);
        }

        public static void GetLineAndOffset(this SourceText text, int position, out int lineNumber, out int offset)
        {
            var line = text.Lines.GetLineFromPosition(position);

            lineNumber = line.LineNumber;
            offset = position - line.Start;
        }

        public static void GetLinesAndOffsets(
            this SourceText text,
            TextSpan textSpan,
            out int startLineNumber,
            out int startOffset,
            out int endLineNumber,
            out int endOffset)
        {
            text.GetLineAndOffset(textSpan.Start, out startLineNumber, out startOffset);
            text.GetLineAndOffset(textSpan.End, out endLineNumber, out endOffset);
        }

        public static bool OverlapsHiddenPosition(
            this SourceText text, TextSpan span, Func<int, CancellationToken, bool> isPositionHidden, CancellationToken cancellationToken)
        {
            var result = TryOverlapsHiddenPosition(text, span, isPositionHidden, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            return result;
        }

        /// <summary>
        /// Same as OverlapsHiddenPosition but doesn't throw on cancellation.  Instead, returns false
        /// in that case.
        /// </summary>
        public static bool TryOverlapsHiddenPosition(
            this SourceText text, TextSpan span, Func<int, CancellationToken, bool> isPositionHidden,
            CancellationToken cancellationToken)
        {
            var startLineNumber = text.Lines.IndexOf(span.Start);
            var endLineNumber = text.Lines.IndexOf(span.End);

            // NOTE(cyrusn): It's safe to examine the start of a line because you can't have a line
            // with both a pp directive and code on it.  so, for example, if a node crosses a region
            // then it must be the case that the start of some line from the start of the node to
            // the end is hidden.  i.e.:
#if false
'           class C
'           {
'#line hidden
'           }
'#line default
#endif
            // The start of the line with the } on it is hidden, and thus the node overlaps a hidden
            // region.

            for (var lineNumber = startLineNumber; lineNumber <= endLineNumber; lineNumber++)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                var linePosition = text.Lines[lineNumber].Start;
                var isHidden = isPositionHidden(linePosition, cancellationToken);
                if (isHidden)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determine if this SourceText has the same text as another SourceText.
        /// </summary>
        public static bool HasSameText(this SourceText text, SourceText otherText)
        {
            if (text == null || otherText == null)
            {
                return text == otherText;
            }

            if (text == otherText)
            {
                return true;
            }

            if (text.Length != otherText.Length)
            {
                return false;
            }

            var buffer1 = SharedPools.CharArray.Allocate();
            var buffer2 = SharedPools.CharArray.Allocate();
            try
            {
                int position = 0;
                while (position < text.Length)
                {
                    int n = Math.Min(text.Length - position, buffer1.Length);
                    text.CopyTo(position, buffer1, 0, n);
                    otherText.CopyTo(position, buffer2, 0, n);

                    for (int i = 0; i < n; i++)
                    {
                        if (buffer1[i] != buffer2[i])
                        {
                            return false;
                        }
                    }

                    position += n;
                }

                return true;
            }
            finally
            {
                SharedPools.CharArray.Free(buffer2);
                SharedPools.CharArray.Free(buffer1);
            }
        }

        public static TextChangeRange GetEncompassingTextChangeRange(this SourceText newText, SourceText oldText)
        {
            var ranges = newText.GetChangeRanges(oldText);
            if (ranges.Count == 0)
            {
                return default(TextChangeRange);
            }

            // simple case.
            if (ranges.Count == 1)
            {
                return ranges[0];
            }

            return TextChangeRange.Collapse(ranges);
        }

        public static int IndexOf(this SourceText text, string value, int startIndex, bool caseSensitive)
        {
            var length = text.Length - value.Length;
            var normalized = caseSensitive ? value : value.ToLowerInvariant();

            for (var i = startIndex; i <= length; i++)
            {
                var match = true;
                for (var j = 0; j < normalized.Length; j++)
                {
                    // just use indexer of source text. perf of indexer depends on actual implementation of SourceText.
                    // * all of our implementation at editor layer should provide either O(1) or O(logn).
                    //
                    // only one implementation we have that could have bad indexer perf is CompositeText with heavily modified text
                    // at compiler layer but I believe that being used in find all reference will be very rare if not none.
                    if (!Match(normalized[j], text[i + j], caseSensitive))
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    return i;
                }
            }

            return -1;
        }

        private static readonly TextInfo invariantTextInfo = CultureInfo.InvariantCulture.TextInfo;

        private static bool Match(char nomalizedLeft, char right, bool caseSensitive)
        {
            return caseSensitive ? nomalizedLeft == right : nomalizedLeft == invariantTextInfo.ToLower(right);
        }
    }
}