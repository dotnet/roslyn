// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.FindSymbols;

public sealed class NavigateToSearchIndexTests
{
    #region Helpers

    private static DeclaredSymbolInfo CreateInfo(string name, string fullyQualifiedContainerName = "")
    {
        var stringTable = new StringTable();
        return DeclaredSymbolInfo.Create(
            stringTable, name, nameSuffix: null, containerDisplayName: null,
            fullyQualifiedContainerName, isPartial: false, hasAttributes: false,
            DeclaredSymbolInfoKind.Class, Accessibility.Public,
            default, ImmutableArray<string>.Empty);
    }

    private static NavigateToSearchIndex CreateIndex(params (string name, string container)[] symbols)
    {
        var infos = symbols.Select(s => CreateInfo(s.name, s.container)).ToImmutableArray();
        return NavigateToSearchIndex.TestAccessor.CreateIndex(infos);
    }

    private static PatternMatch? GetNameMatch(string candidate, string pattern)
    {
        using var matcher = PatternMatcher.CreatePatternMatcher(pattern, includeMatchedSpans: false, allowFuzzyMatching: true);
        return matcher.GetFirstMatch(candidate);
    }

    private static bool GetContainerMatch(string container, string containerPattern)
    {
        using var matches = TemporaryArray<PatternMatch>.Empty;
        using var matcher = PatternMatcher.CreateDotSeparatedContainerMatcher(containerPattern, includeMatchedSpans: false);
        return matcher.AddMatches(container, ref matches.AsRef());
    }

    #endregion

    #region HumpCheck — Mixed-case (bigram-based for multi-hump)

    // The hump set is an exact FrozenSet<string> (not a bloom filter), so all false-case
    // assertions are guaranteed — no false positives to worry about.

    [Theory]
    // ═══ GooBar ═══
    // Character-parts: "Goo", "Bar" → hump initials: G, B
    // Stored: individual chars "G", "B"; bigram "GB"
    //
    // Single-hump patterns: check that the individual hump-initial character is stored.
    [InlineData("GooBar", "G", true)]
    [InlineData("GooBar", "B", true)]
    [InlineData("GooBar", "Goo", true)]       // still single hump (initial 'G')
    [InlineData("GooBar", "Bar", true)]       // still single hump (initial 'B')
    // False: characters that are not hump initials of "GooBar".
    [InlineData("GooBar", "A", false)]
    [InlineData("GooBar", "C", false)]
    [InlineData("GooBar", "D", false)]
    [InlineData("GooBar", "E", false)]
    [InlineData("GooBar", "F", false)]
    [InlineData("GooBar", "H", false)]
    [InlineData("GooBar", "X", false)]
    [InlineData("GooBar", "Z", false)]
    [InlineData("GooBar", "Xyz", false)]      // single hump 'X' — not a hump initial of "GooBar"
    //
    // Multi-hump patterns: check adjacent bigrams in the hump set.
    // "GB" checks bigram G→B, which is stored for "GooBar".
    [InlineData("GooBar", "GB", true)]
    [InlineData("GooBar", "GoBa", true)]      // same humps G, B
    // False: bigrams that don't exist for "GooBar".
    [InlineData("GooBar", "GA", false)]       // G→A: 'A' not a hump initial
    [InlineData("GooBar", "GC", false)]       // G→C: 'C' not a hump initial
    [InlineData("GooBar", "GX", false)]       // G→X: 'X' not a hump initial
    [InlineData("GooBar", "GZ", false)]       // G→Z: 'Z' not a hump initial
    [InlineData("GooBar", "BG", false)]       // B→G: wrong order (only G→B is stored)
    [InlineData("GooBar", "BA", false)]       // B→A: 'A' not a hump initial
    [InlineData("GooBar", "BX", false)]       // B→X: 'X' not a hump initial
    [InlineData("GooBar", "AB", false)]       // A→B: 'A' not a hump initial
    [InlineData("GooBar", "XY", false)]       // neither X nor Y are hump initials
    [InlineData("GooBar", "XZ", false)]       // neither X nor Z are hump initials
    //
    // ═══ GooBarQuux ═══
    // Character-parts: "Goo", "Bar", "Quux" → hump initials: G, B, Q
    // Stored: "G", "B", "Q"; bigrams "GB", "GQ", "BQ" (all ordered pairs)
    //
    // Three humps: "GBQ" checks pairs G→B and B→Q.
    [InlineData("GooBarQuux", "GBQ", true)]
    [InlineData("GooBarQuux", "GoBarQu", true)]
    // Non-contiguous: "GQ" checks bigram G→Q, stored because all ordered pairs are indexed.
    [InlineData("GooBarQuux", "GQ", true)]
    [InlineData("GooBarQuux", "GoQu", true)]
    [InlineData("GooBarQuux", "BQ", true)]    // B→Q stored
    // False: wrong-order bigrams.
    [InlineData("GooBarQuux", "QG", false)]   // Q→G: wrong order
    [InlineData("GooBarQuux", "QB", false)]   // Q→B: wrong order
    [InlineData("GooBarQuux", "BG", false)]   // B→G: wrong order
    [InlineData("GooBarQuux", "QBG", false)]  // Q→B: wrong order (first pair fails)
    // False: one valid pair + one invalid pair.
    [InlineData("GooBarQuux", "GBX", false)]  // G→B ok, B→X not stored
    [InlineData("GooBarQuux", "GXQ", false)]  // G→X not stored (first pair fails)
    [InlineData("GooBarQuux", "XBQ", false)]  // X→B not stored (first pair fails)
    //
    // ═══ XMLDocument ═══
    // Character-parts: "X", "M", "L", "Document" → hump initials: X, M, L, D
    // Stored: "X", "M", "L", "D"; bigrams "XM", "XL", "XD", "ML", "MD", "LD"
    //
    [InlineData("XMLDocument", "XD", true)]   // skip M and L
    [InlineData("XMLDocument", "XM", true)]
    [InlineData("XMLDocument", "ML", true)]
    [InlineData("XMLDocument", "LD", true)]
    [InlineData("XMLDocument", "XL", true)]   // non-adjacent pair X→L
    [InlineData("XMLDocument", "MD", true)]   // non-adjacent pair M→D
    [InlineData("XMLDocument", "XMLD", true)] // X→M, M→L, L→D (chain of 3 adjacent pairs)
    // False: wrong-order bigrams.
    [InlineData("XMLDocument", "DX", false)]  // D→X: wrong order
    [InlineData("XMLDocument", "DM", false)]  // D→M: wrong order
    [InlineData("XMLDocument", "LM", false)]  // L→M: wrong order
    [InlineData("XMLDocument", "LX", false)]  // L→X: wrong order
    [InlineData("XMLDocument", "DL", false)]  // D→L: wrong order
    // False: absent hump initials.
    [InlineData("XMLDocument", "XA", false)]  // 'A' not a hump initial
    [InlineData("XMLDocument", "XZ", false)]  // 'Z' not a hump initial
    [InlineData("XMLDocument", "ZD", false)]  // 'Z' not a hump initial
    [InlineData("XMLDocument", "A", false)]   // 'A' not a hump initial
    [InlineData("XMLDocument", "Z", false)]   // 'Z' not a hump initial
    //
    // ═══ CodeFixProvider ═══
    // Character-parts: "Code", "Fix", "Provider" → hump initials: C, F, P
    // Stored: "C", "F", "P"; bigrams "CF", "CP", "FP"
    //
    [InlineData("CodeFixProvider", "CF", true)]
    [InlineData("CodeFixProvider", "CP", true)]   // non-adjacent C→P
    [InlineData("CodeFixProvider", "FP", true)]
    [InlineData("CodeFixProvider", "CFP", true)]  // C→F, F→P
    // False: wrong-order bigrams.
    [InlineData("CodeFixProvider", "FC", false)]  // F→C: wrong order
    [InlineData("CodeFixProvider", "PC", false)]  // P→C: wrong order
    [InlineData("CodeFixProvider", "PF", false)]  // P→F: wrong order
    // False: absent hump initials.
    [InlineData("CodeFixProvider", "CA", false)]  // 'A' not a hump initial
    [InlineData("CodeFixProvider", "CX", false)]  // 'X' not a hump initial
    [InlineData("CodeFixProvider", "A", false)]   // 'A' not a hump initial
    [InlineData("CodeFixProvider", "X", false)]   // 'X' not a hump initial
    public void HumpCheck_MixedCase(string symbolName, string pattern, bool expected)
    {
        var index = CreateIndex((symbolName, ""));
        Assert.Equal(expected, index.GetTestAccessor().HumpCheckProbablyMatches(pattern));
    }

