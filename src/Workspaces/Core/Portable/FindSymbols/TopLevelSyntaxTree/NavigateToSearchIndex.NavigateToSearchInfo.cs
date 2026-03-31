// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.FindSymbols;

internal sealed partial class NavigateToSearchIndex
{
    /// <summary>
    /// Pre-computed data used by NavigateTo to quickly pre-filter documents before performing
    /// expensive per-symbol pattern matching. Contains:
    /// <list type="bullet">
    /// <item>A <see cref="HashSet{T}"/> for symbol name hump first-characters and hump-initial bigrams.</item>
    /// <item>A bloom filter for lowercased prefixes of each hump (for all-lowercase CamelCase matching).</item>
    /// <item>A bloom filter for lowercased trigrams of symbol name word-parts.</item>
    /// <item>A <see cref="HashSet{T}"/> for container name hump first-characters.</item>
    /// <item>A length bitset indicating which symbol name lengths exist in the document.</item>
    /// </list>
    /// </summary>
    private readonly struct NavigateToSearchInfo
    {
        /// <summary>
        /// The maximum bit index in <see cref="_symbolNameLengthBitset"/>. Since the bitset is a 64-bit
        /// <see langword="ulong"/>, indices 0..62 map 1-to-1 to symbol lengths, and all lengths ≥ 63 are
        /// bucketed into bit 63.
        /// </summary>
        private const int MaxBitIndex = 63;

        /// <summary>
        /// The false positive probability for bloom filters, matching the value used by <see
        /// cref="SyntaxTreeIndex"/> for find-references bloom filters.
        /// </summary>
        private const double FalsePositiveProbability = 0.0001;

        /// <summary>
        /// Exact set storing data derived from symbol name hump structure:
        /// <list type="bullet">
        /// <item>Uppercased first characters of each character-part (e.g. for "GooBar" stores "G" and "B";
        /// for "XMLDocument" stores "X", "M", "L", "D"). Used for single-hump pattern checks (e.g.
        /// pattern "Goo" checks that "G" is stored) and as the base case for all-lowercase patterns.</item>
        /// <item>All ordered pairs of uppercased hump-initial characters. For example, "GooBarQuux" has
        /// hump initials G, B, Q, so we store all ordered pairs: "GB", "GQ", "BQ". At query time, a
        /// multi-hump pattern like "GBQ" is validated by checking that each adjacent pair of the
        /// <em>pattern's</em> hump initials ("GB" and "BQ") is present. Because the index stores all
        /// ordered pairs (not just adjacent), this also handles non-contiguous CamelCase matches: pattern
        /// "GQ" checks the single pair "GQ", which is stored even though G and Q are not adjacent humps
        /// in the candidate.</item>
        /// </list>
        /// For English identifiers the domain is at most 702 values (26 single chars + 26×26 bigrams);
        /// non-English Unicode identifiers may add more, but the set only contains initials actually
        /// present in the document, so it stays small in practice. A <see cref="FrozenSet{T}"/> provides
        /// exact membership with zero false positives, negligible memory, and fast lookups optimized for
        /// read-heavy access.
        /// </summary>
        private readonly FrozenSet<string>? _humpSet;

        /// <summary>
        /// Bloom filter storing lowercased prefixes of each character-part (hump). For example,
        /// "GooBar" has character-parts "Goo" and "Bar", so we store "g", "go", "goo", "b", "ba",
        /// "bar". At query time for an all-lowercase pattern like "goba", a DP tries every way to
        /// split the pattern into segments and checks whether each segment is a stored hump prefix.
        /// The split "go"+"ba" succeeds because "go" is a prefix of "Goo" and "ba" is a prefix of
        /// "Bar". This is much more selective than checking a single character: instead of a small
        /// set of possible initial values (26 for English, slightly more with Unicode), each segment is
        /// checked against a multi-character string space. A key optimization:
        /// if segment pattern[i..j) is not in the filter, we can skip all longer extensions
        /// pattern[i..j+1), pattern[i..j+2), etc., because no hump can have a longer prefix without
        /// also having the shorter one (and bloom filters have no false negatives).
        /// </summary>
        private readonly BloomFilter? _humpPrefixFilter;

        /// <summary>
        /// Bloom filter storing lowercased trigrams (3-character sliding windows) of each word-part.
        /// For example, for "Readline" stores "rea", "ead", "adl", "dli", "lin", "ine". Used to detect
        /// <see cref="PatternMatching.PatternMatchKind.LowercaseSubstring"/> matches like "line"
        /// matching "Readline".
        /// </summary>
        private readonly BloomFilter? _trigramFilter;

        /// <summary>
        /// Exact set storing uppercased first characters of each character-part across all segments of
        /// the fully-qualified container name. For example, "System.GooBar.Quux" produces hump initials
        /// 'S', 'G', 'B', 'Q'. A <see cref="FrozenSet{T}"/> of chars handles any Unicode character (not
        /// just A–Z) and provides exact membership with zero false positives and fast lookups.
        /// </summary>
        private readonly FrozenSet<char>? _containerCharSet;

        /// <summary>
        /// A 64-bit bitset indicating which symbol name lengths exist in the document. Bit <c>i</c> is set
        /// if some symbol has a name of length <c>i</c> (lengths ≥ 63 share bit <see cref="MaxBitIndex"/>).
        /// Used for fuzzy-match pre-filtering: a fuzzy match requires the candidate and pattern lengths to
        /// differ by at most 2, so if no symbol length is within ±2 of the pattern length, the document can
        /// be skipped.
        /// </summary>
        private readonly ulong _symbolNameLengthBitset;

        private NavigateToSearchInfo(
            FrozenSet<string>? humpSet,
            BloomFilter? humpPrefixFilter,
            BloomFilter? trigramFilter,
            FrozenSet<char>? containerCharSet,
            ulong symbolNameLengthBitset)
        {
            _humpSet = humpSet;
            _humpPrefixFilter = humpPrefixFilter;
            _trigramFilter = trigramFilter;
            _containerCharSet = containerCharSet;
            _symbolNameLengthBitset = symbolNameLengthBitset;
        }

        public static NavigateToSearchInfo Create(IReadOnlyList<DeclaredSymbolInfo> infos)
        {
            if (infos.Count == 0)
                return default;

            using var _1 = PooledHashSet<string>.GetInstance(out var humpStrings);
            using var _2 = PooledHashSet<string>.GetInstance(out var humpPrefixStrings);
            using var _3 = PooledHashSet<string>.GetInstance(out var trigramStrings);
            using var _4 = PooledHashSet<char>.GetInstance(out var containerChars);
            var lengthBitset = 0UL;

            // Find the longest name so we can allocate a single buffer for lowercasing.
            var maxNameLength = 0;
            foreach (var info in infos)
                maxNameLength = Math.Max(maxNameLength, info.Name.Length);

            var rentedCharArray = maxNameLength > 256
                ? ArrayPool<char>.Shared.Rent(maxNameLength)
                : null;

            var buffer = rentedCharArray ?? stackalloc char[maxNameLength];

            foreach (var info in infos)
            {
                AddNameData(info.Name, buffer[..info.Name.Length]);
                AddContainerData(info.FullyQualifiedContainerName, containerChars);
            }

            if (rentedCharArray is not null)
                ArrayPool<char>.Shared.Return(rentedCharArray);

            return new NavigateToSearchInfo(
                humpStrings.Count > 0 ? humpStrings.ToFrozenSet() : null,
                humpPrefixStrings.Count > 0 ? new BloomFilter(FalsePositiveProbability, isCaseSensitive: true, humpPrefixStrings) : null,
                trigramStrings.Count > 0 ? new BloomFilter(FalsePositiveProbability, isCaseSensitive: true, trigramStrings) : null,
                containerChars.Count > 0 ? containerChars.ToFrozenSet() : null,
                lengthBitset);

            void AddNameData(string name, Span<char> loweredName)
            {
                if (string.IsNullOrEmpty(name))
                    return;

                // Record symbol name length in the bitset for fuzzy-match pre-filtering.
                var lengthBit = Math.Min(name.Length, MaxBitIndex);
                lengthBitset |= 1UL << lengthBit;

                // Lowercase the name into the provided buffer for hump-prefix and trigram storage.
                name.ToLowerInvariant(loweredName);

                // Break the name into character-parts and store hump-initial data.
                // For "GooBar" -> parts ["Goo", "Bar"] -> hump initials G, B.
                using var charParts = TemporaryArray<TextSpan>.Empty;
                StringBreaker.AddCharacterParts(name, ref charParts.AsRef());

                // Store individual hump-initial characters (uppercased).
                foreach (var part in charParts)
                    AddToSet(humpStrings, [char.ToUpperInvariant(name[part.Start])]);

                // Store all ordered pairs of hump-initial characters (uppercased).
                // For "GooBarQuux" (humps G, B, Q): stores "GB", "GQ", "BQ".
                // Storing ALL pairs (not just adjacent) enables non-contiguous CamelCase matching
                // like "GQ" matching "GooBarQuux" by skipping "Bar".
                for (var i = 0; i < charParts.Count; i++)
                {
                    var ci = char.ToUpperInvariant(name[charParts[i].Start]);
                    for (var j = i + 1; j < charParts.Count; j++)
                    {
                        var cj = char.ToUpperInvariant(name[charParts[j].Start]);
                        AddToSet(humpStrings, [ci, cj]);
                    }
                }

                // Store lowercased prefixes of each character-part (hump).
                // For "GooBar" → parts "Goo", "Bar" → stores "g", "go", "goo", "b", "ba", "bar".
                // Used by the all-lowercase DP to check whether a pattern can be split into segments
                // that each match a hump prefix.
                foreach (var part in charParts)
                {
                    for (var prefixLen = 1; prefixLen <= part.Length; prefixLen++)
                        AddToSet(humpPrefixStrings, loweredName.Slice(part.Start, prefixLen));
                }

                // Break the name into word-parts and store lowercased trigrams (3-character sliding windows).
                using var wordParts = TemporaryArray<TextSpan>.Empty;
                StringBreaker.AddWordParts(name, ref wordParts.AsRef());

                foreach (var part in wordParts)
                {
                    if (part.Length < 3)
                        continue;

                    for (var i = 0; i + 3 <= part.Length; i++)
                        AddToSet(trigramStrings, loweredName.Slice(part.Start + i, 3));
                }
            }

            static void AddToSet(HashSet<string> set, ReadOnlySpan<char> value)
            {
#if NET9_0_OR_GREATER
                set.GetAlternateLookup<ReadOnlySpan<char>>().Add(value);
#else
                set.Add(value.ToString());
#endif
            }
        }

        private static void AddContainerData(string fullyQualifiedContainerName, HashSet<char> containerChars)
        {
            if (string.IsNullOrEmpty(fullyQualifiedContainerName))
                return;

            // Break the full container name into character-parts. Dots are treated as punctuation by
            // StringBreaker and are naturally skipped, so "System.Collections.Generic" produces parts
            // ["System", "Collections", "Generic"] -> stores 'S', 'C', 'G'.
            using var charParts = TemporaryArray<TextSpan>.Empty;
            StringBreaker.AddCharacterParts(fullyQualifiedContainerName, ref charParts.AsRef());

            foreach (var part in charParts)
                containerChars.Add(char.ToUpperInvariant(fullyQualifiedContainerName[part.Start]));
        }

        /// <summary>
        /// Returns <see langword="true"/> if this document probably contains at least one symbol whose
        /// name matches <paramref name="patternName"/> and (if specified) whose container matches
        /// <paramref name="patternContainer"/>. Returns <see langword="false"/> if the document
        /// definitely does not contain such a symbol (modulo intentionally unsupported match kinds like
        /// <see cref="PatternMatching.PatternMatchKind.NonLowercaseSubstring"/>).
        /// <para/>
        /// When returning <see langword="true"/>, <paramref name="allowFuzzyMatching"/> indicates whether the caller
        /// should enable fuzzy matching as a fallback after non-fuzzy checks.
        /// </summary>
        public bool ProbablyContainsMatch(string patternName, string? patternContainer, out bool allowFuzzyMatching)
        {
            var nonFuzzyPasses = NonFuzzyCheckPasses(patternName);
            allowFuzzyMatching = LengthCheckPasses(patternName);

            if (!nonFuzzyPasses && !allowFuzzyMatching)
                return false;

            return patternContainer == null || ContainerProbablyMatches(patternContainer);
        }

        /// <summary>
        /// Checks whether the pattern (after preprocessing) passes hump or trigram checks. Mirrors
        /// the <see cref="PatternMatcher"/>'s preprocessing: strips leading non-letter/digit
        /// characters (e.g., "@static" → "static") and splits at spaces/asterisks (e.g., "get word"
        /// → "get", "word"). Returns <see langword="true"/> only if every effective word passes.
        /// Since the <see cref="PatternMatcher"/> requires all words to match, failing fast on the
        /// first miss avoids the much more expensive full pattern match later.
        /// </summary>
        private bool NonFuzzyCheckPasses(ReadOnlySpan<char> pattern)
        {
            // Strip leading non-letter/digit characters upfront (e.g., "@static" → "static")
            // to avoid a wasted hump/trigram pass on the raw pattern when it has a junk prefix.
            while (pattern.Length > 0 && !char.IsLetterOrDigit(pattern[0]))
                pattern = pattern[1..];

            if (pattern.Length == 0)
                return false;

            // Split at spaces/asterisks and check each word. The PatternMatcher requires ALL words
            // to match, so bail on the first failure. For simple patterns without separators, this
            // naturally checks the whole pattern once and returns.
            while (pattern.Length > 0)
            {
                var sepIndex = pattern.IndexOfAny(' ', '*');
                if (sepIndex == 0)
                {
                    pattern = pattern[1..];
                    continue;
                }

                var end = sepIndex < 0 ? pattern.Length : sepIndex;
                if (!HumpOrTrigramCheckPasses(pattern[..end]))
                    return false;

                pattern = pattern[end..];
            }

            // All words passed.
            return true;
        }

        private bool HumpOrTrigramCheckPasses(ReadOnlySpan<char> pattern)
        {
            var isAllLowercase = IsAllLowercase(pattern);

            // Note: this order is relevant.  The trigram check can often return faster than the hump check
            // (which needs to do a more expensive DP algorithm).
            return TrigramCheckPasses(pattern, isAllLowercase) || HumpCheckPasses(pattern, isAllLowercase);
        }

        /// <summary>
        /// Checks whether the container pattern's hump-initial characters are present in this document.
        /// <para/>
        /// Example: if the document contains a symbol in container <c>System.GooBar.Quux</c>, the
        /// stored <see cref="_containerCharSet"/> is { 'S', 'G', 'B', 'Q' } (the uppercase initial
        /// of each hump across all dot-separated segments).
        /// <para/>
        /// For a <b>mixed-case</b> pattern like <c>"Go.Ba"</c>, we extract hump initials 'G' and 'B',
        /// and verify both are in the set → <see langword="true"/>.  A pattern like <c>"Go.Xy"</c>
        /// extracts 'G' and 'X'; 'X' is not in the set → <see langword="false"/>.
        /// <para/>
        /// For an <b>all-lowercase</b> pattern like <c>"goo"</c>, we cannot determine hump boundaries,
        /// so we fall back to checking just the first character uppercased: 'G' ∈ set → <see langword="true"/>.
        /// </summary>
        public bool ContainerProbablyMatches(ReadOnlySpan<char> patternContainer)
        {
            if (_containerCharSet == null || patternContainer.Length == 0)
                return false;

            if (!IsAllLowercase(patternContainer))
            {
                // Mixed-case container pattern (e.g. "Go.Ba"): extract hump initials and check each.
                using var charParts = TemporaryArray<TextSpan>.Empty;
                StringBreaker.AddCharacterParts(patternContainer, ref charParts.AsRef());

                if (charParts.Count == 0)
                    return false;

                foreach (var part in charParts)
                {
                    if (!_containerCharSet.Contains(char.ToUpperInvariant(patternContainer[part.Start])))
                        return false;
                }

                return true;
            }
            else
            {
                // All-lowercase container pattern (e.g. "goo"): we can't determine hump boundaries
                // without casing, so check just the first character uppercased as a hump initial.
                return _containerCharSet.Contains(char.ToUpperInvariant(patternContainer[0]));
            }
        }

        /// <summary>
        /// Checks whether the pattern's hump structure is compatible with symbols in this document.
        /// <para/>
        /// For <b>mixed-case</b> patterns (e.g. "GoBa", "GB"), StringBreaker identifies explicit hump
        /// boundaries. Single-hump patterns check the individual hump character. Multi-hump patterns
        /// check that each adjacent pair of hump initials exists as a stored bigram, which is
        /// significantly more selective than checking individual characters (eliminates cross-symbol
        /// false positives).
        /// <para/>
        /// For <b>all-lowercase</b> patterns (e.g. "gb", "goba"), a DP checks whether the pattern can
        /// be split into segments where each segment is a stored lowercased hump prefix. For example,
        /// "goba" can be split as "go"+"ba", matching hump prefixes from "GooBar". This is much more
        /// selective than checking a single character: each segment is a multi-character string checked
        /// against a large value space.
        /// </summary>
        public bool HumpCheckPasses(ReadOnlySpan<char> patternName)
            => HumpCheckPasses(patternName, IsAllLowercase(patternName));

        private bool HumpCheckPasses(ReadOnlySpan<char> patternName, bool isAllLowercase)
        {
            if (patternName.Length == 0)
                return false;

            return isAllLowercase
                ? AllLowercaseHumpCheckPasses(patternName, _humpPrefixFilter)
                : MixedCaseHumpCheckPasses(patternName, _humpSet);
        }

        /// <summary>
        /// For an all-lowercase pattern, uses dynamic programming to check whether the pattern can be
        /// partitioned into segments where each segment is a lowercased prefix of some hump in the
        /// document. For example, "wrli" against "WriteLine" (humps "Write", "Line") succeeds via the
        /// split "wr"+"li" — "wr" is a prefix of "write" and "li" is a prefix of "line".
        /// <para/>
        /// <b>Example 1: "getapp" against GetApplicationContext</b> (humps "Get", "Application", "Context").
        /// Stored hump prefixes include "g","ge","get", "a","ap","app",...,"application", "c","co",...,"context".
        /// <code>
        ///   pattern:   g  e  t  a  p  p
        ///   index:     0  1  2  3  4  5  6
        ///   furthest:  0                       (start)
        ///
        ///   i=0: "g"✓ "ge"✓ "get"✓ "geta"✗→break      furthest: 3
        ///   i=1: "e"✗→break                           furthest: 3
        ///   i=2: "t"✗→break                           furthest: 3
        ///   i=3: "a"✓ "ap"✓ "app"✓→j=6=n→done!        partition: "get" + "app" ✓
        /// </code>
        /// <para/>
        /// <b>Example 2: "getapp" against GettyTapple</b> (humps "Getty", "Tapple").
        /// Stored hump prefixes include "g","ge","get","gett","getty", "t","ta","tap","tapp","tappl","tapple".
        /// A greedy algorithm would commit to "get" (prefix of "getty"), leaving "app" with no match.
        /// The DP avoids this by pushing furthest to position 3 via "get", then exploring from
        /// position 2 (reachable via "ge") where "tapp" completes the partition:
        /// <code>
        ///   pattern:   g  e  t  a  p  p
        ///   index:     0  1  2  3  4  5  6
        ///   furthest:  0                       (start)
        ///
        ///   i=0: "g"✓ "ge"✓ "get"✓ "geta"✗→break         furthest: 3
        ///   i=1: "e"✗→break                              furthest: 3
        ///   i=2: "t"✓ "ta"✓ "tap"✓ "tapp"✓→j=6=n→done!   partition: "ge" + "tapp" ✓
        /// </code>
        /// <para/>
        /// Key optimization: when checking extensions from position i, if pattern[i..j) is not in the
        /// filter, we break immediately. No hump can have pattern[i..j+1) as a prefix without also
        /// having pattern[i..j) as a prefix, and bloom filters have no false negatives, so the break
        /// is safe. This makes the DP run in O(n × max_hump_length) rather than O(n²).
        /// </summary>
        private static bool AllLowercaseHumpCheckPasses(ReadOnlySpan<char> pattern, BloomFilter? humpPrefixFilter)
        {
            if (humpPrefixFilter == null)
                return false;

            var n = pattern.Length;

            // Because the bloom filter stores all prefixes of each hump, the reachable positions always
            // form a contiguous range from 0. So we only need to track the furthest reachable index
            // rather than a full bool array.
            var furthest = 0;

            for (var i = 0; i <= furthest && i < n; i++)
            {
                for (var j = i + 1; j <= n; j++)
                {
                    if (humpPrefixFilter.ProbablyContains(pattern[i..j]))
                    {
                        // A segment ending at the end of the pattern matched — the entire
                        // pattern can be partitioned into hump prefixes.
                        if (j == n)
                            return true;

                        // This segment matched a hump prefix but didn't reach the end.
                        // Extend the frontier so the outer loop will explore from this
                        // new position — it may lead to a partition we'd miss if we only
                        // committed to the longest match from earlier positions (see the
                        // GettyTapple example in the doc comment above).
                        furthest = Math.Max(furthest, j);
                    }
                    else
                    {
                        // pattern[i..j) is definitely not a stored hump prefix (bloom filters have
                        // no false negatives). Therefore no longer extension pattern[i..j+1) can be
                        // a prefix either, since it would require pattern[i..j) to also be a prefix.
                        break;
                    }
                }
            }

            // The outer loop caught up to the furthest reachable position without ever
            // reaching the end of the pattern — no valid partition exists.
            return false;
        }

        /// <summary>
        /// For a mixed-case pattern, checks hump-initial characters/bigrams against the hump set.
        /// <para/>
        /// Single-hump patterns (e.g. "Goo") check the individual hump character.
        /// Multi-hump patterns (e.g. "GoBa", "GB", "GBQ") check that each adjacent pair of hump
        /// initials is a stored bigram. Since the index stores all ordered pairs (not just adjacent)
        /// of each symbol's hump initials, this correctly handles non-contiguous matches like "GQ"
        /// matching "GooBarQuux".
        /// </summary>
        private static bool MixedCaseHumpCheckPasses(ReadOnlySpan<char> pattern, FrozenSet<string>? humpSet)
        {
            if (humpSet == null)
                return false;

            using var charParts = TemporaryArray<TextSpan>.Empty;
            StringBreaker.AddCharacterParts(pattern, ref charParts.AsRef());

            if (charParts.Count == 0)
                return false;

            if (charParts.Count == 1)
            {
                // Single hump: check the individual character.
                return ContainsChar(humpSet, char.ToUpperInvariant(pattern[charParts[0].Start]));
            }

            // Multi-hump: check that each adjacent pair of pattern hump initials is a stored bigram.
            for (var i = 0; i < charParts.Count - 1; i++)
            {
                var c1 = char.ToUpperInvariant(pattern[charParts[i].Start]);
                var c2 = char.ToUpperInvariant(pattern[charParts[i + 1].Start]);
                if (!ContainsBigram(humpSet, c1, c2))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if all trigrams of the all-lowercase pattern are present in the trigram filter.
        /// Only applicable for all-lowercase patterns of length >= 3.
        /// </summary>
        public bool TrigramCheckPasses(ReadOnlySpan<char> pattern)
            => TrigramCheckPasses(pattern, IsAllLowercase(pattern));

        private bool TrigramCheckPasses(ReadOnlySpan<char> pattern, bool isAllLowercase)
        {
            if (_trigramFilter == null || !isAllLowercase || pattern.Length < 3)
                return false;

            for (var i = 0; i + 3 <= pattern.Length; i++)
            {
                if (!_trigramFilter.ProbablyContains(pattern.Slice(i, 3)))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if any symbol name length in the document is close enough to the pattern length
        /// for a fuzzy match to be possible (within ±2).
        /// </summary>
        public bool LengthCheckPasses(ReadOnlySpan<char> pattern)
        {
            if (_symbolNameLengthBitset == 0)
                return false;

            var patternLength = pattern.Length;

            for (var delta = -2; delta <= 2; delta++)
            {
                var candidateLength = patternLength + delta;
                if (candidateLength < 0)
                    continue;

                var bit = Math.Min(candidateLength, MaxBitIndex);
                if ((_symbolNameLengthBitset & (1UL << bit)) != 0)
                    return true;
            }

            return false;
        }

        private static bool ContainsChar(FrozenSet<string> set, char c)
        {
#if NET9_0_OR_GREATER
            return set.GetAlternateLookup<ReadOnlySpan<char>>().Contains([c]);
#else
            return set.Contains(c.ToString());
#endif
        }

        private static bool ContainsBigram(FrozenSet<string> set, char c1, char c2)
        {
#if NET9_0_OR_GREATER
            return set.GetAlternateLookup<ReadOnlySpan<char>>().Contains([c1, c2]);
#else
            return set.Contains(new string([c1, c2]));
#endif
        }

        private static bool IsAllLowercase(ReadOnlySpan<char> text)
        {
            foreach (var c in text)
            {
                if (char.IsUpper(c))
                    return false;
            }

            return true;
        }

        public void WriteTo(ObjectWriter writer)
        {
            writer.WriteUInt64(_symbolNameLengthBitset);
            WriteStringSet(writer, _humpSet);
            WriteBloomFilter(writer, _humpPrefixFilter);
            WriteBloomFilter(writer, _trigramFilter);
            WriteCharSet(writer, _containerCharSet);
        }

        private static void WriteStringSet(ObjectWriter writer, FrozenSet<string>? set)
        {
            if (set != null)
            {
                writer.WriteInt32(set.Count);
                foreach (var value in set)
                    writer.WriteString(value);
            }
            else
            {
                writer.WriteInt32(0);
            }
        }

        private static void WriteCharSet(ObjectWriter writer, FrozenSet<char>? set)
        {
            if (set != null)
            {
                using var _ = PooledStringBuilder.GetInstance(out var builder);
                foreach (var c in set)
                    builder.Append(c);

                writer.WriteString(builder.ToString());
            }
            else
            {
                writer.WriteString(null);
            }
        }

        private static void WriteBloomFilter(ObjectWriter writer, BloomFilter? filter)
        {
            if (filter != null)
            {
                writer.WriteBoolean(true);
                filter.WriteTo(writer);
            }
            else
            {
                writer.WriteBoolean(false);
            }
        }

        public static NavigateToSearchInfo? TryReadFrom(ObjectReader reader)
        {
            try
            {
                var lengthBitset = reader.ReadUInt64();
                var humpSet = ReadStringSet(reader);
                var humpPrefixFilter = ReadBloomFilter(reader);
                var trigramFilter = ReadBloomFilter(reader);
                var containerCharSet = ReadCharSet(reader);
                return new NavigateToSearchInfo(humpSet, humpPrefixFilter, trigramFilter, containerCharSet, lengthBitset);
            }
            catch (Exception)
            {
            }

            return null;
        }

        private static FrozenSet<string>? ReadStringSet(ObjectReader reader)
        {
            var count = reader.ReadInt32();
            if (count == 0)
                return null;

            using var _ = PooledHashSet<string>.GetInstance(out var set);
            for (var i = 0; i < count; i++)
                set.Add(reader.ReadString()!);

            return set.ToFrozenSet();
        }

        private static FrozenSet<char>? ReadCharSet(ObjectReader reader)
        {
            var chars = reader.ReadString();
            if (chars == null)
                return null;

            return chars.ToFrozenSet();
        }

        private static BloomFilter? ReadBloomFilter(ObjectReader reader)
        {
            if (reader.ReadBoolean())
                return BloomFilter.ReadFrom(reader);

            return null;
        }
    }
}
