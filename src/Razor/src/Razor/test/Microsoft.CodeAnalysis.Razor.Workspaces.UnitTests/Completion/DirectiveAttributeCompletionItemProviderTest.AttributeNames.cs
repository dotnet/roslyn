// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.IntegrationTests;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.Test.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.Completion;

public partial class DirectiveAttributeCompletionItemProviderTest : RazorToolingIntegrationTestBase
{
    private readonly DirectiveAttributeCompletionItemProvider _provider;
    private readonly TagHelperDocumentContext _defaultTagHelperContext;
    private readonly RazorCompletionOptions _defaultRazorCompletionOptions;
    internal override RazorFileKind? FileKind => RazorFileKind.Component;
    internal override bool UseTwoPhaseCompilation => true;

    public DirectiveAttributeCompletionItemProviderTest(ITestOutputHelper testOutput)
        : base(testOutput)
    {
        _provider = new DirectiveAttributeCompletionItemProvider();

        // Most of these completions rely on stuff in the web namespace.
        ImportItems.Add(CreateProjectItem(
            "_Imports.razor",
            "@using Microsoft.AspNetCore.Components.Web"));

        var codeDocument = GetCodeDocument(string.Empty);
        _defaultTagHelperContext = codeDocument.GetRequiredTagHelperContext();
        _defaultRazorCompletionOptions = new RazorCompletionOptions(SnippetsSupported: true, AutoInsertAttributeQuotes: true, CommitElementsWithSpace: true, UseVsCodeCompletionCommitCharacters: false);
    }

    private RazorCodeDocument GetCodeDocument(string content)
    {
        var result = CompileToCSharp(content, throwOnFailure: false);
        return result.CodeDocument;
    }

    [Fact]
    public void GetCompletionItems_OnNonAttributeArea_ReturnsEmptyCollection()
    {
        // Arrange
        var context = CreateRazorCompletionContext("<in$$put @  />");

        // Act
        var completions = _provider.GetCompletionItems(context);

        // Assert
        Assert.Empty(completions);
    }

    [Fact]
    public void GetCompletionItems_OnDirectiveAttributeName_bind_ReturnsCompletions()
    {
        // Arrange
        var context = CreateRazorCompletionContext("<input @$$  />");

        // Act
        var completions = _provider.GetCompletionItems(context);

        // Assert
        AssertContains(completions, "bind", "@bind", ["="]);
    }

    [Fact]
    public void GetCompletionItems_OnDirectiveAttributeName_bind_ReturnsParameterCompletions()
    {
        // Arrange
        var context = CreateRazorCompletionContext("<input @$$  />");

        // Act
        var completions = _provider.GetCompletionItems(context);

        // Assert
        AssertContainsParameter(completions, "bind-value:format", "@bind-value:format", ["="]);
    }

    [Fact]
    public void GetCompletionItems_OnDirectiveAttributeName_bind_ReturnsParameterSnippetCompletions()
    {
        // Arrange
        var context = CreateRazorCompletionContext("<input @$$  />") with
        {
            Options = _defaultRazorCompletionOptions
        };

        // Act
        var completions = _provider.GetCompletionItems(context);

        // Assert
        AssertContainsParameter(completions, "bind-value:format=\"$0\"", "@bind-value:format", ["="]);
    }

    [Fact]
    public void GetCompletionItems_OnDirectiveAttributeName_attributes_ReturnsCompletions()
    {
        // Arrange
        var context = CreateRazorCompletionContext("<input @$$  />");

        // Act
        var completions = _provider.GetCompletionItems(context);

        // Assert
        AssertContains(completions, "attributes", "@attributes", ["="]);
    }

    [Fact]
    public void GetCompletionItems_AttributeAreaEndOfSelfClosingTag_ReturnsEmptyCollection()
    {
        // Arrange
        var context = CreateRazorCompletionContext("<input @bind:fo $$ />");

        // Act
        var completions = _provider.GetCompletionItems(context);

        // Assert
        Assert.Empty(completions);
    }

