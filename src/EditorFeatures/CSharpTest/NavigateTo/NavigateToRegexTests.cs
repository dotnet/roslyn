// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.UnitTests.NavigateTo;
using Microsoft.CodeAnalysis.Remote.Testing;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.VisualStudio.Language.NavigateTo.Interfaces;
using Microsoft.VisualStudio.Text.PatternMatching;
using Roslyn.Test.EditorUtilities.NavigateTo;
using Roslyn.Test.Utilities;
using Xunit;

#pragma warning disable CS0618 // MatchKind is obsolete

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.NavigateTo;

[Trait(Traits.Feature, Traits.Features.NavigateTo)]
public sealed class NavigateToRegexTests : AbstractNavigateToTests
{
    protected override string Language => "csharp";

    protected override EditorTestWorkspace CreateWorkspace(string content, TestComposition composition)
        => EditorTestWorkspace.CreateCSharp(content, composition: composition);

    private const string MultiSymbolSource = """
        namespace MyNamespace
        {
            class ReadLine { }
            class WriteLine { }
            class StreamReader { }
            class StreamWriter { }
            class GooBar { }
            class BazQuux { }
            class MyGooBarEnd { }
        }
        """;

    #region Alternation

    [Theory, CombinatorialData]
    public Task Regex_Alternation_MatchesFirstBranch(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, MultiSymbolSource, async w =>
        {
            var items = await _aggregator.GetItemsAsync("(Read|Write)Line");
            Assert.Equal(2, items.Count());

            var readLine = items.Single(i => i.Name == "ReadLine");
            VerifyNavigateToResultItem(readLine, "ReadLine", "[|ReadLine|]", PatternMatchKind.Exact, NavigateToItemKind.Class, Glyph.ClassInternal);

            var writeLine = items.Single(i => i.Name == "WriteLine");
            VerifyNavigateToResultItem(writeLine, "WriteLine", "[|WriteLine|]", PatternMatchKind.Exact, NavigateToItemKind.Class, Glyph.ClassInternal);
        });

    [Theory, CombinatorialData]
    public Task Regex_Alternation_NoMatchWhenNoBranchMatches(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, MultiSymbolSource, async w =>
        {
            var items = await _aggregator.GetItemsAsync("(Delete|Append)Line");
            Assert.Empty(items);
        });

    #endregion

    #region Wildcards

    [Theory, CombinatorialData]
    public Task Regex_DotStar_MatchesSubstring(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, MultiSymbolSource, async w =>
        {
            var items = await _aggregator.GetItemsAsync("Goo.*Bar");
            Assert.Equal(2, items.Count());

            var gooBar = items.Single(i => i.Name == "GooBar");
            VerifyNavigateToResultItem(gooBar, "GooBar", "[|GooBar|]", PatternMatchKind.Exact, NavigateToItemKind.Class, Glyph.ClassInternal);

            var myGooBarEnd = items.Single(i => i.Name == "MyGooBarEnd");
            VerifyNavigateToResultItem(myGooBarEnd, "MyGooBarEnd", "My[|GooBar|]End", PatternMatchKind.Substring, NavigateToItemKind.Class, Glyph.ClassInternal);
        });

    [Theory, CombinatorialData]
    public Task Regex_DotStar_NoMatchWhenLiteralsAbsent(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, MultiSymbolSource, async w =>
        {
            var items = await _aggregator.GetItemsAsync("Alpha.*Beta");
            Assert.Empty(items);
        });

    #endregion

    #region Case sensitivity

    [Theory, CombinatorialData]
    public Task Regex_CaseInsensitive_FindsMatch(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, MultiSymbolSource, async w =>
        {
            var items = await _aggregator.GetItemsAsync("readline");
            var item = items.Single(i => i.Name == "ReadLine");
            Assert.Equal(PatternMatchKind.Exact, item.PatternMatch.Kind);
            Assert.False(item.PatternMatch.IsCaseSensitive);
        });

    [Theory, CombinatorialData]
    public Task Regex_CaseSensitive_MatchReportedCorrectly(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, MultiSymbolSource, async w =>
        {
            var items = await _aggregator.GetItemsAsync("(Read|Write)Line");
            var item = items.Single(i => i.Name == "ReadLine");
            Assert.Equal(PatternMatchKind.Exact, item.PatternMatch.Kind);
            Assert.True(item.PatternMatch.IsCaseSensitive);
        });

    #endregion

    #region Character classes

    [Theory, CombinatorialData]
    public Task Regex_CharacterClass_Matches(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, MultiSymbolSource, async w =>
        {
            var items = await _aggregator.GetItemsAsync("Stream[RW]");
            Assert.Equal(2, items.Count());

            var reader = items.Single(i => i.Name == "StreamReader");
            VerifyNavigateToResultItem(reader, "StreamReader", "[|StreamR|]eader", PatternMatchKind.Substring, NavigateToItemKind.Class, Glyph.ClassInternal);

            var writer = items.Single(i => i.Name == "StreamWriter");
            VerifyNavigateToResultItem(writer, "StreamWriter", "[|StreamW|]riter", PatternMatchKind.Substring, NavigateToItemKind.Class, Glyph.ClassInternal);
        });