    #endregion

    #region HumpCheck — Cross-symbol bigram rejection

    /// <summary>
    /// Verifies that bigram-based hump checks are more selective than individual character checks.
    /// When two symbols contribute different hump initials (e.g., "Goo" has 'G' and "Xyz" has 'X'),
    /// the bigram "GX" is NOT stored because no single symbol has humps G→X. This eliminates
    /// cross-symbol false positives that the old individual-character approach would have missed.
    /// Since the hump data is stored in an exact <c>FrozenSet&lt;T&gt;</c>, these rejections are
    /// guaranteed.
    /// </summary>
    [Fact]
    public void HumpCheck_CrossSymbolBigram_Rejected()
    {
        var index = CreateIndex(("Goo", ""), ("Xyz", ""));

        // Individual chars: both 'G' and 'X' are in the set.
        Assert.True(index.GetTestAccessor().HumpCheckProbablyMatches("G"));
        Assert.True(index.GetTestAccessor().HumpCheckProbablyMatches("X"));

        // Bigram: "GX" is NOT stored because no single symbol has hump pair G→X.
        Assert.False(index.GetTestAccessor().HumpCheckProbablyMatches("GX"));

        // Also verify the reverse.
        Assert.False(index.GetTestAccessor().HumpCheckProbablyMatches("XG"));
    }

    #endregion

    #region HumpCheck — All-lowercase (hump prefix DP)

    [Theory]
    // All-lowercase patterns use a DP that splits the pattern into segments, each of which must
    // be a lowercased prefix of some hump in the document.
    //
    // ═══ GooBar ═══
    // Humps: "Goo", "Bar"
    // Stored hump prefixes: "g", "go", "goo", "b", "ba", "bar"
    //
    // True: single-hump prefix matches.
    [InlineData("GooBar", "g", true)]       // "g" → prefix of "goo"
    [InlineData("GooBar", "go", true)]      // "go" → prefix of "goo"
    [InlineData("GooBar", "goo", true)]     // "goo" → full first hump
    [InlineData("GooBar", "b", true)]       // "b" → prefix of "bar"
    [InlineData("GooBar", "ba", true)]      // "ba" → prefix of "bar"
    [InlineData("GooBar", "bar", true)]     // "bar" → full second hump
    //
    // True: multi-segment DP splits.
    [InlineData("GooBar", "gb", true)]      // "g"+"b"
    [InlineData("GooBar", "goba", true)]    // "go"+"ba"
    [InlineData("GooBar", "goobar", true)]  // "goo"+"bar" (full name lowercased)
    [InlineData("GooBar", "goob", true)]    // "goo"+"b"
    [InlineData("GooBar", "gbar", true)]    // "g"+"bar"
    [InlineData("GooBar", "barg", true)]    // "bar"+"g" (DP doesn't enforce hump order)
    //
    // False: first character not a hump prefix.
    [InlineData("GooBar", "x", false)]      // 'x' not a prefix of "goo" or "bar"
    [InlineData("GooBar", "xyz", false)]    // 'x' not a prefix of any hump
    //
    // False: valid prefix followed by invalid character.
    [InlineData("GooBar", "gx", false)]     // "g" ok, "x" not a hump prefix
    [InlineData("GooBar", "goox", false)]   // "goo" ok, "x" not a hump prefix
    [InlineData("GooBar", "barx", false)]   // "bar" ok, "x" not a hump prefix
    [InlineData("GooBar", "gooxyz", false)]  // "goo" ok, "x" not a hump prefix → break
    //
    // False: middle segment is invalid (DP can't bridge the gap).
    [InlineData("GooBar", "gxb", false)]     // "g" ok, "x" fails → can't reach "b"
    [InlineData("GooBar", "gxba", false)]    // "g" ok, "x" fails → can't reach "ba"
    //
    // False: valid substring exists but can't align with hump boundaries.
    // (Note: single-char like "o" can hit hump-prefix bloom filter false positives with small
    // datasets, so we use 'x' which is known not to collide with "GooBar" hump prefixes.)
    [InlineData("GooBar", "ar", false)]      // "ar" is within "Bar" but not a hump prefix
    //
    // False: pattern too long to split into available humps.
    [InlineData("GooBar", "goobarx", false)]     // full name matches but trailing "x" fails
    [InlineData("GooBar", "goobarbaz", false)]   // "goo"+"bar" ok, "baz" fails
    [InlineData("GooBar", "gobaquuxbaz", false)]  // "go"+"ba" ok, "quuxbaz" has no valid split
    [InlineData("GooBar", "xyzxyzxyzxyzxyz", false)] // 'x' not a hump prefix at all
    //
    // ═══ GooBarQuux ═══
    // Humps: "Goo", "Bar", "Quux"
    // Stored hump prefixes: "g","go","goo", "b","ba","bar", "q","qu","quu","quux"
    //
    // True: multi-hump splits.
    [InlineData("GooBarQuux", "gbq", true)]      // "g"+"b"+"q"
    [InlineData("GooBarQuux", "gobarqu", true)]   // "go"+"bar"+"qu"
    [InlineData("GooBarQuux", "goobarquux", true)] // full name lowercased
    [InlineData("GooBarQuux", "gq", true)]        // "g"+"q" (skip "Bar" — non-contiguous)
    [InlineData("GooBarQuux", "goqu", true)]      // "go"+"qu" (skip "Bar")
    [InlineData("GooBarQuux", "bq", true)]        // "b"+"q" (start at second hump)
    [InlineData("GooBarQuux", "barquux", true)]   // "bar"+"quux"
    //
    // False: invalid segments with three humps.
    [InlineData("GooBarQuux", "gbx", false)]      // "g"+"b" ok, "x" not a hump prefix
    [InlineData("GooBarQuux", "gxq", false)]      // "g" ok, "x" fails → can't reach "q"
    [InlineData("GooBarQuux", "goobarzuux", false)] // "goo"+"bar" ok, "zuux" fails ('z' not a prefix)
    //
    // ═══ CodeFixProvider ═══
    // Humps: "Code", "Fix", "Provider"
    // Stored hump prefixes: "c","co","cod","code", "f","fi","fix", "p","pr","pro","prov",...,"provider"
    //
    // True: realistic CamelCase searches.
    [InlineData("CodeFixProvider", "cfp", true)]       // "c"+"f"+"p"
    [InlineData("CodeFixProvider", "cofi", true)]      // "co"+"fi"
    [InlineData("CodeFixProvider", "cofixpro", true)]  // "co"+"fix"+"pro"
    [InlineData("CodeFixProvider", "codefixprovider", true)] // full name lowercased
    //
    // False: segments that aren't hump prefixes.
    [InlineData("CodeFixProvider", "cofyz", false)]    // "co" ok, "f" ok, "yz" fails
    [InlineData("CodeFixProvider", "codxfix", false)]  // "cod" ok, "x" fails → can't reach "fix"
    [InlineData("CodeFixProvider", "cp", true)]        // "c"+"p" → skip "Fix" (non-contiguous) — TRUE
    [InlineData("CodeFixProvider", "cx", false)]       // "c" ok, "x" not a hump prefix
    //
    // ═══ XMLDocument ═══
    // Humps: "X", "M", "L", "Document"  (4 single-letter humps + one word)
    // Stored hump prefixes: "x", "m", "l", "d","do","doc","docu","docum","docume","documen","document"
    //
    [InlineData("XMLDocument", "xmld", true)]       // "x"+"m"+"l"+"d"
    [InlineData("XMLDocument", "xdoc", true)]       // "x"+"doc" (skip M, L)
    [InlineData("XMLDocument", "xmldocument", true)] // full name lowercased
    // (Note: certain single chars like 'z' can hit hump-prefix bloom filter false positives
    // with small datasets, so we use multi-char invalid segments for more robust rejections.)
    [InlineData("XMLDocument", "xmyw", false)]      // "x"+"m" ok, "yw" not a hump prefix
    [InlineData("XMLDocument", "xmlyw", false)]     // "x"+"m"+"l" ok, "yw" not a hump prefix
    public void HumpCheck_AllLowercase(string symbolName, string pattern, bool expected)
    {
        var index = CreateIndex((symbolName, ""));
        Assert.Equal(expected, index.GetTestAccessor().HumpCheckProbablyMatches(pattern));
    }

