// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.PatternMatching;

internal abstract partial class PatternMatcher
{
    /// <summary>
    /// A pattern matcher that composes multiple sub-matchers, trying each in order and
    /// short-circuiting on the first successful match.
    /// </summary>
    private sealed class CompoundPatternMatcher : PatternMatcher
    {
        private readonly ArrayBuilder<PatternMatcher> _matchers;

        public CompoundPatternMatcher(ReadOnlySpan<PatternMatcher> matchers)
            : base(includeMatchedSpans: false, culture: null)
        {
            _matchers = ArrayBuilder<PatternMatcher>.GetInstance(matchers.Length);
            foreach (var matcher in matchers)
                _matchers.Add(matcher);
        }

        public override void Dispose()
        {
            foreach (var matcher in _matchers)
                matcher.Dispose();

            _matchers.Free();
        }

        protected override bool AddMatchesWorker(string candidate, ref TemporaryArray<PatternMatch> matches)
        {
            foreach (var matcher in _matchers)
            {
                if (matcher.AddMatches(candidate, ref matches))
                    return true;
            }

            return false;
        }
    }
}
