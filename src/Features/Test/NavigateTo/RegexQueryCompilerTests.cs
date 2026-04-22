// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.PatternMatching;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.NavigateTo;

public sealed class RegexQueryCompilerTests
{
    #region Compilation — positive (produces Literal nodes)

    [Fact]
    public void PlainText_ProducesLiteral()
    {
        var query = RegexQueryCompiler.Compile("ReadLine");
        var literal = Assert.IsType<RegexQuery.Literal>(query);
        Assert.Equal("readline", literal.Text);
    }

    [Fact]
    public void Alternation_ProducesAny()
    {
        // (Read|Write)Line -> All(Any(Literal("read"), Literal("write")), Literal("line"))
        var query = RegexQueryCompiler.Compile("(Read|Write)Line");
        var all = Assert.IsType<RegexQuery.All>(query);
        Assert.Equal(2, all.Children.Length);

        var any = Assert.IsType<RegexQuery.Any>(all.Children[0]);
        Assert.Equal(2, any.Children.Length);
        Assert.Equal("read", Assert.IsType<RegexQuery.Literal>(any.Children[0]).Text);
        Assert.Equal("write", Assert.IsType<RegexQuery.Literal>(any.Children[1]).Text);

        Assert.Equal("line", Assert.IsType<RegexQuery.Literal>(all.Children[1]).Text);
    }

    [Fact]
    public void Sequence_ProducesAll()
    {
        // Goo.*Bar -> All(Literal("goo"), Literal("bar"))  (the .* becomes None, pruned by optimizer)
        var query = RegexQueryCompiler.Compile("Goo.*Bar");
        var all = Assert.IsType<RegexQuery.All>(query);
        Assert.Equal(2, all.Children.Length);
        Assert.Equal("goo", Assert.IsType<RegexQuery.Literal>(all.Children[0]).Text);
        Assert.Equal("bar", Assert.IsType<RegexQuery.Literal>(all.Children[1]).Text);
    }

    [Fact]
    public void OneOrMore_PreservesInner()
    {
        // Goo+ -> the parser sees "Go" as text, then o+ as OneOrMore(text("o"))
        var query = RegexQueryCompiler.Compile("Goo+");
        Assert.True(query!.HasLiterals);
    }

    [Fact]
    public void EscapedDot_ProducesLiteral()
    {
        // Goo\.Bar -> the escaped dot is a literal '.'
        var query = RegexQueryCompiler.Compile(@"Goo\.Bar");
        Assert.NotNull(query);
        Assert.True(query!.HasLiterals);
    }

    [Fact]
    public void EscapedBracket_ProducesLiteral()
    {
        // \[Test\] -> literals '[', 'T', 'e', 's', 't', ']'
        var query = RegexQueryCompiler.Compile(@"\[Test\]");
        Assert.NotNull(query);
        Assert.True(query!.HasLiterals);
    }

    [Fact]
    public void NonCapturingGroup_RecursesInto()
    {
        // (?:Read|Write) -> Any(Literal("read"), Literal("write"))
        var query = RegexQueryCompiler.Compile("(?:Read|Write)");
        var any = Assert.IsType<RegexQuery.Any>(query);
        Assert.Equal(2, any.Children.Length);
        Assert.Equal("read", Assert.IsType<RegexQuery.Literal>(any.Children[0]).Text);
        Assert.Equal("write", Assert.IsType<RegexQuery.Literal>(any.Children[1]).Text);
    }

    [Fact]
    public void ExactNumericQuantifier_WithMinOne_PreservesInner()
    {
        // (Go){3} -> the group "Go" produces Literal("go"), and exact count >= 1 preserves it
        var query = RegexQueryCompiler.Compile("(Go){3}");
        Assert.True(query!.HasLiterals);
        Assert.Equal("go", Assert.IsType<RegexQuery.Literal>(query).Text);
    }

