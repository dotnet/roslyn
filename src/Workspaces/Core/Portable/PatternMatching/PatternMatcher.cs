// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.Shared;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.PatternMatching
{
    /// <summary>
    /// The pattern matcher is thread-safe.  However, it maintains an internal cache of
    /// information as it is used.  Therefore, you should not keep it around forever and should get
    /// and release the matcher appropriately once you no longer need it.
    /// Also, while the pattern matcher is culture aware, it uses the culture specified in the
    /// constructor.
    /// </summary>
    internal sealed partial class PatternMatcher : IDisposable
    {
        public const int NoBonus = 0;
        public const int CamelCaseContiguousBonus = 1;
        public const int CamelCaseMatchesFromStartBonus = 2;
        public const int CamelCaseMaxWeight = CamelCaseContiguousBonus + CamelCaseMatchesFromStartBonus;

        private static readonly char[] s_dotCharacterArray = { '.' };

        private readonly object _gate = new object();

        private readonly bool _allowFuzzyMatching;
        private readonly bool _invalidPattern;
        private readonly PatternSegment _fullPatternSegment;
        private readonly PatternSegment[] _dotSeparatedPatternSegments;

        private readonly Dictionary<string, StringBreaks> _stringToWordSpans = new Dictionary<string, StringBreaks>();
        private static readonly Func<string, StringBreaks> _breakIntoWordSpans = StringBreaker.BreakIntoWordParts;

        // PERF: Cache the culture's compareInfo to avoid the overhead of asking for them repeatedly in inner loops
        private readonly CompareInfo _compareInfo;

        /// <summary>
        /// Construct a new PatternMatcher using the specified culture.
        /// </summary>
        /// <param name="pattern">The pattern to make the pattern matcher for.</param>
        /// <param name="culture">The culture to use for string searching and comparison.</param>
        /// <param name="allowFuzzyMatching">Whether or not close matches should count as matches.</param>
        public PatternMatcher(
            string pattern,
            CultureInfo culture = null,
            bool allowFuzzyMatching = false)
        {
            culture = culture ?? CultureInfo.CurrentCulture;
            pattern = pattern.Trim();
            _compareInfo = culture.CompareInfo;
            _allowFuzzyMatching = allowFuzzyMatching;

            _fullPatternSegment = new PatternSegment(pattern, allowFuzzyMatching);

            if (pattern.IndexOf('.') < 0)
            {
                // PERF: Avoid string.Split allocations when the pattern doesn't contain a dot.
                _dotSeparatedPatternSegments = pattern.Length > 0
                    ? new PatternSegment[1] { _fullPatternSegment }
                    : Array.Empty<PatternSegment>();
            }
            else
            {
                _dotSeparatedPatternSegments = pattern.Split(s_dotCharacterArray, StringSplitOptions.RemoveEmptyEntries)
                                                .Select(text => new PatternSegment(text.Trim(), allowFuzzyMatching))
                                                .ToArray();
            }

            _invalidPattern = _dotSeparatedPatternSegments.Length == 0 || _dotSeparatedPatternSegments.Any(s => s.IsInvalid);
        }

        public void Dispose()
        {
            _fullPatternSegment.Dispose();

            foreach (var segment in _dotSeparatedPatternSegments)
            {
                segment.Dispose();
            }

            foreach (var kvp in _stringToWordSpans)
            {
                kvp.Value.Dispose();
            }
            _stringToWordSpans.Clear();
        }

        public bool IsDottedPattern => _dotSeparatedPatternSegments.Length > 1;

        private bool SkipMatch(string candidate)
        {
            return _invalidPattern || string.IsNullOrWhiteSpace(candidate);
        }

        public ImmutableArray<PatternMatch> GetMatches(string candidate)
            => GetMatches(candidate, includeMatchSpans: false);

        /// <summary>
        /// Determines if a given candidate string matches under a multiple word query text, as you
        /// would find in features like Navigate To.
        /// </summary>
        /// <param name="candidate">The word being tested.</param>
        /// <param name="includeMatchSpans">Whether or not the matched spans should be included with results</param>
        /// <returns>If this was a match, a set of match types that occurred while matching the
        /// patterns. If it was not a match, it returns null.</returns>
        public ImmutableArray<PatternMatch> GetMatches(string candidate, bool includeMatchSpans)
        {
            if (SkipMatch(candidate))
            {
                return ImmutableArray<PatternMatch>.Empty;
            }

            var result = MatchPatternSegment(candidate, includeMatchSpans, _fullPatternSegment, fuzzyMatch: true);
            if (!result.IsEmpty)
            {
                return result;
            }

            return MatchPatternSegment(candidate, includeMatchSpans, _fullPatternSegment, fuzzyMatch: false);
        }

        public ImmutableArray<PatternMatch> GetMatchesForLastSegmentOfPattern(string candidate)
        {
            if (SkipMatch(candidate))
            {
                return ImmutableArray<PatternMatch>.Empty;
            }

            var result = MatchPatternSegment(candidate, includeMatchSpans: false, patternSegment: _dotSeparatedPatternSegments.Last(), fuzzyMatch: false);
            if (!result.IsEmpty)
            {
                return result;
            }

            return MatchPatternSegment(candidate, includeMatchSpans: false, patternSegment: _dotSeparatedPatternSegments.Last(), fuzzyMatch: true);
        }

        public PatternMatches GetMatches(string candidate, string dottedContainer)
            => GetMatches(candidate, dottedContainer, includeMatchSpans: false);

        /// <summary>
        /// Matches a pattern against a candidate, and an optional dotted container for the 
        /// candidate. If the container is provided, and the pattern itself contains dots, then
        /// the pattern will be tested against the candidate and container.  Specifically,
        /// the part of the pattern after the last dot will be tested against the candidate. If
        /// a match occurs there, then the remaining dot-separated portions of the pattern will
        /// be tested against every successive portion of the container from right to left.
        /// 
        /// i.e. if you have a pattern of "Con.WL" and the candidate is "WriteLine" with a 
        /// dotted container of "System.Console", then "WL" will be tested against "WriteLine".
        /// With a match found there, "Con" will then be tested against "Console".
        /// </summary>
        public PatternMatches GetMatches(
            string candidate, string dottedContainer, bool includeMatchSpans)
        {
            var result = GetMatches(candidate, dottedContainer, includeMatchSpans, fuzzyMatch: false);
            if (!result.IsEmpty)
            {
                return result;
            }

            return GetMatches(candidate, dottedContainer, includeMatchSpans, fuzzyMatch: true);
        }

        private PatternMatches GetMatches(
            string candidate, string dottedContainer, bool includeMatchSpans, bool fuzzyMatch)
        {
            if (fuzzyMatch && !_allowFuzzyMatching)
            {
                return PatternMatches.Empty;
            }

            if (SkipMatch(candidate))
            {
                return PatternMatches.Empty;
            }

            // First, check that the last part of the dot separated pattern matches the name of the
            // candidate.  If not, then there's no point in proceeding and doing the more
            // expensive work.
            var candidateMatch = MatchPatternSegment(candidate, includeMatchSpans, _dotSeparatedPatternSegments.Last(), fuzzyMatch);
            if (candidateMatch.IsDefaultOrEmpty)
            {
                return PatternMatches.Empty;
            }

            dottedContainer = dottedContainer ?? string.Empty;
            var containerParts = dottedContainer.Split(s_dotCharacterArray, StringSplitOptions.RemoveEmptyEntries);

            // -1 because the last part was checked against the name, and only the rest
            // of the parts are checked against the container.
            var relevantDotSeparatedSegmentLength = _dotSeparatedPatternSegments.Length - 1;
            if (relevantDotSeparatedSegmentLength > containerParts.Length)
            {
                // There weren't enough container parts to match against the pattern parts.
                // So this definitely doesn't match.
                return PatternMatches.Empty;
            }

            // So far so good.  Now break up the container for the candidate and check if all
            // the dotted parts match up correctly.
            var containerMatches = ArrayBuilder<PatternMatch>.GetInstance();

            try
            {
                // Don't need to check the last segment.  We did that as the very first bail out step.
                for (int i = 0, j = containerParts.Length - relevantDotSeparatedSegmentLength;
                     i < relevantDotSeparatedSegmentLength;
                     i++, j++)
                {
                    var segment = _dotSeparatedPatternSegments[i];
                    var containerName = containerParts[j];
                    var containerMatch = MatchPatternSegment(containerName, includeMatchSpans, segment, fuzzyMatch);
                    if (containerMatch.IsDefaultOrEmpty)
                    {
                        // This container didn't match the pattern piece.  So there's no match at all.
                        return PatternMatches.Empty;
                    }

                    containerMatches.AddRange(containerMatch);
                }

                // Success, this symbol's full name matched against the dotted name the user was asking
                // about.
                return new PatternMatches(candidateMatch, containerMatches.ToImmutable());
            }
            finally
            {
                containerMatches.Free();
            }
        }

        /// <summary>
        /// Determines if a given candidate string matches under a multiple word query text, as you
        /// would find in features like Navigate To.
        /// </summary>
        /// <remarks>
        /// PERF: This is slightly faster and uses less memory than <see cref="GetMatches(string, bool)"/>
        /// so, unless you need to know the full set of matches, use this version.
        /// </remarks>
        /// <param name="candidate">The word being tested.</param>
        /// <param name="inludeMatchSpans">Whether or not the matched spans should be included with results</param>
        /// <returns>If this was a match, the first element of the set of match types that occurred while matching the
        /// patterns. If it was not a match, it returns null.</returns>
        public PatternMatch? GetFirstMatch(string candidate, bool inludeMatchSpans = false)
        {
            if (SkipMatch(candidate))
            {
                return null;
            }

            return MatchPatternSegment(candidate, inludeMatchSpans, _fullPatternSegment, wantAllMatches: false, allMatches: out _, fuzzyMatch: false) ??
                   MatchPatternSegment(candidate, inludeMatchSpans, _fullPatternSegment, wantAllMatches: false, allMatches: out _, fuzzyMatch: true);
        }

        private StringBreaks GetWordSpans(string word)
        {
            lock (_gate)
            {
                return _stringToWordSpans.GetOrAdd(word, _breakIntoWordSpans);
            }
        }

        internal PatternMatch? MatchSingleWordPattern_ForTestingOnly(string candidate)
        {
            return MatchPatternChunk(candidate, includeMatchSpans: true,
                patternChunk: _fullPatternSegment.TotalTextChunk, punctuationStripped: false,
                fuzzyMatch: false);
        }

        private static bool ContainsUpperCaseLetter(string pattern)
        {
            // Expansion of "foreach(char ch in pattern)" to avoid a CharEnumerator allocation
            for (int i = 0; i < pattern.Length; i++)
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
            bool includeMatchSpans,
            TextChunk patternChunk,
            bool punctuationStripped,
            bool fuzzyMatch)
        {
            int caseInsensitiveIndex = _compareInfo.IndexOf(candidate, patternChunk.Text, CompareOptions.IgnoreCase);
            if (caseInsensitiveIndex == 0)
            {
                if (patternChunk.Text.Length == candidate.Length)
                {
                    // a) Check if the part matches the candidate entirely, in an case insensitive or
                    //    sensitive manner.  If it does, return that there was an exact match.
                    return new PatternMatch(
                        PatternMatchKind.Exact, punctuationStripped, isCaseSensitive: candidate == patternChunk.Text,
                        matchedSpan: GetMatchedSpan(includeMatchSpans, 0, candidate.Length));
                }
                else
                {
                    // b) Check if the part is a prefix of the candidate, in a case insensitive or sensitive
                    //    manner.  If it does, return that there was a prefix match.
                    return new PatternMatch(
                        PatternMatchKind.Prefix, punctuationStripped, isCaseSensitive: _compareInfo.IsPrefix(candidate, patternChunk.Text),
                        matchedSpan: GetMatchedSpan(includeMatchSpans, 0, patternChunk.Text.Length));
                }
            }

            var isLowercase = !ContainsUpperCaseLetter(patternChunk.Text);
            if (isLowercase)
            {
                if (caseInsensitiveIndex > 0)
                {
                    // c) If the part is entirely lowercase, then check if it is contained anywhere in the
                    //    candidate in a case insensitive manner.  If so, return that there was a substring
                    //    match. 
                    //
                    //    Note: We only have a substring match if the lowercase part is prefix match of some
                    //    word part. That way we don't match something like 'Class' when the user types 'a'.
                    //    But we would match 'FooAttribute' (since 'Attribute' starts with 'a').
                    //
                    //    Also, if we matched at location right after punctuation, then this is a good
                    //    substring match.  i.e. if the user is testing mybutton against _myButton
                    //    then this should hit. As we really are finding the match at the beginning of 
                    //    a word.
                    if (char.IsPunctuation(candidate[caseInsensitiveIndex - 1]) ||
                        char.IsPunctuation(patternChunk.Text[0]))
                    {
                        return new PatternMatch(
                            PatternMatchKind.Substring, punctuationStripped,
                            isCaseSensitive: PartStartsWith(
                                candidate, new TextSpan(caseInsensitiveIndex, patternChunk.Text.Length),
                                patternChunk.Text, CompareOptions.None),
                            matchedSpan: GetMatchedSpan(includeMatchSpans, caseInsensitiveIndex, patternChunk.Text.Length));
                    }

                    var wordSpans = GetWordSpans(candidate);
                    for (int i = 0, n = wordSpans.GetCount(); i < n; i++)
                    {
                        var span = wordSpans[i];
                        if (PartStartsWith(candidate, span, patternChunk.Text, CompareOptions.IgnoreCase))
                        {
                            return new PatternMatch(PatternMatchKind.Substring, punctuationStripped,
                                isCaseSensitive: PartStartsWith(candidate, span, patternChunk.Text, CompareOptions.None),
                                matchedSpan: GetMatchedSpan(includeMatchSpans, span.Start, patternChunk.Text.Length));
                        }
                    }
                }
            }
            else
            {
                // d) If the part was not entirely lowercase, then check if it is contained in the
                //    candidate in a case *sensitive* manner. If so, return that there was a substring
                //    match.
                var caseSensitiveIndex = _compareInfo.IndexOf(candidate, patternChunk.Text);
                if (caseSensitiveIndex > 0)
                {
                    return new PatternMatch(
                        PatternMatchKind.Substring, punctuationStripped, isCaseSensitive: true,
                        matchedSpan: GetMatchedSpan(includeMatchSpans, caseSensitiveIndex, patternChunk.Text.Length));
                }
            }

            var match = TryCamelCaseMatch(
                candidate, includeMatchSpans, patternChunk,
                punctuationStripped, isLowercase);
            if (match.HasValue)
            {
                return match.Value;
            }

            if (isLowercase)
            {
                //   g) The word is all lower case. Is it a case insensitive substring of the candidate
                //      starting on a part boundary of the candidate?

                // We could check every character boundary start of the candidate for the pattern. 
                // However, that's an m * n operation in the worst case. Instead, find the first 
                // instance of the pattern  substring, and see if it starts on a capital letter. 
                // It seems unlikely that the user will try to filter the list based on a substring
                // that starts on a capital letter and also with a lowercase one. (Pattern: fogbar, 
                // Candidate: quuxfogbarFogBar).
                if (patternChunk.Text.Length < candidate.Length)
                {
                    if (caseInsensitiveIndex != -1 && char.IsUpper(candidate[caseInsensitiveIndex]))
                    {
                        return new PatternMatch(
                            PatternMatchKind.Substring, punctuationStripped, isCaseSensitive: false,
                            matchedSpan: GetMatchedSpan(includeMatchSpans, caseInsensitiveIndex, patternChunk.Text.Length));
                    }
                }
            }

            if (fuzzyMatch)
            {
                if (patternChunk.SimilarityChecker.AreSimilar(candidate))
                {
                    return new PatternMatch(
                        PatternMatchKind.Fuzzy, punctuationStripped, isCaseSensitive: false, matchedSpan: null);
                }
            }

            return null;
        }

        private static TextSpan? GetMatchedSpan(bool includeMatchSpans, int start, int length)
        {
            return includeMatchSpans ? new TextSpan(start, length) : (TextSpan?)null;
        }

        private static bool ContainsSpaceOrAsterisk(string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                if (ch == ' ' || ch == '*')
                {
                    return true;
                }
            }

            return false;
        }

        private ImmutableArray<PatternMatch> MatchPatternSegment(
            string candidate, bool includeMatchSpans, PatternSegment patternSegment, bool fuzzyMatch)
        {
            if (fuzzyMatch && !_allowFuzzyMatching)
            {
                return ImmutableArray<PatternMatch>.Empty;
            }

            var singleMatch = MatchPatternSegment(candidate, includeMatchSpans, patternSegment,
                wantAllMatches: true, fuzzyMatch: fuzzyMatch, allMatches: out var matches);
            if (singleMatch.HasValue)
            {
                return ImmutableArray.Create(singleMatch.Value);
            }

            return matches;
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
        /// <param name="wantAllMatches">Does the caller want all matches or just the first?</param>
        /// <param name="fuzzyMatch">If a fuzzy match should be performed</param>
        /// <param name="allMatches">If <paramref name="wantAllMatches"/> is true, and there's more than one match, then the list of all matches.</param>
        /// <param name="includeMatchSpans">Whether or not the matched spans should be included with results</param>
        /// <returns>If there's only one match, then the return value is that match. Otherwise it is null.</returns>
        private PatternMatch? MatchPatternSegment(
            string candidate,
            bool includeMatchSpans,
            PatternSegment segment,
            bool wantAllMatches,
            bool fuzzyMatch,
            out ImmutableArray<PatternMatch> allMatches)
        {
            allMatches = ImmutableArray<PatternMatch>.Empty;

            if (fuzzyMatch && !_allowFuzzyMatching)
            {
                return null;
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
                var match = MatchPatternChunk(candidate, includeMatchSpans,
                    segment.TotalTextChunk, punctuationStripped: false, fuzzyMatch: fuzzyMatch);
                if (match != null)
                {
                    return match;
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
            // 3) Matching is as follows:
            //
            //   a) Check if the word matches the candidate entirely, in an case insensitive or
            //    sensitive manner.  If it does, return that there was an exact match.
            //
            //   b) Check if the word is a prefix of the candidate, in a case insensitive or
            //      sensitive manner.  If it does, return that there was a prefix match.
            //
            //   c) If the word is entirely lowercase, then check if it is contained anywhere in the
            //      candidate in a case insensitive manner.  If so, return that there was a substring
            //      match. 
            //
            //      Note: We only have a substring match if the lowercase part is prefix match of
            //      some word part. That way we don't match something like 'Class' when the user
            //      types 'a'. But we would match 'FooAttribute' (since 'Attribute' starts with
            //      'a').
            //
            //   d) If the word was not entirely lowercase, then check if it is contained in the
            //      candidate in a case *sensitive* manner. If so, return that there was a substring
            //      match.
            //
            //   e) If the word was entirely lowercase, then attempt a special lower cased camel cased 
            //      match.  i.e. cofipro would match CodeFixProvider.
            //
            //   f) If the word was not entirely lowercase, then attempt a normal camel cased match.
            //      i.e. CoFiPro would match CodeFixProvider, but CofiPro would not.  
            //
            //   g) The word is all lower case. Is it a case insensitive substring of the candidate starting 
            //      on a part boundary of the candidate?
            //
            // Only if all words have some sort of match is the pattern considered matched.

            var matches = ArrayBuilder<PatternMatch>.GetInstance();

            try
            {
                var subWordTextChunks = segment.SubWordTextChunks;
                foreach (var subWordTextChunk in subWordTextChunks)
                {
                    // Try to match the candidate with this word
                    var result = MatchPatternChunk(candidate, includeMatchSpans,
                        subWordTextChunk, punctuationStripped: true, fuzzyMatch: fuzzyMatch);
                    if (result == null)
                    {
                        return null;
                    }

                    if (!wantAllMatches || subWordTextChunks.Length == 1)
                    {
                        // Stop at the first word
                        return result;
                    }

                    matches.Add(result.Value);
                }

                allMatches = matches.ToImmutable();
                return null;
            }
            finally
            {
                matches.Free();
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

            return _compareInfo.Compare(candidate, candidatePart.Start, patternPart.Length, pattern, patternPart.Start, patternPart.Length, compareOptions) == 0;
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
            string candidate, bool includeMatchSpans, TextChunk patternChunk,
            bool punctuationStripped, bool isLowercase)
        {
            if (isLowercase)
            {
                //   e) If the word was entirely lowercase, then attempt a special lower cased camel cased 
                //      match.  i.e. cofipro would match CodeFixProvider.
                var candidateParts = GetWordSpans(candidate);
                var camelCaseWeight = TryAllLowerCamelCaseMatch(
                    candidate, includeMatchSpans, candidateParts, patternChunk, out var matchedSpans);
                if (camelCaseWeight.HasValue)
                {
                    return new PatternMatch(
                        PatternMatchKind.CamelCase, punctuationStripped, isCaseSensitive: false, camelCaseWeight: camelCaseWeight,
                        matchedSpans: matchedSpans);
                }
            }
            else
            {
                //   f) If the word was not entirely lowercase, then attempt a normal camel cased match.
                //      i.e. CoFiPro would match CodeFixProvider, but CofiPro would not.  
                if (patternChunk.CharacterSpans.GetCount() > 0)
                {
                    var candidateParts = GetWordSpans(candidate);
                    var camelCaseWeight = TryUpperCaseCamelCaseMatch(candidate, includeMatchSpans, candidateParts, patternChunk, CompareOptions.None, out var matchedSpans);
                    if (camelCaseWeight.HasValue)
                    {
                        return new PatternMatch(
                            PatternMatchKind.CamelCase, punctuationStripped, isCaseSensitive: true,
                            camelCaseWeight: camelCaseWeight, matchedSpans: matchedSpans);
                    }

                    camelCaseWeight = TryUpperCaseCamelCaseMatch(candidate, includeMatchSpans, candidateParts, patternChunk, CompareOptions.IgnoreCase, out matchedSpans);
                    if (camelCaseWeight.HasValue)
                    {
                        return new PatternMatch(
                            PatternMatchKind.CamelCase, punctuationStripped, isCaseSensitive: false, 
                            camelCaseWeight: camelCaseWeight, matchedSpans: matchedSpans);
                    }
                }
            }

            return null;
        }

        private int? TryAllLowerCamelCaseMatch(
            string candidate,
            bool includeMatchedSpans,
            StringBreaks candidateParts,
            TextChunk patternChunk,
            out ImmutableArray<TextSpan> matchedSpans)
        {
            var matcher = new AllLowerCamelCaseMatcher(candidate, includeMatchedSpans, candidateParts, patternChunk);
            return matcher.TryMatch(out matchedSpans);
        }

        private int? TryUpperCaseCamelCaseMatch(
            string candidate,
            bool includeMatchedSpans,
            StringBreaks candidateParts,
            TextChunk patternChunk,
            CompareOptions compareOption,
            out ImmutableArray<TextSpan> matchedSpans)
        {
            var patternChunkCharacterSpans = patternChunk.CharacterSpans;

            // Note: we may have more pattern parts than candidate parts.  This is because multiple
            // pattern parts may match a candidate part.  For example "SiUI" against "SimpleUI".
            // We'll have 3 pattern parts Si/U/I against two candidate parts Simple/UI.  However, U
            // and I will both match in UI. 

            int currentCandidate = 0;
            int currentChunkSpan = 0;
            int? firstMatch = null;
            bool? contiguous = null;

            var patternChunkCharacterSpansCount = patternChunkCharacterSpans.GetCount();
            var candidatePartsCount = candidateParts.GetCount();

            var result = ArrayBuilder<TextSpan>.GetInstance();
            while (true)
            {
                // Let's consider our termination cases
                if (currentChunkSpan == patternChunkCharacterSpansCount)
                {
                    Contract.Requires(firstMatch.HasValue);
                    Contract.Requires(contiguous.HasValue);

                    // We did match! We shall assign a weight to this
                    var weight = 0;

                    // Was this contiguous?
                    if (contiguous.Value)
                    {
                        weight += CamelCaseContiguousBonus;
                    }

                    // Did we start at the beginning of the candidate?
                    if (firstMatch.Value == 0)
                    {
                        weight += CamelCaseMatchesFromStartBonus;
                    }

                    matchedSpans = includeMatchedSpans
                        ? new NormalizedTextSpanCollection(result).ToImmutableArray()
                        : ImmutableArray<TextSpan>.Empty;
                    result.Free();
                    return weight;
                }
                else if (currentCandidate == candidatePartsCount)
                {
                    // No match, since we still have more of the pattern to hit
                    matchedSpans = ImmutableArray<TextSpan>.Empty;
                    result.Free();
                    return null;
                }

                var candidatePart = candidateParts[currentCandidate];
                bool gotOneMatchThisCandidate = false;

                // Consider the case of matching SiUI against SimpleUIElement. The candidate parts
                // will be Simple/UI/Element, and the pattern parts will be Si/U/I.  We'll match 'Si'
                // against 'Simple' first.  Then we'll match 'U' against 'UI'. However, we want to
                // still keep matching pattern parts against that candidate part. 
                for (; currentChunkSpan < patternChunkCharacterSpansCount; currentChunkSpan++)
                {
                    var patternChunkCharacterSpan = patternChunkCharacterSpans[currentChunkSpan];

                    if (gotOneMatchThisCandidate)
                    {
                        // We've already gotten one pattern part match in this candidate.  We will
                        // only continue trying to consume pattern parts if the last part and this
                        // part are both upper case.  
                        if (!char.IsUpper(patternChunk.Text[patternChunkCharacterSpans[currentChunkSpan - 1].Start]) ||
                            !char.IsUpper(patternChunk.Text[patternChunkCharacterSpans[currentChunkSpan].Start]))
                        {
                            break;
                        }
                    }

                    if (!PartStartsWith(candidate, candidatePart, patternChunk.Text, patternChunkCharacterSpan, compareOption))
                    {
                        break;
                    }

                    if (includeMatchedSpans)
                    {
                        result.Add(new TextSpan(candidatePart.Start, patternChunkCharacterSpan.Length));
                    }

                    gotOneMatchThisCandidate = true;

                    firstMatch = firstMatch ?? currentCandidate;

                    // If we were contiguous, then keep that value.  If we weren't, then keep that
                    // value.  If we don't know, then set the value to 'true' as an initial match is
                    // obviously contiguous.
                    contiguous = contiguous ?? true;

                    candidatePart = new TextSpan(candidatePart.Start + patternChunkCharacterSpan.Length, candidatePart.Length - patternChunkCharacterSpan.Length);
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
                currentCandidate++;
            }
        }
    }
}