    [Fact]
    public void GetCompletionItems_AttributeAreaEndOfOpeningTag_ReturnsEmptyCollection()
    {
        // Arrange
        var context = CreateRazorCompletionContext("<input @bind:fo $$  ></input>");

        // Act
        var completions = _provider.GetCompletionItems(context);

        // Assert
        Assert.Empty(completions);
    }

    [Fact]
    public void GetCompletionItems_ExistingAttribute_LeadingEdge_ReturnsEmptyCollection()
    {
        // Arrange
        var context = CreateRazorCompletionContext("<input $$src=\"xyz\" />");

        // Act
        var completions = _provider.GetCompletionItems(context);

        // Assert
        Assert.Empty(completions);
    }

    [Fact]
    public void GetCompletionItems_ExistingAttribute_TrailingEdge_ReturnsEmptyCollection()
    {
        // Arrange
        var context = CreateRazorCompletionContext("<input src=\"xyz$$\" />");

        // Act
        var completions = _provider.GetCompletionItems(context);

        // Assert
        Assert.Empty(completions);
    }

    [Fact]
    public void GetCompletionItems_ExistingAttribute_Partial_ReturnsEmptyCollection()
    {
        // Arrange
        var context = CreateRazorCompletionContext("<svg xml:$$ ></svg>");

        // Act
        var completions = _provider.GetCompletionItems(context);

        // Assert
        Assert.Empty(completions);
    }

    [Fact]
    public void GetAttributeCompletions_NoDescriptorsForTag_ReturnsEmptyCollection()
    {
        // Arrange
        var documentContext = TagHelperDocumentContext.GetOrCreate(tagHelpers: []);
        var context = GetDefaultDirectivateAttributeCompletionContext("@bin");

        // Act
        var completions = DirectiveAttributeCompletionItemProvider.GetAttributeCompletions("foobarbaz", context, documentContext);

        // Assert
        Assert.Empty(completions);
    }

    [Fact]
    public void GetAttributeCompletions_NoDirectiveAttributesForTag_ReturnsEmptyCollection()
    {
        // Arrange
        var descriptor = TagHelperDescriptorBuilder.CreateTagHelper("CatchAll", "TestAssembly");
        descriptor.BoundAttributeDescriptor(boundAttribute => boundAttribute.Name = "Test");
        descriptor.TagMatchingRule(rule => rule.RequireTagName("*"));
        var documentContext = TagHelperDocumentContext.GetOrCreate([descriptor.Build()]);

        var context = GetDefaultDirectivateAttributeCompletionContext("@bin");

        // Act
        var completions = DirectiveAttributeCompletionItemProvider.GetAttributeCompletions("input", context, documentContext);

        // Assert
        Assert.Empty(completions);
    }

    [Fact]
    public void GetAttributeCompletions_SelectedDirectiveAttribute_IsIncludedInCompletions()
    {
        // Arrange
        var context = GetDefaultDirectivateAttributeCompletionContext("@bind") with
        {
            ExistingAttributes = ["@bind"],
        };

        // Act
        var completions = DirectiveAttributeCompletionItemProvider.GetAttributeCompletions("input", context, _defaultTagHelperContext);

        // Assert
        AssertContains(completions, "bind=\"$0\"", "@bind", ["="]);
    }

    [Fact]
    public void GetAttributeCompletions_Parameter_IsIncludedInCompletions()
    {
        // Arrange
        var context = GetDefaultDirectivateAttributeCompletionContext("@bind");

        // Act
        var completions = DirectiveAttributeCompletionItemProvider.GetAttributeCompletions("input", context, _defaultTagHelperContext);

        // Assert
        AssertContains(completions, "bind-value=\"$0\"", "@bind-value", ["="]);
    }

