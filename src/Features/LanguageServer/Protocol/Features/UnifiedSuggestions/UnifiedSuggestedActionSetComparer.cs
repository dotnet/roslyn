// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.UnifiedSuggestions
{
    internal class UnifiedSuggestedActionSetComparer : IComparer<UnifiedSuggestedActionSet>
    {
        private readonly TextSpan? _targetSpan;

        public UnifiedSuggestedActionSetComparer(TextSpan? targetSpan)
            => _targetSpan = targetSpan;

        private static int Distance(TextSpan? maybeA, TextSpan? maybeB)
        {
            // If we don't have a text span or target point we cannot calculate the distance between them
            if (!maybeA.HasValue || !maybeB.HasValue)
            {
                return int.MaxValue;
            }

            var a = maybeA.Value;
            var b = maybeB.Value;

            // The distance of two spans is symetric sumation of:
            // - the distance of a's start to b's start
            // - the distance of a's end to b's end
            //
            // This particular metric has been chosen because it is both simple
            // and uses the all the information in both spans. A weighting (i.e.
            // the distance of starts is more important) could be added but it
            // didn't seem necessary.
            //
            // E.g.: for spans [ ] and $ $ the distance is distanceOfStarts+distanceOfEnds:
            // $ $ [  ] has distance 2+3
            // $ [   ]$ has distance 1+0
            // $[    ]$ has distance 0+0
            // $ []   $ has distance 1+3
            // $[]    $ has distance 0+4
            var startsDistance = Math.Abs(a.Start - b.Start);
            var endsDistance = Math.Abs(a.End - b.End);

            return startsDistance + endsDistance;
        }

        public int Compare(UnifiedSuggestedActionSet x, UnifiedSuggestedActionSet y)
        {
            if (!_targetSpan.HasValue || !x.ApplicableToSpan.HasValue || !y.ApplicableToSpan.HasValue)
            {
                // Not enough data to compare, consider them equal
                return 0;
            }

            var distanceX = Distance(x.ApplicableToSpan, _targetSpan);
            var distanceY = Distance(y.ApplicableToSpan, _targetSpan);

            return distanceX.CompareTo(distanceY);
        }
    }
}