    /// <summary>
    /// Demonstrates that the hump prefix DP correctly allows cross-symbol matches (both
    /// hump prefixes are from the document, even if from different symbols) and rejects
    /// patterns that contain segments not present in any symbol's hump prefixes.
    /// </summary>
    [Fact]
    public void HumpCheck_AllLowercase_CrossSymbolBehavior()
    {
        var index = CreateIndex(("Goo", ""), ("Xab", ""));

        // Single-hump prefixes from each symbol work.
        Assert.True(index.GetTestAccessor().HumpCheckProbablyMatches("goo"));
        Assert.True(index.GetTestAccessor().HumpCheckProbablyMatches("xab"));

        // Cross-symbol: "gx" splits as "g"+"x" — "g" is a prefix of "Goo" and "x" is a
        // prefix of "Xab". These are from different symbols, but the hump prefix filter
        // stores all hump prefixes from the whole document, so the DP passes. This is
        // inherent to document-level filtering.
        Assert.True(index.GetTestAccessor().HumpCheckProbablyMatches("gx"));

        // "gooxab" splits as "goo"+"xab" — both stored, so passes (cross-symbol).
        Assert.True(index.GetTestAccessor().HumpCheckProbablyMatches("gooxab"));
    }

    /// <summary>
    /// Verifies the "bar" matches "GooBar" via hump prefix (the second hump), not just
    /// the first character.
    /// </summary>
    [Fact]
    public void HumpCheck_AllLowercase_SecondHumpPrefix()
    {
        var index = CreateIndex(("GooBar", ""));

        // "bar" as a full second hump prefix
        Assert.True(index.GetTestAccessor().HumpCheckProbablyMatches("bar"));

        // "ba" as a partial second hump prefix
        Assert.True(index.GetTestAccessor().HumpCheckProbablyMatches("ba"));

        // "bax" — "ba" ok but "x" is not a hump prefix
        Assert.False(index.GetTestAccessor().HumpCheckProbablyMatches("bax"));
    }

    /// <summary>
    /// Tests the break optimization in <c>AllLowercaseHumpCheckPasses</c>. The DP breaks
    /// immediately when extending a segment from position <c>i</c> fails, because stored
    /// hump prefixes are prefix-closed: if "go" is a hump prefix then "g" must also be one,
    /// and if "gob" is NOT a prefix then "goba", "gobar" etc. also can't be. The break makes
    /// the inner loop O(max_hump_length) instead of O(n), but must not cause false negatives.
    /// </summary>
    [Fact]
    public void HumpCheck_AllLowercase_BreakOptimization_NonGreedySplitSucceeds()
    {
        // Symbol "GooBar" — humps "Goo", "Bar"
        // Stored hump prefixes: "g", "go", "goo", "b", "ba", "bar"
        var index = CreateIndex(("GooBar", ""));

        // Pattern "gobar": a greedy approach from position 0 would consume "go" then try
        // "gob" which fails → break. But "go" already marked position 2 as reachable, and
        // from position 2, "bar" succeeds. Split: "go"+"bar".
        Assert.True(index.GetTestAccessor().HumpCheckProbablyMatches("gobar"));

        // Pattern "gooba": from position 0, "g"→"go"→"goo" succeed, "goob" fails → break.
        // Position 3 is reachable (via "goo"). From position 3, "b"→"ba" succeed.
        // canReach[5] = true. Split: "goo"+"ba".
        Assert.True(index.GetTestAccessor().HumpCheckProbablyMatches("gooba"));

        // Pattern "goobarg": from position 0, "goo" marks 3. From 3, "bar" marks 6.
        // From 6, "g" marks 7. Split: "goo"+"bar"+"g".
        Assert.True(index.GetTestAccessor().HumpCheckProbablyMatches("goobarg"));

        // Pattern "gobarg": from 0, "g" marks 1, "go" marks 2, "gob" fails → break.
        // From 1, "o" fails → break. From 2, "b" marks 3, "ba" marks 4, "bar" marks 5,
        // "barg" fails → break. From 3, "a" fails → break. From 4, "r" fails → break.
        // From 5, "g" marks 6. canReach[6] = true. Split: "go"+"bar"+"g".
        Assert.True(index.GetTestAccessor().HumpCheckProbablyMatches("gobarg"));
    }

    [Fact]
    public void HumpCheck_AllLowercase_BreakOptimization_BreakPreventsUnreachablePositions()
    {
        // Symbol "GooBar" — humps "Goo", "Bar"
        // Stored hump prefixes: "g", "go", "goo", "b", "ba", "bar"
        var index = CreateIndex(("GooBar", ""));

        // Pattern "gxbar": from 0, "g" marks 1, "gx" fails → break (no "gxb", "gxba", etc.).
        // From 1, "x" fails → break. Positions 2-5 never become reachable.
        // Even though "bar" exists in the filter, position 2 is never reached, so "bar"
        // starting at position 2 is never tried. Result: false.
        Assert.False(index.GetTestAccessor().HumpCheckProbablyMatches("gxbar"));

        // Pattern "gooxbar": from 0, "goo" marks 3, "goox" fails → break.
        // From 1-2: "o"/"o" fail. From 3, "x" fails → break.
        // Positions 4-7 never reachable. Result: false.
        Assert.False(index.GetTestAccessor().HumpCheckProbablyMatches("gooxbar"));

        // Pattern "gxb": from 0, "g" marks 1, "gx" fails → break.
        // From 1, "x" fails → break. Position 2 never reachable, "b" never tried. False.
        Assert.False(index.GetTestAccessor().HumpCheckProbablyMatches("gxb"));
    }