    [Fact]
    public void GetAttributeCompletions_NonIndexer_ReturnsCompletion()
    {
        // Arrange
        var context = GetDefaultDirectivateAttributeCompletionContext("@");

        // Act
        var completions = DirectiveAttributeCompletionItemProvider.GetAttributeCompletions("input", context, _defaultTagHelperContext);

        // Assert
        AssertContains(completions, "bind=\"$0\"", "@bind", ["="]);
    }

    [Fact]
    public void GetAttributeCompletions_NonIndexer_ReturnsCompletionWithEqualsCommitInsertFalse()
    {
        // Arrange
        var context = GetDefaultDirectivateAttributeCompletionContext("@");

        // Act
        var completions = DirectiveAttributeCompletionItemProvider.GetAttributeCompletions("input", context, _defaultTagHelperContext);

        // Assert
        AssertContains(completions, "bind=\"$0\"", "@bind", [new RazorCommitCharacter("=", Insert: false)]);
    }

    [Fact]
    public void GetAttributeCompletions_WithNoAutoQuotesOption_ReturnsNonQuotedSnippet()
    {
        // Arrange
        var noAutoQuotesRazorCompletionOptions = new RazorCompletionOptions(SnippetsSupported: true, AutoInsertAttributeQuotes: false, CommitElementsWithSpace: true, UseVsCodeCompletionCommitCharacters: false);
        var context = GetDefaultDirectivateAttributeCompletionContext("@") with
        {
            Options = noAutoQuotesRazorCompletionOptions
        };

        // Act
        var completions = DirectiveAttributeCompletionItemProvider.GetAttributeCompletions("input", context, _defaultTagHelperContext);

        // Assert
        AssertContains(completions, "bind=$0", "@bind", ["="]);
    }

    [Fact]
    public void GetAttributeCompletions_WithNoSnippetsOption_ReturnsNoSnippets()
    {
        // Arrange
        var noAutoQuotesRazorCompletionOptions = new RazorCompletionOptions(SnippetsSupported: false, AutoInsertAttributeQuotes: true, CommitElementsWithSpace: true, UseVsCodeCompletionCommitCharacters: false);
        var context = GetDefaultDirectivateAttributeCompletionContext("@") with
        {
            UseSnippets = false,
            Options = noAutoQuotesRazorCompletionOptions
        };

        // Act
        var completions = DirectiveAttributeCompletionItemProvider.GetAttributeCompletions("input", context, _defaultTagHelperContext);

        // Assert
        AssertContains(completions, "bind", "@bind", ["="]);
    }

    [Fact]
    public void GetAttributeCompletions_ExistingAttributeWithValue_ReturnsNoSnippets()
    {
        // Arrange
        var noAutoQuotesRazorCompletionOptions = new RazorCompletionOptions(SnippetsSupported: false, AutoInsertAttributeQuotes: true, CommitElementsWithSpace: true, UseVsCodeCompletionCommitCharacters: false);
        var context = GetDefaultDirectivateAttributeCompletionContext("@") with
        {
            UseSnippets = false,
            Options = noAutoQuotesRazorCompletionOptions
        };

        // Act
        var completions = DirectiveAttributeCompletionItemProvider.GetAttributeCompletions("input", context, _defaultTagHelperContext);

        // Assert
        AssertContains(completions, "bind", "@bind", ["="]);
    }

    [Fact]
    public void GetAttributeCompletions_Indexer_ReturnsCompletion()
    {
        // Arrange
        var context = GetDefaultDirectivateAttributeCompletionContext("@");

        // Act
        var completions = DirectiveAttributeCompletionItemProvider.GetAttributeCompletions("input", context, _defaultTagHelperContext);

        // Assert
        AssertContains(completions, "bind-", "@bind-...", ImmutableArray<string>.Empty);
    }

    [Fact]
    public void GetAttributeCompletions_BaseDirectiveAttributeAlreadyExists_IncludesBaseAttribute()
    {
        // Arrange
        var context = GetDefaultDirectivateAttributeCompletionContext("@") with
        {
            ExistingAttributes = ["@bind", "@"],
        };

        // Act
        var completions = DirectiveAttributeCompletionItemProvider.GetAttributeCompletions("input", context, _defaultTagHelperContext);

        // Assert
        AssertContains(completions, "bind=\"$0\"", "@bind", ["="]);
    }