    [Fact]
    public void OpenRangeQuantifier_WithMinOne_PreservesInner()
    {
        // (Go){1,} -> Literal("go")
        var query = RegexQueryCompiler.Compile("(Go){1,}");
        Assert.True(query!.HasLiterals);
        Assert.Equal("go", Assert.IsType<RegexQuery.Literal>(query).Text);
    }

    [Fact]
    public void ClosedRangeQuantifier_WithMinOne_PreservesInner()
    {
        // (Go){1,5} -> Literal("go")
        var query = RegexQueryCompiler.Compile("(Go){1,5}");
        Assert.True(query!.HasLiterals);
        Assert.Equal("go", Assert.IsType<RegexQuery.Literal>(query).Text);
    }

    #endregion

    #region Compilation — negative (returns null: no extractable literals)

    [Fact]
    public void DotStar_ReturnsNull()
        => Assert.Null(RegexQueryCompiler.Compile(".*"));

    [Fact]
    public void SingleDot_ReturnsNull()
        => Assert.Null(RegexQueryCompiler.Compile("."));

    [Fact]
    public void CharacterClassEscape_ReturnsNull()
        => Assert.Null(RegexQueryCompiler.Compile(@"\d+"));

    [Fact]
    public void CharacterClass_ReturnsNull()
        => Assert.Null(RegexQueryCompiler.Compile("[abc]"));

    [Fact]
    public void ZeroOrMore_ReturnsNull()
        => Assert.Null(RegexQueryCompiler.Compile("a*"));

    [Fact]
    public void ZeroOrOne_ReturnsNull()
        => Assert.Null(RegexQueryCompiler.Compile("a?"));

    [Fact]
    public void ZeroOrMoreLazy_ReturnsNull()
        => Assert.Null(RegexQueryCompiler.Compile("a*?"));

    [Fact]
    public void NumericQuantifier_WithMinZero_ReturnsNull()
        => Assert.Null(RegexQueryCompiler.Compile("a{0,5}"));

    [Fact]
    public void OpenRangeQuantifier_WithMinZero_ReturnsNull()
        => Assert.Null(RegexQueryCompiler.Compile("a{0,}"));

    [Fact]
    public void InvalidRegex_ReturnsNull()
        => Assert.Null(RegexQueryCompiler.Compile("(abc"));

    [Fact]
    public void AnchorOnly_ReturnsNull()
        => Assert.Null(RegexQueryCompiler.Compile("^$"));

    [Fact]
    public void WhitespaceOnlyText_ReturnsNull()
        => Assert.Null(RegexQueryCompiler.Compile("^ $"));

    #endregion

    #region Optimization

    [Fact]
    public void Optimizer_FlattenNestedAll()
    {
        var inner = new RegexQuery.All([new RegexQuery.Literal("aa"), new RegexQuery.Literal("bb")]);
        var outer = new RegexQuery.All([inner, new RegexQuery.Literal("cc")]);

        var optimized = RegexQuery.Optimize(outer);
        var all = Assert.IsType<RegexQuery.All>(optimized);
        Assert.Equal(3, all.Children.Length);
        Assert.Equal("aa", Assert.IsType<RegexQuery.Literal>(all.Children[0]).Text);
        Assert.Equal("bb", Assert.IsType<RegexQuery.Literal>(all.Children[1]).Text);
        Assert.Equal("cc", Assert.IsType<RegexQuery.Literal>(all.Children[2]).Text);
    }

    [Fact]
    public void Optimizer_FlattenNestedAny()
    {
        var inner = new RegexQuery.Any([new RegexQuery.Literal("aa"), new RegexQuery.Literal("bb")]);
        var outer = new RegexQuery.Any([inner, new RegexQuery.Literal("cc")]);

        var optimized = RegexQuery.Optimize(outer);
        var any = Assert.IsType<RegexQuery.Any>(optimized);
        Assert.Equal(3, any.Children.Length);
    }

