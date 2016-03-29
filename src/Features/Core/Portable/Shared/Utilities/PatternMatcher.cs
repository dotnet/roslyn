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
    internal sealed class PatternMatcher
    {
        // First we break up the pattern given by dots.  Each portion of the pattern between the
        // dots is a 'Segment'.  The 'Segment' contains information about the entire section of 
        // text between the dots, as well as information about any individual 'Words' that we 
        // can break the segment into.
        private struct Segment
        {
            // Information about the entire piece of text between the dots.  For example, if the 
            // text between the dots is 'GetKeyword', then TotalTextChunk.Text will be 'GetKeyword' and 
            // TotalTextChunk.CharacterSpans will correspond to 'G', 'et', 'K' and 'eyword'.
            public readonly TextChunk TotalTextChunk;

            // Information about the subwords compromising the total word.  For example, if the 
            // text between the dots is 'GetKeyword', then the subwords will be 'Get' and 'Keyword'
            // Those individual words will have CharacterSpans of ('G' and 'et') and ('K' and 'eyword')
            // respectively.
            public readonly TextChunk[] SubWordTextChunks;

            public Segment(string text, bool verbatimIdentifierPrefixIsWordCharacter)
            {
                this.TotalTextChunk = new TextChunk(text);
                this.SubWordTextChunks = BreakPatternIntoTextChunks(text, verbatimIdentifierPrefixIsWordCharacter);
            }

            public bool IsInvalid => this.SubWordTextChunks.Length == 0;

            private static int CountTextChunks(string pattern, bool verbatimIdentifierPrefixIsWordCharacter)
            {
                int count = 0;
                int wordLength = 0;

                for (int i = 0; i < pattern.Length; i++)
                {
                    if (IsWordChar(pattern[i], verbatimIdentifierPrefixIsWordCharacter))
                    {
                        wordLength++;
                    }
                    else
                    {
                        if (wordLength > 0)
                        {
                            count++;
                            wordLength = 0;
                        }
                    }
                }

                if (wordLength > 0)
                {
                    count++;
                }

                return count;
            }

            private static TextChunk[] BreakPatternIntoTextChunks(string pattern, bool verbatimIdentifierPrefixIsWordCharacter)
            {
                int partCount = CountTextChunks(pattern, verbatimIdentifierPrefixIsWordCharacter);

                if (partCount == 0)
                {
                    return SpecializedCollections.EmptyArray<TextChunk>();
                }

                var result = new TextChunk[partCount];
                int resultIndex = 0;
                int wordStart = 0;
                int wordLength = 0;

                for (int i = 0; i < pattern.Length; i++)
                {
                    var ch = pattern[i];
                    if (IsWordChar(ch, verbatimIdentifierPrefixIsWordCharacter))
                    {
                        if (wordLength++ == 0)
                        {
                            wordStart = i;
                        }
                    }
                    else
                    {
                        if (wordLength > 0)
                        {
                            result[resultIndex++] = new TextChunk(pattern.Substring(wordStart, wordLength));
                            wordLength = 0;
                        }
                    }
                }

                if (wordLength > 0)
                {
                    result[resultIndex++] = new TextChunk(pattern.Substring(wordStart, wordLength));
                }

                return result;
            }
        }

        // Information about a chunk of text from the pattern.  The chunk is a piece of text, with 
        // cached information about the character spans within in.  Character spans separate out
        // capitalized runs and lowercase runs.  i.e. if you have AAbb, then there will be two 
        // character spans, one for AA and one for BB.
        private struct TextChunk
        {
            public readonly string Text;
            public readonly StringBreaks CharacterSpans;

            public TextChunk(string text)
            {
                this.Text = text;
                this.CharacterSpans = StringBreaker.BreakIntoCharacterParts(text);
            }
        }

        private static readonly char[] s_dotCharacterArray = new[] { '.' };

        private readonly object _gate = new object();

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
        public PatternMatcher(string pattern, bool verbatimIdentifierPrefixIsWordCharacter = false) : this(pattern, CultureInfo.CurrentCulture, verbatimIdentifierPrefixIsWordCharacter)
        {
        }

        /// <summary>
        /// Construct a new PatternMatcher using the specified culture.
        /// </summary>
        /// <param name="pattern">The pattern to make the pattern matcher for.</param>
        /// <param name="culture">The culture to use for string searching and comparison.</param>
        /// <param name="verbatimIdentifierPrefixIsWordCharacter">Whether to consider "@" as a word character</param>
        public PatternMatcher(string pattern, CultureInfo culture, bool verbatimIdentifierPrefixIsWordCharacter)
        {
            pattern = pattern.Trim();
            _compareInfo = culture.CompareInfo;

            _fullPatternSegment = new Segment(pattern, verbatimIdentifierPrefixIsWordCharacter);

            if (pattern.IndexOf('.') < 0)
            {
                // PERF: Avoid string.Split allocations when the pattern doesn't contain a dot.
                _dotSeparatedSegments = pattern.Length > 0 ? new Segment[1] { _fullPatternSegment } : SpecializedCollections.EmptyArray<Segment>();
            }
            else
            {
                _dotSeparatedSegments = pattern.Split(s_dotCharacterArray, StringSplitOptions.RemoveEmptyEntries)
                                                .Select(text => new Segment(text.Trim(), verbatimIdentifierPrefixIsWordCharacter))
                                                .ToArray();
            }

            _invalidPattern = _dotSeparatedSegments.Length == 0 || _dotSeparatedSegments.Any(s => s.IsInvalid);
        }

        public bool IsDottedPattern => _dotSeparatedSegments.Length > 1;

        private bool SkipMatch(string candidate)
        {
            return _invalidPattern || string.IsNullOrWhiteSpace(candidate);
        }

        /// <summary>
        /// Determines if a given candidate string matches under a multiple word query text, as you
        /// would find in features like Navigate To.
        /// </summary>
        /// <param name="candidate">The word being tested.</param>
        /// <returns>If this was a match, a set of match types that occurred while matching the
        /// patterns. If it was not a match, it returns null.</returns>
        public IEnumerable<PatternMatch> GetMatches(string candidate)
        {
            if (SkipMatch(candidate))
            {
                return null;
            }

            return MatchSegment(candidate, _fullPatternSegment);
        }

        public IEnumerable<PatternMatch> GetMatchesForLastSegmentOfPattern(string candidate)
        {
            if (SkipMatch(candidate))
            {
                return null;
            }

            return MatchSegment(candidate, _dotSeparatedSegments.Last());
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
        public IEnumerable<PatternMatch> GetMatches(string candidate, string dottedContainer)
        {
            if (SkipMatch(candidate))
            {
                return null;
            }

            // First, check that the last part of the dot separated pattern matches the name of the
            // candidate.  If not, then there's no point in proceeding and doing the more
            // expensive work.
            var candidateMatch = MatchSegment(candidate, _dotSeparatedSegments.Last());
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
                var containerMatch = MatchSegment(containerName, segment);
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
        /// PERF: This is slightly faster and uses less memory than <see cref="GetMatches(string)"/>
        /// so, unless you need to know the full set of matches, use this version.
        /// </remarks>
        /// <param name="candidate">The word being tested.</param>
        /// <returns>If this was a match, the first element of the set of match types that occurred while matching the
        /// patterns. If it was not a match, it returns null.</returns>
        public PatternMatch? GetFirstMatch(string candidate)
        {
            if (SkipMatch(candidate))
            {
                return null;
            }

            PatternMatch[] ignored;
            return MatchSegment(candidate, _fullPatternSegment, wantAllMatches: false, allMatches: out ignored);
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
            return MatchTextChunk(candidate, _fullPatternSegment.TotalTextChunk, punctuationStripped: false);
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

        private PatternMatch? MatchTextChunk(string candidate, TextChunk chunk, bool punctuationStripped)
        {
            int index = _compareInfo.IndexOf(candidate, chunk.Text, CompareOptions.IgnoreCase);
            if (index == 0)
            {
                if (chunk.Text.Length == candidate.Length)
                {
                    // a) Check if the part matches the candidate entirely, in an case insensitive or
                    //    sensitive manner.  If it does, return that there was an exact match.
                    return new PatternMatch(PatternMatchKind.Exact, punctuationStripped, isCaseSensitive: candidate == chunk.Text);
                }
                else
                {
                    // b) Check if the part is a prefix of the candidate, in a case insensitive or sensitive
                    //    manner.  If it does, return that there was a prefix match.
                    return new PatternMatch(PatternMatchKind.Prefix, punctuationStripped, isCaseSensitive: _compareInfo.IsPrefix(candidate, chunk.Text));
                }
            }

            var isLowercase = !ContainsUpperCaseLetter(chunk.Text);
            if (isLowercase)
            {
                if (index > 0)
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
                                isCaseSensitive: PartStartsWith(candidate, span, chunk.Text, CompareOptions.None));
                        }
                    }
                }
            }
            else
            {
                // d) If the part was not entirely lowercase, then check if it is contained in the
                //    candidate in a case *sensitive* manner. If so, return that there was a substring
                //    match.
                if (_compareInfo.IndexOf(candidate, chunk.Text) > 0)
                {
                    return new PatternMatch(PatternMatchKind.Substring, punctuationStripped, isCaseSensitive: true);
                }
            }

            if (!isLowercase)
            {
                // e) If the part was not entirely lowercase, then attempt a camel cased match as well.
                if (chunk.CharacterSpans.Count > 0)
                {
                    var candidateParts = GetWordSpans(candidate);
                    var camelCaseWeight = TryCamelCaseMatch(candidate, candidateParts, chunk, CompareOptions.None);
                    if (camelCaseWeight.HasValue)
                    {
                        return new PatternMatch(PatternMatchKind.CamelCase, punctuationStripped, isCaseSensitive: true, camelCaseWeight: camelCaseWeight);
                    }

                    camelCaseWeight = TryCamelCaseMatch(candidate, candidateParts, chunk, CompareOptions.IgnoreCase);
                    if (camelCaseWeight.HasValue)
                    {
                        return new PatternMatch(PatternMatchKind.CamelCase, punctuationStripped, isCaseSensitive: false, camelCaseWeight: camelCaseWeight);
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
                    if (index != -1 && char.IsUpper(candidate[index]))
                    {
                        return new PatternMatch(PatternMatchKind.Substring, punctuationStripped, isCaseSensitive: false);
                    }
                }
            }

            return null;
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

        private IEnumerable<PatternMatch> MatchSegment(string candidate, Segment segment)
        {
            PatternMatch[] matches;
            var singleMatch = MatchSegment(candidate, segment, wantAllMatches: true, allMatches: out matches);
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
        /// <param name="allMatches">If <paramref name="wantAllMatches"/> is true, and there's more than one match, then the list of all matches.</param>
        /// <returns>If there's only one match, then the return value is that match. Otherwise it is null.</returns>
        private PatternMatch? MatchSegment(string candidate, Segment segment, bool wantAllMatches, out PatternMatch[] allMatches)
        {
            allMatches = null;

            // First check if the segment matches as is.  This is also useful if the segment contains
            // characters we would normally strip when splitting into parts that we also may want to
            // match in the candidate.  For example if the segment is "@int" and the candidate is
            // "@int", then that will show up as an exact match here.
            //
            // Note: if the segment contains a space or an asterisk then we must assume that it's a
            // multi-word segment.
            if (!ContainsSpaceOrAsterisk(segment.TotalTextChunk.Text))
            {
                var match = MatchTextChunk(candidate, segment.TotalTextChunk, punctuationStripped: false);
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
                var result = MatchTextChunk(candidate, subWordTextChunk, punctuationStripped: true);
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

        private int? TryCamelCaseMatch(string candidate, StringBreaks candidateParts, TextChunk chunk, CompareOptions compareOption)
        {
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
                        // only continue trying to consumer pattern parts if the last part and this
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
