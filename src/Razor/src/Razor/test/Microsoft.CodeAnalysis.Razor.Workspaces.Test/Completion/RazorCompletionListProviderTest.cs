// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.LanguageServer.Test;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.AspNetCore.Razor.Test.Common.Logging;
using Microsoft.CodeAnalysis.Razor.Tooltip;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.Completion;

public class RazorCompletionListProviderTest
{
    private readonly IRazorCompletionFactsService _completionFactsService;
    private readonly CompletionListCache _completionListCache;
    private readonly VSInternalClientCapabilities _clientCapabilities;
    private readonly VSInternalCompletionContext _defaultCompletionContext;
    private readonly RazorCompletionOptions _razorCompletionOptions;
    private readonly TestOutputLoggerFactory _loggerFactory;

    public RazorCompletionListProviderTest(ITestOutputHelper testOutput)
    {
        _completionFactsService = new LspRazorCompletionFactsService(GetCompletionProviders());
        _completionListCache = new CompletionListCache();
        _clientCapabilities = new VSInternalClientCapabilities()
        {
            TextDocument = new TextDocumentClientCapabilities()
            {
                Completion = new VSInternalCompletionSetting()
                {
                    CompletionItemKind = new CompletionItemKindSetting()
                    {
                        ValueSet = [CompletionItemKind.TagHelper]
                    },
                    CompletionList = new VSInternalCompletionListSetting()
                    {
                        CommitCharacters = true,
                        Data = true,
                    }
                }
            }
        };

        _defaultCompletionContext = new VSInternalCompletionContext();
        _razorCompletionOptions = new RazorCompletionOptions(SnippetsSupported: true, AutoInsertAttributeQuotes: true, CommitElementsWithSpace: true, UseVsCodeCompletionCommitCharacters: false);

        _loggerFactory = new TestOutputLoggerFactory(testOutput);
    }

    private static IRazorCompletionItemProvider[] GetCompletionProviders()
        => [
            new DirectiveCompletionItemProvider(),
            new DirectiveAttributeCompletionItemProvider(),
            new TagHelperCompletionProvider(new TagHelperCompletionService())
        ];

    [Fact]
    public void TryConvert_Directive_ReturnsTrue()
    {
        // Arrange
        var completionItem = RazorCompletionItem.CreateDirective(
            displayText: "testDisplay",
            insertText: "testInsert",
            sortText: null,
            descriptionInfo: new("Something"),
            commitCharacters: [],
            isSnippet: false);

        // Act
        Assert.True(RazorCompletionListProvider.TryConvert(completionItem, _clientCapabilities, out var converted));

        // Assert
        Assert.Equal(completionItem.DisplayText, converted.Label);
        Assert.Equal(completionItem.InsertText, converted.InsertText);
        Assert.Equal(completionItem.DisplayText, converted.FilterText);
        Assert.Equal(completionItem.DisplayText, converted.SortText);
        Assert.Null(converted.Detail);
        Assert.Null(converted.Documentation);
    }

    [Fact]
    public void TryConvert_Directive_SerializationDoesNotThrow()
    {
        // Arrange
        var completionItem = RazorCompletionItem.CreateDirective(
            displayText: "testDisplay",
            insertText: "testInsert",
            sortText: null,
            descriptionInfo: new("Something"),
            commitCharacters: [],
            isSnippet: false);

        RazorCompletionListProvider.TryConvert(completionItem, _clientCapabilities, out var converted);

        // Act & Assert
        JsonSerializer.Serialize(converted);
    }

    [Fact]
    public void TryConvert_DirectiveAttributeTransition_SerializationDoesNotThrow()
    {
        // Arrange
        var directiveAttributeTransitionCompletionItemProvider = new DirectiveAttributeTransitionCompletionItemProvider(new TestClientCapabilitiesService(new()));
        var completionItem = directiveAttributeTransitionCompletionItemProvider.TransitionCompletionItem;
        RazorCompletionListProvider.TryConvert(completionItem, _clientCapabilities, out var converted);

        // Act & Assert
        JsonSerializer.Serialize(converted);
    }

    [Fact]
    public void TryConvert_DirectiveAttributeTransition_ReturnsTrue()
    {
        // Arrange
        var directiveAttributeTransitionCompletionItemProvider = new DirectiveAttributeTransitionCompletionItemProvider(new TestClientCapabilitiesService(new()));
        var completionItem = directiveAttributeTransitionCompletionItemProvider.TransitionCompletionItem;

        // Act
        Assert.True(RazorCompletionListProvider.TryConvert(completionItem, _clientCapabilities, out var converted));

        // Assert
        Assert.False(converted.Preselect);
        Assert.Equal(completionItem.DisplayText, converted.Label);
        Assert.Equal(completionItem.InsertText, converted.InsertText);
        Assert.Equal(completionItem.DisplayText, converted.FilterText);
        Assert.Equal(completionItem.DisplayText, converted.SortText);
        Assert.Null(converted.Detail);
        Assert.Null(converted.Documentation);
        Assert.NotNull(converted.Command);
    }