    [Fact]
    public void Optimizer_PruneNoneFromAll()
    {
        var query = new RegexQuery.All([
            new RegexQuery.Literal("aa"),
            RegexQuery.None.Instance,
            new RegexQuery.Literal("bb"),
        ]);

        var optimized = RegexQuery.Optimize(query);
        var all = Assert.IsType<RegexQuery.All>(optimized);
        Assert.Equal(2, all.Children.Length);
        Assert.Equal("aa", Assert.IsType<RegexQuery.Literal>(all.Children[0]).Text);
        Assert.Equal("bb", Assert.IsType<RegexQuery.Literal>(all.Children[1]).Text);
    }

    [Fact]
    public void Optimizer_NoneInAny_CollapsesToNone()
    {
        var query = new RegexQuery.Any([
            new RegexQuery.Literal("aa"),
            RegexQuery.None.Instance,
        ]);

        var optimized = RegexQuery.Optimize(query);
        Assert.IsType<RegexQuery.None>(optimized);
    }

    [Fact]
    public void Optimizer_SingleChildAll_Unwraps()
    {
        var query = new RegexQuery.All([new RegexQuery.Literal("aa")]);
        var optimized = RegexQuery.Optimize(query);
        Assert.Equal("aa", Assert.IsType<RegexQuery.Literal>(optimized).Text);
    }

    [Fact]
    public void Optimizer_SingleChildAny_Unwraps()
    {
        var query = new RegexQuery.Any([new RegexQuery.Literal("aa")]);
        var optimized = RegexQuery.Optimize(query);
        Assert.Equal("aa", Assert.IsType<RegexQuery.Literal>(optimized).Text);
    }

    [Fact]
    public void Optimizer_AllNone_CollapsesToNone()
    {
        var query = new RegexQuery.All([RegexQuery.None.Instance, RegexQuery.None.Instance]);
        var optimized = RegexQuery.Optimize(query);
        Assert.IsType<RegexQuery.None>(optimized);
    }

    #endregion

    #region Single-char literals are too short for pre-filtering

    [Fact]
    public void SingleCharLiteral_ReturnsNull()
        => Assert.Null(RegexQueryCompiler.Compile("a"));

    [Fact]
    public void SingleCharAlternation_ReturnsNull()
        => Assert.Null(RegexQueryCompiler.Compile("(a|b)"));

    [Fact]
    public void SingleCharWithWildcard_ReturnsNull()
        => Assert.Null(RegexQueryCompiler.Compile("a.b"));

    #endregion

    #region HasLiterals

    [Fact]
    public void HasLiterals_True_ForLiteral()
    {
        Assert.True(new RegexQuery.Literal("goo").HasLiterals);
    }

    [Fact]
    public void HasLiterals_False_ForNone()
    {
        Assert.False(RegexQuery.None.Instance.HasLiterals);
    }

    [Fact]
    public void HasLiterals_True_ForAllWithLiteral()
    {
        var query = new RegexQuery.All([new RegexQuery.Literal("goo"), RegexQuery.None.Instance]);
        Assert.True(query.HasLiterals);
    }

    [Fact]
    public void HasLiterals_True_ForAnyWithLiteral()
    {
        var query = new RegexQuery.Any([new RegexQuery.Literal("goo"), new RegexQuery.Literal("bar")]);
        Assert.True(query.HasLiterals);
    }

    [Fact]
    public void HasLiterals_False_ForAllOfNone()
    {
        var query = new RegexQuery.All([RegexQuery.None.Instance]);
        Assert.False(query.HasLiterals);
    }

    #endregion

    #region End-to-end compilation + optimization

    [Fact]
    public void EndToEnd_AlternationWithSharedSuffix()
    {
        var query = RegexQueryCompiler.Compile("(Read|Write)Line")!;
        Assert.True(query.HasLiterals);

        var all = Assert.IsType<RegexQuery.All>(query);
        Assert.Equal(2, all.Children.Length);
        Assert.IsType<RegexQuery.Any>(all.Children[0]);
        Assert.IsType<RegexQuery.Literal>(all.Children[1]);
    }

