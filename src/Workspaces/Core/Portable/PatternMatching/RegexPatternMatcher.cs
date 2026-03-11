// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.PatternMatching;

internal abstract partial class PatternMatcher
{
    /// <summary>
    /// A pattern matcher that uses a compiled <see cref="Regex"/> for matching. Performs
    /// case-insensitive finding (so "readline" matches "ReadLine"), but categorizes matches
    /// as case-sensitive when the case-sensitive regex also matches. This is consistent with
    /// how standard NavigateTo works: find broadly, then rank case-sensitive matches higher.
    /// </summary>
    internal sealed class RegexPatternMatcher : PatternMatcher
    {
        private readonly Regex _caseInsensitiveRegex;
        private readonly Regex _caseSensitiveRegex;

        public RegexPatternMatcher(string pattern, bool includeMatchedSpans)
            : base(includeMatchedSpans, culture: null)
        {
            _caseInsensitiveRegex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            _caseSensitiveRegex = new Regex(pattern, RegexOptions.CultureInvariant);
        }

        protected override bool AddMatchesWorker(string candidate, ref TemporaryArray<PatternMatch> matches)
        {
            var ciMatch = _caseInsensitiveRegex.Match(candidate);
            if (!ciMatch.Success)
                return false;

            var csMatch = _caseSensitiveRegex.Match(candidate);
            var isCaseSensitive = csMatch.Success;

            var bestMatch = isCaseSensitive ? csMatch : ciMatch;
            var kind = bestMatch.Length == candidate.Length && bestMatch.Index == 0
                ? PatternMatchKind.Exact
                : PatternMatchKind.NonLowercaseSubstring;

            var matchedSpan = GetMatchedSpan(bestMatch.Index, bestMatch.Length);

            matches.Add(new PatternMatch(kind, punctuationStripped: false, isCaseSensitive, matchedSpan));
            return true;
        }
    }
}