    [Fact]
    public void HumpCheck_AllLowercase_BreakOptimization_MultipleReachablePositions()
    {
        // Symbol "GooBarBaz" — humps "Goo", "Bar", "Baz"
        // Stored hump prefixes: "g","go","goo", "b","ba","bar","baz"
        // (Note: "ba" is a prefix of both "Bar" and "Baz", stored once)
        var index = CreateIndex(("GooBarBaz", ""));

        // Pattern "gbba": from 0, "g" marks 1. From 1, "b" marks 2, "bb" fails → break.
        // From 2, "b" marks 3, "ba" marks 4. canReach[4] = true. Split: "g"+"b"+"ba".
        Assert.True(index.GetTestAccessor().HumpCheckProbablyMatches("gbba"));

        // Pattern "gbbar": from 0, "g" marks 1. From 1, "b" marks 2, "bb" fails → break.
        // From 2, "b" marks 3, "ba" marks 4, "bar" marks 5. Split: "g"+"b"+"bar".
        Assert.True(index.GetTestAccessor().HumpCheckProbablyMatches("gbbar"));

        // Pattern "goobarbaz": full name lowercased. Split: "goo"+"bar"+"baz".
        Assert.True(index.GetTestAccessor().HumpCheckProbablyMatches("goobarbaz"));

        // Pattern "goobazbaz": "goo" marks 3, from 3 "baz" marks 6, from 6 "baz" marks 9.
        // Split: "goo"+"baz"+"baz" (DP allows reusing hump prefixes). 
        Assert.True(index.GetTestAccessor().HumpCheckProbablyMatches("goobazbaz"));

        // Pattern "goobxbaz": "goo" marks 3, from 3 "b" marks 4, "bx" fails → break.
        // From 4, "x" fails → break. Position 4 reachable but "xbaz" can't split.
        // canReach[8] = false.
        Assert.False(index.GetTestAccessor().HumpCheckProbablyMatches("goobxbaz"));
    }

    #endregion

    #region TrigramCheck

    [Theory]
    // ═══ Readline ═══
    // Word-part: "Readline" (one word-part since 'R' followed by lowercase).
    // Stored trigrams: "rea", "ead", "adl", "dli", "lin", "ine".
    //
    // True: all trigrams of the pattern are present.
    [InlineData("Readline", "rea", true)]
    [InlineData("Readline", "ead", true)]
    [InlineData("Readline", "adl", true)]
    [InlineData("Readline", "dli", true)]
    [InlineData("Readline", "lin", true)]
    [InlineData("Readline", "ine", true)]
    [InlineData("Readline", "read", true)]       // trigrams: "rea", "ead" — both stored
    [InlineData("Readline", "line", true)]       // trigrams: "lin", "ine" — both stored
    [InlineData("Readline", "readline", true)]   // all trigrams present
    [InlineData("Readline", "adli", true)]       // trigrams: "adl", "dli" — both stored
    //
    // False: pattern contains a trigram not present in the filter.
    [InlineData("Readline", "xyz", false)]       // "xyz" not stored
    [InlineData("Readline", "rex", false)]       // "rex" not stored
    [InlineData("Readline", "reax", false)]      // "rea" stored, but "eax" not stored
    [InlineData("Readline", "xead", false)]      // "xea" not stored (even though "ead" is)
    [InlineData("Readline", "readz", false)]     // "rea","ead" ok, but "adz" not stored
    [InlineData("Readline", "linex", false)]     // "lin","ine" ok, but "nex" not stored
    [InlineData("Readline", "readlinex", false)] // all original trigrams ok, but trailing "nex" fails
    //
    // False: too short for trigrams (need at least 3 characters).
    [InlineData("Readline", "re", false)]
    [InlineData("Readline", "r", false)]
    //
    // False: mixed-case patterns are not checked by the trigram filter.
    [InlineData("Readline", "Read", false)]
    [InlineData("Readline", "Line", false)]
    [InlineData("Readline", "REA", false)]
    //
    // ═══ Combine ═══
    // Word-part: "Combine"
    // Stored trigrams: "com", "omb", "mbi", "bin", "ine".
    //
    [InlineData("Combine", "com", true)]
    [InlineData("Combine", "bin", true)]
    [InlineData("Combine", "bine", true)]        // "bin", "ine" — both stored
    [InlineData("Combine", "comb", true)]        // "com", "omb" — both stored
    [InlineData("Combine", "combine", true)]     // all trigrams present
    [InlineData("Combine", "xyz", false)]        // no matching trigrams
    [InlineData("Combine", "comx", false)]       // "com" ok, but "omx" not stored
    [InlineData("Combine", "xbin", false)]       // "xbi" not stored
    [InlineData("Combine", "binez", false)]      // "bin","ine" ok, but "nez" not stored
    //
    // ═══ GooBar ═══
    // Word-parts: "Goo" and "Bar" (two separate word-parts from CamelCase).
    // Stored trigrams: "goo" (from "Goo"), "bar" (from "Bar").
    // Each word-part is only 3 chars, so exactly one trigram each.
    //
    [InlineData("GooBar", "goo", true)]
    [InlineData("GooBar", "bar", true)]
    // False: cross-word-part trigrams are NOT stored.
    [InlineData("GooBar", "oob", false)]         // spans "Goo" → "Bar" boundary
    [InlineData("GooBar", "oba", false)]         // spans boundary
    //
    // ═══ Longer symbol: "Transformer" ═══
    // Word-part: "Transformer"
    // Stored trigrams: "tra","ran","ans","nsf","sfo","for","orm","rme","mer"
    //
    [InlineData("Transformer", "tra", true)]
    [InlineData("Transformer", "transform", true)] // "tra","ran","ans","nsf","sfo","for","orm" — all stored
    [InlineData("Transformer", "former", true)]    // "for","orm","rme","mer" — all stored
    [InlineData("Transformer", "transformer", true)]
    [InlineData("Transformer", "xyz", false)]
    [InlineData("Transformer", "trax", false)]     // "tra" ok, "rax" not stored
    [InlineData("Transformer", "formerx", false)]  // "for","orm","rme","mer" ok, but "erx" not stored
    public void TrigramCheck(string symbolName, string pattern, bool expected)
    {
        var index = CreateIndex((symbolName, ""));
        Assert.Equal(expected, index.GetTestAccessor().TrigramCheckProbablyMatches(pattern));
    }

    #endregion

    #region LengthCheck

