// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
    private sealed class RegexPatternMatcher(
        Regex caseInsensitiveRegex, Regex caseSensitiveRegex, bool includeMatchedSpans, CultureInfo? culture)
        : PatternMatcher(includeMatchedSpans, culture)
    {
        /// <summary>
        /// Tries to create a <see cref="RegexPatternMatcher"/> for the given pattern. Returns
        /// <see langword="null"/> if the pattern is not a valid .NET regex (e.g. unclosed groups,
        /// invalid escape sequences). Callers should fall back to the standard search path.
        /// </summary>
        public static RegexPatternMatcher? TryCreate(string pattern, bool includeMatchedSpans, CultureInfo? culture = null)
        {
            try
            {
                // IgnorePatternWhitespace lets users write readable patterns like `( Read | Write ) Line`
                // without needing manual stripping. Symbol names never contain whitespace, so this is safe.
                //
                // Both regexes are compiled to native code on first use, amortizing the compilation
                // cost across the many candidate strings checked during a single NavigateTo search.
                const RegexOptions commonOptions = RegexOptions.CultureInvariant | RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace;

                // Ensure that regexes that run too long (e.g. due to catastrophic backtracking) always terminate.
                var timeout = TimeSpan.FromSeconds(1);

                var caseInsensitive = new Regex(pattern, commonOptions | RegexOptions.IgnoreCase, timeout);
                var caseSensitive = new Regex(pattern, commonOptions, timeout);
                return new RegexPatternMatcher(caseInsensitive, caseSensitive, includeMatchedSpans, culture);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        protected override bool AddMatchesWorker(string candidate, ref TemporaryArray<PatternMatch> matches)
        {
            var caseInsensitiveMatch = caseInsensitiveRegex.Match(candidate);
            if (!caseInsensitiveMatch.Success)
                return false;

            // Run the case-sensitive regex to determine categorization. The UI ranks
            // case-sensitive matches higher, so we report it when both match.
            var caseSensitiveMatch = caseSensitiveRegex.Match(candidate);
            var isCaseSensitive = caseSensitiveMatch.Success;

            var bestMatch = isCaseSensitive ? caseSensitiveMatch : caseInsensitiveMatch;

            // Regex matching intentionally uses a simplified two-tier kind system (Exact vs
            // NonLowercaseSubstring) rather than the full CamelCase/Prefix/Substring hierarchy.
            // The standard hierarchy relies on word-boundary analysis that doesn't apply to
            // arbitrary regex matches.
            var kind = bestMatch.Length == candidate.Length && bestMatch.Index == 0
                ? PatternMatchKind.Exact
                : PatternMatchKind.NonLowercaseSubstring;

            var matchedSpan = GetMatchedSpan(bestMatch.Index, bestMatch.Length);

            matches.Add(new PatternMatch(kind, punctuationStripped: false, isCaseSensitive, matchedSpan));
            return true;
        }
    }
}
