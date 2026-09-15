// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Components;
using Microsoft.AspNetCore.Razor.Language.IntegrationTests;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Razor.Completion;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Remote.Razor.Completion;

public class AssetPathCompletionItemProviderTest : RazorToolingIntegrationTestBase
{
    // The runtime types the compiler keys off aren't in the reference assemblies used by these
    // tests, so they're declared here. Mirrors the stubs the compiler's tilde-path tests use.
    private const string AssetPathStubs = """
        namespace Microsoft.AspNetCore.Components
        {
            [System.AttributeUsage(System.AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
            public sealed class AssetPathAttribute : System.Attribute
            {
                public AssetPathAttribute() { }
            }

            [System.AttributeUsage(System.AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
            public sealed class AcceptsAssetPathAttribute : System.Attribute
            {
                public AcceptsAssetPathAttribute(string elementName, string attributeName)
                {
                    ElementName = elementName;
                    AttributeName = attributeName;
                }

                public string ElementName { get; }
                public string AttributeName { get; }
            }
        }

        namespace Microsoft.AspNetCore.Components.Web
        {
            [Microsoft.AspNetCore.Components.AcceptsAssetPath("img", "src")]
            [Microsoft.AspNetCore.Components.AcceptsAssetPath("link", "href")]
            [Microsoft.AspNetCore.Components.AcceptsAssetPath("script", "src")]
            public static class AssetPathAttributes { }
        }
        """;

    private const string ImageComponent = """
        using Microsoft.AspNetCore.Components;

        namespace Test
        {
            public class Image : ComponentBase
            {
                [Parameter]
                [AssetPath]
                public string Source { get; set; }

                [Parameter]
                public string Alt { get; set; }
            }
        }
        """;

    private static readonly ImmutableArray<string> s_assets =
    [
        "app.css",
        "images/logo.png",
        "images/hero.png",
        "_framework/blazor.web.js"
    ];

    private readonly AssetPathCompletionItemProvider _provider = new();
    private readonly RazorCompletionOptions _options = new(
        SnippetsSupported: true,
        AutoInsertAttributeQuotes: true,
        CommitElementsWithSpace: true,
        IsVsCode: false);

    internal override RazorFileKind? FileKind => RazorFileKind.Component;
    internal override bool UseTwoPhaseCompilation => true;

    // Asset path expansion is gated to Razor 11.0, and RazorLanguageVersion.Latest is still 9.0, so
    // the default configuration would compile these documents with the feature switched off.
    internal override RazorConfiguration Configuration { get; } =
        RazorConfiguration.Default with { LanguageVersion = RazorLanguageVersion.Version_11_0 };

    public AssetPathCompletionItemProviderTest(ITestOutputHelper testOutput)
        : base(testOutput)
    {
    }

    [Fact]
    public void AllowlistedHtmlAttribute_OffersAssets()
    {
        TestCode testCode = """<img src="~/ima$$" />""";

        var completions = GetCompletionItems(testCode);

        Assert.Equal(s_assets.Sort(), completions.Select(c => c.DisplayText).Order());
    }

    [Fact]
    public void AllowlistedHtmlAttribute_ReplacesOnlyThePathAfterTheTilde()
    {
        TestCode testCode = """<img src="~/ima$$ges/old.png" />""";

        var completions = GetCompletionItems(testCode);

        // The replacement has to cover the whole path, not just the segment after the last '/',
        // or committing leaves the old value behind.
        var item = Assert.Single(completions, c => c.DisplayText == "images/logo.png");
        var range = Assert.NotNull(item.ReplacementRange);
        Assert.Equal(12, range.Start.Character);
        Assert.Equal(26, range.End.Character);
    }

    [Fact]
    public void EmptyPathAfterTilde_OffersAssets()
    {
        TestCode testCode = """<img src="~/$$" />""";

        var completions = GetCompletionItems(testCode);

        Assert.NotEmpty(completions);
    }

    [Fact]
    public void CursorInsideTildePrefix_OffersNothing()
    {
        TestCode testCode = """<img src="~$$/" />""";

        Assert.Empty(GetCompletionItems(testCode));
    }

    [Fact]
    public void ValueWithoutTilde_OffersNothing()
    {
        TestCode testCode = """<img src="images/$$" />""";

        Assert.Empty(GetCompletionItems(testCode));
    }

    [Fact]
    public void AttributeNotOnTheAllowlist_OffersNothing()
    {
        TestCode testCode = """<img alt="~/ima$$" />""";

        Assert.Empty(GetCompletionItems(testCode));
    }

    [Fact]
    public void ElementNotOnTheAllowlist_OffersNothing()
    {
        TestCode testCode = """<div data-src="~/ima$$"></div>""";

        Assert.Empty(GetCompletionItems(testCode));
    }

    [Fact]
    public void AttributeName_OffersNothing()
    {
        TestCode testCode = """<img sr$$c="~/images/logo.png" />""";

        Assert.Empty(GetCompletionItems(testCode));
    }

    [Fact]
    public void NoAssets_OffersNothing()
    {
        TestCode testCode = """<img src="~/ima$$" />""";

        Assert.Empty(GetCompletionItems(testCode, assets: []));
    }