    [Theory]
    // "GooBar" has length 6. Length check passes for patterns within ±2 of 6.
    //
    // True: within ±2 range.
    [InlineData("GooBar", "abcd", true)]       // length 4, delta = -2 (boundary)
    [InlineData("GooBar", "abcde", true)]      // length 5, delta = -1
    [InlineData("GooBar", "abcdef", true)]     // length 6, delta = 0 (exact)
    [InlineData("GooBar", "abcdefg", true)]    // length 7, delta = +1
    [InlineData("GooBar", "abcdefgh", true)]   // length 8, delta = +2 (boundary)
    //
    // False: outside ±2 range.
    [InlineData("GooBar", "abc", false)]       // length 3, delta = -3 (just outside)
    [InlineData("GooBar", "abcdefghi", false)] // length 9, delta = +3 (just outside)
    [InlineData("GooBar", "ab", false)]        // length 2, delta = -4
    [InlineData("GooBar", "a", false)]         // length 1, delta = -5
    [InlineData("GooBar", "abcdefghijklmnop", false)] // length 16, far outside
    //
    // Short symbol: "Xy" has length 2.
    [InlineData("Xy", "a", true)]              // length 1, delta = -1
    [InlineData("Xy", "ab", true)]             // length 2, delta = 0
    [InlineData("Xy", "abcd", true)]           // length 4, delta = +2
    [InlineData("Xy", "abcde", false)]         // length 5, delta = +3
    //
    // Long symbol: "CodeFixProviderService" has length 22.
    [InlineData("CodeFixProviderService", "abcdefghijklmnopqrst", true)]   // length 20, delta = -2
    [InlineData("CodeFixProviderService", "abcdefghijklmnopqrstuvwx", true)] // length 24, delta = +2
    [InlineData("CodeFixProviderService", "abcdefghijklmnopqrs", false)]   // length 19, delta = -3
    [InlineData("CodeFixProviderService", "abcdefghijklmnopqrstuvwxy", false)] // length 25, delta = +3
    public void LengthCheck(string symbolName, string pattern, bool expected)
    {
        var index = CreateIndex((symbolName, ""));
        Assert.Equal(expected, index.GetTestAccessor().LengthCheckProbablyMatches(pattern));
    }

    #endregion

    #region ContainerCheck

    // The container char set is an exact FrozenSet<char> (not a bloom filter), so all
    // false-case assertions are guaranteed — no false positives to worry about.

    [Theory]
    // ═══ System.Collections.Generic ═══
    // Character-parts: "System", "Collections", "Generic"
    // Hump initials stored: S, C, G
    //
    // True: individual hump initials present.
    [InlineData("System.Collections.Generic", "S", true)]
    [InlineData("System.Collections.Generic", "C", true)]
    [InlineData("System.Collections.Generic", "G", true)]
    // True: multiple hump initials (all present, order doesn't matter for container).
    [InlineData("System.Collections.Generic", "SC", true)]
    [InlineData("System.Collections.Generic", "SG", true)]
    [InlineData("System.Collections.Generic", "CG", true)]
    [InlineData("System.Collections.Generic", "SCG", true)]
    [InlineData("System.Collections.Generic", "GC", true)]   // order doesn't matter
    //
    // False: characters that are not hump initials of the container.
    [InlineData("System.Collections.Generic", "A", false)]   // 'A' not a hump initial
    [InlineData("System.Collections.Generic", "B", false)]   // 'B' not a hump initial
    [InlineData("System.Collections.Generic", "D", false)]   // 'D' not a hump initial
    [InlineData("System.Collections.Generic", "X", false)]   // 'X' not a hump initial
    [InlineData("System.Collections.Generic", "Z", false)]   // 'Z' not a hump initial
    [InlineData("System.Collections.Generic", "W", false)]   // 'W' not a hump initial
    // False: one present + one absent.
    [InlineData("System.Collections.Generic", "SX", false)]  // S present, X absent
    [InlineData("System.Collections.Generic", "SA", false)]  // S present, A absent
    [InlineData("System.Collections.Generic", "CZ", false)]  // C present, Z absent
    [InlineData("System.Collections.Generic", "GW", false)]  // G present, W absent
    // False: all absent.
    [InlineData("System.Collections.Generic", "XYZ", false)]
    [InlineData("System.Collections.Generic", "WZ", false)]
    [InlineData("System.Collections.Generic", "ABD", false)]
    //
    // All-lowercase container patterns: only the first character (uppercased) is checked,
    // because we can't determine hump boundaries without casing.
    [InlineData("System.Collections.Generic", "s", true)]    // first char → 'S' present
    [InlineData("System.Collections.Generic", "c", true)]    // first char → 'C' present
    [InlineData("System.Collections.Generic", "g", true)]    // first char → 'G' present
    [InlineData("System.Collections.Generic", "a", false)]   // first char → 'A' not present
    [InlineData("System.Collections.Generic", "b", false)]   // first char → 'B' not present
    [InlineData("System.Collections.Generic", "x", false)]   // first char → 'X' not present
    [InlineData("System.Collections.Generic", "z", false)]   // first char → 'Z' not present
    [InlineData("System.Collections.Generic", "sys", true)]  // first char → 'S' present
    [InlineData("System.Collections.Generic", "col", true)]  // first char → 'C' present
    [InlineData("System.Collections.Generic", "xyz", false)] // first char → 'X' not present
    [InlineData("System.Collections.Generic", "abc", false)] // first char → 'A' not present
    //
    // ═══ Goo.Bar ═══
    // Character-parts: "Goo", "Bar"
    // Hump initials stored: G, B
    //
    [InlineData("Goo.Bar", "G", true)]
    [InlineData("Goo.Bar", "B", true)]
    [InlineData("Goo.Bar", "GB", true)]
    [InlineData("Goo.Bar", "BG", true)]    // order doesn't matter for container
    [InlineData("Goo.Bar", "GA", false)]   // G present, A absent
    [InlineData("Goo.Bar", "GX", false)]   // G present, X absent
    [InlineData("Goo.Bar", "A", false)]    // 'A' not a hump initial
    [InlineData("Goo.Bar", "X", false)]    // 'X' not a hump initial
    [InlineData("Goo.Bar", "XY", false)]   // neither X nor Y are hump initials
    //
    // ═══ Microsoft.CodeAnalysis.CSharp ═══
    // Character-parts: "Microsoft", "Code", "Analysis", "CSharp"
    // Hump initials stored: M, C, A (C appears twice but is deduplicated)
    //
    [InlineData("Microsoft.CodeAnalysis.CSharp", "MCA", true)]
    [InlineData("Microsoft.CodeAnalysis.CSharp", "MC", true)]
    [InlineData("Microsoft.CodeAnalysis.CSharp", "MA", true)]
    [InlineData("Microsoft.CodeAnalysis.CSharp", "CA", true)]
    [InlineData("Microsoft.CodeAnalysis.CSharp", "M", true)]
    [InlineData("Microsoft.CodeAnalysis.CSharp", "A", true)]
    [InlineData("Microsoft.CodeAnalysis.CSharp", "C", true)]
    [InlineData("Microsoft.CodeAnalysis.CSharp", "MX", false)]  // M present, X absent
    [InlineData("Microsoft.CodeAnalysis.CSharp", "B", false)]   // 'B' not a hump initial
    [InlineData("Microsoft.CodeAnalysis.CSharp", "X", false)]   // 'X' not a hump initial
    [InlineData("Microsoft.CodeAnalysis.CSharp", "XZ", false)]  // neither present
    //
    // ═══ GooBarBaz ═══ (single segment, multi-hump — not dotted)
    // Character-parts: "Goo", "Bar", "Baz"
    // Hump initials stored: G, B (B appears twice but is deduplicated)
    //
    [InlineData("GooBarBaz", "GB", true)]
    [InlineData("GooBarBaz", "G", true)]
    [InlineData("GooBarBaz", "B", true)]
    [InlineData("GooBarBaz", "A", false)]  // 'A' not a hump initial
    [InlineData("GooBarBaz", "C", false)]  // 'C' not a hump initial
    [InlineData("GooBarBaz", "X", false)]  // 'X' not a hump initial
    [InlineData("GooBarBaz", "GX", false)] // G present, X absent
    [InlineData("GooBarBaz", "GA", false)] // G present, A absent
    public void ContainerCheck(string symbolContainer, string containerPattern, bool expected)
    {
        // Use a dummy symbol name; we're testing the container set.
        var index = CreateIndex(("Dummy", symbolContainer));
        Assert.Equal(expected, index.GetTestAccessor().ContainerCheckProbablyMatches(containerPattern));
    }