    #endregion

    #region Anchored patterns

    [Theory, CombinatorialData]
    public Task Regex_AnchoredExact_MatchesFullName(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, """
            class GooBar { }
            class MyGooBar { }
            """, async w =>
        {
            var items = await _aggregator.GetItemsAsync("^GooBar$");
            var item = items.Single();
            Assert.Equal("GooBar", item.Name);
            Assert.Equal(PatternMatchKind.Exact, item.PatternMatch.Kind);
        });

    #endregion

    #region Container.Name splitting

    [Theory, CombinatorialData]
    public Task Regex_ContainerDotName_SplitsCorrectly(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, """
            namespace MyNamespace
            {
                class Target { }
            }
            """, async w =>
        {
            var items = await _aggregator.GetItemsAsync("MyNamespace.Target");
            var item = items.Single(i => i.Name == "Target");
            Assert.Equal(PatternMatchKind.Exact, item.PatternMatch.Kind);
        });

    [Theory, CombinatorialData]
    public Task Regex_ContainerRegex_DotName(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, """
            namespace Alpha
            {
                class Target { }
            }
            namespace Beta
            {
                class Target { }
            }
            namespace Gamma
            {
                class Other { }
            }
            """, async w =>
        {
            var items = await _aggregator.GetItemsAsync("(Alpha|Beta).Target");
            Assert.Equal(2, items.Count());
            Assert.All(items, i => Assert.Equal("Target", i.Name));
        });

    #endregion

    #region Whitespace stripping

    [Theory, CombinatorialData]
    public Task Regex_WhitespaceInPattern_IsIgnored(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, MultiSymbolSource, async w =>
        {
            var items = await _aggregator.GetItemsAsync("( Read | Write ) Line");
            Assert.Equal(2, items.Count());
            Assert.Contains(items, i => i.Name == "ReadLine");
            Assert.Contains(items, i => i.Name == "WriteLine");
        });

    #endregion

    #region Invalid regex

    [Theory, CombinatorialData]
    public Task Regex_InvalidPattern_ProducesNoResults(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, MultiSymbolSource, async w =>
        {
            var items = await _aggregator.GetItemsAsync("(unclosed");
            Assert.Empty(items);
        });

    #endregion

    #region Negative — no false positives from regex path

    [Theory, CombinatorialData]
    public Task Regex_NoMatch_WhenPatternDoesNotMatchAnySymbol(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, MultiSymbolSource, async w =>
        {
            var items = await _aggregator.GetItemsAsync("^ZzzNotPresent$");
            Assert.Empty(items);
        });

    [Theory, CombinatorialData]
    public Task Regex_NoMatch_WhenAlternationMisses(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, MultiSymbolSource, async w =>
        {
            var items = await _aggregator.GetItemsAsync("(Alpha|Beta)Line");
            Assert.Empty(items);
        });

    #endregion

    #region Mixed regex and non-regex

    [Theory, CombinatorialData]
    public Task NonRegex_PlainText_StillWorks(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, MultiSymbolSource, async w =>
        {
            var items = await _aggregator.GetItemsAsync("GooBar");
            var item = items.Single(i => i.Name == "GooBar");
            Assert.Equal(PatternMatchKind.Exact, item.PatternMatch.Kind);
        });

    [Theory, CombinatorialData]
    public Task NonRegex_DotSeparated_StillWorks(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, """
            namespace MyNamespace
            {
                class Target { }
            }
            """, async w =>
        {
            var item = (await _aggregator.GetItemsAsync("MyNamespace.Target")).Single(i => i.Name == "Target");
            Assert.Equal(PatternMatchKind.Exact, item.PatternMatch.Kind);
        });

    #endregion

    #region Quantifiers

    [Theory, CombinatorialData]
    public Task Regex_OneOrMore_Matches(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, """
            class GoBar { }
            class GooBar { }
            class GoooBar { }
            """, async w =>
        {
            var items = await _aggregator.GetItemsAsync("Go+Bar");
            Assert.Equal(3, items.Count());
        });

    [Theory, CombinatorialData]
    public Task Regex_ZeroOrMore_MatchesZeroOccurrences(TestHost testHost, Composition composition)
        => TestAsync(testHost, composition, """
            class GBar { }
            class GoBar { }
            """, async w =>
        {
            var items = await _aggregator.GetItemsAsync("Go*Bar");
            Assert.Equal(2, items.Count());
            Assert.Contains(items, i => i.Name == "GBar");
        });

    #endregion
}
