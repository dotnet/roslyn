// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
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
        private const int MaxSymbolNameLengthBitIndex = 63;

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
        private readonly FrozenSet<string> _humpSet;

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
        private readonly BloomFilter _humpPrefixFilter;

        /// <summary>
        /// Bloom filter storing lowercased trigrams (3-character sliding windows) of each word-part.
        /// For example, for "Readline" stores "rea", "ead", "adl", "dli", "lin", "ine". Used to detect
        /// <see cref="PatternMatching.PatternMatchKind.LowercaseSubstring"/> matches like "line"
        /// matching "Readline".
        /// </summary>
        private readonly BloomFilter _trigramFilter;

        /// <summary>
        /// Exact set storing uppercased first characters of each character-part across all segments of
        /// the fully-qualified container name. For example, "System.GooBar.Quux" produces hump initials
        /// 'S', 'G', 'B', 'Q'. A <see cref="FrozenSet{T}"/> of chars handles any Unicode character (not
        /// just A–Z) and provides exact membership with zero false positives and fast lookups.
        /// </summary>
        private readonly FrozenSet<char> _containerCharSet;

        /// <summary>
        /// A 64-bit bitset indicating which symbol name lengths exist in the document. Bit <c>i</c> is set
        /// if some symbol has a name of length <c>i</c> (lengths ≥ 63 share bit <see cref="MaxSymbolNameLengthBitIndex"/>).
        /// Used for fuzzy-match pre-filtering: a fuzzy match requires the candidate and pattern lengths to
        /// differ by at most <see cref="WordSimilarityChecker.GetThreshold(int)"/>, so if no symbol length
        /// is within that range, the document can be skipped.
        /// </summary>
        private readonly ulong _symbolNameLengthBitset;

        /// <summary>
        /// Number of distinct character indices in the fuzzy bigram alphabet: lowercase letters a-z (26),
        /// digits 0-9 (10), underscore (1), and a single "other" bucket for all remaining characters
        /// (Unicode letters, etc.).
        /// </summary>
        private const int FuzzyBigramAlphabetSize = 26 + 10 + 1 + 1;

        /// <summary>
        /// Total number of bits in the fuzzy bigram bitset: one bit per ordered character pair, giving
        /// <see cref="FuzzyBigramAlphabetSize"/> × <see cref="FuzzyBigramAlphabetSize"/> = 1369 bits.
        /// </summary>
        private const int FuzzyBigramBitCount = FuzzyBigramAlphabetSize * FuzzyBigramAlphabetSize;

        /// <summary>
        /// Number of <see langword="ulong"/> elements needed to store <see cref="FuzzyBigramBitCount"/> bits.
        /// </summary>
        private const int FuzzyBigramUlongCount = (FuzzyBigramBitCount + 63) / 64;

        /// <summary>
        /// Exact bitset storing lowercased bigrams (2-character sliding windows) of all symbol names in the
        /// document. For example, "GooBar" contributes bigrams "go", "oo", "ob", "ba", "ar" (lowercased).
        /// <para/>
        /// Used for fuzzy-match pre-filtering via the q-gram count lemma (Ukkonen, 1992): each edit
        /// operation can destroy at most q=2 bigrams, so if <c>edit_distance(pattern, candidate) &lt;= k</c>,
        /// then at least <c>|pattern| - 1 - 2k</c> of the pattern's bigrams must also be bigrams of the
        /// candidate. If the count of matching bigrams falls below this threshold, the document can be
        /// skipped for fuzzy matching.
        /// <para/>
        /// See: Ukkonen, E. (1992). "Approximate string-matching with q-grams and maximal matches."
        /// <i>Theoretical Computer Science</i>, 92(1), 191–211.
        /// <see href="https://doi.org/10.1016/0304-3975(92)90143-4"/>
        /// <para/>
        /// Characters are mapped to a 38-element alphabet via <see cref="FuzzyBigramCharIndex"/>:
        /// a-z → 0..25, 0-9 → 26..35, '_' → 36, everything else → 37 ("other"). This gives a
        /// 38×38 = 1444-bit bitset (23 ulongs, 184 bytes) — compact enough to store per-document with
        /// near-exact precision. The "other" bucket means two distinct Unicode characters (e.g. 'α' and
        /// 'β') hash to the same index, but this is rare in practice and only causes a slightly higher
        /// false-positive rate for those characters.
        /// </summary>
        private readonly ImmutableArray<ulong> _fuzzyBigramBitset;

        private NavigateToSearchInfo(
            FrozenSet<string> humpSet,
            BloomFilter humpPrefixFilter,
            BloomFilter trigramFilter,
            FrozenSet<char> containerCharSet,
            ulong symbolNameLengthBitset,
            ImmutableArray<ulong> fuzzyBigramBitset)
        {
            _humpSet = humpSet;
            _humpPrefixFilter = humpPrefixFilter;
            _trigramFilter = trigramFilter;
            _containerCharSet = containerCharSet;
            _symbolNameLengthBitset = symbolNameLengthBitset;
            _fuzzyBigramBitset = fuzzyBigramBitset;
        }

        public static NavigateToSearchInfo Create(IReadOnlyList<DeclaredSymbolInfo> infos)
        {
            using var _1 = PooledHashSet<string>.GetInstance(out var humpStrings);
            using var _2 = PooledHashSet<string>.GetInstance(out var humpPrefixStrings);
            using var _3 = PooledHashSet<string>.GetInstance(out var trigramStrings);
            using var _4 = PooledHashSet<char>.GetInstance(out var containerChars);
            var lengthBitset = 0UL;
            var fuzzyBigramBitset = new ulong[FuzzyBigramUlongCount];

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
                humpStrings.ToFrozenSet(),
                new BloomFilter(FalsePositiveProbability, isCaseSensitive: true, humpPrefixStrings),
                new BloomFilter(FalsePositiveProbability, isCaseSensitive: true, trigramStrings),
                containerChars.ToFrozenSet(),
                lengthBitset,
                ImmutableCollectionsMarshal.AsImmutableArray(fuzzyBigramBitset));

            void AddNameData(string name, Span<char> loweredName)
            {
                if (string.IsNullOrEmpty(name))
                    return;

                // Record symbol name length in the bitset for fuzzy-match pre-filtering.
                lengthBitset |= 1UL << Math.Min(name.Length, MaxSymbolNameLengthBitIndex);

                // Lowercase the name into the provided buffer for hump-prefix and trigram storage.
                name.ToLowerInvariant(loweredName);

                // Break the name into character-parts and store hump-initial data.
                // For "GooBar" -> parts ["Goo", "Bar"] -> hump initials G, B.
                using var charParts = TemporaryArray<TextSpan>.Empty;
                StringBreaker.AddCharacterParts(name, ref charParts.AsRef());

                AddHumpData();
                AddHumpPrefixData(loweredName);
                AddTrigramData(loweredName);
                AddFuzzyBigramData();

                void AddHumpData()
                {
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
                }

                // Store lowercased prefixes of each character-part (hump).
                // For "GooBar" → parts "Goo", "Bar" → stores "g", "go", "goo", "b", "ba", "bar".
                // Used by the all-lowercase DP to check whether a pattern can be split into segments
                // that each match a hump prefix.
                void AddHumpPrefixData(ReadOnlySpan<char> loweredName)
                {
                    foreach (var part in charParts)
                    {
                        for (var prefixLen = 1; prefixLen <= part.Length; prefixLen++)
                            AddToSet(humpPrefixStrings, loweredName.Slice(part.Start, prefixLen));
                    }
                }

                void AddTrigramData(ReadOnlySpan<char> loweredName)
                {
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

                // Populate the fuzzy bigram bitset with lowercased bigrams (2-character sliding windows)
                // of the full name. For "GooBar" this stores: "go", "oo", "ob", "ba", "ar".
                void AddFuzzyBigramData()
                {
                    for (var i = 0; i < name.Length - 1; i++)
                    {
                        var idx = FuzzyBigramCharIndex(char.ToLowerInvariant(name[i])) * FuzzyBigramAlphabetSize
                                + FuzzyBigramCharIndex(char.ToLowerInvariant(name[i + 1]));
                        fuzzyBigramBitset[idx >> 6] |= 1UL << (idx & 63);
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
        /// Returns a <see cref="PatternMatcherKind"/> flags value indicating which matching strategies
        /// are worth attempting on this document's symbols. Returns <see cref="PatternMatcherKind.None"/>
        /// if the document definitely does not contain a symbol matching <paramref name="patternName"/>
        /// (modulo intentionally unsupported match kinds like
        /// <see cref="PatternMatching.PatternMatchKind.NonLowercaseSubstring"/>).
        /// </summary>
        public PatternMatcherKind ProbablyContainsMatch(string patternName, string? patternContainer)
        {
            var result = PatternMatcherKind.None;

            if (NonFuzzyCheckPasses(patternName))
                result |= PatternMatcherKind.Standard;

            // Fuzzy matching requires BOTH: (1) a symbol of compatible length exists in the document,
            // AND (2) enough of the pattern's bigrams are present. The length check is cheap and fast;
            // the bigram check uses the q-gram count lemma to filter more precisely for longer patterns.
            if (LengthCheckPasses(patternName) && BigramCountCheckPasses(patternName))
                result |= PatternMatcherKind.Fuzzy;

            if (result == PatternMatcherKind.None)
                return PatternMatcherKind.None;

            if (patternContainer != null && !ContainerProbablyMatches(patternContainer))
                return PatternMatcherKind.None;

            return result;
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
            // Strip leading and trailing non-word characters from each sub-word (e.g., "@static" →
            // "static", "[class]" → "class") to match how PatternMatcher.PatternSegment extracts
            // sub-words. Done here so that each segment after space/asterisk splitting is cleaned
            // independently (e.g., "[class] [structure]" → "class", "structure").
            while (pattern.Length > 0 && !PatternMatcher.IsWordChar(pattern[0]))
                pattern = pattern[1..];

            while (pattern.Length > 0 && !PatternMatcher.IsWordChar(pattern[^1]))
                pattern = pattern[..^1];

            if (pattern.Length == 0)
                return false;

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
        /// for a fuzzy match to be possible. The allowed delta is determined by
        /// <see cref="WordSimilarityChecker.GetThreshold(int)"/>: ±1 for patterns of length 3–4,
        /// ±2 for length 5+. Patterns shorter than <see cref="WordSimilarityChecker.MinFuzzyLength"/>
        /// are rejected outright because <see cref="WordSimilarityChecker.AreSimilar(string, out double)"/>
        /// disables fuzzy matching for them.
        /// </summary>
        public bool LengthCheckPasses(ReadOnlySpan<char> pattern)
        {
            if (_symbolNameLengthBitset == 0 || pattern.Length < WordSimilarityChecker.MinFuzzyLength)
                return false;

            var threshold = WordSimilarityChecker.GetThreshold(pattern.Length);

            for (var delta = -threshold; delta <= threshold; delta++)
            {
                var candidateLength = pattern.Length + delta;
                if (candidateLength < 0)
                    continue;

                var bit = Math.Min(candidateLength, MaxSymbolNameLengthBitIndex);
                if ((_symbolNameLengthBitset & (1UL << bit)) != 0)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Maps a character to its index in the <see cref="FuzzyBigramAlphabetSize"/>-element alphabet used
        /// by <see cref="_fuzzyBigramBitset"/>. Lowercase letters get unique indices (0–25), digits get
        /// unique indices (26–35), underscore gets index 36, and all other characters (Unicode letters,
        /// etc.) map to a single "other" bucket (37).
        /// </summary>
        private static int FuzzyBigramCharIndex(char c)
            => c switch
            {
                >= 'a' and <= 'z' => c - 'a',
                >= '0' and <= '9' => 26 + (c - '0'),
                '_' => 36,
                _ => 37,
            };

        /// <summary>
        /// Checks whether enough of the pattern's lowercased bigrams are present in
        /// <see cref="_fuzzyBigramBitset"/> for a fuzzy match to be possible, using the q-gram count
        /// lemma (Ukkonen, 1992): if <c>edit_distance(pattern, candidate) &lt;= k</c>, then at least
        /// <c>|pattern| - 1 - 2k</c> of the pattern's bigram positions must have a matching bigram in
        /// the candidate. (Each edit operation can destroy at most <c>q = 2</c> bigrams.)
        /// <para/>
        /// <b>Effectiveness by pattern length</b> (k from <see cref="WordSimilarityChecker.GetThreshold(int)"/>):
        /// <list type="bullet">
        /// <item>Length 3: k=1, min_shared = 3−1−2 = 0 → cannot filter (always returns true)</item>
        /// <item>Length 4: k=1, min_shared = 4−1−2 = 1 → need ≥ 1 of 3 bigrams</item>
        /// <item>Length 5: k=2, min_shared = 5−1−4 = 0 → cannot filter (always returns true)</item>
        /// <item>Length 6: k=2, min_shared = 6−1−4 = 1 → need ≥ 1 of 5 bigrams</item>
        /// <item>Length 7: k=2, min_shared = 7−1−4 = 2 → need ≥ 2 of 6 bigrams</item>
        /// <item>Length 8+: increasingly strong filtering</item>
        /// </list>
        /// <para/>
        /// See: Ukkonen, E. (1992). "Approximate string-matching with q-grams and maximal matches."
        /// <i>Theoretical Computer Science</i>, 92(1), 191–211.
        /// <see href="https://doi.org/10.1016/0304-3975(92)90143-4"/>
        /// </summary>
        public bool BigramCountCheckPasses(ReadOnlySpan<char> pattern)
        {
            var k = WordSimilarityChecker.GetThreshold(pattern.Length);
            var minShared = pattern.Length - 1 - 2 * k;
            if (minShared <= 0)
                return true;

            var count = 0;
            for (var i = 0; i < pattern.Length - 1; i++)
            {
                var idx = FuzzyBigramCharIndex(char.ToLowerInvariant(pattern[i])) * FuzzyBigramAlphabetSize
                        + FuzzyBigramCharIndex(char.ToLowerInvariant(pattern[i + 1]));
                if ((_fuzzyBigramBitset[idx >> 6] & (1UL << (idx & 63))) != 0)
                    count++;
            }

            return count >= minShared;
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
            WriteBigramBitset(writer, _fuzzyBigramBitset);
        }

        private static void WriteStringSet(ObjectWriter writer, FrozenSet<string> set)
        {
            writer.WriteInt32(set.Count);
            foreach (var value in set)
                writer.WriteString(value);
        }

        private static void WriteCharSet(ObjectWriter writer, FrozenSet<char> set)
        {
            using var _ = PooledStringBuilder.GetInstance(out var builder);
            foreach (var c in set)
                builder.Append(c);

            writer.WriteString(builder.ToString());
        }

        private static void WriteBloomFilter(ObjectWriter writer, BloomFilter filter)
            => filter.WriteTo(writer);

        public static NavigateToSearchInfo? TryReadFrom(ObjectReader reader)
        {
            try
            {
                var lengthBitset = reader.ReadUInt64();
                var humpSet = ReadStringSet(reader);
                var humpPrefixFilter = ReadBloomFilter(reader);
                var trigramFilter = ReadBloomFilter(reader);
                var containerCharSet = ReadCharSet(reader);
                var fuzzyBigramBitset = ReadBigramBitset(reader);
                return new NavigateToSearchInfo(humpSet, humpPrefixFilter, trigramFilter, containerCharSet, lengthBitset, fuzzyBigramBitset);
            }
            catch (Exception)
            {
            }

            return null;
        }

        private static FrozenSet<string> ReadStringSet(ObjectReader reader)
        {
            using var _ = PooledHashSet<string>.GetInstance(out var set);

            var count = reader.ReadInt32();
            for (var i = 0; i < count; i++)
                set.Add(reader.ReadString()!);

            return set.ToFrozenSet();
        }

        private static FrozenSet<char> ReadCharSet(ObjectReader reader)
        {
            var chars = reader.ReadRequiredString();
            return chars.ToFrozenSet();
        }

        private static BloomFilter ReadBloomFilter(ObjectReader reader)
            => BloomFilter.ReadFrom(reader);

        private static void WriteBigramBitset(ObjectWriter writer, ImmutableArray<ulong> bitset)
            => writer.WriteArray(bitset, static (writer, value) => writer.WriteUInt64(value));

        private static ImmutableArray<ulong> ReadBigramBitset(ObjectReader reader)
            => reader.ReadArray(static reader => reader.ReadUInt64());
    }
}