    [Fact]
    public void TryConvert_MarkupTransition_ReturnsTrue()
    {
        // Arrange
        var completionItem = MarkupTransitionCompletionItemProvider.MarkupTransitionCompletionItem;

        // Act
        Assert.True(RazorCompletionListProvider.TryConvert(completionItem, _clientCapabilities, out var converted));

        // Assert
        Assert.Equal(completionItem.DisplayText, converted.Label);
        Assert.Equal(completionItem.InsertText, converted.InsertText);
        Assert.Equal(completionItem.DisplayText, converted.FilterText);
        Assert.Equal(completionItem.DisplayText, converted.SortText);
        Assert.Null(converted.Detail);
        Assert.Null(converted.Documentation);
        Assert.Equal(converted.CommitCharacters, completionItem.CommitCharacters.Select(c => c.Character));
    }

    [Fact]
    public void TryConvert_MarkupTransition_SerializationDoesNotThrow()
    {
        // Arrange
        var completionItem = MarkupTransitionCompletionItemProvider.MarkupTransitionCompletionItem;
        RazorCompletionListProvider.TryConvert(completionItem, _clientCapabilities, out var converted);

        // Act & Assert
        JsonSerializer.Serialize(converted);
    }

    [Fact]
    public void TryConvert_DirectiveAttribute_ReturnsTrue()
    {
        // Arrange
        var completionItem = RazorCompletionItem.CreateDirectiveAttribute(
            displayText: "@testDisplay",
            insertText: "testInsert",
            descriptionInfo: null!,
            commitCharacters: RazorCommitCharacter.CreateArray(["=", ":"]),
            isSnippet: false);

        // Act
        Assert.True(RazorCompletionListProvider.TryConvert(completionItem, _clientCapabilities, out var converted));

        // Assert
        Assert.Equal(completionItem.DisplayText, converted.Label);
        Assert.Equal(completionItem.InsertText, converted.InsertText);
        Assert.Equal(completionItem.InsertText, converted.FilterText);
        Assert.Equal(completionItem.DisplayText, converted.SortText);
        Assert.Equal(completionItem.CommitCharacters.Select(c => c.Character), converted.CommitCharacters);
        Assert.Null(converted.Detail);
        Assert.Null(converted.Documentation);
        Assert.Null(converted.Command);
    }

    [Fact]
    public void TryConvert_DirectiveAttributeParameter_ReturnsTrue()
    {
        // Arrange
        var completionItem = RazorCompletionItem.CreateDirectiveAttributeParameter(displayText: "format", insertText: "format", descriptionInfo: null!, commitCharacters: [], isSnippet: false);

        // Act
        Assert.True(RazorCompletionListProvider.TryConvert(completionItem, _clientCapabilities, out var converted));

        // Assert
        Assert.Equal(completionItem.DisplayText, converted.Label);
        Assert.Equal(completionItem.InsertText, converted.InsertText);
        Assert.Equal(completionItem.InsertText, converted.FilterText);
        Assert.Equal(completionItem.InsertText, converted.SortText);
        Assert.Null(converted.Detail);
        Assert.Null(converted.Documentation);
        Assert.Null(converted.Command);
    }

    [Fact]
    public void TryConvert_TagHelperElement_ReturnsTrue()
    {
        // Arrange
        var completionItem = RazorCompletionItem.CreateTagHelperElement(displayText: "format", insertText: "format", descriptionInfo: null!, commitCharacters: []);

        // Act
        Assert.True(RazorCompletionListProvider.TryConvert(completionItem, _clientCapabilities, out var converted));

        // Assert
        Assert.Equal(completionItem.DisplayText, converted.Label);
        Assert.Equal(completionItem.InsertText, converted.InsertText);
        Assert.Equal(completionItem.InsertText, converted.FilterText);
        Assert.Equal(completionItem.InsertText, converted.SortText);
        Assert.Null(converted.Detail);
        Assert.Null(converted.Documentation);
        Assert.Null(converted.Command);
    }

