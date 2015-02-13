// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private static readonly char[] DotCharacterArray = new[] { '.' };

        private readonly object _gate = new object();
        private readonly Dictionary<string, List<TextSpan>> _stringToWordSpans = new Dictionary<string, List<TextSpan>>();
        private readonly Dictionary<string, List<TextSpan>> _stringToCharacterSpans = new Dictionary<string, List<TextSpan>>();
        private readonly Func<string, List<TextSpan>> _breakIntoWordSpans = StringBreaker.BreakIntoWordParts;
        private readonly Func<string, List<TextSpan>> _breakIntoCharacterSpans = StringBreaker.BreakIntoCharacterParts;

        private readonly Dictionary<string, string[]> _patternToWordParts = new Dictionary<string, string[]>();
        private readonly Dictionary<string, string[]> _patternToDotSeparatedParts = new Dictionary<string, string[]>();

        private readonly Func<string, string[]> _breakPatternIntoWordParts;
        private readonly Func<string, string[]> _breakPatternIntoDotSeparatedParts;

        // PERF: Cache the culture's compareInfo to avoid the overhead of asking for them repeatedly in inner loops
        private readonly CompareInfo _compareInfo;

        /// <summary>
        /// Construct a new PatternMatcher using the calling thread's culture for string searching and comparison.
        /// </summary>
        public PatternMatcher(bool verbatimIdentifierPrefixIsWordCharacter = false) : this(CultureInfo.CurrentCulture, verbatimIdentifierPrefixIsWordCharacter)
        {
        }

        /// <summary>
        /// Construct a new PatternMatcher using the specified culture.
        /// </summary>
        /// <param name="culture">The culture to use for string searching and comparison.</param>
        /// <param name="verbatimIdentifierPrefixIsWordCharacter">Whether to consider "@" as a word character</param>
        public PatternMatcher(CultureInfo culture, bool verbatimIdentifierPrefixIsWordCharacter)
        {
            _compareInfo = culture.CompareInfo;
            _breakPatternIntoWordParts = pattern => BreakPatternIntoParts(pattern, verbatimIdentifierPrefixIsWordCharacter);
            _breakPatternIntoDotSeparatedParts = BreakIntoDotSeparatedParts;
        }

        private static string[] BreakIntoDotSeparatedParts(string value)
        {
            if (value.IndexOf('.') >= 0) {
                var split = value.Split(DotCharacterArray, StringSplitOptions.RemoveEmptyEntries);
                if (split.Length > 0) {
                    return split;
                }
            }

            return null;
        }

        private List<TextSpan> GetCharacterSpans(string pattern)
        {
            lock (_gate)
            {
                return _stringToCharacterSpans.GetOrAdd(pattern, _breakIntoCharacterSpans);
            }
        }

        private List<TextSpan> GetWordSpans(string word)
        {
            lock (_gate)
            {
                return _stringToWordSpans.GetOrAdd(word, _breakIntoWordSpans);
            }
        }

        private string[] GetWordParts(string pattern)
        {
            lock (_gate)
            {
                return _patternToWordParts.GetOrAdd(pattern, _breakPatternIntoWordParts);
            }
        }

        private string[] GetDotSeparatedParts(string pattern)
        {
            if (pattern == null)
            {
                return null;
            }

            lock (_gate)
            {
                return _patternToDotSeparatedParts.GetOrAdd(pattern, _breakPatternIntoDotSeparatedParts);
            }
        }

        internal PatternMatch? MatchSingleWordPattern_ForTestingOnly(string candidate, string pattern)
        {
            return MatchSingleWordPattern(candidate, pattern, punctuationStripped: false);
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

        /// <summary>
        /// Determines if a candidate string should matched given the user's pattern. 
        /// </summary>
        /// <param name="candidate">The string to test.</param>
        /// <param name="pattern">The pattern to match against, which may use things like
        /// Camel-Cased patterns.</param>
        /// <param name="punctuationStripped">Whether punctuation (space or asterisk) was stripped
        /// from the pattern.</param>
        private PatternMatch? MatchSingleWordPattern(string candidate, string pattern, bool punctuationStripped)
        {
            // We never match whitespace only
            if (string.IsNullOrWhiteSpace(pattern) || string.IsNullOrWhiteSpace(candidate))
            {
                return null;
            }

            // The logic for pattern matching is now as follows:
            //
            // 1) Break the pattern passed in into pattern parts.  Breaking is rather simple and a
            //    good way to think about it that if gives you all the individual alphanumeric chunks
            //    of the pattern.
            //
            // 2) For each part try to match the part against the candidate value.
            //
            // 3) Matching is as follows:
            //
            //   a) Check if the part matches the candidate entirely, in an case insensitive or
            //    sensitive manner.  If it does, return that there was an exact match.
            //
            //   b) Check if the part is a prefix of the candidate, in a case insensitive or
            //      sensitive manner.  If it does, return that there was a prefix match.
            //
            //   c) If the part is entirely lowercase, then check if it is contained anywhere in the
            //      candidate in a case insensitive manner.  If so, return that there was a substring
            //      match. 
            //
            //      Note: We only have a substring match if the lowercase part is prefix match of
            //      some word part. That way we don't match something like 'Class' when the user
            //      types 'a'. But we would match 'FooAttribute' (since 'Attribute' starts with
            //      'a').
            //
            //   d) If the part was not entirely lowercase, then check if it is contained in the
            //      candidate in a case *sensitive* manner. If so, return that there was a substring
            //      match.
            //
            //   e) If the part was not entirely lowercase, then attempt a camel cased match as
            //      well.
            //
            //   f) The pattern is all lower case. Is it a case insensitive substring of the candidate starting 
            //      on a part boundary of the candidate?
            //
            // Only if all parts have some sort of match is the pattern considered matched.

            int index = _compareInfo.IndexOf(candidate, pattern, CompareOptions.IgnoreCase);
            if (index == 0)
            {
                if (pattern.Length == candidate.Length)
                {
                    // a) Check if the part matches the candidate entirely, in an case insensitive or
                    //    sensitive manner.  If it does, return that there was an exact match.
                    return new PatternMatch(PatternMatchKind.Exact, punctuationStripped, isCaseSensitive: candidate == pattern);
                }
                else
                {
                    // b) Check if the part is a prefix of the candidate, in a case insensitive or sensitive
                    //    manner.  If it does, return that there was a prefix match.
                    return new PatternMatch(PatternMatchKind.Prefix, punctuationStripped, isCaseSensitive: _compareInfo.IsPrefix(candidate, pattern));
                }
            }

            var isLowercase = !ContainsUpperCaseLetter(pattern);
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
                    foreach (var span in wordSpans)
                    {
                        if (PartStartsWith(candidate, span, pattern, CompareOptions.IgnoreCase))
                        {
                            return new PatternMatch(PatternMatchKind.Substring, punctuationStripped, isCaseSensitive: PartStartsWith(candidate, span, pattern, CompareOptions.None));
                        }
                    }
                }
            }
            else
            {
                // d) If the part was not entirely lowercase, then check if it is contained in the
                //    candidate in a case *sensitive* manner. If so, return that there was a substring
                //    match.
                if (_compareInfo.IndexOf(candidate, pattern) > 0)
                {
                    return new PatternMatch(PatternMatchKind.Substring, punctuationStripped, isCaseSensitive: true);
                }
            }

            if (!isLowercase)
            {
                // e) If the part was not entirely lowercase, then attempt a camel cased match as well.
                var characterSpans = GetCharacterSpans(pattern);
                if (characterSpans.Count > 0)
                {
                    var candidateParts = GetWordSpans(candidate);
                    var camelCaseWeight = TryCamelCaseMatch(candidate, candidateParts, pattern, characterSpans, CompareOptions.None);
                    if (camelCaseWeight.HasValue)
                    {
                        return new PatternMatch(PatternMatchKind.CamelCase, punctuationStripped, isCaseSensitive: true, camelCaseWeight: camelCaseWeight);
                    }

                    camelCaseWeight = TryCamelCaseMatch(candidate, candidateParts, pattern, characterSpans, CompareOptions.IgnoreCase);
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
                // an m * n operation in the wost case. Instead, find the first instance of the pattern 
                // substring, and see if it starts on a capital letter. It seems unlikely that the user will try to 
                // filter the list based on a substring that starts on a capital letter and also with a lowercase one.
                // (Pattern: fogbar, Candidate: quuxfogbarFogBar).
                if (pattern.Length < candidate.Length)
                {
                    var firstInstance = _compareInfo.IndexOf(candidate, pattern, CompareOptions.IgnoreCase);
                    if (firstInstance != -1 && char.IsUpper(candidate[firstInstance]))
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

        /// <summary>
        /// Determines if a given candidate string matches under a multiple word query text, as you
        /// would find in features like Navigate To.
        /// </summary>
        /// <param name="candidate">The word being tested.</param>
        /// <param name="pattern">The multiple-word query pattern.</param>
        /// <returns>If this was a match, a set of match types that occurred while matching the
        /// patterns. If it was not a match, it returns null.</returns>
        public IEnumerable<PatternMatch> MatchPattern(string candidate, string pattern)
        {
            return MatchNonDottedPattern(candidate, pattern);
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
        public IEnumerable<PatternMatch> MatchPattern(string candidate, string dottedContainer, string pattern)
        {
            var dotParts = GetDotSeparatedParts(pattern);
            return dotParts == null
                ? MatchNonDottedPattern(candidate, pattern)
                : MatchDottedPattern(candidate, dottedContainer, pattern, dotParts);
        }

        private IEnumerable<PatternMatch> MatchDottedPattern(string candidate, string dottedContainer, string pattern, string[] patternParts)
        {
            Debug.Assert(patternParts.Length > 0);

            // First, check that the last part of the dot separated pattern matches the name of the
            // candidate.  If not, then there's no point in proceeding and doing the more
            // expensive work.
            var candidateMatch = MatchNonDottedPattern(candidate, patternParts.Last());
            if (candidateMatch == null)
            {
                return null;
            }

             var containerParts = dottedContainer.Split(DotCharacterArray, StringSplitOptions.RemoveEmptyEntries);

            // -1 because the last part was checked against the name, and only the rest
            // of the parts are checked against the container.
            if (patternParts.Length - 1 > containerParts.Length)
            {
                // There weren't enough container parts to match against the pattern parts.
                // So this definitely doesn't match.
                return null;
            }

            // So far so good.  Now break up the container for the candidate and check if all
            // the dotted parts match up correctly.
            var totalMatch = candidateMatch.ToList();

            for (int i = patternParts.Length - 2, j = containerParts.Length - 1;
                    i >= 0;
                    i--, j--)
            {
                var patternPart = patternParts[i];
                var containerName = containerParts[j];
                var containerMatch = MatchNonDottedPattern(containerName, patternPart);
                if (containerMatch == null)
                {
                    // This container didn't match the pattern piece.  So there's no match at all.
                    return null;
                }

                totalMatch.AddRange(containerMatch);
            }

            // Success, this symbol's full name matched against the dotted name the user was asking
            // about.
            return totalMatch;
        }

        private IEnumerable<PatternMatch> MatchNonDottedPattern(string candidate, string pattern)
        {
            PatternMatch[] matches;
            var singleMatch = MatchNonDottedPattern(candidate, pattern, wantAllMatches: true, allMatches: out matches);
            if (singleMatch.HasValue)
            {
                return SpecializedCollections.SingletonEnumerable(singleMatch.Value);
            }

            return matches;
        }

        /// <summary>
        /// Determines if a given candidate string matches under a multiple word query text, as you
        /// would find in features like Navigate To.
        /// </summary>
        /// <remarks>
        /// PERF: This is slightly faster and uses less memory than <see cref="MatchPattern(string, string)"/>
        /// so, unless you need to know the full set of matches, use this version.
        /// </remarks>
        /// <param name="candidate">The word being tested.</param>
        /// <param name="pattern">The multiple-word query pattern.</param>
        /// <returns>If this was a match, the first element of the set of match types that occurred while matching the
        /// patterns. If it was not a match, it returns null.</returns>
        public PatternMatch? MatchPatternFirstOrNullable(string candidate, string pattern)
        {
            PatternMatch[] ignored;
            return MatchNonDottedPattern(candidate, pattern, wantAllMatches: false, allMatches: out ignored);
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
        /// <param name="pattern">The multiple-word query pattern.</param>
        /// <param name="wantAllMatches">Does the caller want all matches or just the first?</param>
        /// <param name="allMatches">If <paramref name="wantAllMatches"/> is true, and there's more than one match, then the list of all matches.</param>
        /// <returns>If there's only one match, then the return value is that match. Otherwise it is null.</returns>
        private PatternMatch? MatchNonDottedPattern(string candidate, string pattern, bool wantAllMatches, out PatternMatch[] allMatches)
        {
            allMatches = null;

            // We never match whitespace only
            if (string.IsNullOrWhiteSpace(pattern) || string.IsNullOrWhiteSpace(candidate))
            {
                return null;
            }

            // First check if the pattern matches as is.  This is also useful if the pattern contains
            // characters we would normally strip when splitting into parts that we also may want to
            // match in the candidate.  For example if the pattern is "@int" and the candidate is
            // "@int", then that will show up as an exact match here.
            //
            // Note: if the pattern contains a space or an asterisk then we must assume that it's a
            // multi-word pattern.
            if (!ContainsSpaceOrAsterisk(pattern))
            {
                var match = MatchSingleWordPattern(candidate, pattern, punctuationStripped: false);
                if (match != null)
                {
                    return match;
                }
            }

            var patternParts = GetWordParts(pattern);
            PatternMatch[] matches = null;

            for (int i = 0; i < patternParts.Length; i++)
            {
                var word = patternParts[i];

                // Try to match the candidate with this word
                var result = MatchSingleWordPattern(candidate, word, punctuationStripped: true);
                if (result == null)
                {
                    return null;
                }

                if (!wantAllMatches || patternParts.Length == 1)
                {
                    // Stop at the first word
                    return result;
                }

                if (matches == null)
                {
                    matches = new PatternMatch[patternParts.Length];
                }

                matches[i] = result.Value;
            }

            allMatches = matches;
            return null;
        }

        private static bool IsWordChar(char ch, bool verbatimIdentifierPrefixIsWordCharacter)
        {
            return char.IsLetterOrDigit(ch) || ch == '_' || (verbatimIdentifierPrefixIsWordCharacter && ch == '@');
        }

        private static int CountParts(string pattern, bool verbatimIdentifierPrefixIsWordCharacter)
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

        private static string[] BreakPatternIntoParts(string pattern, bool verbatimIdentifierPrefixIsWordCharacter)
        {
            int partCount = CountParts(pattern, verbatimIdentifierPrefixIsWordCharacter);

            if (partCount == 0)
            {
                return SpecializedCollections.EmptyArray<string>();
            }

            var result = new string[partCount];
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
                        result[resultIndex++] = pattern.Substring(wordStart, wordLength);
                        wordLength = 0;
                    }
                }
            }

            if (wordLength > 0)
            {
                result[resultIndex++] = pattern.Substring(wordStart, wordLength);
            }

            return result;
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

        private int? TryCamelCaseMatch(string candidate, List<TextSpan> candidateParts, string pattern, List<TextSpan> patternParts, CompareOptions compareOption)
        {
            // Note: we may have more pattern parts than candidate parts.  This is because multiple
            // pattern parts may match a candidate part.  For example "SiUI" against "SimpleUI".
            // We'll have 3 pattern parts Si/U/I against two candidate parts Simple/UI.  However, U
            // and I will both match in UI. 

            int candidateCurrent = 0;
            int patternCurrent = 0;
            int? firstMatch = null;
            bool? contiguous = null;

            while (true)
            {
                // Let's consider our termination cases
                if (patternCurrent == patternParts.Count)
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
                else if (candidateCurrent == candidateParts.Count)
                {
                    // No match, since we still have more of the pattern to hit
                    return null;
                }

                var candidatePart = candidateParts[candidateCurrent];
                bool gotOneMatchThisCandidate = false;

                // Consider the case of matching SiUI against SimpleUIElement. The candidate parts
                // will be Simple/UI/Element, and the pattern parts will be Si/U/I.  We'll match 'Si'
                // against 'Simple' first.  Then we'll match 'U' against 'UI'. However, we want to
                // still keep matching pattern parts against that candidate part. 
                for (; patternCurrent < patternParts.Count; patternCurrent++)
                {
                    var patternPart = patternParts[patternCurrent];

                    if (gotOneMatchThisCandidate)
                    {
                        // We've already gotten one pattern part match in this candidate.  We will
                        // only continue trying to consumer pattern parts if the last part and this
                        // part are both upper case.  
                        if (!char.IsUpper(pattern[patternParts[patternCurrent - 1].Start]) ||
                            !char.IsUpper(pattern[patternParts[patternCurrent].Start]))
                        {
                            break;
                        }
                    }

                    if (!PartStartsWith(candidate, candidatePart, pattern, patternPart, compareOption))
                    {
                        break;
                    }

                    gotOneMatchThisCandidate = true;

                    firstMatch = firstMatch ?? candidateCurrent;

                    // If we were contiguous, then keep that value.  If we weren't, then keep that
                    // value.  If we don't know, then set the value to 'true' as an initial match is
                    // obviously contiguous.
                    contiguous = contiguous ?? true;

                    candidatePart = new TextSpan(candidatePart.Start + patternPart.Length, candidatePart.Length - patternPart.Length);
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
                candidateCurrent++;
            }
        }
    }
}
