// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.IntegrationTests;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.Test.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.Completion;

public class BlazorDataAttributeCompletionItemProviderTest : RazorToolingIntegrationTestBase
{
    private readonly BlazorDataAttributeCompletionItemProvider _provider;
    private readonly RazorCompletionOptions _defaultRazorCompletionOptions;

    internal override RazorFileKind? FileKind => RazorFileKind.Component;
    internal override bool UseTwoPhaseCompilation => true;

    public BlazorDataAttributeCompletionItemProviderTest(ITestOutputHelper testOutput)
        : base(testOutput)
    {
        _provider = new BlazorDataAttributeCompletionItemProvider();
        _defaultRazorCompletionOptions = new RazorCompletionOptions(
            SnippetsSupported: true,
            AutoInsertAttributeQuotes: true,
            CommitElementsWithSpace: true,
            UseVsCodeCompletionCommitCharacters: false);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/9378")]
    public void GetCompletionItems_OnFormElement_ReturnsDataEnhance()
    {
        // Arrange
        TestCode testCode = "<form d$$></form>";
        var context = CreateRazorCompletionContext(testCode);

        // Act
        var completions = _provider.GetCompletionItems(context);

        // Assert
        Assert.NotEmpty(completions);
        var dataEnhance = completions.FirstOrDefault(c => c.DisplayText == "data-enhance");
        Assert.NotNull(dataEnhance);
        // Check that the insert text starts with the attribute name (may or may not have snippet)
        Assert.StartsWith("data-enhance", dataEnhance.InsertText);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/9378")]
    public void GetCompletionItems_OnAnchorElement_ReturnsDataEnhanceNav()
    {
        // Arrange
        TestCode testCode = "<a d$$></a>";
        var context = CreateRazorCompletionContext(testCode);

        // Act
        var completions = _provider.GetCompletionItems(context);

        // Assert
        Assert.NotEmpty(completions);
        var dataEnhanceNav = completions.FirstOrDefault(c => c.DisplayText == "data-enhance-nav");
        Assert.NotNull(dataEnhanceNav);
        // Check that the insert text starts with the attribute name (may or may not have snippet)
        Assert.StartsWith("data-enhance-nav", dataEnhanceNav.InsertText);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/9378")]
    public void GetCompletionItems_OnDivElement_ReturnsDataPermanent()
    {
        // Arrange
        TestCode testCode = "<div d$$></div>";
        var context = CreateRazorCompletionContext(testCode);

        // Act
        var completions = _provider.GetCompletionItems(context);

        // Assert
        Assert.NotEmpty(completions);
        var dataPermanent = completions.FirstOrDefault(c => c.DisplayText == "data-permanent");
        Assert.NotNull(dataPermanent);
        // Check that the insert text starts with the attribute name (may or may not have snippet)
        Assert.StartsWith("data-permanent", dataPermanent.InsertText);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/9378")]
    public void GetCompletionItems_OnNonFormElement_DoesNotReturnDataEnhance()
    {
        // Arrange
        TestCode testCode = "<div d$$></div>";
        var context = CreateRazorCompletionContext(testCode);

        // Act
        var completions = _provider.GetCompletionItems(context);

        // Assert
        var dataEnhance = completions.FirstOrDefault(c => c.DisplayText == "data-enhance");
        Assert.Null(dataEnhance);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/9378")]
    public void GetCompletionItems_OnDivElement_ReturnsDataEnhanceNav()
    {
        // Arrange - data-enhance-nav can go on any element, not just anchors
        TestCode testCode = "<div d$$></div>";
        var context = CreateRazorCompletionContext(testCode);

        // Act
        var completions = _provider.GetCompletionItems(context);

        // Assert
        var dataEnhanceNav = completions.FirstOrDefault(c => c.DisplayText == "data-enhance-nav");
        Assert.NotNull(dataEnhanceNav);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/9378")]
    public void GetCompletionItems_OnNonComponentFile_ReturnsEmpty()
    {
        // Arrange - need to test with non-component file, which requires different setup
        TestCode testCode = "<form $$></form>";
        var codeDocument = GetCodeDocument(testCode.Text, RazorFileKind.Legacy);
        var syntaxTree = codeDocument.GetRequiredTagHelperRewrittenSyntaxTree();
        var tagHelperContext = codeDocument.GetRequiredTagHelperContext();
        var owner = syntaxTree.Root.FindInnermostNode(testCode.Position, includeWhitespace: true, walkMarkersBack: true);
        owner = AbstractRazorCompletionFactsService.AdjustSyntaxNodeForWordBoundary(owner, testCode.Position);
        var context = new RazorCompletionContext(
            codeDocument,
            testCode.Position,
            owner,
            syntaxTree,
            tagHelperContext,
            CompletionReason.Typing,
            _defaultRazorCompletionOptions);

        // Act
        var completions = _provider.GetCompletionItems(context);

        // Assert
        Assert.Empty(completions);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/9378")]
    public void GetCompletionItems_OnDirectiveAttribute_ReturnsEmpty()
    {
        // Arrange
        TestCode testCode = "<form @$$></form>";
        var context = CreateRazorCompletionContext(testCode);

        // Act
        var completions = _provider.GetCompletionItems(context);

        // Assert
        Assert.Empty(completions);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/9378")]
    public void GetCompletionItems_ExistingDataEnhanceAttribute_DoesNotDuplicateOnDifferentAttribute()
    {
        // Arrange
        TestCode testCode = "<form data-enhance $$></form>";
        var context = CreateRazorCompletionContext(testCode);

        // Act
        var completions = _provider.GetCompletionItems(context);

        // Assert
        // Should not suggest data-enhance again when typing a different attribute
        var dataEnhance = completions.FirstOrDefault(c => c.DisplayText == "data-enhance");
        Assert.Null(dataEnhance);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/9378")]
    public void GetCompletionItems_WithSnippetsDisabled_ReturnsPlainText()
    {
        // Arrange
        var optionsWithoutSnippets = new RazorCompletionOptions(
            SnippetsSupported: false,
            AutoInsertAttributeQuotes: true,
            CommitElementsWithSpace: true,
            UseVsCodeCompletionCommitCharacters: false);
        TestCode testCode = "<form d$$></form>";
        var codeDocument = GetCodeDocument(testCode.Text);
        var syntaxTree = codeDocument.GetRequiredTagHelperRewrittenSyntaxTree();
        var tagHelperContext = codeDocument.GetRequiredTagHelperContext();
        var owner = syntaxTree.Root.FindInnermostNode(testCode.Position, includeWhitespace: true, walkMarkersBack: true);
        owner = AbstractRazorCompletionFactsService.AdjustSyntaxNodeForWordBoundary(owner, testCode.Position);
        var context = new RazorCompletionContext(
            codeDocument,
            testCode.Position,
            owner,
            syntaxTree,
            tagHelperContext,
            CompletionReason.Typing,
            optionsWithoutSnippets);

        // Act
        var completions = _provider.GetCompletionItems(context);

        // Assert
        var dataEnhance = completions.FirstOrDefault(c => c.DisplayText == "data-enhance");
        Assert.NotNull(dataEnhance);
        Assert.Equal("data-enhance", dataEnhance.InsertText);
        Assert.False(dataEnhance.IsSnippet);
    }

    private RazorCodeDocument GetCodeDocument(string content, RazorFileKind? fileKind = null)
    {
        var actualFileKind = fileKind ?? FileKind ?? RazorFileKind.Component;
        var result = CompileToCSharp("Test.razor", content, throwOnFailure: false, fileKind: actualFileKind);
        return result.CodeDocument;
    }

    private RazorCompletionContext CreateRazorCompletionContext(TestCode testCode)
    {
        var codeDocument = GetCodeDocument(testCode.Text);
        var syntaxTree = codeDocument.GetRequiredTagHelperRewrittenSyntaxTree();
        var tagHelperContext = codeDocument.GetRequiredTagHelperContext();

        var owner = syntaxTree.Root.FindInnermostNode(testCode.Position, includeWhitespace: true, walkMarkersBack: true);
        owner = AbstractRazorCompletionFactsService.AdjustSyntaxNodeForWordBoundary(owner, testCode.Position);

        return new RazorCompletionContext(codeDocument, testCode.Position, owner, syntaxTree, tagHelperContext, CompletionReason.Typing, _defaultRazorCompletionOptions);
    }
}
