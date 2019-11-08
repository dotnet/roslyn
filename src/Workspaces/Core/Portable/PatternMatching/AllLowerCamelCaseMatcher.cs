// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.PatternMatching
{
    internal partial class PatternMatcher
    {
        /// <summary>
        /// Encapsulated matches responsible for matching an all lowercase pattern against
        /// a candidate using CamelCase matching. i.e. this code is responsible for finding the
        /// match between "cofipro" and "CodeFixProvider". 
        /// </summary>
        private struct AllLowerCamelCaseMatcher
        {
            private readonly bool _includeMatchedSpans;
            private readonly string _candidate;
            private readonly ArrayBuilder<TextSpan> _candidateHumps;
            private readonly TextChunk _patternChunk;
            private readonly string _patternText;
            private readonly TextInfo _textInfo;

            public AllLowerCamelCaseMatcher(
                bool includeMatchedSpans, string candidate,
                ArrayBuilder<TextSpan> candidateHumps, TextChunk patternChunk,
                TextInfo textInfo)
            {
                _includeMatchedSpans = includeMatchedSpans;
                _candidate = candidate;
                _candidateHumps = candidateHumps;
                _patternChunk = patternChunk;
                _patternText = _patternChunk.Text;
                _textInfo = textInfo;
            }

            /// <summary>
            /// Returns null if no match was found, 1 if a contiguous match was found, 2 if a 
            /// match as found that starts at the beginning of the candidate, and 3 if a contiguous
            /// match was found that starts at the beginning of the candidate.
            /// </summary>
            public PatternMatchKind? TryMatch(out ImmutableArray<TextSpan> matchedSpans)
            {
                // We have something like cofipro and we want to match CodeFixProvider.  
                //
                // Note that this is incredibly ambiguous.  We'd also want this to match 
                // CorporateOfficePartsRoom So, for example, if we were to consume the "co" 
                // as matching "Corporate", then "f" wouldn't match any camel hump.  So we
                // basically have to branch out and try all options at every character
                // in the pattern chunk.

                var result = TryMatch(
                   patternIndex: 0, candidateHumpIndex: 0, contiguous: null);

                if (result == null)
                {
                    matchedSpans = ImmutableArray<TextSpan>.Empty;
                    return null;
                }

                matchedSpans = _includeMatchedSpans && result.Value.MatchedSpansInReverse != null
                    ? new NormalizedTextSpanCollection(result.Value.MatchedSpansInReverse).ToImmutableArray()
                    : ImmutableArray<TextSpan>.Empty;

                result?.Free();
                return GetKind(result.Value);
            }

            private PatternMatchKind GetKind(CamelCaseResult result)
                => PatternMatcher.GetCamelCaseKind(result, _candidateHumps);

            private CamelCaseResult? TryMatch(
                int patternIndex, int candidateHumpIndex, bool? contiguous)
            {
                if (patternIndex == _patternText.Length)
                {
                    // We hit the end.  So we were able to match against this candidate.
                    // We are contiguous if our contiguous tracker was not set to false.
                    var matchedSpansInReverse = _includeMatchedSpans ? ArrayBuilder<TextSpan>.GetInstance() : null;
                    return new CamelCaseResult(
                        fromStart: false,
                        contiguous: contiguous != false,
                        matchCount: 0,
                        matchedSpansInReverse: matchedSpansInReverse);
                }

                var bestResult = default(CamelCaseResult?);

                // Look for a hump in the candidate that matches the current letter we're on.
                var patternCharacter = _patternText[patternIndex];
                for (int humpIndex = candidateHumpIndex, n = _candidateHumps.Count; humpIndex < n; humpIndex++)
                {
                    // If we've been contiguous, but we jumped past a hump, then we're no longer contiguous.
                    if (contiguous is { HasValue: true, Value: true })
                    {
                        contiguous = humpIndex == candidateHumpIndex;
                    }

                    var candidateHump = _candidateHumps[humpIndex];
                    if (ToLower(_candidate[candidateHump.Start], _textInfo) == patternCharacter)
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
                        var localContiguous = contiguous == null ? true : contiguous.Value;

                        var result = TryConsumePatternOrMatchNextHump(
                            patternIndex, humpIndex, localContiguous);

                        if (result == null)
                        {
                            continue;
                        }

                        if (UpdateBestResultIfBetter(result.Value, ref bestResult, matchSpanToAdd: null))
                        {
                            // We found the best result so far.  We can stop immediately.
                            break;
                        }
                    }
                }

                return bestResult;
            }

            private static char ToLower(char v, TextInfo textInfo)
            {
                return IsAscii(v)
                    ? ToLowerAsciiInvariant(v)
                    : textInfo.ToLower(v);
            }

            private static bool IsAscii(char v)
                => v < 0x80;

            private static char ToLowerAsciiInvariant(char c)
                => 'A' <= c && c <= 'Z'
                    ? (char)(c | 0x20)
                    : c;

            private CamelCaseResult? TryConsumePatternOrMatchNextHump(
                int patternIndex, int humpIndex, bool contiguous)
            {
                var bestResult = default(CamelCaseResult?);

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

                    var resultOpt = TryMatch(
                        patternIndex + possibleHumpMatchLength, humpIndex + 1, contiguous);

                    if (resultOpt == null)
                    {
                        // Didn't match.  Try the next longer pattern chunk.
                        continue;
                    }

                    var result = resultOpt.Value;
                    // If this is our first hump add a 'from start' bonus.
                    if (humpIndex == 0)
                    {
                        result = result.WithFromStart(true);
                    }

                    // This is the span of the hump of the candidate we matched.
                    var matchSpanToAdd = new TextSpan(candidateHump.Start, possibleHumpMatchLength);
                    if (UpdateBestResultIfBetter(result, ref bestResult, matchSpanToAdd))
                    {
                        // We found the best result so far.  We can stop immediately.
                        break;
                    }
                }

                return bestResult;
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
                CamelCaseResult result, ref CamelCaseResult? bestResult, TextSpan? matchSpanToAdd)
            {
                if (matchSpanToAdd != null)
                {
                    result = result.WithAddedMatchedSpan(matchSpanToAdd.Value);
                }

                if (!IsBetter(result, bestResult))
                {
                    // Even though we matched this current candidate hump we failed to match
                    // the remainder of the pattern.  Continue to the next candidate hump
                    // to see if our pattern character will match it and potentially succeed.
                    result.Free();

                    // We need to keep going.
                    return false;
                }

                // This was result was better than whatever previous best result we had was.
                // Free and overwrite the existing best results, and keep going.
                bestResult?.Free();
                bestResult = result;

                // We found a path that allowed us to match everything contiguously
                // from the beginning.  This is the best match possible.  So we can
                // just break out now and return this result.
                return GetKind(result) == PatternMatchKind.CamelCaseExact;
            }

            private bool IsBetter(CamelCaseResult result, CamelCaseResult? currentBestResult)
            {
                if (currentBestResult == null)
                {
                    // We have no current best.  So this result is the best.
                    return true;
                }

                return GetKind(result) < GetKind(currentBestResult.Value);
            }

            private bool LowercaseSubstringsMatch(
                string s1, int start1, string s2, int start2, int length)
            {
                var textInfo = _textInfo;
                for (var i = 0; i < length; i++)
                {
                    if (ToLower(s1[start1 + i], textInfo) != ToLower(s2[start2 + i], textInfo))
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