    #endregion

    #region ProbablyContainsMatch — Positive (filter must return true)

    /// <summary>
    /// Tests that <see cref="NavigateToSearchIndex.CouldContainNavigateToMatch"/> returns
    /// <see langword="true"/> for patterns that should match, and verifies that the
    /// <see cref="PatternMatcher"/> produces the expected match kind.
    /// </summary>
    [Theory]
    // ── Exact ──
    [InlineData("GooBar", "", "GooBar", null, PatternMatchKind.Exact)]
    [InlineData("GooBar", "", "goobar", null, PatternMatchKind.Exact)]
    // ── Prefix ──
    [InlineData("GooBar", "", "Goo", null, PatternMatchKind.Prefix)]
    [InlineData("GooBar", "", "goo", null, PatternMatchKind.Prefix)]
    [InlineData("GooBar", "", "G", null, PatternMatchKind.Prefix)]
    [InlineData("GooBar", "", "g", null, PatternMatchKind.Prefix)]
    [InlineData("GooBarQuux", "", "GooBar", null, PatternMatchKind.Prefix)]
    [InlineData("XMLDocument", "", "XMLDoc", null, PatternMatchKind.Prefix)]
    [InlineData("XMLDocument", "", "xmldoc", null, PatternMatchKind.Prefix)]
    // ── CamelCaseExact (mixed-case pattern) ──
    [InlineData("GooBar", "", "GB", null, PatternMatchKind.CamelCaseExact)]
    [InlineData("GooBar", "", "GoBa", null, PatternMatchKind.CamelCaseExact)]
    [InlineData("GooBarQuux", "", "GBQ", null, PatternMatchKind.CamelCaseExact)]
    [InlineData("GooBarQuux", "", "GoBarQu", null, PatternMatchKind.CamelCaseExact)]
    [InlineData("NonEmptyList", "", "NEL", null, PatternMatchKind.CamelCaseExact)]
    [InlineData("CodeFixProvider", "", "CFP", null, PatternMatchKind.CamelCaseExact)]
    [InlineData("XMLDocument", "", "XD", null, PatternMatchKind.CamelCaseExact)]
    // ── CamelCaseExact (all-lowercase pattern) ──
    [InlineData("GooBar", "", "gb", null, PatternMatchKind.CamelCaseExact)]
    [InlineData("GooBar", "", "goba", null, PatternMatchKind.CamelCaseExact)]
    [InlineData("GooBarQuux", "", "gbq", null, PatternMatchKind.CamelCaseExact)]
    [InlineData("NonEmptyList", "", "nel", null, PatternMatchKind.CamelCaseExact)]
    [InlineData("NonEmptyList", "", "neli", null, PatternMatchKind.CamelCaseExact)]
    [InlineData("CodeFixProvider", "", "cofipro", null, PatternMatchKind.CamelCaseExact)]
    // ── CamelCasePrefix (mixed-case) ──
    [InlineData("GooBarQuux", "", "GB", null, PatternMatchKind.CamelCasePrefix)]
    [InlineData("GooBarQuux", "", "GoBa", null, PatternMatchKind.CamelCasePrefix)]
    [InlineData("GooBarQuux", "", "GoBar", null, PatternMatchKind.CamelCasePrefix)]
    [InlineData("CodeFixProviderService", "", "CFP", null, PatternMatchKind.CamelCasePrefix)]
    // ── CamelCasePrefix (all-lowercase) ──
    [InlineData("GooBarQuux", "", "gb", null, PatternMatchKind.CamelCasePrefix)]
    [InlineData("GooBarQuux", "", "goba", null, PatternMatchKind.CamelCasePrefix)]
    [InlineData("CodeFixProviderService", "", "cfp", null, PatternMatchKind.CamelCasePrefix)]
    // ── CamelCaseNonContiguousPrefix (mixed-case) ──
    [InlineData("GooBarQuux", "", "GQ", null, PatternMatchKind.CamelCaseNonContiguousPrefix)]
    [InlineData("GooBarQuux", "", "GoQu", null, PatternMatchKind.CamelCaseNonContiguousPrefix)]
    // ── CamelCaseNonContiguousPrefix (all-lowercase) ──
    [InlineData("GooBarQuux", "", "gq", null, PatternMatchKind.CamelCaseNonContiguousPrefix)]
    [InlineData("GooBarQuux", "", "goqu", null, PatternMatchKind.CamelCaseNonContiguousPrefix)]
    // ── CamelCaseSubstring (mixed-case) ──
    [InlineData("CodeFixProviderService", "", "FP", null, PatternMatchKind.CamelCaseSubstring)]
    // ── CamelCaseSubstring (all-lowercase) ──
    [InlineData("CodeFixProviderService", "", "fp", null, PatternMatchKind.CamelCaseSubstring)]
    // ── CamelCaseNonContiguousSubstring (mixed-case) ──
    [InlineData("CodeFixProviderService", "", "FS", null, PatternMatchKind.CamelCaseNonContiguousSubstring)]
    // ── CamelCaseNonContiguousSubstring (all-lowercase) ──
    [InlineData("CodeFixProviderService", "", "fs", null, PatternMatchKind.CamelCaseNonContiguousSubstring)]
    // ── StartOfWordSubstring ──
    [InlineData("OperatorBinary", "", "Binary", null, PatternMatchKind.StartOfWordSubstring)]
    [InlineData("OperatorBinary", "", "binary", null, PatternMatchKind.StartOfWordSubstring)]
    [InlineData("OperatorBinary", "", "Bin", null, PatternMatchKind.StartOfWordSubstring)]
    [InlineData("OperatorBinary", "", "bin", null, PatternMatchKind.StartOfWordSubstring)]
    // ── LowercaseSubstring ──
    [InlineData("Readline", "", "line", null, PatternMatchKind.LowercaseSubstring)]
    [InlineData("Combine", "", "bin", null, PatternMatchKind.LowercaseSubstring)]
    // ── Fuzzy ──
    [InlineData("GooBar", "", "GozBar", null, PatternMatchKind.Fuzzy)]
    // ── Container matching ──
    [InlineData("Quux", "Goo.Bar", "Qu", "Ba", PatternMatchKind.Prefix)]
    [InlineData("Quux", "Goo.Bar", "Qu", "Go.Ba", PatternMatchKind.Prefix)]
    [InlineData("Quux", "Goo.Bar.Baz", "Qu", "Ba.Baz", PatternMatchKind.Prefix)]
    [InlineData("BazQuux", "GooBar", "BQ", "GB", PatternMatchKind.CamelCaseExact)]
    [InlineData("Quux", "GooBar", "Qu", "gb", PatternMatchKind.Prefix)]
    internal void ProbablyContainsMatch_Positive(
        string symbolName, string symbolContainer,
        string patternName, string? patternContainer,
        PatternMatchKind expectedNameMatchKind)
    {
        var index = CreateIndex((symbolName, symbolContainer));

        Assert.True(index.CouldContainNavigateToMatch(patternName, patternContainer, out var allowFuzzyMatching));

        // All these test cases have simple patterns, so non-fuzzy checks pass.
        // If the length check also passes, the result is All (fuzzy fallback enabled);
        // otherwise it's NonFuzzy.
        var expectedFuzzy = index.GetTestAccessor().LengthCheckProbablyMatches(patternName);
        Assert.Equal(expectedFuzzy, allowFuzzyMatching);

        var nameMatch = GetNameMatch(symbolName, patternName);
        Assert.NotNull(nameMatch);
        Assert.Equal(expectedNameMatchKind, nameMatch.Value.Kind);

        if (patternContainer != null)
        {
            Assert.True(GetContainerMatch(symbolContainer, patternContainer));
        }
    }

