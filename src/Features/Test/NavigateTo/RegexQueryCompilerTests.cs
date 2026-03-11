// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.NavigateTo;
using Microsoft.CodeAnalysis.PatternMatching;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests.NavigateTo;

public class RegexQueryCompilerTests
{
    #region Compilation — positive (produces Literal nodes)

    [Fact]
    public void PlainText_ProducesLiteral()
    {
        var query = RegexQueryCompiler.Compile("ReadLine");
        var literal = Assert.IsType<RegexQuery.Literal>(query);
        Assert.Equal("ReadLine", literal.Text);
    }

    [Fact]
    public void Alternation_ProducesAny()
    {
        // (Read|Write)Line -> All(Any(Literal("Read"), Literal("Write")), Literal("Line"))
        var query = RegexQueryCompiler.Compile("(Read|Write)Line");
        var all = Assert.IsType<RegexQuery.All>(query);
        Assert.Equal(2, all.Children.Length);

        var any = Assert.IsType<RegexQuery.Any>(all.Children[0]);
        Assert.Equal(2, any.Children.Length);
        Assert.Equal("Read", Assert.IsType<RegexQuery.Literal>(any.Children[0]).Text);
        Assert.Equal("Write", Assert.IsType<RegexQuery.Literal>(any.Children[1]).Text);

        Assert.Equal("Line", Assert.IsType<RegexQuery.Literal>(all.Children[1]).Text);
    }

    [Fact]
    public void Sequence_ProducesAll()
    {
        // Goo.*Bar -> All(Literal("Goo"), Literal("Bar"))  (the .* becomes None, pruned by optimizer)
        var query = RegexQueryCompiler.Compile("Goo.*Bar");
        var all = Assert.IsType<RegexQuery.All>(query);
        Assert.Equal(2, all.Children.Length);
        Assert.Equal("Goo", Assert.IsType<RegexQuery.Literal>(all.Children[0]).Text);
        Assert.Equal("Bar", Assert.IsType<RegexQuery.Literal>(all.Children[1]).Text);
    }

    [Fact]
    public void OneOrMore_PreservesInner()
    {
        // Goo+ -> the parser sees "Fo" as text, then o+ as OneOrMore(text("o"))
        // Result: All(Literal("Fo"), Literal("o")) or just Literal("Fo") + Literal("o")
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
        // (?:Read|Write) -> Any(Literal("Read"), Literal("Write"))
        var query = RegexQueryCompiler.Compile("(?:Read|Write)");
        var any = Assert.IsType<RegexQuery.Any>(query);
        Assert.Equal(2, any.Children.Length);
        Assert.Equal("Read", Assert.IsType<RegexQuery.Literal>(any.Children[0]).Text);
        Assert.Equal("Write", Assert.IsType<RegexQuery.Literal>(any.Children[1]).Text);
    }

    [Fact]
    public void ExactNumericQuantifier_WithMinOne_PreservesInner()
    {
        // a{3} -> Literal("a") (exact count >= 1, so at least one match required)
        var query = RegexQueryCompiler.Compile("a{3}");
        Assert.True(query!.HasLiterals);
    }

    [Fact]
    public void OpenRangeQuantifier_WithMinOne_PreservesInner()
    {
        // a{1,} -> Literal("a")
        var query = RegexQueryCompiler.Compile("a{1,}");
        Assert.True(query!.HasLiterals);
    }

    [Fact]
    public void ClosedRangeQuantifier_WithMinOne_PreservesInner()
    {
        // a{1,5} -> Literal("a")
        var query = RegexQueryCompiler.Compile("a{1,5}");
        Assert.True(query!.HasLiterals);
    }

    #endregion

    #region Compilation — negative (produces None / no literals)

    [Fact]
    public void DotStar_ProducesNone()
    {
        var query = RegexQueryCompiler.Compile(".*");
        Assert.IsType<RegexQuery.None>(query);
    }

    [Fact]
    public void SingleDot_ProducesNone()
    {
        var query = RegexQueryCompiler.Compile(".");
        Assert.IsType<RegexQuery.None>(query);
    }

    [Fact]
    public void CharacterClassEscape_ProducesNone()
    {
        // \d+ -> None (character class escapes cannot produce literals)
        var query = RegexQueryCompiler.Compile(@"\d+");
        Assert.IsType<RegexQuery.None>(query);
    }

    [Fact]
    public void CharacterClass_ProducesNone()
    {
        // [abc] -> None
        var query = RegexQueryCompiler.Compile("[abc]");
        Assert.IsType<RegexQuery.None>(query);
    }

    [Fact]
    public void ZeroOrMore_DropsStar()
    {
        // a* -> None (zero matches is valid, can't require the literal)
        var query = RegexQueryCompiler.Compile("a*");
        Assert.IsType<RegexQuery.None>(query);
    }

    [Fact]
    public void ZeroOrOne_DropsQuestion()
    {
        // a? -> None
        var query = RegexQueryCompiler.Compile("a?");
        Assert.IsType<RegexQuery.None>(query);
    }

