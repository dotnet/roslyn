// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Shared.Collections;

namespace Microsoft.CodeAnalysis.PatternMatching;

internal static class PatternMatcherExtensions
{
    extension(PatternMatcher matcher)
    {
        public PatternMatch? GetFirstMatch(string? candidate)
        {
            using var matches = TemporaryArray<PatternMatch>.Empty;
            matcher.AddMatches(candidate, ref matches.AsRef());
            return matches.Count > 0 ? matches[0] : null;
        }

        public bool Matches([NotNullWhen(true)] string? candidate)
            => matcher.GetFirstMatch(candidate) != null;
    }
}
