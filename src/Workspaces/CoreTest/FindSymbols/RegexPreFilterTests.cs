// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.PatternMatching;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.FindSymbols;

public class RegexPreFilterTests
{
    private static NavigateToSearchIndex CreateIndex(params (string name, string container)[] symbols)
    {
        var infos = symbols.Select(s => CreateInfo(s.name, s.container)).ToImmutableArray();
        return NavigateToSearchIndex.TestAccessor.CreateIndex(infos);
    }

    private static DeclaredSymbolInfo CreateInfo(string name, string container)
    {
        var stringTable = new StringTable();
        return DeclaredSymbolInfo.Create(
            stringTable, name, nameSuffix: null, containerDisplayName: null,
            fullyQualifiedContainerName: container, isPartial: false, hasAttributes: false,
            DeclaredSymbolInfoKind.Class, Accessibility.Public,
            default, ImmutableArray<string>.Empty);
    }

    #region RegexQueryCheckPasses — positive (passes pre-filter)

    [Fact]
    public void LiteralQuery_MatchingBigrams_Passes()
    {
        var index = CreateIndex(("ReadLine", ""));
        var query = new RegexQuery.Literal("ReadLine");
        Assert.True(index.GetTestAccessor().RegexQueryCheckPasses(query));
    }

    [Fact]
    public void LiteralQuery_SubstringPresent_Passes()
    {
        var index = CreateIndex(("ReadLine", ""));
        var query = new RegexQuery.Literal("Read");
        Assert.True(index.GetTestAccessor().RegexQueryCheckPasses(query));
    }

    [Fact]
    public void AllQuery_BothLiteralsPresent_Passes()
    {
        // "ReadLine" contains bigrams/trigrams for both "Read" and "Line"
        var index = CreateIndex(("ReadLine", ""));
        var query = new RegexQuery.All([
            new RegexQuery.Literal("Read"),
            new RegexQuery.Literal("Line"),
        ]);
        Assert.True(index.GetTestAccessor().RegexQueryCheckPasses(query));
    }

    [Fact]
    public void AnyQuery_OneBranchPresent_Passes()
    {
        var index = CreateIndex(("ReadLine", ""));
        var query = new RegexQuery.Any([
            new RegexQuery.Literal("Read"),
            new RegexQuery.Literal("Write"),
        ]);
        Assert.True(index.GetTestAccessor().RegexQueryCheckPasses(query));
    }

    [Fact]
    public void NoneQuery_AlwaysPasses()
    {
        var index = CreateIndex(("Anything", ""));
        Assert.True(index.GetTestAccessor().RegexQueryCheckPasses(RegexQuery.None.Instance));
    }

    [Fact]
    public void AllWithNone_PassesIfLiteralsPass()
    {
        var index = CreateIndex(("ReadLine", ""));
        var query = new RegexQuery.All([
            new RegexQuery.Literal("Read"),
            RegexQuery.None.Instance,
        ]);
        Assert.True(index.GetTestAccessor().RegexQueryCheckPasses(query));
    }

    [Fact]
    public void CaseInsensitive_Passes()
    {
        // Index stores lowercased bigrams; literal "READ" should be lowercased in the check
        var index = CreateIndex(("ReadLine", ""));
        var query = new RegexQuery.Literal("READ");
        Assert.True(index.GetTestAccessor().RegexQueryCheckPasses(query));
    }

    [Fact]
    public void ShortLiteral_SingleChar_Passes()
    {
        // Single character literal has no bigrams/trigrams to check — should pass
        var index = CreateIndex(("ReadLine", ""));
        var query = new RegexQuery.Literal("R");
        Assert.True(index.GetTestAccessor().RegexQueryCheckPasses(query));
    }

    [Fact]
    public void EmptyLiteral_Passes()
    {
        var index = CreateIndex(("ReadLine", ""));
        var query = new RegexQuery.Literal("");
        Assert.True(index.GetTestAccessor().RegexQueryCheckPasses(query));
    }

    [Fact]
    public void ComplexQuery_ReadOrWriteLine_Passes()
    {
        // Document has "ReadLine" and "WriteLine"
        // Query: All(Any(Literal("Read"), Literal("Write")), Literal("Line"))
        var index = CreateIndex(("ReadLine", ""), ("WriteLine", ""));
        var query = new RegexQuery.All([
            new RegexQuery.Any([
                new RegexQuery.Literal("Read"),
                new RegexQuery.Literal("Write"),
            ]),
            new RegexQuery.Literal("Line"),
        ]);
        Assert.True(index.GetTestAccessor().RegexQueryCheckPasses(query));
    }

