// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Diagnostics;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.PatternMatching;

internal partial class PatternMatcher
{
    private readonly struct CamelCaseResult
    {
        public readonly bool FromStart;
        public readonly bool Contiguous;
        public readonly int MatchCount;
        public readonly ArrayBuilder<TextSpan> MatchedSpansInReverse;

        public CamelCaseResult(bool fromStart, bool contiguous, int matchCount, ArrayBuilder<TextSpan> matchedSpansInReverse)
        {
            FromStart = fromStart;
            Contiguous = contiguous;
            MatchCount = matchCount;
            MatchedSpansInReverse = matchedSpansInReverse;

            Debug.Assert(matchedSpansInReverse == null || matchedSpansInReverse.Count == matchCount);
        }

        public void Free()
            => MatchedSpansInReverse?.Free();

        public CamelCaseResult WithFromStart(bool fromStart)
            => new(fromStart, Contiguous, MatchCount, MatchedSpansInReverse);

        public CamelCaseResult WithAddedMatchedSpan(TextSpan value)
        {
            MatchedSpansInReverse?.Add(value);
            return new CamelCaseResult(FromStart, Contiguous, MatchCount + 1, MatchedSpansInReverse);
        }
    }

    private static PatternMatchKind GetCamelCaseKind(CamelCaseResult result, in TemporaryArray<TextSpan> candidateHumps)
    {
        var toEnd = result.MatchCount == candidateHumps.Count;
        if (result.FromStart)
        {
            if (result.Contiguous)
            {
                // We contiguously matched humps from the start of this candidate.  If we 
                // matched all the humps, then this was an exact match, otherwise it was a 
                // contiguous prefix match
                return toEnd
                    ? PatternMatchKind.CamelCaseExact
                    : PatternMatchKind.CamelCasePrefix;
            }
            else
            {
                return PatternMatchKind.CamelCaseNonContiguousPrefix;
            }
        }
        else
        {
            // We didn't match from the start.  Distinguish between a match whose humps are all
            // contiguous, and one that isn't.
            return result.Contiguous
                ? PatternMatchKind.CamelCaseSubstring
                : PatternMatchKind.CamelCaseNonContiguousSubstring;
        }
    }
}
