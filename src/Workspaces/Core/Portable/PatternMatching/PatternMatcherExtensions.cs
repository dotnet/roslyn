// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.PatternMatching
{
    internal static class PatternMatcherExtensions
    {
        public static PatternMatch? GetFirstMatch(this PatternMatcher matcher, string candidate)
        {
            using var _ = ArrayBuilder<PatternMatch>.GetInstance(out var matches);
            matcher.AddMatches(candidate, matches);
            return matches.Any() ? (PatternMatch?)matches.First() : null;
        }

        public static bool Matches(this PatternMatcher matcher, string candidate)
            => matcher.GetFirstMatch(candidate) != null;
    }
}
