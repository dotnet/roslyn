// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Utilities
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
        private static readonly char[] s_dotCharacterArray = { '.' };

        private readonly object _gate = new object();

        private readonly bool _allowFuzzyMatching;
        private readonly bool _invalidPattern;
        private readonly Segment _fullPatternSegment;
        private readonly Segment[] _dotSeparatedSegments;

        private readonly Dictionary<string, StringBreaks> _stringToWordSpans = new Dictionary<string, StringBreaks>();
        private readonly Func<string, StringBreaks> _breakIntoWordSpans = StringBreaker.BreakIntoWordParts;

        // PERF: Cache the culture's compareInfo to avoid the overhead of asking for them repeatedly in inner loops
        private readonly CompareInfo _compareInfo;

        /// <summary>
        /// Construct a new PatternMatcher using the calling thread's culture for string searching and comparison.
        /// </summary>
        public PatternMatcher(
                string pattern,
                bool verbatimIdentifierPrefixIsWordCharacter = false,
                bool allowFuzzyMatching = false) : 
            this(pattern, CultureInfo.CurrentCulture, verbatimIdentifierPrefixIsWordCharacter, allowFuzzyMatching)
        {
        }

        /// <summary>
        /// Construct a new PatternMatcher using the specified culture.
        /// </summary>
        /// <param name="pattern">The pattern to make the pattern matcher for.</param>
        /// <param name="culture">The culture to use for string searching and comparison.</param>
        /// <param name="verbatimIdentifierPrefixIsWordCharacter">Whether to consider "@" as a word character</param>
        /// <param name="allowFuzzyMatching">Whether or not close matches should count as matches.</param>
        public PatternMatcher(
            string pattern,
            CultureInfo culture,
            bool verbatimIdentifierPrefixIsWordCharacter,
            bool allowFuzzyMatching)
        {
            pattern = pattern.Trim();
            _compareInfo = culture.CompareInfo;
            _allowFuzzyMatching = allowFuzzyMatching;

            _fullPatternSegment = new Segment(pattern, verbatimIdentifierPrefixIsWordCharacter, allowFuzzyMatching);

            if (pattern.IndexOf('.') < 0)
            {
                // PERF: Avoid string.Split allocations when the pattern doesn't contain a dot.
                _dotSeparatedSegments = pattern.Length > 0
                    ? new Segment[1] { _fullPatternSegment }
                    : Array.Empty<Segment>();
            }
            else
            {
                _dotSeparatedSegments = pattern.Split(s_dotCharacterArray, StringSplitOptions.RemoveEmptyEntries)
                                                .Select(text => new Segment(text.Trim(), verbatimIdentifierPrefixIsWordCharacter, allowFuzzyMatching))
                                                .ToArray();
            }

            _invalidPattern = _dotSeparatedSegments.Length == 0 || _dotSeparatedSegments.Any(s => s.IsInvalid);
        }

        public void Dispose()
        {
            _fullPatternSegment.Dispose();
            foreach (var segment in _dotSeparatedSegments)
            {
                segment.Dispose();
            }
        }

        public bool IsDottedPattern => _dotSeparatedSegments.Length > 1;

        private bool SkipMatch(string candidate)
        {
            return _invalidPattern || string.IsNullOrWhiteSpace(candidate);
        }

        public IEnumerable<PatternMatch> GetMatches(string candidate)
        {
            return GetMatches(candidate, includeMatchSpans: false);
        }

        /// <summary>
        /// Determines if a given candidate string matches under a multiple word query text, as you
        /// would find in features like Navigate To.
        /// </summary>
        /// <param name="candidate">The word being tested.</param>
        /// <param name="includeMatchSpans">Whether or not the matched spans should be included with results</param>
        /// <returns>If this was a match, a set of match types that occurred while matching the
        /// patterns. If it was not a match, it returns null.</returns>
        public IEnumerable<PatternMatch> GetMatches(string candidate, bool includeMatchSpans)
        {
            if (SkipMatch(candidate))
            {
                return null;
            }

            return MatchSegment(candidate, includeMatchSpans, _fullPatternSegment, fuzzyMatch: true) ??
                   MatchSegment(candidate, includeMatchSpans, _fullPatternSegment, fuzzyMatch: false);
        }

        public IEnumerable<PatternMatch> GetMatchesForLastSegmentOfPattern(string candidate)
        {
            if (SkipMatch(candidate))
            {
                return null;
            }

            return MatchSegment(candidate, includeMatchSpans: false, segment: _dotSeparatedSegments.Last(), fuzzyMatch: false) ??
                   MatchSegment(candidate, includeMatchSpans: false, segment: _dotSeparatedSegments.Last(), fuzzyMatch: true);
        }

        public IEnumerable<PatternMatch> GetMatches(string candidate, string dottedContainer)
        {
            return GetMatches(candidate, dottedContainer, includeMatchSpans: false);
        }


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
        public IEnumerable<PatternMatch> GetMatches(
            string candidate, string dottedContainer, bool includeMatchSpans)
        {
            return GetMatches(candidate, dottedContainer, includeMatchSpans, fuzzyMatch: false) ??
                   GetMatches(candidate, dottedContainer, includeMatchSpans, fuzzyMatch: true);
        }

        private IEnumerable<PatternMatch> GetMatches(
            string candidate, string dottedContainer, bool includeMatchSpans, bool fuzzyMatch)
        {
            if (fuzzyMatch && !_allowFuzzyMatching)
            {
                return null;
            }

            if (SkipMatch(candidate))
            {
                return null;
            }

            // First, check that the last part of the dot separated pattern matches the name of the
            // candidate.  If not, then there's no point in proceeding and doing the more
            // expensive work.
            var candidateMatch = MatchSegment(candidate, includeMatchSpans, _dotSeparatedSegments.Last(), fuzzyMatch);
            if (candidateMatch == null)
            {
                return null;
            }

            dottedContainer = dottedContainer ?? string.Empty;
            var containerParts = dottedContainer.Split(s_dotCharacterArray, StringSplitOptions.RemoveEmptyEntries);

            // -1 because the last part was checked against the name, and only the rest
            // of the parts are checked against the container.
            var relevantDotSeparatedSegmentLength = _dotSeparatedSegments.Length - 1;
            if (relevantDotSeparatedSegmentLength > containerParts.Length)
            {
                // There weren't enough container parts to match against the pattern parts.
                // So this definitely doesn't match.
                return null;
            }

            // So far so good.  Now break up the container for the candidate and check if all
            // the dotted parts match up correctly.
            var totalMatch = new List<PatternMatch>();

            // Don't need to check the last segment.  We did that as the very first bail out step.
            for (int i = 0, j = containerParts.Length - relevantDotSeparatedSegmentLength;
                 i < relevantDotSeparatedSegmentLength;
                 i++, j++)
            {
                var segment = _dotSeparatedSegments[i];
                var containerName = containerParts[j];
                var containerMatch = MatchSegment(containerName, includeMatchSpans, segment, fuzzyMatch);
                if (containerMatch == null)
                {
                    // This container didn't match the pattern piece.  So there's no match at all.
                    return null;
                }

                totalMatch.AddRange(containerMatch);
            }

            totalMatch.AddRange(candidateMatch);

            // Success, this symbol's full name matched against the dotted name the user was asking
            // about.
            return totalMatch;
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

            PatternMatch[] ignored;
            return MatchSegment(candidate, inludeMatchSpans, _fullPatternSegment, wantAllMatches: false, allMatches: out ignored, fuzzyMatch: false) ??
                   MatchSegment(candidate, inludeMatchSpans, _fullPatternSegment, wantAllMatches: false, allMatches: out ignored, fuzzyMatch: true);
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
            return MatchTextChunk(candidate, includeMatchSpans: true,
                chunk: _fullPatternSegment.TotalTextChunk, punctuationStripped: false,
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

        private PatternMatch? MatchTextChunk(
            string candidate,
            bool includeMatchSpans,
            TextChunk chunk,
            bool punctuationStripped,
            bool fuzzyMatch)
        {
            int caseInsensitiveIndex = _compareInfo.IndexOf(candidate, chunk.Text, CompareOptions.IgnoreCase);
            if (caseInsensitiveIndex == 0)
            {
                if (chunk.Text.Length == candidate.Length)
                {
                    // a) Check if the part matches the candidate entirely, in an case insensitive or
                    //    sensitive manner.  If it does, return that there was an exact match.
                    return new PatternMatch(
                        PatternMatchKind.Exact, punctuationStripped, isCaseSensitive: candidate == chunk.Text,
                        matchedSpan: GetMatchedSpan(includeMatchSpans, 0, candidate.Length));
                }
                else
                {
                    // b) Check if the part is a prefix of the candidate, in a case insensitive or sensitive
                    //    manner.  If it does, return that there was a prefix match.
                    return new PatternMatch(
                        PatternMatchKind.Prefix, punctuationStripped, isCaseSensitive: _compareInfo.IsPrefix(candidate, chunk.Text),
                        matchedSpan: GetMatchedSpan(includeMatchSpans, 0, chunk.Text.Length));
                }
            }

            var isLowercase = !ContainsUpperCaseLetter(chunk.Text);
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
                    var wordSpans = GetWordSpans(candidate);
                    for (int i = 0; i < wordSpans.Count; i++)
                    {
                        var span = wordSpans[i];
                        if (PartStartsWith(candidate, span, chunk.Text, CompareOptions.IgnoreCase))
                        {
                            return new PatternMatch(PatternMatchKind.Substring, punctuationStripped,
                                isCaseSensitive: PartStartsWith(candidate, span, chunk.Text, CompareOptions.None),
                                matchedSpan: GetMatchedSpan(includeMatchSpans, span.Start, chunk.Text.Length));
                        }
                    }
                }
            }
            else
            {
                // d) If the part was not entirely lowercase, then check if it is contained in the
                //    candidate in a case *sensitive* manner. If so, return that there was a substring
                //    match.
                var caseSensitiveIndex = _compareInfo.IndexOf(candidate, chunk.Text);
                if (caseSensitiveIndex > 0)
                {
                    return new PatternMatch(
                        PatternMatchKind.Substring, punctuationStripped, isCaseSensitive: true,
                        matchedSpan: GetMatchedSpan(includeMatchSpans, caseSensitiveIndex, chunk.Text.Length));
                }
            }

            if (!isLowercase)
            {
                // e) If the part was not entirely lowercase, then attempt a camel cased match as well.
                if (chunk.CharacterSpans.Count > 0)
                {
                    var candidateParts = GetWordSpans(candidate);
                    List<TextSpan> matchedSpans;
                    var camelCaseWeight = TryCamelCaseMatch(candidate, includeMatchSpans, candidateParts, chunk, CompareOptions.None, out matchedSpans);
                    if (camelCaseWeight.HasValue)
                    {
                        return new PatternMatch(
                            PatternMatchKind.CamelCase, punctuationStripped, isCaseSensitive: true, camelCaseWeight: camelCaseWeight,
                            matchedSpans: GetMatchedSpans(includeMatchSpans, matchedSpans));
                    }

                    camelCaseWeight = TryCamelCaseMatch(candidate, includeMatchSpans, candidateParts, chunk, CompareOptions.IgnoreCase, out matchedSpans);
                    if (camelCaseWeight.HasValue)
                    {
                        return new PatternMatch(
                            PatternMatchKind.CamelCase, punctuationStripped, isCaseSensitive: false, camelCaseWeight: camelCaseWeight,
                            matchedSpans: GetMatchedSpans(includeMatchSpans, matchedSpans));
                    }
                }
            }

            if (isLowercase)
            {
                // f) Is the pattern a substring of the candidate starting on one of the candidate's word boundaries?

                // We could check every character boundary start of the candidate for the pattern. However, that's
                // an m * n operation in the worst case. Instead, find the first instance of the pattern 
                // substring, and see if it starts on a capital letter. It seems unlikely that the user will try to 
                // filter the list based on a substring that starts on a capital letter and also with a lowercase one.
                // (Pattern: fogbar, Candidate: quuxfogbarFogBar).
                if (chunk.Text.Length < candidate.Length)
                {
                    if (caseInsensitiveIndex != -1 && char.IsUpper(candidate[caseInsensitiveIndex]))
                    {
                        return new PatternMatch(
                            PatternMatchKind.Substring, punctuationStripped, isCaseSensitive: false,
                            matchedSpan: GetMatchedSpan(includeMatchSpans, caseInsensitiveIndex, chunk.Text.Length));
                    }
                }
            }

            if (fuzzyMatch)
            {
                if (chunk.SimilarityChecker.AreSimilar(candidate))
                {
                    return new PatternMatch(
                        PatternMatchKind.Fuzzy, punctuationStripped, isCaseSensitive: false, matchedSpan: null);
                }
            }

            return null;
        }

        private TextSpan[] GetMatchedSpans(bool includeMatchSpans, List<TextSpan> matchedSpans)
        {
            return includeMatchSpans ? new NormalizedTextSpanCollection(matchedSpans).ToArray() : null;
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

        private IEnumerable<PatternMatch> MatchSegment(
            string candidate, bool includeMatchSpans, Segment segment, bool fuzzyMatch)
        {
            if (fuzzyMatch && !_allowFuzzyMatching)
            {
                return null;
            }

            PatternMatch[] matches;
            var singleMatch = MatchSegment(candidate, includeMatchSpans, segment, 
                wantAllMatches: true, fuzzyMatch: fuzzyMatch, allMatches: out matches);
            if (singleMatch.HasValue)
            {
                return SpecializedCollections.SingletonEnumerable(singleMatch.Value);
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
        private PatternMatch? MatchSegment(
            string candidate,
            bool includeMatchSpans,
            Segment segment,
            bool wantAllMatches,
            bool fuzzyMatch,
            out PatternMatch[] allMatches)
        {
            allMatches = null;

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
                var match = MatchTextChunk(candidate, includeMatchSpans, 
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
            //   e) If the word was not entirely lowercase, then attempt a camel cased match as
            //      well.
            //
            //   f) The word is all lower case. Is it a case insensitive substring of the candidate starting 
            //      on a part boundary of the candidate?
            //
            // Only if all words have some sort of match is the pattern considered matched.

            var subWordTextChunks = segment.SubWordTextChunks;
            PatternMatch[] matches = null;

            for (int i = 0; i < subWordTextChunks.Length; i++)
            {
                var subWordTextChunk = subWordTextChunks[i];

                // Try to match the candidate with this word
                var result = MatchTextChunk(candidate, includeMatchSpans, 
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

                matches = matches ?? new PatternMatch[subWordTextChunks.Length];
                matches[i] = result.Value;
            }

            allMatches = matches;
            return null;
        }

        private static bool IsWordChar(char ch, bool verbatimIdentifierPrefixIsWordCharacter)
        {
            return char.IsLetterOrDigit(ch) || ch == '_' || (verbatimIdentifierPrefixIsWordCharacter && ch == '@');
        }

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
        {
            return PartStartsWith(candidate, candidatePart, pattern, new TextSpan(0, pattern.Length), compareOptions);
        }

        private int? TryCamelCaseMatch(
            string candidate, 
            bool includeMatchedSpans,
            StringBreaks candidateParts, 
            TextChunk chunk, 
            CompareOptions compareOption,
            out List<TextSpan> matchedSpans)
        {
            matchedSpans = null;
            var chunkCharacterSpans = chunk.CharacterSpans;

            // Note: we may have more pattern parts than candidate parts.  This is because multiple
            // pattern parts may match a candidate part.  For example "SiUI" against "SimpleUI".
            // We'll have 3 pattern parts Si/U/I against two candidate parts Simple/UI.  However, U
            // and I will both match in UI. 

            int currentCandidate = 0;
            int currentChunkSpan = 0;
            int? firstMatch = null;
            bool? contiguous = null;

            while (true)
            {
                // Let's consider our termination cases
                if (currentChunkSpan == chunkCharacterSpans.Count)
                {
                    Contract.Requires(firstMatch.HasValue);
                    Contract.Requires(contiguous.HasValue);

                    // We did match! We shall assign a weight to this
                    int weight = 0;

                    // Was this contiguous?
                    if (contiguous.Value)
                    {
                        weight += 1;
                    }

                    // Did we start at the beginning of the candidate?
                    if (firstMatch.Value == 0)
                    {
                        weight += 2;
                    }

                    return weight;
                }
                else if (currentCandidate == candidateParts.Count)
                {
                    // No match, since we still have more of the pattern to hit
                    matchedSpans = null;
                    return null;
                }

                var candidatePart = candidateParts[currentCandidate];
                bool gotOneMatchThisCandidate = false;

                // Consider the case of matching SiUI against SimpleUIElement. The candidate parts
                // will be Simple/UI/Element, and the pattern parts will be Si/U/I.  We'll match 'Si'
                // against 'Simple' first.  Then we'll match 'U' against 'UI'. However, we want to
                // still keep matching pattern parts against that candidate part. 
                for (; currentChunkSpan < chunkCharacterSpans.Count; currentChunkSpan++)
                {
                    var chunkCharacterSpan = chunkCharacterSpans[currentChunkSpan];

                    if (gotOneMatchThisCandidate)
                    {
                        // We've already gotten one pattern part match in this candidate.  We will
                        // only continue trying to consume pattern parts if the last part and this
                        // part are both upper case.  
                        if (!char.IsUpper(chunk.Text[chunkCharacterSpans[currentChunkSpan - 1].Start]) ||
                            !char.IsUpper(chunk.Text[chunkCharacterSpans[currentChunkSpan].Start]))
                        {
                            break;
                        }
                    }

                    if (!PartStartsWith(candidate, candidatePart, chunk.Text, chunkCharacterSpan, compareOption))
                    {
                        break;
                    }

                    if (includeMatchedSpans)
                    {
                        matchedSpans = matchedSpans ?? new List<TextSpan>();
                        matchedSpans.Add(new TextSpan(candidatePart.Start, chunkCharacterSpan.Length));
                    }

                    gotOneMatchThisCandidate = true;

                    firstMatch = firstMatch ?? currentCandidate;

                    // If we were contiguous, then keep that value.  If we weren't, then keep that
                    // value.  If we don't know, then set the value to 'true' as an initial match is
                    // obviously contiguous.
                    contiguous = contiguous ?? true;

                    candidatePart = new TextSpan(candidatePart.Start + chunkCharacterSpan.Length, candidatePart.Length - chunkCharacterSpan.Length);
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