    [Fact]
    public void ZeroOrMoreLazy_DropsStarQuestion()
    {
        // a*? -> None
        var query = RegexQueryCompiler.Compile("a*?");
        Assert.IsType<RegexQuery.None>(query);
    }

    [Fact]
    public void NumericQuantifier_WithMinZero_ProducesNone()
    {
        // a{0,5} -> None
        var query = RegexQueryCompiler.Compile("a{0,5}");
        Assert.IsType<RegexQuery.None>(query);
    }

    [Fact]
    public void OpenRangeQuantifier_WithMinZero_ProducesNone()
    {
        // a{0,} -> None (equivalent to a*)
        var query = RegexQueryCompiler.Compile("a{0,}");
        Assert.IsType<RegexQuery.None>(query);
    }

    [Fact]
    public void InvalidRegex_ReturnsNull()
    {
        // Unbalanced parenthesis
        var query = RegexQueryCompiler.Compile("(abc");
        Assert.Null(query);
    }

    [Fact]
    public void AnchorOnly_ProducesNone()
    {
        // ^$ -> anchors only, no literal text
        var query = RegexQueryCompiler.Compile("^$");
        Assert.False(query!.HasLiterals);
    }

    #endregion

    #region Optimization

    [Fact]
    public void Optimizer_FlattenNestedAll()
    {
        // Manually construct All(All(a, b), c) and verify it flattens
        var inner = new RegexQuery.All([new RegexQuery.Literal("a"), new RegexQuery.Literal("b")]);
        var outer = new RegexQuery.All([inner, new RegexQuery.Literal("c")]);

        var optimized = RegexQuery.Optimize(outer);
        var all = Assert.IsType<RegexQuery.All>(optimized);
        Assert.Equal(3, all.Children.Length);
        Assert.Equal("a", Assert.IsType<RegexQuery.Literal>(all.Children[0]).Text);
        Assert.Equal("b", Assert.IsType<RegexQuery.Literal>(all.Children[1]).Text);
        Assert.Equal("c", Assert.IsType<RegexQuery.Literal>(all.Children[2]).Text);
    }

    [Fact]
    public void Optimizer_FlattenNestedAny()
    {
        var inner = new RegexQuery.Any([new RegexQuery.Literal("a"), new RegexQuery.Literal("b")]);
        var outer = new RegexQuery.Any([inner, new RegexQuery.Literal("c")]);

        var optimized = RegexQuery.Optimize(outer);
        var any = Assert.IsType<RegexQuery.Any>(optimized);
        Assert.Equal(3, any.Children.Length);
    }

    [Fact]
    public void Optimizer_PruneNoneFromAll()
    {
        // All(Literal("a"), None, Literal("b")) -> All(Literal("a"), Literal("b"))
        var query = new RegexQuery.All([
            new RegexQuery.Literal("a"),
            RegexQuery.None.Instance,
            new RegexQuery.Literal("b"),
        ]);

        var optimized = RegexQuery.Optimize(query);
        var all = Assert.IsType<RegexQuery.All>(optimized);
        Assert.Equal(2, all.Children.Length);
        Assert.Equal("a", Assert.IsType<RegexQuery.Literal>(all.Children[0]).Text);
        Assert.Equal("b", Assert.IsType<RegexQuery.Literal>(all.Children[1]).Text);
    }

    [Fact]
    public void Optimizer_NoneInAny_CollapsesToNone()
    {
        // Any(Literal("a"), None) -> None (OR with unknown = could match anything)
        var query = new RegexQuery.Any([
            new RegexQuery.Literal("a"),
            RegexQuery.None.Instance,
        ]);

        var optimized = RegexQuery.Optimize(query);
        Assert.IsType<RegexQuery.None>(optimized);
    }

    [Fact]
    public void Optimizer_SingleChildAll_Unwraps()
    {
        var query = new RegexQuery.All([new RegexQuery.Literal("a")]);
        var optimized = RegexQuery.Optimize(query);
        Assert.Equal("a", Assert.IsType<RegexQuery.Literal>(optimized).Text);
    }

    [Fact]
    public void Optimizer_SingleChildAny_Unwraps()
    {
        var query = new RegexQuery.Any([new RegexQuery.Literal("a")]);
        var optimized = RegexQuery.Optimize(query);
        Assert.Equal("a", Assert.IsType<RegexQuery.Literal>(optimized).Text);
    }

    [Fact]
    public void Optimizer_AllNone_CollapsesToNone()
    {
        var query = new RegexQuery.All([RegexQuery.None.Instance, RegexQuery.None.Instance]);
        var optimized = RegexQuery.Optimize(query);
        Assert.IsType<RegexQuery.None>(optimized);
    }

    #endregion

    #region HasLiterals

    [Fact]
    public void HasLiterals_True_ForLiteral()
    {
        Assert.True(new RegexQuery.Literal("foo").HasLiterals);
    }