    #endregion

    #region ProbablyContainsMatch — Negative (filter must return false)

    [Theory]
    // ── Name mismatches: pattern hump characters are absent from symbol humps ──
    [InlineData("GooBar", "", "Xyz", null)]
    [InlineData("GooBar", "", "GBQ", null)]
    [InlineData("GooBar", "", "XY", null)]
    [InlineData("GooBar", "", "XZ", null)]
    [InlineData("GooBar", "", "ZZ", null)]
    // ── Container mismatches: name matches but container fails ──
    [InlineData("Quux", "Goo.Bar", "Qu", "Xyz")]
    [InlineData("Quux", "Goo.Bar", "Qu", "Go.Qu")]
    // ── Both name and container mismatch ──
    [InlineData("Quux", "Goo.Bar", "XY", "Xyz")]
    public void ProbablyContainsMatch_Negative(
        string symbolName, string symbolContainer,
        string patternName, string? patternContainer)
    {
        var index = CreateIndex((symbolName, symbolContainer));

        Assert.False(index.CouldContainNavigateToMatch(patternName, patternContainer, out _));

        if (patternContainer == null)
        {
            var nameMatch = GetNameMatch(symbolName, patternName);
            Assert.Null(nameMatch);
        }
    }

    #endregion

    #region CrossHumpSubstring — Not Guaranteed

    /// <summary>
    /// Documents that <see cref="PatternMatchKind.NonLowercaseSubstring"/> (cross-hump substring)
    /// matches are found by the PatternMatcher but are intentionally NOT targeted by the prefilter.
    /// The prefilter has no specific logic for these patterns; however, it may incidentally return
    /// true via other checks (e.g. "ooBa" for "GooBar" passes the fuzzy length check because
    /// len 4 vs 6 is within ±2). We intentionally do NOT assert on the prefilter result here --
    /// whether it returns true or false for these cases is an implementation detail, not a contract.
    /// If we ever want to drop support for this match kind entirely, the prefilter cleanly enables
    /// that: just stop searching symbols for documents where only the length check passed.
    /// </summary>
    [Theory]
    [InlineData("GooBar", "ooBa", PatternMatchKind.NonLowercaseSubstring)]
    [InlineData("GooBar", "oBa", PatternMatchKind.NonLowercaseSubstring)]
    [InlineData("OperatorBinary", "torBin", PatternMatchKind.NonLowercaseSubstring)]
    internal void CrossHumpSubstring_NotGuaranteed(
        string symbolName, string patternName,
        PatternMatchKind actualPatternMatcherKind)
    {
        var nameMatch = GetNameMatch(symbolName, patternName);
        Assert.NotNull(nameMatch);
        Assert.Equal(actualPatternMatcherKind, nameMatch.Value.Kind);
    }

    #endregion

    #region Multiple Symbols

    [Fact]
    public void ProbablyContainsMatch_MultipleSymbols_MatchesAny()
    {
        var index = CreateIndex(
            ("Alpha", ""),
            ("GooBar", ""),
            ("Quux", "System.Collections"));

        Assert.True(index.CouldContainNavigateToMatch("GB", null, out _));
        Assert.True(index.CouldContainNavigateToMatch("Al", null, out _));
        Assert.True(index.CouldContainNavigateToMatch("Qu", "Co", out _));

        // "XyzXyzXyzXyz" matches nothing (long enough to escape the ±2 length check too).
        Assert.False(index.CouldContainNavigateToMatch("XyzXyzXyzXyz", null, out _));
    }

    [Fact]
    public void ProbablyContainsMatch_EmptyDocument()
    {
        var index = CreateIndex();
        Assert.False(index.CouldContainNavigateToMatch("anything", null, out _));
    }

    #endregion

    #region Bigram selectivity with multiple symbols

    /// <summary>
    /// Demonstrates that bigram-based hump checks prevent false positives from
    /// characters contributed by different symbols in the same document. Since the
    /// hump data is in an exact <c>FrozenSet&lt;T&gt;</c>, these rejections are guaranteed.
    /// </summary>
    [Fact]
    public void HumpCheck_MultipleSymbols_BigramsPreventCrossSymbolFalsePositives()
    {
        // "Global" has hump 'G'. "Binary" has hump 'B'. No single symbol has hump pair G→B.
        var index = CreateIndex(("Global", ""), ("Binary", ""));

        // Individual chars: both G and B present in the hump set.
        Assert.True(index.GetTestAccessor().HumpCheckProbablyMatches("G"));
        Assert.True(index.GetTestAccessor().HumpCheckProbablyMatches("B"));

        // Multi-hump "GB": bigram G→B is NOT stored because no single symbol has hump pair G→B.
        Assert.False(index.GetTestAccessor().HumpCheckProbablyMatches("GB"));

        // Characters that are not hump initials of any symbol in the document.
        Assert.False(index.GetTestAccessor().HumpCheckProbablyMatches("A"));
        Assert.False(index.GetTestAccessor().HumpCheckProbablyMatches("C"));
        Assert.False(index.GetTestAccessor().HumpCheckProbablyMatches("X"));
        Assert.False(index.GetTestAccessor().HumpCheckProbablyMatches("Z"));
    }

    /// <summary>
    /// When a symbol has the correct bigram, the check passes even with other symbols present.
    /// </summary>
    [Fact]
    public void HumpCheck_MultipleSymbols_CorrectBigramPasses()
    {
        var index = CreateIndex(("Global", ""), ("Binary", ""), ("GooBar", ""));

        // "GooBar" contributes bigram G→B, so "GB" is stored.
        Assert.True(index.GetTestAccessor().HumpCheckProbablyMatches("GB"));

        // Bigram B→G is not stored — no symbol has humps in that order.
        Assert.False(index.GetTestAccessor().HumpCheckProbablyMatches("BG"));
    }

    #endregion

    #region Individual checks are independent

