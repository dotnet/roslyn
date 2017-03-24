// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.PatternMatching
{
    internal sealed partial class PatternMatcher : IDisposable
    {
        /// <summary>
        /// Encapsulated matches responsible for mathcing an all lowercase pattern against
        /// a candidate using CamelCase matching. i.e. this code is responsible for finding the
        /// match between "cofipro" and "CodeFixProvider". 
        /// </summary>
        private struct AllLowerCamelCaseMatcher
        {
            private readonly string _candidate;
            private readonly bool _includeMatchedSpans;
            private readonly StringBreaks _candidateParts;
            private readonly TextChunk _patternChunk;
            private readonly string _patternText;

            public AllLowerCamelCaseMatcher(string candidate, bool includeMatchedSpans, StringBreaks candidateParts, TextChunk patternChunk)
            {
                _candidate = candidate;
                _includeMatchedSpans = includeMatchedSpans;
                _candidateParts = candidateParts;
                _patternChunk = patternChunk;
                _patternText = _patternChunk.Text;
            }

            /// <summary>
            /// Returns null if no match was found, 1 if a contiguous match was found, 2 if a 
            /// match as found that starts at the beginning of the candidate, and 3 if a continguous
            /// match was found that starts at the beginning of the candidate.
            /// </summary>
            public int? TryMatch(out List<TextSpan> matchedSpans)
            {
                // We have something like cofipro and we want to match CodeFixProvider.  
                //
                // Note that this is incredibly ambiguous.  We'd also want this to match 
                // CorporateOfficePartsRoom So, for example, if we were to consume the "co" 
                // as matching "Corporate", then "f" wouldn't match any camel hump.  So we
                // basically have to branch out and try all options at every character
                // in the pattern chunk.

                var patternIndex = 0;
                var candidatePartIndex = 0;

                var (bestWeight, localMatchedSpans) = TryMatch(
                   patternIndex, candidatePartIndex, contiguous: null);

                matchedSpans = localMatchedSpans;
                return bestWeight;
            }

            private (int? bestWeight, List<TextSpan> matchedSpans) TryMatch(
                int patternIndex, int candidatePartIndex, bool? contiguous)
            {
                if (patternIndex == _patternText.Length)
                {
                    // We hit the end.  So we were able to match against this candidate.
                    return (bestWeight: contiguous == false ? 0 : CamelCaseContiguousBonus,
                            matchedSpans: _includeMatchedSpans ? new List<TextSpan>() : null);
                }

                var bestWeight = default(int?);
                var bestMatchedSpans = default(List<TextSpan>);

                // Look for a part of the candidate that matches the current letter we're on.
                var patternCharacter = _patternText[patternIndex];
                for (var partIndex = candidatePartIndex; partIndex < _candidateParts.Count; partIndex++)
                {
                    // If we've been contiguous, but we jumped past a part, then we're no longer contiguous.
                    if (contiguous.HasValue && contiguous.Value)
                    {
                        contiguous = partIndex == candidatePartIndex;
                    }

                    var candidatePart = _candidateParts[partIndex];
                    if (char.ToLower(_candidate[candidatePart.Start]) == patternCharacter)
                    {
                        // Found a part of the candidate string that matches the current pattern
                        // character we're on.  i.e. we matched the c in cofipro against the C in 
                        // CodeFixProvider.
                        //
                        // Now, for each subsequent character, we need to both try to consume it
                        // as part of the candidate text, or see if it should match the next hump
                        // in the candidate.
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

                        var (weight, matchedSpans) = TryConsumePatternOrMatchNextPart(
                            patternIndex, partIndex, contiguous.Value);
                        if (weight == null)
                        {
                            // Even though we matched this current candidate part we failed to match
                            // the remainder of the pattern.  Continue to the next candidate hump
                            // to see if our pattern character will match it and potentially succed.
                            continue;
                        }

                        Debug.Assert(weight >= 0);
                        if (weight == CamelCaseMaxWeight)
                        {
                            // We found a path that allowed us to match everything contiguously
                            // from the beginning.  This is the best match possible.  So we can
                            // just stop now and return thie result.
                            return (weight, matchedSpans);
                        }

                        // This is a decent match.  But something else could beat it, store
                        // it if it's the best match we have so far, but keep searching.
                        if (bestWeight == null || weight > bestWeight)
                        {
                            bestWeight = weight;
                            bestMatchedSpans = matchedSpans;
                        }
                    }
                }

                return (bestWeight, bestMatchedSpans);
            }

            private (int? bestWeight, List<TextSpan> matchedSpans) TryConsumePatternOrMatchNextPart(
                int patternIndex, int partIndex, bool contiguous)
            {
                var bestWeight = default(int?);
                var bestMatchedSpans = default(List<TextSpan>);

                var candidatePart = _candidateParts[partIndex];

                var maxPatternHumpLength = _patternText.Length - patternIndex;
                var maxCandidateHumpLength = candidatePart.Length;
                var maxHumpMatchLength = Math.Min(maxPatternHumpLength, maxCandidateHumpLength);
                for (var possibleHumpMatchLength = 1; possibleHumpMatchLength <= maxHumpMatchLength; possibleHumpMatchLength++)
                {
                    if (!LowercaseSubstringsMatch(
                        _candidate, candidatePart.Start,
                        _patternText, patternIndex, possibleHumpMatchLength))
                    {
                        // Stop trying to consume once the pattern contents no longer matches
                        // against the current candidate hump.
                        break;
                    }

                    // This is the span of the part of the candidate we matched.
                    var candidateMatchSpan = new TextSpan(candidatePart.Start, possibleHumpMatchLength);

                    // The pattern substring 'f' has matched against 'F', or 'fi' has matched
                    // against 'Fi'.  recurse and let the rest of the pattern match the remainder
                    // of the candidate.

                    var (weight, matchedSpans) = TryMatch(
                        patternIndex + possibleHumpMatchLength, partIndex + 1, contiguous);

                    if (weight == null)
                    {
                        // Didn't match when we recursed.  Try to consume more and see if that gets us 
                        // somewhere.
                        continue;
                    }

                    Debug.Assert(weight <= CamelCaseContiguousBonus);

                    if (partIndex == 0)
                    {
                        weight += CamelCaseMatchesFromStartBonus;
                    }

                    if (weight == CamelCaseMaxWeight)
                    {
                        // We found a path that allowed us to match everything contiguously
                        // from the beginning.  This is the best match possible.  So we can
                        // just stop now and return thie result.
                        matchedSpans?.Insert(0, candidateMatchSpan);
                        return (weight, matchedSpans);
                    }

                    // This is a decent match.  But something else could beat it, store
                    // it if it's the best match we have so far, but keep searching.
                    if (bestWeight == null || weight > bestWeight)
                    {
                        matchedSpans?.Insert(0, candidateMatchSpan);

                        bestWeight = weight;
                        bestMatchedSpans = matchedSpans;
                    }
                }

                return (bestWeight, bestMatchedSpans);
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