    [Fact]
    public void ComponentParameterMarkedAssetPath_OffersAssets()
    {
        AdditionalSyntaxTrees.Add(Parse(ImageComponent));

        TestCode testCode = """<Image Source="~/ima$$" />""";

        Assert.NotEmpty(GetCompletionItems(testCode));
    }

    [Fact]
    public void ComponentParameterWithoutAssetPath_OffersNothing()
    {
        AdditionalSyntaxTrees.Add(Parse(ImageComponent));

        TestCode testCode = """<Image Alt="~/ima$$" />""";

        Assert.Empty(GetCompletionItems(testCode));
    }

    [Fact]
    public void MixedContent_OffersNothing()
    {
        TestCode testCode = """<img src="~/@folder/ima$$" />""";

        Assert.Empty(GetCompletionItems(testCode));
    }

    [Fact]
    public void LegacyDocument_OffersNothing()
    {
        TestCode testCode = """<img src="~/ima$$" />""";

        Assert.Empty(GetCompletionItems(testCode, fileKind: RazorFileKind.Legacy));
    }

    [Theory]
    // The producer that discovers [AcceptsAssetPath] is registered from Razor 3.0, but
    // ComponentTildePathPass only expands from 11.0, so a project can carry the opt-in metadata
    // while nothing it writes will actually be rewritten. Note 'Latest' is still 9.0; the feature
    // is only on under 'preview'.
    [InlineData("10.0", RazorFileKind.Component, false)]
    [InlineData("latest", RazorFileKind.Component, false)]
    [InlineData("11.0", RazorFileKind.Component, true)]
    [InlineData("preview", RazorFileKind.Component, true)]
    [InlineData("11.0", RazorFileKind.Legacy, false)]
    public void IsSupported(string version, RazorFileKind fileKind, bool expected)
    {
        var options = RazorParserOptions.Create(RazorLanguageVersion.Parse(version), fileKind);

        Assert.Equal(expected, AssetPathCompletionFacts.IsSupported(options));
    }

    [Fact]
    public void Create_IgnoresTagHelpersWithoutAssetPathMetadata()
    {
        var info = AssetPathCompletionInfo.Create(TagHelperCollection.Create(
            [TagHelperDescriptorBuilder.Create("Test.Component", "TestAssembly").Build()]));

        Assert.Same(AssetPathCompletionInfo.Empty, info);
    }

    [Fact]
    public void AcceptsAssetPath_IsCaseInsensitive()
    {
        var info = CreateInfo(s_assets);

        Assert.True(info.AcceptsAssetPath("IMG", "SRC"));
        Assert.False(info.AcceptsAssetPath("img", "alt"));
    }

    private ImmutableArray<RazorCompletionItem> GetCompletionItems(
        TestCode testCode,
        ImmutableArray<string>? assets = null,
        RazorFileKind fileKind = RazorFileKind.Component)
    {
        AdditionalSyntaxTrees.Add(Parse(AssetPathStubs));

        var result = CompileToCSharp("Test.razor", testCode.Text, throwOnFailure: false, fileKind: fileKind);
        var codeDocument = result.CodeDocument;

        var syntaxTree = codeDocument.GetRequiredTagHelperRewrittenSyntaxTree();
        var owner = syntaxTree.Root.FindInnermostNode(testCode.Position, includeWhitespace: true, walkMarkersBack: true);
        owner = RazorCompletionFactsService.AdjustSyntaxNodeForWordBoundary(owner, testCode.Position);

        var context = new RazorCompletionContext(
            codeDocument,
            testCode.Position,
            owner,
            syntaxTree,
            codeDocument.GetRequiredTagHelperContext(),
            CompletionReason.Typing,
            _options)
        {
            AssetPathInfo = CreateInfo(assets ?? s_assets)
        };

        return _provider.GetCompletionItems(context);
    }

    /// <summary>
    /// Builds the allowlist from carrier descriptors directly rather than from the compiled
    /// document's tag helpers, because the document's set is namespace-scoped and asset path
    /// expansion deliberately isn't.
    /// </summary>
    private static AssetPathCompletionInfo CreateInfo(ImmutableArray<string> assets)
    {
        var carriers = new[]
        {
            CreateCarrier("img", "src"),
            CreateCarrier("link", "href"),
            CreateCarrier("script", "src")
        };

        return AssetPathCompletionInfo.Create(TagHelperCollection.Create(carriers)).WithAssets(assets);
    }

    private static TagHelperDescriptor CreateCarrier(string element, string attribute)
    {
        var builder = TagHelperDescriptorBuilder.Create(
            TagHelperKind.AssetPath, $"{element}[{attribute}]", "Microsoft.AspNetCore.Components");

        builder.SetTypeName(
            "Microsoft.AspNetCore.Components.Web.AssetPathAttributes",
            "Microsoft.AspNetCore.Components.Web",
            "AssetPathAttributes");

        builder.CaseSensitive = true;
        builder.SetMetadata(new AssetPathMetadata { Element = element, Attribute = attribute });

        return builder.Build();
    }
}
