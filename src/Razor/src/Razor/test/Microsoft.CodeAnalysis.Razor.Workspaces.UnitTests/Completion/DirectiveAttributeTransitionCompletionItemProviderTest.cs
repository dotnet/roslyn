// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.LanguageServer.Test;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.Completion;

public class DirectiveAttributeTransitionCompletionItemProviderTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    private readonly TagHelperDocumentContext _tagHelperDocumentContext = TagHelperDocumentContext.GetOrCreate(tagHelpers: []);
    private readonly DirectiveAttributeTransitionCompletionItemProvider _provider = new(new TestClientCapabilitiesService(new VSInternalClientCapabilities()));

    [Fact]
    public void IsValidCompletionPoint_AtPrefixLeadingEdge_ReturnsFalse()
    {
        // Arrange

        // <p| class=""></p>
        var absoluteIndex = 2;
        var prefixLocation = new TextSpan(2, 1);
        var attributeNameLocation = new TextSpan(3, 5);

        // Act
        var result = DirectiveAttributeTransitionCompletionItemProvider.IsValidCompletionPoint(absoluteIndex, prefixLocation, attributeNameLocation);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidCompletionPoint_WithinPrefix_ReturnsTrue()
    {
        // Arrange

        // <p | class=""></p>
        var absoluteIndex = 3;
        var prefixLocation = new TextSpan(2, 2);
        var attributeNameLocation = new TextSpan(4, 5);

        // Act
        var result = DirectiveAttributeTransitionCompletionItemProvider.IsValidCompletionPoint(absoluteIndex, prefixLocation, attributeNameLocation);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidCompletionPoint_NullPrefix_ReturnsFalse()
    {
        // Arrange

        // <svg xml:base="abc"xm| ></svg>
        var absoluteIndex = 21;
        TextSpan? prefixLocation = null;
        var attributeNameLocation = new TextSpan(4, 5);

        // Act
        var result = DirectiveAttributeTransitionCompletionItemProvider.IsValidCompletionPoint(absoluteIndex, prefixLocation, attributeNameLocation);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidCompletionPoint_AtNameLeadingEdge_ReturnsFalse()
    {
        // Arrange

        // <p |class=""></p>
        var absoluteIndex = 3;
        var prefixLocation = new TextSpan(2, 1);
        var attributeNameLocation = new TextSpan(3, 5);

        // Act
        var result = DirectiveAttributeTransitionCompletionItemProvider.IsValidCompletionPoint(absoluteIndex, prefixLocation, attributeNameLocation);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidCompletionPoint_WithinName_ReturnsFalse()
    {
        // Arrange

        // <p cl|ass=""></p>
        var absoluteIndex = 5;
        var prefixLocation = new TextSpan(2, 1);
        var attributeNameLocation = new TextSpan(3, 5);

        // Act
        var result = DirectiveAttributeTransitionCompletionItemProvider.IsValidCompletionPoint(absoluteIndex, prefixLocation, attributeNameLocation);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidCompletionPoint_OutsideOfNameAndPrefix_ReturnsFalse()
    {
        // Arrange

        // <p class=|""></p>
        var absoluteIndex = 9;
        var prefixLocation = new TextSpan(2, 1);
        var attributeNameLocation = new TextSpan(3, 5);

        // Act
        var result = DirectiveAttributeTransitionCompletionItemProvider.IsValidCompletionPoint(absoluteIndex, prefixLocation, attributeNameLocation);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetCompletionItems_AttributeAreaInNonComponentFile_ReturnsEmptyList()
    {
        // Arrange
        var context = CreateContext("<input $$ />", RazorFileKind.Legacy);

        // Act
        var result = _provider.GetCompletionItems(context);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetCompletionItems_NonAttribute_ReturnsEmptyList()
    {
        // Arrange
        var context = CreateContext("<i$$nput  />");

        // Act
        var result = _provider.GetCompletionItems(context);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetCompletionItems_ExistingAttribute_ReturnsEmptyList()
    {
        // Arrange
        var context = CreateContext("<input @$$ />");

        // Act
        var result = _provider.GetCompletionItems(context);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetCompletionItems_InbetweenSelfClosingEnd_ReturnsEmptyList()
    {
        // Arrange
        var context = CreateContext("""
            <input /$$
            
            """);

        // Act
        var result = _provider.GetCompletionItems(context);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetCompletionItems_AttributeAreaInComponentFile_ReturnsTransitionCompletionItem()
    {
        // Arrange
        var context = CreateContext("<input $$ />");

        // Act
        var result = _provider.GetCompletionItems(context);

        // Assert
        var item = Assert.Single(result);
        Assert.True(DirectiveAttributeTransitionCompletionItemProvider.IsTransitionCompletionItem(item));
    }

    [Fact]
    public void GetCompletionItems_AttributeAreaEndOfSelfClosingTag_ReturnsTransitionCompletionItem()
    {
        // Arrange
        var context = CreateContext("<input $$/>");

        // Act
        var result = _provider.GetCompletionItems(context);

        // Assert
        var item = Assert.Single(result);
        Assert.True(DirectiveAttributeTransitionCompletionItemProvider.IsTransitionCompletionItem(item));
    }

    [Fact]
    public void GetCompletionItems_AttributeAreaEndOfOpeningTag_ReturnsTransitionCompletionItem()
    {
        // Arrange
        var context = CreateContext("<input $$></input>");

        // Act
        var result = _provider.GetCompletionItems(context);

        // Assert
        var item = Assert.Single(result);
        Assert.True(DirectiveAttributeTransitionCompletionItemProvider.IsTransitionCompletionItem(item));
    }

    [Fact]
    public void GetCompletionItems_ExistingAttribute_LeadingEdge_ReturnsEmptyList()
    {
        // Arrange
        var context = CreateContext("<input $$src=\"xyz\" />");

        // Act
        var result = _provider.GetCompletionItems(context);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetCompletionItems_ExistingAttribute_TrailingEdge_ReturnsEmptyList()
    {
        // Arrange
        var context = CreateContext("<input src=\"xyz\"$$ />");

        // Act
        var result = _provider.GetCompletionItems(context);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetCompletionItems_ExistingAttribute_TrailingEdgeOnSpace_ReturnsEmptyList()
    {
        // Arrange
        var context = CreateContext("<input src=\"xyz\"$$   />");

        // Act
        var result = _provider.GetCompletionItems(context);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetCompletionItems_ExistingAttribute_Partial_ReturnsEmptyList()
    {
        // Arrange
        var context = CreateContext("<svg xml:$$ ></svg>");

        // Act
        var result = _provider.GetCompletionItems(context);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void GetCompletionItems_AttributeAreaInIncompleteAttributeTransition_ReturnsTransitionCompletionItem()
    {
        // Arrange
        var context = CreateContext("<input $$  @{");

        // Act
        var result = _provider.GetCompletionItems(context);

        // Assert
        var item = Assert.Single(result);
        Assert.True(DirectiveAttributeTransitionCompletionItemProvider.IsTransitionCompletionItem(item));
    }

    [Fact]
    public void GetCompletionItems_AttributeAreaInIncompleteComponent_ReturnsTransitionCompletionItem()
    {
        // Arrange
        var context = CreateContext("<svg $$ xml:base=\"d\"></svg>");

        // Act
        var result = _provider.GetCompletionItems(context);

        // Assert
        var item = Assert.Single(result);
        Assert.True(DirectiveAttributeTransitionCompletionItemProvider.IsTransitionCompletionItem(item));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void GetCompletionItems_WithAvoidExplicitCommitOption_ReturnsAppropriateCommitCharacters(bool supportsSoftSelection)
    {
        var clientCapabilities = new VSInternalClientCapabilities()
        {
            SupportsVisualStudioExtensions = supportsSoftSelection
        };

        // Arrange
        var context = CreateContext("<input $$ />");
        var provider = new DirectiveAttributeTransitionCompletionItemProvider(new TestClientCapabilitiesService(clientCapabilities));

        // Act
        var result = provider.GetCompletionItems(context);

        // Assert
        var item = Assert.Single(result);
        Assert.True(DirectiveAttributeTransitionCompletionItemProvider.IsTransitionCompletionItem(item));
        if (supportsSoftSelection)
        {
            Assert.NotEmpty(item.CommitCharacters);
        }
        else
        {
            Assert.Empty(item.CommitCharacters);
        }
    }

    private static RazorCodeDocument GetCodeDocument(TestCode text, RazorFileKind? fileKind = null)
    {
        var fileKindValue = fileKind ?? RazorFileKind.Component;

        var sourceDocument = TestRazorSourceDocument.Create(text.Text);
        var projectEngine = RazorProjectEngine.Create(builder =>
        {
            builder.ConfigureParserOptions(builder =>
            {
                builder.UseRoslynTokenizer = true;
            });
        });

        return projectEngine.Process(sourceDocument, fileKindValue, importSources: default, tagHelpers: []);
    }

    private RazorCompletionContext CreateContext(TestCode text, RazorFileKind? fileKind = null)
    {
        var absoluteIndex = text.Position;
        var codeDocument = GetCodeDocument(text.Text, fileKind);
        var syntaxTree = codeDocument.GetRequiredTagHelperRewrittenSyntaxTree();
        var owner = syntaxTree.Root.FindInnermostNode(absoluteIndex, includeWhitespace: true, walkMarkersBack: true);
        owner = AbstractRazorCompletionFactsService.AdjustSyntaxNodeForWordBoundary(owner, absoluteIndex);

        return new RazorCompletionContext(codeDocument, absoluteIndex, owner, syntaxTree, _tagHelperDocumentContext);
    }
}