    /// <summary>
    /// Verifies that the hump check, trigram check, and length check operate independently.
    /// A pattern that fails one check can pass through another.
    /// </summary>
    [Fact]
    public void IndividualChecks_AreIndependent()
    {
        var index = CreateIndex(("Readline", ""));

        // "line" — fails hump check (no hump 'L' in "Readline"? Actually 'R' is the only hump)
        // Wait: "Readline" has character parts: "Readline" (one part since 'R' followed by lowercase).
        // So hump initial is just 'R'. Let's verify:

        // Hump check: "line" is all-lowercase, first char 'L'. 'L' is not a hump initial of "Readline" (only 'R').
        // So hump check fails.
        Assert.False(index.GetTestAccessor().HumpCheckProbablyMatches("line"));

        // Trigram check: "line" has trigrams "lin", "ine". Both stored from "Readline". Passes.
        Assert.True(index.GetTestAccessor().TrigramCheckProbablyMatches("line"));

        // Length check: "line" has length 4, "Readline" has length 8. Delta = 4, outside ±2. Fails.
        Assert.False(index.GetTestAccessor().LengthCheckProbablyMatches("line"));

        // Overall: CouldContainNavigateToMatch returns true (trigram check saves it).
        // NonFuzzy because the length check failed — no symbol has length within ±2 of "line".
        Assert.True(index.CouldContainNavigateToMatch("line", null, out var allowFuzzyMatching));
        Assert.False(allowFuzzyMatching);
    }

    /// <summary>
    /// A fuzzy match that only passes via the length check.
    /// </summary>
    [Fact]
    public void LengthCheck_EnablesFuzzyMatch()
    {
        var index = CreateIndex(("GooBar", ""));

        // "GozBar" is a fuzzy match (edit distance 1, same length 6).
        // Hump check: "GozBar" mixed-case, parts ["Goz","Bar"], humps G, B → bigram "GB" → passes.
        // But let's test with a pattern where hump/trigram don't help.

        // "Goxxar" — mixed-case, parts ["Goxxar"] (one part: G followed by lowercase).
        // Single hump 'G' → hump check passes trivially.
        // So let's use a pattern where hump check fails:

        // "Xoobar" — mixed case? No, 'X' upper then all lower → one part, hump 'X'. 'X' not stored.
        Assert.False(index.GetTestAccessor().HumpCheckProbablyMatches("Xoobar"));
        // Trigram: "Xoobar" is not all-lowercase (capital X) → false.
        Assert.False(index.GetTestAccessor().TrigramCheckProbablyMatches("Xoobar"));
        // Length: "Xoobar" length 6 matches "GooBar" length 6. Within ±2. Passes.
        Assert.True(index.GetTestAccessor().LengthCheckProbablyMatches("Xoobar"));
    }

    #endregion

    #region End-to-end: pre-filter → PatternMatcher integration

    /// <summary>
    /// Verifies that a verbatim identifier pattern like "@static" correctly produces an Exact match
    /// against the symbol "static". The pre-filter strips the leading '@' before running hump/trigram
    /// checks, so NonFuzzy is correctly set.
    /// </summary>
    [Fact]
    public void EndToEnd_VerbatimIdentifierPattern_ProducesExactMatch()
    {
        var index = CreateIndex(("static", ""));

        Assert.True(index.CouldContainNavigateToMatch("@static", null, out var allowFuzzyMatching));

        // All because stripping '@' → "static" passes hump check AND length delta = 1 is within ±2.
        Assert.True(allowFuzzyMatching);

        // Verify the PatternMatcher finds the Exact match with the pre-filter's matchKinds.
        using var matcher = PatternMatcher.CreatePatternMatcher("@static", includeMatchedSpans: false, allowFuzzyMatching);
        var match = matcher.GetFirstMatch("static");
        Assert.NotNull(match);
        Assert.Equal(PatternMatchKind.Exact, match.Value.Kind);
    }

    /// <summary>
    /// Verifies that a multi-word pattern like "get word" (with a space) correctly matches
    /// symbols. The pre-filter splits at spaces and checks each word — "get" passes the hump
    /// check because hump 'G' exists, so NonFuzzy is set.
    /// </summary>
    [Fact]
    public void EndToEnd_SpaceInPattern_ProducesSubstringMatch()
    {
        var index = CreateIndex(("get_key_word", ""), ("GetKeyWord", ""));

        Assert.True(index.CouldContainNavigateToMatch("get word", null, out var allowFuzzyMatching));

        // Non-fuzzy checks pass because splitting at space → "get" passes hump check (hump 'G' exists).
        Assert.True(allowFuzzyMatching);

        // Verify the PatternMatcher finds a non-fuzzy match.
        using var matcher = PatternMatcher.CreatePatternMatcher("get word", includeMatchedSpans: false, allowFuzzyMatching);
        var match = matcher.GetFirstMatch("get_key_word");
        Assert.NotNull(match);
        Assert.NotEqual(PatternMatchKind.Fuzzy, match.Value.Kind);
    }

    /// <summary>
    /// Verifies that "line" vs "Readline" produces a <see cref="PatternMatchKind.LowercaseSubstring"/>
    /// match (all-lowercase pattern at a non-word-boundary). NavigateTo maps this to
    /// <c>NavigateToMatchKind.Fuzzy</c> because there is no dedicated <c>NavigateToMatchKind</c>
    /// for lowercase substrings — Fuzzy is the closest available quality tier.
    /// The pre-filter correctly sets NonFuzzy (trigram check passes for "lin"/"ine").
    /// </summary>
    [Fact]
    public void EndToEnd_LowercaseSubstring_MapsToFuzzyInNavigateTo()
    {
        var index = CreateIndex(("Readline", ""));

        Assert.True(index.CouldContainNavigateToMatch("line", null, out var allowFuzzyMatching));

        // NonFuzzy set via trigram check. Fuzzy not set (length 4 vs 8, delta 4 > ±2).
        Assert.False(allowFuzzyMatching);

        // PatternMatcher returns LowercaseSubstring, NOT Substring.
        // "line" is all-lowercase and matches at position 4 in "Readline" (not at a word boundary).
        using var matcher = PatternMatcher.CreatePatternMatcher("line", includeMatchedSpans: false, allowFuzzyMatching);
        var match = matcher.GetFirstMatch("Readline");
        Assert.NotNull(match);
        Assert.Equal(PatternMatchKind.LowercaseSubstring, match.Value.Kind);
    }

    /// <summary>
    /// When hump/trigram fail even after preprocessing, only Fuzzy is set (from the length check).
    /// "Xoobar" shares no hump structure or trigrams with "GooBar", but same length → fuzzy match.
    /// </summary>
    [Fact]
    public void EndToEnd_OnlyLengthCheckPasses_FuzzyOnlyMatchUsed()
    {
        var index = CreateIndex(("GooBar", ""));

        Assert.False(index.GetTestAccessor().HumpCheckProbablyMatches("Xoobar"));
        Assert.False(index.GetTestAccessor().TrigramCheckProbablyMatches("Xoobar"));
        Assert.True(index.GetTestAccessor().LengthCheckProbablyMatches("Xoobar"));

        Assert.True(index.CouldContainNavigateToMatch("Xoobar", null, out var allowFuzzyMatching));

        // All — only length check passes, which enables fuzzy fallback.
        Assert.True(allowFuzzyMatching);

        // PatternMatcher with Fuzzy only: non-fuzzy is skipped, fuzzy finds edit-distance match.
        using var matcher = PatternMatcher.CreatePatternMatcher("Xoobar", includeMatchedSpans: false, allowFuzzyMatching);
        var match = matcher.GetFirstMatch("GooBar");
        Assert.NotNull(match);
        Assert.Equal(PatternMatchKind.Fuzzy, match.Value.Kind);
    }

    #endregion
}