    [Fact]
    public void TryConvert_TagHelperAttribute_ForBool_ReturnsTrue()
    {
        // Arrange
        var attributeCompletionDescription = new AggregateBoundAttributeDescription([
            new BoundAttributeDescriptionInfo("System.Boolean", "Stuff", "format", "SomeDocs")
        ]);

        var completionItem = RazorCompletionItem.CreateTagHelperAttribute(
            displayText: "format",
            insertText: "format",
            sortText: null,
            descriptionInfo: attributeCompletionDescription,
            commitCharacters: [],
            isSnippet: false);

        // Act
        Assert.True(RazorCompletionListProvider.TryConvert(completionItem, _clientCapabilities, out var converted));

        // Assert
        Assert.Equal(completionItem.DisplayText, converted.Label);
        Assert.Equal("format", converted.InsertText);
        Assert.Equal(InsertTextFormat.Plaintext, converted.InsertTextFormat);
        Assert.Equal(completionItem.InsertText, converted.FilterText);
        Assert.Equal(completionItem.InsertText, converted.SortText);
        Assert.Null(converted.Detail);
        Assert.Null(converted.Documentation);
        Assert.Null(converted.Command);
    }

    [Fact]
    public void TryConvert_TagHelperAttribute_ForHtml_ReturnsTrue()
    {
        // Arrange
        var completionItem = RazorCompletionItem.CreateTagHelperAttribute(
            displayText: "format",
            insertText: "format=\"$0\"",
            sortText: null,
            descriptionInfo: AggregateBoundAttributeDescription.Empty,
            commitCharacters: [],
            isSnippet: true);

        // Act
        Assert.True(RazorCompletionListProvider.TryConvert(completionItem, _clientCapabilities, out var converted));

        // Assert
        Assert.Equal(completionItem.DisplayText, converted.Label);
        Assert.Equal("format=\"$0\"", converted.InsertText);
        Assert.Equal(InsertTextFormat.Snippet, converted.InsertTextFormat);
        Assert.Equal(completionItem.DisplayText, converted.FilterText);
        Assert.Equal(completionItem.DisplayText, converted.SortText);
        Assert.Null(converted.Detail);
        Assert.Null(converted.Documentation);
        Assert.Null(converted.Command);
    }

    [Fact]
    public void TryConvert_TagHelperAttribute_ReturnsTrue()
    {
        // Arrange
        var completionItem = RazorCompletionItem.CreateTagHelperAttribute(
            displayText: "format",
            insertText: "format=\"$0\"",
            sortText: null,
            descriptionInfo: null!,
            commitCharacters: [],
            isSnippet: true);

        // Act
        Assert.True(RazorCompletionListProvider.TryConvert(completionItem, _clientCapabilities, out var converted));

        // Assert
        Assert.Equal(completionItem.DisplayText, converted.Label);
        Assert.Equal("format=\"$0\"", converted.InsertText);
        Assert.Equal(InsertTextFormat.Snippet, converted.InsertTextFormat);
        Assert.Equal(completionItem.DisplayText, converted.FilterText);
        Assert.Equal(completionItem.DisplayText, converted.SortText);
        Assert.Null(converted.Detail);
        Assert.Null(converted.Documentation);
        Assert.Null(converted.Command);
    }

    // This is more of an integration test to validate that all the pieces work together
    [Theory]
    [InlineData("@$$")]
    [InlineData("@$$\r\n")]
    [InlineData("@page\r\n@$$")]
    [InlineData("@page\r\n@$$\r\n")]
    [InlineData("@page\r\n<div></div>\r\n@f$$")]
    [InlineData("@page\r\n<div></div>\r\n@f$$\r\n")]
    [WorkItem("https://github.com/dotnet/razor-tooling/issues/4547")]
    [WorkItem("https://github.com/dotnet/razor/issues/9955")]
    public void GetCompletionList_ProvidesDirectiveCompletionItems(string documentText)
    {
        // Arrange
        var documentPath = "C:/path/to/document.cshtml";
        TestFileMarkupParser.GetPosition(documentText, out documentText, out var cursorPosition);
        var codeDocument = CreateCodeDocument(documentText, documentPath);
        var provider = new RazorCompletionListProvider(_completionFactsService, _completionListCache, _loggerFactory);

        // Act
        var completionList = provider.GetCompletionList(
            codeDocument, absoluteIndex: cursorPosition, _defaultCompletionContext, _clientCapabilities, existingCompletions: null, _razorCompletionOptions);

        // Assert

        Assert.NotNull(completionList);

        // These are the default directives that don't need to be separately registered, they should always be part of the completion list.
        Assert.Collection(completionList.Items,
            DirectiveVerifier.DefaultDirectiveCollectionVerifiers
        );
    }

