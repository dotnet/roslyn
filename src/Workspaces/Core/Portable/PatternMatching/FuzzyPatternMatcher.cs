// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Shared.Collections;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.PatternMatching;

internal abstract partial class PatternMatcher
{
    /// <summary>
    /// Pattern matcher that performs only fuzzy (edit-distance) matching via <see
    /// cref="WordSimilarityChecker"/>. Does not attempt any non-fuzzy strategies (exact, prefix,
    /// camelCase, substring) — those are handled separately by <see cref="SimplePatternMatcher"/>.
    /// Callers compose the two matchers: try <see cref="SimplePatternMatcher"/> first, then fall
    /// back to <see cref="FuzzyPatternMatcher"/> only when non-fuzzy matching fails.
    /// </summary>
    internal sealed class FuzzyPatternMatcher : PatternMatcher
    {
        private WordSimilarityChecker _similarityChecker;

        public FuzzyPatternMatcher(
            string pattern,
            bool includeMatchedSpans)
            : base(includeMatchedSpans, culture: null)
        {
            pattern = pattern.Trim();

            _invalidPattern = string.IsNullOrWhiteSpace(pattern) || pattern.Length < WordSimilarityChecker.MinFuzzyLength;
            if (!_invalidPattern)
                _similarityChecker = new WordSimilarityChecker(pattern, substringsAreSimilar: false);
        }

        public override void Dispose()
        {
            base.Dispose();
            _similarityChecker.Dispose();
        }

        protected override bool AddMatchesWorker(string candidate, ref TemporaryArray<PatternMatch> matches)
        {
            if (_similarityChecker.AreSimilar(candidate))
            {
                matches.Add(new PatternMatch(
                    PatternMatchKind.Fuzzy, punctuationStripped: false,
                    isCaseSensitive: false, matchedSpan: null));
                return true;
            }

            return false;
        }

        internal TestAccessor GetTestAccessor()
            => new(this);

        internal readonly struct TestAccessor(FuzzyPatternMatcher matcher)
        {
            public bool LastCacheResultIs(bool areSimilar, string candidateText)
                => matcher._similarityChecker.LastCacheResultIs(areSimilar, candidateText);
        }
    }
}
