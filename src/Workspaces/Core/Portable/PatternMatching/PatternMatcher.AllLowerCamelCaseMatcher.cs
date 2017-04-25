// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Shared;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.PatternMatching
{
    internal sealed partial class PatternMatcher : IDisposable
    {
        /// <summary>
        /// Encapsulated matches responsible for matching an all lowercase pattern against
        /// a candidate using CamelCase matching. i.e. this code is responsible for finding the
        /// match between "cofipro" and "CodeFixProvider". 
        /// </summary>
        private struct AllLowerCamelCaseMatcher
        {
            private readonly string _candidate;
            private readonly bool _includeMatchedSpans;
            private readonly StringBreaks _candidateHumps;
            private readonly TextChunk _patternChunk;
            private readonly string _patternText;

            public AllLowerCamelCaseMatcher(string candidate, bool includeMatchedSpans, StringBreaks candidateHumps, TextChunk patternChunk)
            {
                _candidate = candidate;
                _includeMatchedSpans = includeMatchedSpans;
                _candidateHumps = candidateHumps;
                _patternChunk = patternChunk;
                _patternText = _patternChunk.Text;
            }

            /// <summary>
            /// Returns null if no match was found, 1 if a contiguous match was found, 2 if a 
            /// match as found that starts at the beginning of the candidate, and 3 if a contiguous
            /// match was found that starts at the beginning of the candidate.
            /// </summary>
            public int? TryMatch(out ImmutableArray<TextSpan> matchedSpans)
            {
                // We have something like cofipro and we want to match CodeFixProvider.  
                //
                // Note that this is incredibly ambiguous.  We'd also want this to match 
                // CorporateOfficePartsRoom So, for example, if we were to consume the "co" 
                // as matching "Corporate", then "f" wouldn't match any camel hump.  So we
                // basically have to branch out and try all options at every character
                // in the pattern chunk.

                var patternIndex = 0;
                var candidateHumpIndex = 0;

                var (bestWeight, localMatchedSpansInReverse) = TryMatch(
                   patternIndex, candidateHumpIndex, contiguous: null);

                matchedSpans = _includeMatchedSpans && localMatchedSpansInReverse != null
                    ? new NormalizedTextSpanCollection(localMatchedSpansInReverse).ToImmutableArray()
                    : ImmutableArray<TextSpan>.Empty;

                localMatchedSpansInReverse?.Free();
                return bestWeight;
            }

            private (int? bestWeight, ArrayBuilder<TextSpan> matchedSpansInReverse) TryMatch(
                int patternIndex, int candidateHumpIndex, bool? contiguous)
            {
                if (patternIndex == _patternText.Length)
                {
                    // We hit the end.  So we were able to match against this candidate.
                    var weight = contiguous == false ? 0 : CamelCaseContiguousBonus;
                    var matchedSpansInReverse = _includeMatchedSpans ? ArrayBuilder<TextSpan>.GetInstance() : null;

                    return (weight, matchedSpansInReverse);
                }

                var bestWeight = default(int?);
                var bestMatchedSpansInReverse = default(ArrayBuilder<TextSpan>);

                // Look for a hump in the candidate that matches the current letter we're on.
                var patternCharacter = _patternText[patternIndex];
                for (int humpIndex = candidateHumpIndex, n = _candidateHumps.GetCount(); humpIndex < n; humpIndex++)
                {
                    // If we've been contiguous, but we jumped past a hump, then we're no longer contiguous.
                    if (contiguous.HasValue && contiguous.Value)
                    {
                        contiguous = humpIndex == candidateHumpIndex;
                    }

                    var candidateHump = _candidateHumps[humpIndex];
                    if (char.ToLower(_candidate[candidateHump.Start]) == patternCharacter)
                    {
                        // Found a hump in the candidate string that matches the current pattern
                        // character we're on.  i.e. we matched the c in cofipro against the C in 
                        // CodeFixProvider.
                        //
                        // Now, for each subsequent character, we need to both try to consume it
                        // as part of the current hump, or see if it should match the next hump.
                        //
                        // Note, if the candidate is something like CodeFixProvider and our pattern
                        // is cofipro, and we've matched the 'f' against the 'F', then the max of
                        // the pattern we'll want to consume is "fip" against "Fix".  We don't want
                        // consume parts of the pattern once we reach the next hump.

                        // We matched something.  If this was our first match, consider ourselves
                        // contiguous.
                        if (contiguous == null)
                        {
                            contiguous = true;
                        }

                        var (weight, matchedSpansInReverse) = TryConsumePatternOrMatchNextHump(
                            patternIndex, humpIndex, contiguous.Value);

                        if (UpdateBestResultIfBetter(
                                weight, matchedSpansInReverse,
                                ref bestWeight, ref bestMatchedSpansInReverse,
                                matchSpanToAdd: null))
                        {
                            // We found the best result so far.  We can stop immediately.
                            break;
                        }
                    }
                }

                return (bestWeight, bestMatchedSpansInReverse);
            }

            private (int? bestWeight, ArrayBuilder<TextSpan> matchedSpans) TryConsumePatternOrMatchNextHump(
                int patternIndex, int humpIndex, bool contiguous)
            {
                var bestWeight = default(int?);
                var bestMatchedSpansInReverse = default(ArrayBuilder<TextSpan>);

                var candidateHump = _candidateHumps[humpIndex];

                var maxPatternHumpLength = _patternText.Length - patternIndex;
                var maxCandidateHumpLength = candidateHump.Length;
                var maxHumpMatchLength = Math.Min(maxPatternHumpLength, maxCandidateHumpLength);
                for (var possibleHumpMatchLength = 1; possibleHumpMatchLength <= maxHumpMatchLength; possibleHumpMatchLength++)
                {
                    if (!LowercaseSubstringsMatch(
                            _candidate, candidateHump.Start,
                            _patternText, patternIndex, possibleHumpMatchLength))
                    {
                        // Stop trying to consume once the pattern contents no longer matches
                        // against the current candidate hump.
                        break;
                    }

                    // The pattern substring 'f' has matched against 'F', or 'fi' has matched
                    // against 'Fi'.  recurse and let the rest of the pattern match the remainder
                    // of the candidate.

                    var (weight, matchedSpansInReverse) = TryMatch(
                        patternIndex + possibleHumpMatchLength, humpIndex + 1, contiguous);

                    // If this is our first hump add a 'from start' bonus.  Note, if we didn't
                    // match anything than 'weight' will remain null.
                    if (humpIndex == 0)
                    {
                        weight += CamelCaseMatchesFromStartBonus;
                    }

                    // This is the span of the hump of the candidate we matched.
                    var matchSpanToAdd = new TextSpan(candidateHump.Start, possibleHumpMatchLength);
                    if (UpdateBestResultIfBetter(
                            weight, matchedSpansInReverse,
                            ref bestWeight, ref bestMatchedSpansInReverse,
                            matchSpanToAdd))
                    {
                        // We found the best result so far.  We can stop immediately.
                        break;
                    }
                }

                return (bestWeight, bestMatchedSpansInReverse);
            }

            /// <summary>
            /// Updates the currently stored 'best result' if the current result is better.
            /// Returns 'true' if no further work is required and we can break early, or 
            /// 'false' if we need to keep on going.
            /// 
            /// If 'weight' is better than 'bestWeight' and matchSpanToAdd is not null, then
            /// matchSpanToAdd will be added to matchedSpansInReverse.
            /// </summary>
            private bool UpdateBestResultIfBetter(
                int? weight, ArrayBuilder<TextSpan> matchedSpansInReverse,
                ref int? bestWeight, ref ArrayBuilder<TextSpan> bestMatchedSpansInReverse,
                TextSpan? matchSpanToAdd)
            {
                if (!IsBetter(weight, bestWeight))
                {
                    // Even though we matched this current candidate hump we failed to match
                    // the remainder of the pattern.  Continue to the next candidate hump
                    // to see if our pattern character will match it and potentially succeed.
                    matchedSpansInReverse?.Free();

                    // We need to keep going.
                    return false;
                }

                if (matchSpanToAdd != null)
                {
                    matchedSpansInReverse?.Add(matchSpanToAdd.Value);
                }

                // This was result was better than whatever previous best result we had was.
                // Free and overwrite the existing best results, and keep going.
                bestWeight = weight;
                bestMatchedSpansInReverse?.Free();
                bestMatchedSpansInReverse = matchedSpansInReverse;

                // We found a path that allowed us to match everything contiguously
                // from the beginning.  This is the best match possible.  So we can
                // just break out now and return this result.
                return weight == CamelCaseMaxWeight;
            }

            private static bool IsBetter(int? weight, int? currentBestWeight)
            {
                // If we got no weight, this results is definitely not better.
                if (weight == null)
                {
                    return false;
                }

                // If the current best weight is greater than the weight we just got, then this
                // result is definitely not better.
                if (currentBestWeight > weight)
                {
                    return false;
                }

                return true;
            }

            private bool LowercaseSubstringsMatch(
                string s1, int start1, string s2, int start2, int length)
            {
                for (var i = 0; i < length; i++)
                {
                    if (char.ToLower(s1[start1 + i]) != char.ToLower(s2[start2 + i]))
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}