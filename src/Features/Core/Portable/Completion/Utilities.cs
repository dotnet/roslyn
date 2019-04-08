// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion
{
    internal static class Utilities
    {
        public static TextChange Collapse(SourceText newText, ImmutableArray<TextChange> changes)
        {
            if (changes.Length == 0)
            {
                return new TextChange(new TextSpan(0, 0), "");
            }
            else if (changes.Length == 1)
            {
                return changes[0];
            }

            // The span we want to replace goes from the start of the first span to the end of
            // the  last span.
            var totalOldSpan = TextSpan.FromBounds(changes.First().Span.Start, changes.Last().Span.End);

            // We figure out the text we're replacing with by actually just figuring out the
            // new span in the newText and grabbing the text out of that.  The newSpan will
            // start from the same position as the oldSpan, but it's length will be the old
            // span's length + all the deltas we accumulate through each text change.  i.e.
            // if the first change adds 2 characters and the second change adds 4, then 
            // the newSpan will be 2+4=6 characters longer than the old span.
            var sumOfDeltas = changes.Sum(c => c.NewText.Length - c.Span.Length);
            var totalNewSpan = new TextSpan(totalOldSpan.Start, totalOldSpan.Length + sumOfDeltas);

            return new TextChange(totalOldSpan, newText.ToString(totalNewSpan));
        }
    }
}
