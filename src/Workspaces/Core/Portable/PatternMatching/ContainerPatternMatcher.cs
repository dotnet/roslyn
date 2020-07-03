// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.PatternMatching
{
    internal partial class PatternMatcher
    {
        private sealed partial class ContainerPatternMatcher : PatternMatcher
        {
            private readonly PatternSegment[] _patternSegments;
            private readonly char[] _containerSplitCharacters;

            public ContainerPatternMatcher(
                string[] patternParts, char[] containerSplitCharacters,
                CultureInfo culture,
                bool allowFuzzyMatching = false)
                : base(false, culture, allowFuzzyMatching)
            {
                _containerSplitCharacters = containerSplitCharacters;

                _patternSegments = patternParts
                    .Select(text => new PatternSegment(text.Trim(), allowFuzzyMatching: allowFuzzyMatching))
                    .ToArray();

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

            public override bool AddMatches(string container, ArrayBuilder<PatternMatch> matches)
            {
                if (SkipMatch(container))
                {
                    return false;
                }

                return AddMatches(container, matches, fuzzyMatch: false) ||
                       AddMatches(container, matches, fuzzyMatch: true);
            }

            private bool AddMatches(string container, ArrayBuilder<PatternMatch> matches, bool fuzzyMatch)
            {
                if (fuzzyMatch && !_allowFuzzyMatching)
                {
                    return false;
                }

                using var _ = ArrayBuilder<PatternMatch>.GetInstance(out var tempContainerMatches);

                var containerParts = container.Split(_containerSplitCharacters, StringSplitOptions.RemoveEmptyEntries);

                var relevantDotSeparatedSegmentLength = _patternSegments.Length;
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
                    var segment = _patternSegments[i];
                    var containerName = containerParts[j];
                    if (!MatchPatternSegment(containerName, segment, tempContainerMatches, fuzzyMatch))
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
}