    [Fact]
    public void HasLiterals_False_ForNone()
    {
        Assert.False(RegexQuery.None.Instance.HasLiterals);
    }

    [Fact]
    public void HasLiterals_True_ForAllWithLiteral()
    {
        var query = new RegexQuery.All([new RegexQuery.Literal("foo"), RegexQuery.None.Instance]);
        Assert.True(query.HasLiterals);
    }

    [Fact]
    public void HasLiterals_True_ForAnyWithLiteral()
    {
        var query = new RegexQuery.Any([new RegexQuery.Literal("foo"), new RegexQuery.Literal("bar")]);
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
        // (Read|Write)Line -> All(Any(Literal("Read"), Literal("Write")), Literal("Line"))
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
        // Read(Line)? -> "Read" is required, "(Line)?" is optional (None)
        // After optimization: Literal("Read")
        var query = RegexQueryCompiler.Compile("Read(Line)?")!;
        Assert.True(query.HasLiterals);
        Assert.Equal("Read", Assert.IsType<RegexQuery.Literal>(query).Text);
    }

    [Fact]
    public void EndToEnd_DotStarBetweenLiterals()
    {
        // Goo.*Bar -> All(Literal("Goo"), Literal("Bar"))
        var query = RegexQueryCompiler.Compile("Goo.*Bar")!;
        var all = Assert.IsType<RegexQuery.All>(query);
        Assert.Equal(2, all.Children.Length);
        Assert.Equal("Goo", Assert.IsType<RegexQuery.Literal>(all.Children[0]).Text);
        Assert.Equal("Bar", Assert.IsType<RegexQuery.Literal>(all.Children[1]).Text);
    }

    [Fact]
    public void EndToEnd_DotPlusBetweenLiterals()
    {
        // Goo.+Bar -> All(Literal("Goo"), Literal("Bar"))
        // The .+ is OneOrMore(Wildcard) -> Wildcard compiles to None -> OneOrMore preserves inner -> None
        // After optimization: All(Literal("Goo"), Literal("Bar"))
        var query = RegexQueryCompiler.Compile("Goo.+Bar")!;
        var all = Assert.IsType<RegexQuery.All>(query);
        Assert.Equal(2, all.Children.Length);
        Assert.Equal("Goo", Assert.IsType<RegexQuery.Literal>(all.Children[0]).Text);
        Assert.Equal("Bar", Assert.IsType<RegexQuery.Literal>(all.Children[1]).Text);
    }

    [Fact]
    public void EndToEnd_ComplexPattern()
    {
        // (Get|Set)(Value|Item)s? -> All(Any("Get","Set"), Any("Value","Item"))
        // The s? is optional (None), pruned from All
        var query = RegexQueryCompiler.Compile("(Get|Set)(Value|Item)s?")!;
        Assert.True(query.HasLiterals);

        var all = Assert.IsType<RegexQuery.All>(query);
        Assert.Equal(2, all.Children.Length);

        var first = Assert.IsType<RegexQuery.Any>(all.Children[0]);
        Assert.Equal("Get", Assert.IsType<RegexQuery.Literal>(first.Children[0]).Text);
        Assert.Equal("Set", Assert.IsType<RegexQuery.Literal>(first.Children[1]).Text);

        var second = Assert.IsType<RegexQuery.Any>(all.Children[1]);
        Assert.Equal("Value", Assert.IsType<RegexQuery.Literal>(second.Children[0]).Text);
        Assert.Equal("Item", Assert.IsType<RegexQuery.Literal>(second.Children[1]).Text);
    }

    [Fact]
    public void EndToEnd_AlternationOfPureDotStar_ProducesNone()
    {
        // (.*|.+) -> Any(None, None) -> None
        var query = RegexQueryCompiler.Compile("(.*|.+)")!;
        Assert.IsType<RegexQuery.None>(query);
    }

    [Fact]
    public void EndToEnd_AnchorWithLiteral()
    {
        // ^Goo$ -> All(Literal("Goo")) -> Literal("Goo")
        // Anchors become None, pruned from All
        var query = RegexQueryCompiler.Compile("^Goo$")!;
        Assert.Equal("Goo", Assert.IsType<RegexQuery.Literal>(query).Text);
    }

    [Fact]
    public void EndToEnd_MixedLiteralsAndWildcards()
    {
        // Read.Line -> All(Literal("Read"), Literal("Line"))
        // The bare '.' is a wildcard (None), pruned from All
        var query = RegexQueryCompiler.Compile("Read.Line")!;
        var all = Assert.IsType<RegexQuery.All>(query);
        Assert.Equal(2, all.Children.Length);
        Assert.Equal("Read", Assert.IsType<RegexQuery.Literal>(all.Children[0]).Text);
        Assert.Equal("Line", Assert.IsType<RegexQuery.Literal>(all.Children[1]).Text);
    }

    #endregion
}
