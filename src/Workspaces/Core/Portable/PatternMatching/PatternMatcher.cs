// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.CodeAnalysis.Shared;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.PatternMatching
{
    /// <summary>
    /// The pattern matcher is not thread-safe.  Do not use the pattern matcher across mutiple threads concurrently.  It
    /// also keeps an internal cache of data for speeding up operations.  As such, it should be disposed when done to
    /// release the cached data back. and release the matcher appropriately once you no longer need it. Also, while the
    /// pattern matcher is culture aware, it uses the culture specified in the constructor.
    /// </summary>
    internal abstract partial class PatternMatcher : IDisposable
    {
        private static readonly char[] s_dotCharacterArray = ['.'];

        public const int NoBonus = 0;
        public const int CamelCaseContiguousBonus = 1;
        public const int CamelCaseMatchesFromStartBonus = 2;
        public const int CamelCaseMaxWeight = CamelCaseContiguousBonus + CamelCaseMatchesFromStartBonus;

        private readonly bool _includeMatchedSpans;
        private readonly bool _allowFuzzyMatching;

        // PERF: Cache the culture's compareInfo to avoid the overhead of asking for them repeatedly in inner loops
        private readonly CompareInfo _compareInfo;
        private readonly TextInfo _textInfo;

        private bool _invalidPattern;

        /// <summary>
        /// Construct a new PatternMatcher using the specified culture.
        /// </summary>
        /// <param name="culture">The culture to use for string searching and comparison.</param>
        /// <param name="includeMatchedSpans">Whether or not the matching parts of the candidate should be supplied in results.</param>
        /// <param name="allowFuzzyMatching">Whether or not close matches should count as matches.</param>
        protected PatternMatcher(
            bool includeMatchedSpans,
            CultureInfo? culture,
            bool allowFuzzyMatching = false)
        {
            culture ??= CultureInfo.CurrentCulture;

            _compareInfo = culture.CompareInfo;
            _textInfo = culture.TextInfo;

            _includeMatchedSpans = includeMatchedSpans;
            _allowFuzzyMatching = allowFuzzyMatching;
        }

        public virtual void Dispose()
        {
        }

        public static PatternMatcher CreatePatternMatcher(
            string pattern,
            CultureInfo? culture = null,
            bool includeMatchedSpans = false,
            bool allowFuzzyMatching = false)
        {
            return new SimplePatternMatcher(pattern, culture, includeMatchedSpans, allowFuzzyMatching);
        }

        public static PatternMatcher CreateContainerPatternMatcher(
            string[] patternParts,
            char[] containerSplitCharacters,
            bool includeMatchedSpans = false,
            CultureInfo? culture = null,
            bool allowFuzzyMatching = false)
        {
            return new ContainerPatternMatcher(
                patternParts, containerSplitCharacters, includeMatchedSpans, culture, allowFuzzyMatching);
        }

        public static PatternMatcher CreateDotSeparatedContainerMatcher(
            string pattern,
            bool includeMatchedSpans = false,
            CultureInfo? culture = null,
            bool allowFuzzyMatching = false)
        {
            return CreateContainerPatternMatcher(
                pattern.Split(s_dotCharacterArray, StringSplitOptions.RemoveEmptyEntries),
                s_dotCharacterArray, includeMatchedSpans, culture, allowFuzzyMatching);
        }

        internal static (string name, string? containerOpt) GetNameAndContainer(string pattern)
        {
            var dotIndex = pattern.LastIndexOf('.');
            var containsDots = dotIndex >= 0;
            return containsDots
                ? (name: pattern[(dotIndex + 1)..], containerOpt: pattern[..dotIndex])
                : (name: pattern, containerOpt: null);
        }

        public abstract bool AddMatches(string? candidate, ref TemporaryArray<PatternMatch> matches);

        private bool SkipMatch([NotNullWhen(false)] string? candidate)
            => _invalidPattern || string.IsNullOrWhiteSpace(candidate);

        private static bool ContainsUpperCaseLetter(string pattern)
        {
            // Expansion of "foreach(char ch in pattern)" to avoid a CharEnumerator allocation
            for (var i = 0; i < pattern.Length; i++)
            {
                if (char.IsUpper(pattern[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private PatternMatch? MatchPatternChunk(
            string candidate,
            ref TextChunk patternChunk,
            bool punctuationStripped,
            bool fuzzyMatch)
        {
            return fuzzyMatch
                ? FuzzyMatchPatternChunk(candidate, ref patternChunk, punctuationStripped)
                : NonFuzzyMatchPatternChunk(candidate, patternChunk, punctuationStripped);
        }

        private static PatternMatch? FuzzyMatchPatternChunk(
            string candidate,
            ref TextChunk patternChunk,
            bool punctuationStripped)
        {
            Contract.ThrowIfTrue(patternChunk.SimilarityChecker.IsDefault);
            if (patternChunk.SimilarityChecker.AreSimilar(candidate))
            {
                return new PatternMatch(
                    PatternMatchKind.Fuzzy, punctuationStripped, isCaseSensitive: false, matchedSpan: null);
            }

            return null;
        }

        private PatternMatch? NonFuzzyMatchPatternChunk(
            string candidate,
            in TextChunk patternChunk,
            bool punctuationStripped)
        {
            var candidateLength = candidate.Length;

            var caseInsensitiveIndex = _compareInfo.IndexOf(candidate, patternChunk.Text, CompareOptions.IgnoreCase);
            if (caseInsensitiveIndex == 0)
            {
                // We found the pattern at the start of the candidate.  This is either an exact or
                // prefix match. 

                if (patternChunk.Text.Length == candidateLength)
                {
                    // Lengths were the same, this is either a case insensitive or sensitive exact match.
                    return new PatternMatch(
                        PatternMatchKind.Exact, punctuationStripped, isCaseSensitive: candidate == patternChunk.Text,
                        matchedSpan: GetMatchedSpan(0, candidateLength));
                }
                else
                {
                    // Lengths were the same, this is either a case insensitive or sensitive prefix match.
                    return new PatternMatch(
                        PatternMatchKind.Prefix, punctuationStripped, isCaseSensitive: _compareInfo.IsPrefix(candidate, patternChunk.Text),
                        matchedSpan: GetMatchedSpan(0, patternChunk.Text.Length));
                }
            }

            using var candidateHumps = TemporaryArray<TextSpan>.Empty;

            var patternIsLowercase = patternChunk.IsLowercase;
            if (caseInsensitiveIndex > 0)
            {
                // We found the pattern somewhere in the candidate.  This could be a substring match.
                // However, we don't want to be overaggressive in returning just any substring results.
                // So do a few more checks to make sure this is a good result.

                if (!patternIsLowercase)
                {
                    // Pattern contained uppercase letters.  This is a strong indication from the
                    // user that they expect the same letters to be uppercase in the result.  As 
                    // such, only return this if we can find this pattern exactly in the candidate.

                    var caseSensitiveIndex = _compareInfo.IndexOf(candidate, patternChunk.Text, CompareOptions.None);
                    if (caseSensitiveIndex > 0)
                    {
                        if (char.IsUpper(candidate[caseInsensitiveIndex]))
                        {
                            return new PatternMatch(
                                PatternMatchKind.StartOfWordSubstring, punctuationStripped, isCaseSensitive: true,
                                matchedSpan: GetMatchedSpan(caseInsensitiveIndex, patternChunk.Text.Length));
                        }
                        else
                        {
                            return new PatternMatch(
                                PatternMatchKind.NonLowercaseSubstring, punctuationStripped, isCaseSensitive: true,
                                matchedSpan: GetMatchedSpan(caseSensitiveIndex, patternChunk.Text.Length));
                        }
                    }
                }
                else
                {
                    // Pattern was all lowercase.  This can lead to lots of hits.  For example, "bin" in
                    // "CombineUnits".  Instead, we want it to match "Operator[|Bin|]ary" first rather than
                    // Com[|bin|]eUnits

                    // If the lowercase search string matched what looks to be the start of a word then that's a
                    // reasonable hit. This is equivalent to 'bin' matching 'Operator[|Bin|]ary'
                    if (char.IsUpper(candidate[caseInsensitiveIndex]))
                    {
                        return new PatternMatch(PatternMatchKind.StartOfWordSubstring, punctuationStripped,
                            isCaseSensitive: false,
                            matchedSpan: GetMatchedSpan(caseInsensitiveIndex, patternChunk.Text.Length));
                    }

                    // Now do the more expensive check to see if we're at the start of a word.  This is to catch
                    // word matches like CombineBinary.  We want to find the hit against '[|Bin|]ary' not
                    // 'Com[|bin|]e'
                    StringBreaker.AddWordParts(candidate, ref candidateHumps.AsRef());
                    for (int i = 0, n = candidateHumps.Count; i < n; i++)
                    {
                        var hump = TextSpan.FromBounds(candidateHumps[i].Start, candidateLength);
                        if (PartStartsWith(candidate, hump, patternChunk.Text, CompareOptions.IgnoreCase))
                        {
                            return new PatternMatch(PatternMatchKind.StartOfWordSubstring, punctuationStripped,
                                isCaseSensitive: PartStartsWith(candidate, hump, patternChunk.Text, CompareOptions.None),
                                matchedSpan: GetMatchedSpan(hump.Start, patternChunk.Text.Length));
                        }
                    }
                }
            }

            // Didn't have an exact/prefix match, or a high enough quality substring match.
            // See if we can find a camel case match.
            if (candidateHumps.Count == 0)
                StringBreaker.AddWordParts(candidate, ref candidateHumps.AsRef());

            // Didn't have an exact/prefix match, or a high enough quality substring match.
            // See if we can find a camel case match.  
            var match = TryCamelCaseMatch(candidate, patternChunk, punctuationStripped, patternIsLowercase, candidateHumps);
            if (match != null)
                return match;

            // If pattern was all lowercase, we allow it to match an all lowercase section of the candidate.  But
            // only after we've tried all other forms first.  This is the weakest of all matches.  For example, if
            // user types 'bin' we want to match 'OperatorBinary' (start of word) or 'BinaryInformationNode' (camel
            // humps) before matching 'Combine'.
            // 
            // We only do this for strings longer than three characters to avoid too many false positives when the
            // user has only barely started writing a word.
            if (patternIsLowercase && caseInsensitiveIndex > 0 && patternChunk.Text.Length >= 3)
            {
                var caseSensitiveIndex = _compareInfo.IndexOf(candidate, patternChunk.Text, CompareOptions.None);
                if (caseSensitiveIndex > 0)
                {
                    return new PatternMatch(
                        PatternMatchKind.LowercaseSubstring, punctuationStripped, isCaseSensitive: true,
                        matchedSpan: GetMatchedSpan(caseSensitiveIndex, patternChunk.Text.Length));
                }
            }

            return null;
        }

        private TextSpan? GetMatchedSpan(int start, int length)
            => _includeMatchedSpans ? new TextSpan(start, length) : null;

        private static bool ContainsSpaceOrAsterisk(string text)
        {
            for (var i = 0; i < text.Length; i++)
            {
                var ch = text[i];
                if (ch is ' ' or '*')
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Internal helper for MatchPatternInternal
        /// </summary>
        /// <remarks>
        /// PERF: Designed to minimize allocations in common cases.
        /// If there's no match, then null is returned.
        /// If there's a single match, or the caller only wants the first match, then it is returned (as a Nullable)
        /// If there are multiple matches, and the caller wants them all, then a List is allocated.
        /// </remarks>
        /// <param name="candidate">The word being tested.</param>
        /// <param name="segment">The segment of the pattern to check against the candidate.</param>
        /// <param name="matches">The result array to place the matches in.</param>
        /// <param name="fuzzyMatch">If a fuzzy match should be performed</param>
        /// <returns>If there's only one match, then the return value is that match. Otherwise it is null.</returns>
        private bool MatchPatternSegment(
            string candidate,
            ref PatternSegment segment,
            ref TemporaryArray<PatternMatch> matches,
            bool fuzzyMatch)
        {
            if (fuzzyMatch && !_allowFuzzyMatching)
            {
                return false;
            }

            // First check if the segment matches as is.  This is also useful if the segment contains
            // characters we would normally strip when splitting into parts that we also may want to
            // match in the candidate.  For example if the segment is "@int" and the candidate is
            // "@int", then that will show up as an exact match here.
            //
            // Note: if the segment contains a space or an asterisk then we must assume that it's a
            // multi-word segment.
            if (!ContainsSpaceOrAsterisk(segment.TotalTextChunk.Text))
            {
                var match = MatchPatternChunk(
                    candidate, ref segment.TotalTextChunk, punctuationStripped: false, fuzzyMatch: fuzzyMatch);
                if (match != null)
                {
                    matches.Add(match.Value);
                    return true;
                }
            }

            // The logic for pattern matching is now as follows:
            //
            // 1) Break the segment passed in into words.  Breaking is rather simple and a
            //    good way to think about it that if gives you all the individual alphanumeric words
            //    of the pattern.
            //
            // 2) For each word try to match the word against the candidate value.
            //
            // 3) Matching logic is outlined in NonFuzzyMatchPatternChunk. It's not repeated here to
            //    prevent having multiple places to keep up to date.
            //
            // Only if all words have some sort of match is the pattern considered matched.

            // Special case a simple pattern (alpha-numeric with no spaces).  This is the common
            // case and we want to prevent unnecessary overhead.
            var subWordTextChunks = segment.SubWordTextChunks;

            if (subWordTextChunks.Length == 1)
            {
                var result = MatchPatternChunk(
                    candidate, ref subWordTextChunks[0], punctuationStripped: true, fuzzyMatch: fuzzyMatch);
                if (result == null)
                {
                    return false;
                }

                matches.Add(result.Value);
                return true;
            }
            else
            {
                using var tempMatches = TemporaryArray<PatternMatch>.Empty;

                for (int i = 0, n = subWordTextChunks.Length; i < n; i++)
                {
                    // Try to match the candidate with this word
                    var result = MatchPatternChunk(
                        candidate, ref subWordTextChunks[i], punctuationStripped: true, fuzzyMatch: fuzzyMatch);
                    if (result == null)
                        return false;

                    tempMatches.Add(result.Value);
                }

                matches.AddRange(tempMatches);
                return tempMatches.Count > 0;
            }
        }

        private static bool IsWordChar(char ch)
            => char.IsLetterOrDigit(ch) || ch == '_';

        /// <summary>
        /// Do the two 'parts' match? i.e. Does the candidate part start with the pattern part?
        /// </summary>
        /// <param name="candidate">The candidate text</param>
        /// <param name="candidatePart">The span within the <paramref name="candidate"/> text</param>
        /// <param name="pattern">The pattern text</param>
        /// <param name="patternPart">The span within the <paramref name="pattern"/> text</param>
        /// <param name="compareOptions">Options for doing the comparison (case sensitive or not)</param>
        /// <returns>True if the span identified by <paramref name="candidatePart"/> within <paramref name="candidate"/> starts with
        /// the span identified by <paramref name="patternPart"/> within <paramref name="pattern"/>.</returns>
        private bool PartStartsWith(string candidate, TextSpan candidatePart, string pattern, TextSpan patternPart, CompareOptions compareOptions)
        {
            if (patternPart.Length > candidatePart.Length)
            {
                // Pattern part is longer than the candidate part. There can never be a match.
                return false;
            }

            return _compareInfo.Compare(
                candidate, candidatePart.Start, patternPart.Length,
                pattern, patternPart.Start, patternPart.Length, compareOptions) == 0;
        }

        /// <summary>
        /// Does the given part start with the given pattern?
        /// </summary>
        /// <param name="candidate">The candidate text</param>
        /// <param name="candidatePart">The span within the <paramref name="candidate"/> text</param>
        /// <param name="pattern">The pattern text</param>
        /// <param name="compareOptions">Options for doing the comparison (case sensitive or not)</param>
        /// <returns>True if the span identified by <paramref name="candidatePart"/> within <paramref name="candidate"/> starts with <paramref name="pattern"/></returns>
        private bool PartStartsWith(string candidate, TextSpan candidatePart, string pattern, CompareOptions compareOptions)
            => PartStartsWith(candidate, candidatePart, pattern, new TextSpan(0, pattern.Length), compareOptions);

        private PatternMatch? TryCamelCaseMatch(
            string candidate,
            in TextChunk patternChunk,
            bool punctuationStripped,
            bool isLowercase,
            in TemporaryArray<TextSpan> candidateHumps)
        {
            if (isLowercase)
            {
                //   e) If the word was entirely lowercase, then attempt a special lower cased camel cased 
                //      match.  i.e. cofipro would match CodeFixProvider.
                var camelCaseKind = TryAllLowerCamelCaseMatch(
                    candidate, candidateHumps, patternChunk, out var matchedSpans);
                if (camelCaseKind.HasValue)
                {
                    return new PatternMatch(
                        camelCaseKind.Value, punctuationStripped, isCaseSensitive: false,
                        matchedSpans: matchedSpans);
                }
            }
            else
            {
                //   f) If the word was not entirely lowercase, then attempt a normal camel cased match.
                //      i.e. CoFiPro would match CodeFixProvider, but CofiPro would not.  
                if (patternChunk.PatternHumps.Count > 0)
                {
                    // PERF: This can be called thousands of times per completion session with only a handful of matches found.
                    // Checking for case insensitive initially reduces the TryUpperCaseCamelCaseMatch call count to 1 for the
                    // non-matching candidates, but increases the call count to 2 for the much less frequent matching candidates.
                    var camelCaseKindIgnoreCase = TryUpperCaseCamelCaseMatch(candidate, candidateHumps, patternChunk, CompareOptions.IgnoreCase, out var matchedSpansIgnoreCase);
                    if (camelCaseKindIgnoreCase.HasValue)
                    {
                        var camelCaseKind = TryUpperCaseCamelCaseMatch(candidate, candidateHumps, patternChunk, CompareOptions.None, out var matchedSpans);
                        if (camelCaseKind.HasValue)
                        {
                            return new PatternMatch(
                                camelCaseKind.Value, punctuationStripped, isCaseSensitive: true,
                                matchedSpans: matchedSpans);
                        }

                        return new PatternMatch(
                            camelCaseKindIgnoreCase.Value, punctuationStripped, isCaseSensitive: false,
                            matchedSpans: matchedSpansIgnoreCase);
                    }
                }
            }

            return null;
        }

        private PatternMatchKind? TryAllLowerCamelCaseMatch(
            string candidate,
            in TemporaryArray<TextSpan> candidateHumps,
            in TextChunk patternChunk,
            out ImmutableArray<TextSpan> matchedSpans)
        {
            var matcher = new AllLowerCamelCaseMatcher(_includeMatchedSpans, candidate, patternChunk.Text, _textInfo);
            return matcher.TryMatch(candidateHumps, out matchedSpans);
        }

        private PatternMatchKind? TryUpperCaseCamelCaseMatch(
            string candidate,
            in TemporaryArray<TextSpan> candidateHumps,
            in TextChunk patternChunk,
            CompareOptions compareOption,
            out ImmutableArray<TextSpan> matchedSpans)
        {
            ref readonly var patternHumps = ref patternChunk.PatternHumps;

            // Note: we may have more pattern parts than candidate parts.  This is because multiple
            // pattern parts may match a candidate part.  For example "SiUI" against "SimpleUI".
            // We'll have 3 pattern parts Si/U/I against two candidate parts Simple/UI.  However, U
            // and I will both match in UI. 

            var currentCandidateHump = 0;
            var currentPatternHump = 0;
            int? firstMatch = null;
            bool? contiguous = null;

            var patternHumpCount = patternHumps.Count;
            var candidateHumpCount = candidateHumps.Count;

            using var matchSpans = TemporaryArray<TextSpan>.Empty;

            while (true)
            {
                // Let's consider our termination cases
                if (currentPatternHump == patternHumpCount)
                {
                    Debug.Assert(firstMatch.HasValue);
                    Debug.Assert(contiguous.HasValue);

                    var matchCount = matchSpans.Count;
                    matchedSpans = _includeMatchedSpans
                        ? new NormalizedTextSpanCollection(matchSpans.ToImmutableAndClear()).ToImmutableArray()
                        : ImmutableArray<TextSpan>.Empty;

                    var camelCaseResult = new CamelCaseResult(firstMatch == 0, contiguous.Value, matchCount, null);
                    return GetCamelCaseKind(camelCaseResult, candidateHumps);
                }
                else if (currentCandidateHump == candidateHumpCount)
                {
                    // No match, since we still have more of the pattern to hit
                    matchedSpans = ImmutableArray<TextSpan>.Empty;
                    return null;
                }

                var candidateHump = candidateHumps[currentCandidateHump];
                var gotOneMatchThisCandidate = false;

                // Consider the case of matching SiUI against SimpleUIElement. The candidate parts
                // will be Simple/UI/Element, and the pattern parts will be Si/U/I.  We'll match 'Si'
                // against 'Simple' first.  Then we'll match 'U' against 'UI'. However, we want to
                // still keep matching pattern parts against that candidate part. 
                for (; currentPatternHump < patternHumpCount; currentPatternHump++)
                {
                    var patternChunkCharacterSpan = patternHumps[currentPatternHump];

                    if (gotOneMatchThisCandidate)
                    {
                        // We've already gotten one pattern part match in this candidate.  We will
                        // only continue trying to consume pattern parts if the last part and this
                        // part are both upper case.  
                        if (!char.IsUpper(patternChunk.Text[patternHumps[currentPatternHump - 1].Start]) ||
                            !char.IsUpper(patternChunk.Text[patternHumps[currentPatternHump].Start]))
                        {
                            break;
                        }
                    }

                    if (!PartStartsWith(candidate, candidateHump, patternChunk.Text, patternChunkCharacterSpan, compareOption))
                    {
                        break;
                    }

                    matchSpans.Add(new TextSpan(candidateHump.Start, patternChunkCharacterSpan.Length));
                    gotOneMatchThisCandidate = true;

                    firstMatch ??= currentCandidateHump;

                    // If we were contiguous, then keep that value.  If we weren't, then keep that
                    // value.  If we don't know, then set the value to 'true' as an initial match is
                    // obviously contiguous.
                    contiguous ??= true;

                    candidateHump = new TextSpan(candidateHump.Start + patternChunkCharacterSpan.Length, candidateHump.Length - patternChunkCharacterSpan.Length);
                }

                // Check if we matched anything at all.  If we didn't, then we need to unset the
                // contiguous bit if we currently had it set.
                // If we haven't set the bit yet, then that means we haven't matched anything so
                // far, and we don't want to change that.
                if (!gotOneMatchThisCandidate && contiguous.HasValue)
                {
                    contiguous = false;
                }

                // Move onto the next candidate.
                currentCandidateHump++;
            }
        }
    }
}