    [Fact]
    public void GetAttributeCompletions_BaseDirectiveAttributeAndParameterVariationsExist_ExcludesCompletion()
    {
        // Arrange
        var context = GetDefaultDirectivateAttributeCompletionContext("@") with
        {
            ExistingAttributes = ["@bind", "@bind:format", "@bind:event", "@bind:culture", "@bind:get", "@bind:set", "@bind:after", "@"],
        };

        // Act
        var completions = DirectiveAttributeCompletionItemProvider.GetAttributeCompletions("input", context, _defaultTagHelperContext);

        // Assert
        AssertDoesNotContain(completions, "bind", "@bind");
    }

    private static void AssertContainsParameter(ImmutableArray<RazorCompletionItem> completions, string insertText, string displayText, ImmutableArray<string> commitCharacters)
        => AssertContains(completions, insertText, displayText, commitCharacters, RazorCompletionItemKind.DirectiveAttributeParameter);

    private static void AssertContains(ImmutableArray<RazorCompletionItem> completions, string insertText, string displayText, ImmutableArray<string> commitCharacters)
        => AssertContains(completions, insertText, displayText, commitCharacters, RazorCompletionItemKind.DirectiveAttribute);

    private static void AssertContains(ImmutableArray<RazorCompletionItem> completions, string insertText, string displayText, ImmutableArray<string> commitCharacters, RazorCompletionItemKind kind)
    {
        displayText ??= insertText;

        Assert.Contains(completions, completion =>
            insertText == completion.InsertText &&
            displayText == completion.DisplayText &&
            commitCharacters.SequenceEqual(completion.CommitCharacters.Select(c => c.Character)) &&
            kind == completion.Kind);
    }

    private static void AssertContains(ImmutableArray<RazorCompletionItem> completions, string insertText, string displayText, ImmutableArray<RazorCommitCharacter> commitCharacters)
    {
        displayText ??= insertText;

        Assert.Contains(completions, completion =>
            insertText == completion.InsertText &&
            displayText == completion.DisplayText &&
            commitCharacters.SequenceEqual(completion.CommitCharacters) &&
            RazorCompletionItemKind.DirectiveAttribute == completion.Kind);
    }

    private static void AssertDoesNotContain(IReadOnlyList<RazorCompletionItem> completions, string insertText, string displayText)
    {
        displayText ??= insertText;

        Assert.DoesNotContain(completions, completion => insertText == completion.InsertText &&
               displayText == completion.DisplayText &&
               RazorCompletionItemKind.DirectiveAttribute == completion.Kind);
    }

    internal RazorCompletionContext CreateRazorCompletionContext(TestCode testCode)
    {
        var codeDocument = GetCodeDocument(testCode.Text);
        var syntaxTree = codeDocument.GetRequiredTagHelperRewrittenSyntaxTree();
        var tagHelperContext = codeDocument.GetRequiredTagHelperContext();

        var owner = syntaxTree.Root.FindInnermostNode(testCode.Position, includeWhitespace: true, walkMarkersBack: true);
        owner = AbstractRazorCompletionFactsService.AdjustSyntaxNodeForWordBoundary(owner, testCode.Position);

        return new RazorCompletionContext(codeDocument, testCode.Position, owner, syntaxTree, tagHelperContext);
    }

    private DirectiveAttributeCompletionContext GetDefaultDirectivateAttributeCompletionContext(string selectedAttributeName)
    {
        return new DirectiveAttributeCompletionContext()
        {
            SelectedAttributeName = selectedAttributeName,
            InAttributeName = true,
            InParameterName = false,
            UseSnippets = true,
            Options = _defaultRazorCompletionOptions
        };
    }
}
