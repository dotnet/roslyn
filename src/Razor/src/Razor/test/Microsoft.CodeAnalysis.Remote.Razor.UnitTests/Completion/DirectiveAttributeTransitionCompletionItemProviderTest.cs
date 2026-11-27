// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.LanguageServer.Test;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Remote.Razor.Completion;

public class DirectiveAttributeTransitionCompletionItemProviderTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    private readonly TagHelperDocumentContext _tagHelperDocumentContext = TagHelperDocumentContext.GetOrCreate(
        [
            TagHelperDescriptorBuilder.CreateTagHelper("DirectiveAttribute", "TestAssembly")
                .TypeName("DirectiveAttribute")
                .TagMatchingRule(tagName: "*")
                .BoundAttributeDescriptor(builder =>
                {
                    builder.Name = "@test";
                    builder.TypeName = typeof(string).FullName;
                    builder.PropertyName = "Test";
                    builder.IsDirectiveAttribute = true;
                })
                .Build(),
        ]);
    private readonly TagHelperDocumentContext _tagHelperWithParameterContext = TagHelperDocumentContext.GetOrCreate(
        [
            TagHelperDescriptorBuilder.CreateTagHelper("DirectiveAttributeWithParameter", "TestAssembly")
                .TypeName("DirectiveAttributeWithParameter")
                .TagMatchingRule(tagName: "*")
                .BoundAttributeDescriptor(builder =>
                {
                    builder.Name = "@test";
                    builder.TypeName = typeof(string).FullName;
                    builder.PropertyName = "Test";
                    builder.IsDirectiveAttribute = true;
                    builder.BindAttributeParameter(parameter =>
                    {
                        parameter.Name = "something";
                        parameter.TypeName = typeof(string).FullName;
                        parameter.PropertyName = "Something";
                    });
                })
                .Build(),
        ]);
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

    [Fact]
    public void GetCompletionItems_NoTagHelpers_ReturnsEmptyList()
    {
        var context = CreateContext("<input $$ />", documentContext: TagHelperDocumentContext.GetOrCreate(tagHelpers: []));

        var result = _provider.GetCompletionItems(context);

        Assert.Empty(result);
    }

    [Fact]
    public void GetCompletionItems_NoDirectiveAttributesForTag_ReturnsEmptyList()
    {
        var descriptor = TagHelperDescriptorBuilder.CreateTagHelper("Input", "TestAssembly")
            .TagMatchingRule(tagName: "input")
            .BoundAttributeDescriptor(builder => builder.Name = "value")
            .Build();
        var context = CreateContext("<input $$ />", documentContext: TagHelperDocumentContext.GetOrCreate([descriptor]));

        var result = _provider.GetCompletionItems(context);

        Assert.Empty(result);
    }

    [Fact]
    public void GetCompletionItems_DirectiveAttributesForDifferentTag_ReturnsEmptyList()
    {
        var descriptor = TagHelperDescriptorBuilder.CreateTagHelper("DirectiveAttribute", "TestAssembly")
            .TagMatchingRule(tagName: "button")
            .BoundAttributeDescriptor(builder =>
            {
                builder.Name = "@test";
                builder.IsDirectiveAttribute = true;
            })
            .Build();
        var context = CreateContext("<input $$ />", documentContext: TagHelperDocumentContext.GetOrCreate([descriptor]));

        var result = _provider.GetCompletionItems(context);

        Assert.Empty(result);
    }

    [Fact]
    public void GetCompletionItems_AllDirectiveAttributesAlreadyPresent_ReturnsEmptyList()
    {
        var context = CreateContext("""<input @test="value" $$ />""");

        var result = _provider.GetCompletionItems(context);

        Assert.Empty(result);
    }

    [Fact]
    public void GetCompletionItems_ExistingDirectiveAttributeWithAnotherAvailable_ReturnsTransitionCompletionItem()
    {
        var descriptor = TagHelperDescriptorBuilder.CreateTagHelper("DirectiveAttributes", "TestAssembly")
            .TypeName("DirectiveAttributes")
            .TagMatchingRule(tagName: "*")
            .BoundAttributeDescriptor(builder =>
            {
                builder.Name = "@already";
                builder.TypeName = typeof(string).FullName;
                builder.PropertyName = "Already";
                builder.IsDirectiveAttribute = true;
            })
            .BoundAttributeDescriptor(builder =>
            {
                builder.Name = "@remaining";
                builder.TypeName = typeof(string).FullName;
                builder.PropertyName = "Remaining";
                builder.IsDirectiveAttribute = true;
            })
            .Build();
        var context = CreateContext("""<input @already="value" $$ />""", documentContext: TagHelperDocumentContext.GetOrCreate([descriptor]));

        var result = _provider.GetCompletionItems(context);

        var item = Assert.Single(result);
        Assert.True(DirectiveAttributeTransitionCompletionItemProvider.IsTransitionCompletionItem(item));
    }

    [Fact]
    public void GetCompletionItems_ExistingDirectiveAttributeWithAvailableParameter_ReturnsTransitionCompletionItem()
    {
        var context = CreateContext("""<input @test="value" $$ />""", documentContext: _tagHelperWithParameterContext);

        var result = _provider.GetCompletionItems(context);

        var item = Assert.Single(result);
        Assert.True(DirectiveAttributeTransitionCompletionItemProvider.IsTransitionCompletionItem(item));
    }

    [Fact]
    public void GetCompletionItems_ExistingDirectiveAttributeWithAllParametersPresent_ReturnsEmptyList()
    {
        var context = CreateContext("""<input @test="value" @test:something="value" $$ />""", documentContext: _tagHelperWithParameterContext);

        var result = _provider.GetCompletionItems(context);

        Assert.Empty(result);
    }

    [Fact]
    public void GetCompletionItems_ExistingDirectiveAttributeWithAvailableIndexer_ReturnsTransitionCompletionItem()
    {
        var descriptor = TagHelperDescriptorBuilder.CreateTagHelper("DirectiveAttributeWithIndexer", "TestAssembly")
            .TypeName("DirectiveAttributeWithIndexer")
            .TagMatchingRule(tagName: "*")
            .BoundAttributeDescriptor(builder =>
            {
                builder.Name = "@test";
                builder.TypeName = typeof(string).FullName;
                builder.PropertyName = "Test";
                builder.IsDirectiveAttribute = true;
                builder.IsDictionary = true;
                builder.IndexerAttributeNamePrefix = "@test-";
                builder.IndexerValueTypeName = typeof(string).FullName;
            })
            .Build();
        var context = CreateContext("""<input @test="value" $$ />""", documentContext: TagHelperDocumentContext.GetOrCreate([descriptor]));

        var result = _provider.GetCompletionItems(context);

        var item = Assert.Single(result);
        Assert.True(DirectiveAttributeTransitionCompletionItemProvider.IsTransitionCompletionItem(item));
    }

    [Fact]
    public void GetCompletionItems_NoTagHelpersBeforeExistingAttribute_ReturnsEmptyList()
    {
        var context = CreateContext("""<input $$ value="test" />""", documentContext: TagHelperDocumentContext.GetOrCreate(tagHelpers: []));

        var result = _provider.GetCompletionItems(context);

        Assert.Empty(result);
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

    private RazorCompletionContext CreateContext(TestCode text, RazorFileKind? fileKind = null, TagHelperDocumentContext? documentContext = null)
    {
        var absoluteIndex = text.Position;
        var codeDocument = GetCodeDocument(text.Text, fileKind);
        var syntaxTree = codeDocument.GetRequiredTagHelperRewrittenSyntaxTree();
        var owner = syntaxTree.Root.FindInnermostNode(absoluteIndex, includeWhitespace: true, walkMarkersBack: true);
        owner = RazorCompletionFactsService.AdjustSyntaxNodeForWordBoundary(owner, absoluteIndex);

        return new RazorCompletionContext(codeDocument, absoluteIndex, owner, syntaxTree, documentContext ?? _tagHelperDocumentContext);
    }
}