    [Fact]
    public void MultipleSymbols_BigramsAccumulate()
    {
        // "Foo" contributes "fo","oo" bigrams; "Bar" contributes "ba","ar"
        // Query All(Literal("Foo"), Literal("Bar")) should pass because each literal's
        // bigrams are present across the accumulated index.
        var index = CreateIndex(("Foo", ""), ("Bar", ""));
        var query = new RegexQuery.All([
            new RegexQuery.Literal("Foo"),
            new RegexQuery.Literal("Bar"),
        ]);
        Assert.True(index.GetTestAccessor().RegexQueryCheckPasses(query));
    }

    #endregion

    #region RegexQueryCheckPasses — negative (rejects document)

    [Fact]
    public void LiteralQuery_NoMatchingBigrams_Fails()
    {
        var index = CreateIndex(("ReadLine", ""));
        var query = new RegexQuery.Literal("Xyz");
        Assert.False(index.GetTestAccessor().RegexQueryCheckPasses(query));
    }

    [Fact]
    public void AllQuery_OneChildFails_Fails()
    {
        var index = CreateIndex(("ReadLine", ""));
        var query = new RegexQuery.All([
            new RegexQuery.Literal("Read"),
            new RegexQuery.Literal("Xyz"),
        ]);
        Assert.False(index.GetTestAccessor().RegexQueryCheckPasses(query));
    }

    [Fact]
    public void AnyQuery_AllChildrenFail_Fails()
    {
        var index = CreateIndex(("ReadLine", ""));
        var query = new RegexQuery.Any([
            new RegexQuery.Literal("Xyz"),
            new RegexQuery.Literal("Qwerty"),
        ]);
        Assert.False(index.GetTestAccessor().RegexQueryCheckPasses(query));
    }

    [Fact]
    public void EmptyIndex_FailsOnLiteral()
    {
        var index = CreateIndex();
        var query = new RegexQuery.Literal("Foo");
        Assert.False(index.GetTestAccessor().RegexQueryCheckPasses(query));
    }

    [Fact]
    public void TrigramMismatch_Fails()
    {
        // "abc" trigram "abc" — if the document has "xyz", the Bloom filter rejects
        var index = CreateIndex(("xyz", ""));
        var query = new RegexQuery.Literal("abc");
        Assert.False(index.GetTestAccessor().RegexQueryCheckPasses(query));
    }

    #endregion

    #region False-positive baselines

    /// <summary>
    /// Documents the known false-positive rate of bigram/trigram pre-filtering.
    /// These are cases where the pre-filter passes but the regex would not actually match.
    /// This establishes a baseline — future improvements (e.g. sparse n-grams) should
    /// reduce these false positives.
    /// </summary>
    [Fact]
    public void FalsePositive_Baseline_ReorderedBigrams()
    {
        // "FooBar" has bigrams: fo, oo, ob, ba, ar
        // Query Literal("BarFoo") has bigrams: ba, ar, rf, fo, oo
        // "rf" is not present, so bigram check should reject this.
        var index = CreateIndex(("FooBar", ""));
        var query = new RegexQuery.Literal("BarFoo");
        // This should correctly fail because "rf" bigram is missing.
        Assert.False(index.GetTestAccessor().RegexQueryCheckPasses(query));
    }

    [Fact]
    public void FalsePositive_Baseline_SharedBigramsAcrossSymbols()
    {
        // Two symbols "Foo" and "Bar" together contribute bigrams: fo, oo, ba, ar
        // Query Literal("ooba") has bigrams: oo, ob, ba
        // "ob" is not from either "Foo" or "Bar" individually, but bigrams accumulate
        // across symbols. "Foo" has 'o' and "Bar" has 'b' but the bigram "ob" is only
        // present if some symbol has those two chars adjacent.
        var index = CreateIndex(("Foo", ""), ("Bar", ""));
        var query = new RegexQuery.Literal("ooba");
        // "ob" bigram is NOT present (no symbol has 'o' followed by 'b')
        Assert.False(index.GetTestAccessor().RegexQueryCheckPasses(query));
    }

    [Fact]
    public void FalsePositive_Baseline_PermutedTrigrams()
    {
        // Document has "abcdef" — trigrams: abc, bcd, cde, def
        // Query Literal("defabc") has trigrams: def, efa, fab, abc
        // "efa" and "fab" are not present → correctly rejected.
        var index = CreateIndex(("abcdef", ""));
        var query = new RegexQuery.Literal("defabc");
        Assert.False(index.GetTestAccessor().RegexQueryCheckPasses(query));
    }

    [Fact]
    public void FalsePositive_Baseline_BigramCollision()
    {
        // The bigram bitset maps non-ASCII chars to an "other" bucket (index 37).
        // Two different non-ASCII chars may collide, causing a false positive.
        // "αβ" maps to (37,37). If we search for "γδ" it also maps to (37,37) → false positive.
        var index = CreateIndex(("αβ", ""));
        var query = new RegexQuery.Literal("γδ");
        // This is a known false positive: both map to the "other" bigram bucket.
        Assert.True(index.GetTestAccessor().RegexQueryCheckPasses(query));
    }

    #endregion
}
