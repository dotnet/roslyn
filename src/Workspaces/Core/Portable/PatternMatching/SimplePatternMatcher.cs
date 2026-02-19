// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Globalization;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Shared.Collections;

namespace Microsoft.CodeAnalysis.PatternMatching;

internal abstract partial class PatternMatcher
{
    internal sealed partial class SimplePatternMatcher : PatternMatcher
    {
        private readonly bool _allowFuzzyMatching;
        private PatternSegment _fullPatternSegment;

        public SimplePatternMatcher(
            string pattern,
            CultureInfo culture,
            bool includeMatchedSpans,
            bool allowFuzzyMatching)
            : base(includeMatchedSpans, culture)
        {
            _allowFuzzyMatching = allowFuzzyMatching;
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
        public override bool AddMatches(string candidate, ref TemporaryArray<PatternMatch> matches)
        {
            if (SkipMatch(candidate))
                return false;

            // Always do the cheap non-fuzzy check first.  If that fails, and we allow fuzzy matching, fall back to trying that.
            if (MatchPatternSegment(candidate, ref _fullPatternSegment, ref matches, allowFuzzyMatching: false))
                return true;

            return _allowFuzzyMatching && MatchPatternSegment(candidate, ref _fullPatternSegment, ref matches, allowFuzzyMatching: true);
        }

        public TestAccessor GetTestAccessor()
            => new(this);

        public readonly struct TestAccessor(SimplePatternMatcher simplePatternMatcher)
        {
            public readonly bool LastCacheResultIs(bool areSimilar, string candidateText)
                => simplePatternMatcher._fullPatternSegment.TotalTextChunk.SimilarityChecker.LastCacheResultIs(areSimilar, candidateText);
        }
    }
}
