// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.Collections;

namespace Microsoft.CodeAnalysis.PatternMatching;

internal abstract partial class PatternMatcher
{
    /// <summary>
    /// Pattern matcher for matching against the container of a symbol (like <c>System.Collections.Generic</c>).  Understands
    /// how to break on dots and match subportions of that container.  Note: all matching is done in a non-fuzzy way.  Fuzzy
    /// matching is only performed by the <see cref="SimplePatternMatcher"/>.
    /// </summary>
    private sealed partial class ContainerPatternMatcher : PatternMatcher
    {
        private readonly PatternSegment[] _patternSegments;
        private readonly char[] _containerSplitCharacters;

        internal ContainerPatternMatcher(
            string[] patternParts,
            char[] containerSplitCharacters,
            bool includeMatchedSpans,
            CultureInfo? culture)
            : base(includeMatchedSpans, culture)
        {
            _containerSplitCharacters = containerSplitCharacters;

            _patternSegments = [.. patternParts.Select(text => new PatternSegment(text.Trim(), allowFuzzyMatching: false))];

            _invalidPattern = _patternSegments.Length == 0 || _patternSegments.Any(s => s.IsInvalid);
        }

        public override void Dispose()
        {
            base.Dispose();

            foreach (var segment in _patternSegments)
            {
                segment.Dispose();
            }
        }

        /// <summary>
        /// Container matching is always non-fuzzy
        /// </summary>
        public override bool AddMatches(string? container, ref TemporaryArray<PatternMatch> matches)
        {
            if (SkipMatch(container))
                return false;

            using var tempContainerMatches = TemporaryArray<PatternMatch>.Empty;

            var containerParts = container.Split(_containerSplitCharacters, StringSplitOptions.RemoveEmptyEntries);

            if (_patternSegments.Length > containerParts.Length)
            {
                // There weren't enough container parts to match against the pattern parts.
                // So this definitely doesn't match.
                return false;
            }

            // So far so good.  Now break up the container for the candidate and check if all
            // the dotted parts match up correctly.

            for (int i = _patternSegments.Length - 1, j = containerParts.Length - 1;
                    i >= 0;
                    i--, j--)
            {
                var containerName = containerParts[j];
                if (!MatchPatternSegment(containerName, ref _patternSegments[i], ref tempContainerMatches.AsRef(), allowFuzzyMatching: false))
                {
                    // This container didn't match the pattern piece.  So there's no match at all.
                    return false;
                }
            }

            // Success, this symbol's full name matched against the dotted name the user was asking
            // about.
            matches.AddRange(tempContainerMatches);
            return true;
        }
    }
}
