// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.PatternMatching
{
    internal partial class PatternMatcher
    {
        private sealed partial class SimplePatternMatcher : PatternMatcher
        {
            private readonly PatternSegment _fullPatternSegment;

            public SimplePatternMatcher(
                string pattern,
                CultureInfo culture,
                bool includeMatchedSpans,
                bool allowFuzzyMatching)
                : base(includeMatchedSpans, culture, allowFuzzyMatching)
            {
                pattern = pattern.Trim();

                _fullPatternSegment = new PatternSegment(pattern, allowFuzzyMatching);
                _invalidPattern = _fullPatternSegment.IsInvalid;
            }

            public override void Dispose()
            {
                base.Dispose();
                _fullPatternSegment.Dispose();
            }

            /// <summary>
            /// Determines if a given candidate string matches under a multiple word query text, as you
            /// would find in features like Navigate To.
            /// </summary>
            /// <returns>If this was a match, a set of match types that occurred while matching the
            /// patterns. If it was not a match, it returns null.</returns>
            public override bool AddMatches(string candidate, ArrayBuilder<PatternMatch> matches)
            {
                if (SkipMatch(candidate))
                {
                    return false;
                }

                return MatchPatternSegment(candidate, _fullPatternSegment, matches, fuzzyMatch: false) ||
                       MatchPatternSegment(candidate, _fullPatternSegment, matches, fuzzyMatch: true);
            }
        }
    }
}
