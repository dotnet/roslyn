// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
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
    internal sealed class RegexPatternMatcher(string pattern, bool includeMatchedSpans, CultureInfo? culture = null)
        : PatternMatcher(includeMatchedSpans, culture)
    {
        // Both regexes are compiled to native code on first use, amortizing the compilation
        // cost across the many candidate strings checked during a single NavigateTo search.
        private readonly Regex _caseInsensitiveRegex = new(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private readonly Regex _caseSensitiveRegex = new(pattern, RegexOptions.CultureInvariant | RegexOptions.Compiled);

        protected override bool AddMatchesWorker(string candidate, ref TemporaryArray<PatternMatch> matches)
        {
            var ciMatch = _caseInsensitiveRegex.Match(candidate);
            if (!ciMatch.Success)
                return false;

            // Run the case-sensitive regex to determine categorization. The UI ranks
            // case-sensitive matches higher, so we report it when both match.
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