    [Fact]
    public void GetCompletionListAsync_ProvidesDirectiveCompletions_IncompleteTriggerOnDeletion()
    {
        // Arrange
        var documentPath = "C:/path/to/document.cshtml";
        var codeDocument = CreateCodeDocument("@", documentPath);
        var completionContext = new VSInternalCompletionContext()
        {
            TriggerKind = CompletionTriggerKind.TriggerForIncompleteCompletions,
            InvokeKind = VSInternalCompletionInvokeKind.Deletion,
        };

        var provider = new RazorCompletionListProvider(_completionFactsService, _completionListCache, _loggerFactory);

        // Act
        var completionList = provider.GetCompletionList(
            codeDocument, absoluteIndex: 1, completionContext, _clientCapabilities, existingCompletions: null, _razorCompletionOptions);

        // Assert
        Assert.NotNull(completionList);

        // These are the default directives that don't need to be separately registered, they should always be part of the completion list.
        Assert.Contains(completionList.Items, item => item.InsertText == "addTagHelper");
        Assert.Contains(completionList.Items, item => item.InsertText == "removeTagHelper");
        Assert.Contains(completionList.Items, item => item.InsertText == "tagHelperPrefix");
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor-tooling/issues/4547")]
    public void GetCompletionList_ProvidesInjectOnIncomplete_KeywordIn()
    {
        // Arrange
        var documentPath = "C:/path/to/document.razor";
        var builder = TagHelperDescriptorBuilder.CreateComponent("TestTagHelper", "TestAssembly");
        builder.TypeName = "TestNamespace.TestTagHelper";
        builder.TagMatchingRule(rule => rule.TagName = "Test");
        var tagHelper = builder.Build();
        var codeDocument = CreateCodeDocument("@in", documentPath, [tagHelper]);
        var provider = new RazorCompletionListProvider(_completionFactsService, _completionListCache, _loggerFactory);
        var completionContext = new VSInternalCompletionContext()
        {
            TriggerKind = CompletionTriggerKind.TriggerForIncompleteCompletions,
        };

        // Act
        var completionList = provider.GetCompletionList(
            codeDocument, absoluteIndex: 1, completionContext, _clientCapabilities, existingCompletions: null, _razorCompletionOptions);

        // Assert
        Assert.NotNull(completionList);

        Assert.Collection(completionList.Items,
            DirectiveVerifier.DefaultDirectiveCollectionVerifiers
        );
    }

    [Fact]
    public void GetCompletionList_DoesNotProvideInjectOnInvoked()
    {
        // Arrange
        var documentPath = "C:/path/to/document.razor";
        var builder = TagHelperDescriptorBuilder.CreateComponent("TestTagHelper", "TestAssembly");
        builder.TypeName = "TestNamespace.TestTagHelper";
        builder.TagMatchingRule(rule => rule.TagName = "Test");
        var tagHelper = builder.Build();
        var codeDocument = CreateCodeDocument("@inje", documentPath, [tagHelper]);
        var provider = new RazorCompletionListProvider(_completionFactsService, _completionListCache, _loggerFactory);
        var completionContext = new VSInternalCompletionContext()
        {
            TriggerKind = CompletionTriggerKind.TriggerCharacter,
        };

        // Act
        var completionList = provider.GetCompletionList(
            codeDocument, absoluteIndex: 1, completionContext, _clientCapabilities, existingCompletions: null, _razorCompletionOptions);

        // Assert
        Assert.Null(completionList);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor-tooling/issues/4547")]
    public void GetCompletionList_ProvidesInjectOnIncomplete()
    {
        // Arrange
        var documentPath = "C:/path/to/document.razor";
        var builder = TagHelperDescriptorBuilder.CreateComponent("TestTagHelper", "TestAssembly");
        builder.TypeName = "TestNamespace.TestTagHelper";
        builder.TagMatchingRule(rule => rule.TagName = "Test");
        var tagHelper = builder.Build();
        var codeDocument = CreateCodeDocument("@inje", documentPath, [tagHelper]);
        var provider = new RazorCompletionListProvider(_completionFactsService, _completionListCache, _loggerFactory);
        var completionContext = new VSInternalCompletionContext()
        {
            TriggerKind = CompletionTriggerKind.TriggerForIncompleteCompletions,
        };

        // Act
        var completionList = provider.GetCompletionList(
            codeDocument, absoluteIndex: 1, completionContext, _clientCapabilities, existingCompletions: null, _razorCompletionOptions);

        // Assert
        Assert.NotNull(completionList);

        Assert.Collection(completionList.Items,
            DirectiveVerifier.DefaultDirectiveCollectionVerifiers
        );
    }

    // This is more of an integration test to validate that all the pieces work together
    [Fact]
    public void GetCompletionList_ProvidesTagHelperElementCompletionItems()
    {
        // Arrange
        var documentPath = "C:/path/to/document.cshtml";
        var builder = TagHelperDescriptorBuilder.CreateComponent("TestTagHelper", "TestAssembly");
        builder.TypeName = "TestNamespace.TestTagHelper";
        builder.TagMatchingRule(rule => rule.TagName = "Test");
        var tagHelper = builder.Build();
        var codeDocument = CreateCodeDocument("<", documentPath, [tagHelper]);
        var provider = new RazorCompletionListProvider(_completionFactsService, _completionListCache, _loggerFactory);

        // Act
        var completionList = provider.GetCompletionList(
            codeDocument, absoluteIndex: 1, _defaultCompletionContext, _clientCapabilities, existingCompletions: null, _razorCompletionOptions);

        // Assert
        Assert.NotNull(completionList);
        Assert.Contains(completionList.Items, item => item.InsertText == "Test");
    }

    // This is more of an integration test to validate that all the pieces work together
    [Fact]
    public void GetCompletionList_ProvidesTagHelperAttributeItems()
    {
        // Arrange
        var documentPath = "C:/path/to/document.cshtml";
        var builder = TagHelperDescriptorBuilder.CreateComponent("TestTagHelper", "TestAssembly");
        builder.TypeName = "TestNamespace.TestTagHelper";
        builder.TagMatchingRule(rule => rule.TagName = "*");
        builder.BindAttribute(attribute =>
        {
            attribute.Name = "testAttribute";
            attribute.TypeName = typeof(string).FullName;
            attribute.PropertyName = "TestAttribute";
        });
        var tagHelper = builder.Build();
        var codeDocument = CreateCodeDocument("<test  ", documentPath, [tagHelper]);
        var provider = new RazorCompletionListProvider(_completionFactsService, _completionListCache, _loggerFactory);

        // Act
        var completionList = provider.GetCompletionList(
            codeDocument, absoluteIndex: 6, _defaultCompletionContext, _clientCapabilities, existingCompletions: null, _razorCompletionOptions);

        // Assert
        Assert.NotNull(completionList);
        Assert.Contains(completionList.Items, item => item.InsertText == "testAttribute=\"$0\"");
    }

    [Fact]
    public void GetCompletionList_ProvidesTagHelperAttributeItems_AttributeQuotesOff()
    {
        // Arrange
        var documentPath = "C:/path/to/document.cshtml";
        var builder = TagHelperDescriptorBuilder.CreateComponent("TestTagHelper", "TestAssembly");
        builder.TypeName = "TestNamespace.TestTagHelper";
        builder.TagMatchingRule(rule => rule.TagName = "*");
        builder.BindAttribute(attribute =>
        {
            attribute.Name = "testAttribute";
            attribute.TypeName = typeof(string).FullName;
            attribute.PropertyName = "TestAttribute";
        });
        var tagHelper = builder.Build();
        var codeDocument = CreateCodeDocument("<test  ", documentPath, [tagHelper]);

        // Set up desired options
        var razorCompletionOptions = new RazorCompletionOptions(SnippetsSupported: true, AutoInsertAttributeQuotes: false, CommitElementsWithSpace: true, UseVsCodeCompletionCommitCharacters: false);

        var completionFactsService = new LspRazorCompletionFactsService(GetCompletionProviders());
        var provider = new RazorCompletionListProvider(completionFactsService, _completionListCache, _loggerFactory);

        // Act
        var completionList = provider.GetCompletionList(
            codeDocument, absoluteIndex: 6, _defaultCompletionContext, _clientCapabilities, existingCompletions: null, razorCompletionOptions);

        // Assert
        Assert.NotNull(completionList);
        Assert.Contains(completionList.Items, item => item.InsertText == "testAttribute=$0");
    }

    private static RazorCodeDocument CreateCodeDocument(string text, string documentFilePath, TagHelperCollection? tagHelpers = null)
    {
        var codeDocument = TestRazorCodeDocument.CreateEmpty();
        var sourceDocument = TestRazorSourceDocument.Create(text, filePath: documentFilePath);
        var syntaxTree = RazorSyntaxTree.Parse(sourceDocument);
        codeDocument = codeDocument.WithTagHelperRewrittenSyntaxTree(syntaxTree);
        var tagHelperDocumentContext = TagHelperDocumentContext.GetOrCreate(tagHelpers ?? []);
        codeDocument = codeDocument.WithTagHelperContext(tagHelperDocumentContext);
        return codeDocument;
    }
}
