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

public sealed class RegexPreFilterTests
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
        var query = new RegexQuery.Literal("readline");
        Assert.True(index.GetTestAccessor().RegexQueryCheckPasses(query));
    }

    [Fact]
    public void LiteralQuery_SubstringPresent_Passes()
    {
        var index = CreateIndex(("ReadLine", ""));
        var query = new RegexQuery.Literal("read");
        Assert.True(index.GetTestAccessor().RegexQueryCheckPasses(query));
    }

    [Fact]
    public void AllQuery_BothLiteralsPresent_Passes()
    {
        var index = CreateIndex(("ReadLine", ""));
        var query = new RegexQuery.All([
            new RegexQuery.Literal("read"),
            new RegexQuery.Literal("line"),
        ]);
        Assert.True(index.GetTestAccessor().RegexQueryCheckPasses(query));
    }

    [Fact]
    public void AnyQuery_OneBranchPresent_Passes()
    {
        var index = CreateIndex(("ReadLine", ""));
        var query = new RegexQuery.Any([
            new RegexQuery.Literal("read"),
            new RegexQuery.Literal("write"),
        ]);
        Assert.True(index.GetTestAccessor().RegexQueryCheckPasses(query));
    }

    [Fact]
    public void CaseInsensitive_Passes()
    {
        // RegexQueryCompiler lowercases literals at compile time, so the literal
        // arriving here is already lowercase. The index stores lowercased bigrams,
        // so the check succeeds.
        var index = CreateIndex(("ReadLine", ""));
        var query = new RegexQuery.Literal("readline");
        Assert.True(index.GetTestAccessor().RegexQueryCheckPasses(query));
    }

    [Fact]
    public void ComplexQuery_ReadOrWriteLine_Passes()
    {
        var index = CreateIndex(("ReadLine", ""), ("WriteLine", ""));
        var query = new RegexQuery.All([
            new RegexQuery.Any([
                new RegexQuery.Literal("read"),
                new RegexQuery.Literal("write"),
            ]),
            new RegexQuery.Literal("line"),
        ]);
        Assert.True(index.GetTestAccessor().RegexQueryCheckPasses(query));
    }

    [Fact]
    public void MultipleSymbols_BigramsAccumulate()
    {
        // "Goo" contributes "go","oo" bigrams; "Bar" contributes "ba","ar"
        var index = CreateIndex(("Goo", ""), ("Bar", ""));
        var query = new RegexQuery.All([
            new RegexQuery.Literal("goo"),
            new RegexQuery.Literal("bar"),
        ]);
        Assert.True(index.GetTestAccessor().RegexQueryCheckPasses(query));
    }

    #endregion

    #region RegexQueryCheckPasses — negative (rejects document)

    [Fact]
    public void LiteralQuery_NoMatchingBigrams_Fails()
    {
        var index = CreateIndex(("ReadLine", ""));
        var query = new RegexQuery.Literal("xyz");
        Assert.False(index.GetTestAccessor().RegexQueryCheckPasses(query));
    }

    [Fact]
    public void AllQuery_OneChildFails_Fails()
    {
        var index = CreateIndex(("ReadLine", ""));
        var query = new RegexQuery.All([
            new RegexQuery.Literal("read"),
            new RegexQuery.Literal("xyz"),
        ]);
        Assert.False(index.GetTestAccessor().RegexQueryCheckPasses(query));
    }

    [Fact]
    public void AnyQuery_AllChildrenFail_Fails()
    {
        var index = CreateIndex(("ReadLine", ""));
        var query = new RegexQuery.Any([
            new RegexQuery.Literal("xyz"),
            new RegexQuery.Literal("qwerty"),
        ]);
        Assert.False(index.GetTestAccessor().RegexQueryCheckPasses(query));
    }

    [Fact]
    public void EmptyIndex_FailsOnLiteral()
    {
        var index = CreateIndex();
        var query = new RegexQuery.Literal("goo");
        Assert.False(index.GetTestAccessor().RegexQueryCheckPasses(query));
    }

    [Fact]
    public void TrigramMismatch_Fails()
    {
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
        // "GooBar" has bigrams: go, oo, ob, ba, ar
        // Query Literal("bargoo") has bigrams: ba, ar, rg, go, oo
        // "rg" is not present, so bigram check should reject this.
        var index = CreateIndex(("GooBar", ""));
        var query = new RegexQuery.Literal("bargoo");
        Assert.False(index.GetTestAccessor().RegexQueryCheckPasses(query));
    }

    [Fact]
    public void FalsePositive_Baseline_SharedBigramsAcrossSymbols()
    {
        // Two symbols "Goo" and "Bar" together contribute bigrams: go, oo, ba, ar
        // Query Literal("ooba") has bigrams: oo, ob, ba
        // "ob" is not from either "Goo" or "Bar" individually, but bigrams accumulate
        // across symbols. "Goo" has 'o' and "Bar" has 'b' but the bigram "ob" is only
        // present if some symbol has those two chars adjacent.
        var index = CreateIndex(("Goo", ""), ("Bar", ""));
        var query = new RegexQuery.Literal("ooba");
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
        Assert.True(index.GetTestAccessor().RegexQueryCheckPasses(query));
    }

    #endregion
}
