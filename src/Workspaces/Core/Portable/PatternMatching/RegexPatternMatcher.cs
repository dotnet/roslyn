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
    internal sealed class RegexPatternMatcher : PatternMatcher
    {
        private readonly Regex _caseInsensitiveRegex;
        private readonly Regex _caseSensitiveRegex;

        private RegexPatternMatcher(Regex caseInsensitiveRegex, Regex caseSensitiveRegex, bool includeMatchedSpans, CultureInfo? culture)
            : base(includeMatchedSpans, culture)
        {
            _caseInsensitiveRegex = caseInsensitiveRegex;
            _caseSensitiveRegex = caseSensitiveRegex;
        }

        /// <summary>
        /// Tries to create a <see cref="RegexPatternMatcher"/> for the given pattern. Returns
        /// <see langword="null"/> if the pattern is not a valid .NET regex (e.g. unclosed groups,
        /// invalid escape sequences). Callers should fall back to the standard search path.
        /// </summary>
        public static RegexPatternMatcher? TryCreate(string pattern, bool includeMatchedSpans, CultureInfo? culture = null)
        {
            // Symbol names never contain whitespace, so strip it from the pattern to allow
            // users to write readable regexes like `( Read | Write ) Line`.
            var strippedPattern = StripWhitespace(pattern);

            try
            {
                // Both regexes are compiled to native code on first use, amortizing the compilation
                // cost across the many candidate strings checked during a single NavigateTo search.
                var ci = new Regex(strippedPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
                var cs = new Regex(strippedPattern, RegexOptions.CultureInvariant | RegexOptions.Compiled);
                return new RegexPatternMatcher(ci, cs, includeMatchedSpans, culture);
            }
            catch (ArgumentException)
            {
                // Pattern is syntactically invalid for System.Text.RegularExpressions.
                return null;
            }
        }

        private static string StripWhitespace(string pattern)
        {
            // Fast path: most patterns have no whitespace.
            if (pattern.IndexOf(' ') < 0)
                return pattern;

            return pattern.Replace(" ", "");
        }

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
