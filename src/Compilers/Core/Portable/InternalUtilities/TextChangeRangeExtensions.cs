// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Utilities
{
    internal static class TextChangeRangeExtensions
    {
        public static TextChangeRange? Accumulate(this TextChangeRange? accumulatedTextChangeSoFar, IEnumerable<TextChangeRange> changesInNextVersion)
        {
            if (!changesInNextVersion.Any())
            {
                return accumulatedTextChangeSoFar;
            }

            // get encompassing text change and accumulate it once. 
            // we could apply each one individually like we do in SyntaxDiff::ComputeSpansInNew by calculating delta
            // between each change in changesInNextVersion which is already sorted in its textual position ascending order.
            // but end result will be same as just applying it once with encompassed text change range.
            var newChange = TextChangeRange.Collapse(changesInNextVersion);

            // no previous accumulated change, return the new value.
            if (accumulatedTextChangeSoFar == null)
            {
                return newChange;
            }

            // set initial value from the old one.
            var currentStart = accumulatedTextChangeSoFar.Value.Span.Start;
            var currentOldEnd = accumulatedTextChangeSoFar.Value.Span.End;
            var currentNewEnd = accumulatedTextChangeSoFar.Value.Span.Start + accumulatedTextChangeSoFar.Value.NewLength;

            // this is a port from 
            //      csharp\rad\Text\SourceText.cpp - CSourceText::OnChangeLineText
            // which accumulate text changes to one big text change that would encompass all changes

            // Merge incoming edit data with old edit data here.
            // RULES:
            // 1) position values are always associated with a buffer version.
            // 2) Comparison between position values is only allowed if their
            //    buffer version is the same.
            // 3) newChange.Span.End and newChange.Span.Start + newChange.NewLength (both stored and incoming) 
            //    refer to the same position, but have different buffer versions.
            // 4) The incoming end position is associated with buffer versions
            //    n-1 (old) and n(new).
            // 5) The stored end position BEFORE THIS EDIT is associated with
            //    buffer versions 0 (old) and n-1 (new).
            // 6) The stored end position AFTER THIS EDIT should be associated
            //    with buffer versions 0 (old) and n(new).
            // 7) To transform a position P from buffer version of x to y, apply
            //    the delta between any position C(x) and C(y), ASSUMING that
            //    both positions P and C are affected by all edits between
            //    buffer versions x and y.
            // 8) The start position is relative to all buffer versions, because
            //    it precedes all edits(by definition)

            // First, the start position.  This one is easy, because it is not
            // complicated by buffer versioning -- it is always the "earliest"
            // of all incoming values.
            if (newChange.Span.Start < currentStart)
            {
                currentStart = newChange.Span.Start;
            }

            // Okay, now the end position.  We must make a choice between the
            // stored end position and the incoming end position.  Per rule #2,
            // we must use the stored NEW end and the incoming OLD end, both of
            // which are relative to buffer version n-1.
            if (currentNewEnd > newChange.Span.End)
            {
                // We have chosen to keep the stored end because it occurs past
                // the incoming edit.  So, we need currentOldEnd and
                // currentNewEnd.  Since currentOldEnd is already relative
                // to buffer 0, it is unmodified.  Since currentNewEnd is
                // relative to buffer n-1 (and we need n), apply to it the delta
                // between the incoming end position values, which are n-1 and n.
                currentNewEnd = currentNewEnd + newChange.NewLength - newChange.Span.Length;
            }
            else
            {
                // We have chosen to use the incoming end because it occurs past
                // the stored edit.  So, we need newChange.Span.End and (newChange.Span.Start + newChange.NewLength).
                // Since (newChange.Span.Start + newChange.NewLength) is already relative to buffer n, it is copied
                // unmodified.  Since newChange.Span.End is relative to buffer n-1 (and
                // we need 0), apply to it the delta between the stored end
                // position values, which are relative to 0 and n-1.
                currentOldEnd = currentOldEnd + newChange.Span.End - currentNewEnd;
                currentNewEnd = newChange.Span.Start + newChange.NewLength;
            }

            return new TextChangeRange(TextSpan.FromBounds(currentStart, currentOldEnd), currentNewEnd - currentStart);
        }
    }
}