    [Fact]
    public void EndToEnd_OptionalSuffix()
    {
        // Read(Line)? -> "read" is required, "(Line)?" is optional (None)
        // After optimization: Literal("read")
        var query = RegexQueryCompiler.Compile("Read(Line)?")!;
        Assert.True(query.HasLiterals);
        Assert.Equal("read", Assert.IsType<RegexQuery.Literal>(query).Text);
    }

    [Fact]
    public void EndToEnd_DotStarBetweenLiterals()
    {
        // Goo.*Bar -> All(Literal("goo"), Literal("bar"))
        var query = RegexQueryCompiler.Compile("Goo.*Bar")!;
        var all = Assert.IsType<RegexQuery.All>(query);
        Assert.Equal(2, all.Children.Length);
        Assert.Equal("goo", Assert.IsType<RegexQuery.Literal>(all.Children[0]).Text);
        Assert.Equal("bar", Assert.IsType<RegexQuery.Literal>(all.Children[1]).Text);
    }

    [Fact]
    public void EndToEnd_DotPlusBetweenLiterals()
    {
        // Goo.+Bar -> All(Literal("goo"), Literal("bar"))
        var query = RegexQueryCompiler.Compile("Goo.+Bar")!;
        var all = Assert.IsType<RegexQuery.All>(query);
        Assert.Equal(2, all.Children.Length);
        Assert.Equal("goo", Assert.IsType<RegexQuery.Literal>(all.Children[0]).Text);
        Assert.Equal("bar", Assert.IsType<RegexQuery.Literal>(all.Children[1]).Text);
    }

    [Fact]
    public void EndToEnd_ComplexPattern()
    {
        // (Get|Set)(Value|Item)s? -> All(Any("get","set"), Any("value","item"))
        var query = RegexQueryCompiler.Compile("(Get|Set)(Value|Item)s?")!;
        Assert.True(query.HasLiterals);

        var all = Assert.IsType<RegexQuery.All>(query);
        Assert.Equal(2, all.Children.Length);

        var first = Assert.IsType<RegexQuery.Any>(all.Children[0]);
        Assert.Equal("get", Assert.IsType<RegexQuery.Literal>(first.Children[0]).Text);
        Assert.Equal("set", Assert.IsType<RegexQuery.Literal>(first.Children[1]).Text);

        var second = Assert.IsType<RegexQuery.Any>(all.Children[1]);
        Assert.Equal("value", Assert.IsType<RegexQuery.Literal>(second.Children[0]).Text);
        Assert.Equal("item", Assert.IsType<RegexQuery.Literal>(second.Children[1]).Text);
    }

    [Fact]
    public void EndToEnd_AlternationOfPureDotStar_ReturnsNull()
        => Assert.Null(RegexQueryCompiler.Compile("(.*|.+)"));

    [Fact]
    public void EndToEnd_AnchorWithLiteral()
    {
        // ^Goo$ -> All(Literal("goo")) -> Literal("goo")
        var query = RegexQueryCompiler.Compile("^Goo$")!;
        Assert.Equal("goo", Assert.IsType<RegexQuery.Literal>(query).Text);
    }

    [Fact]
    public void EndToEnd_MixedLiteralsAndWildcards()
    {
        // Read.Line -> All(Literal("read"), Literal("line"))
        var query = RegexQueryCompiler.Compile("Read.Line")!;
        var all = Assert.IsType<RegexQuery.All>(query);
        Assert.Equal(2, all.Children.Length);
        Assert.Equal("read", Assert.IsType<RegexQuery.Literal>(all.Children[0]).Text);
        Assert.Equal("line", Assert.IsType<RegexQuery.Literal>(all.Children[1]).Text);
    }

    #endregion
}
