// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.Completion;

public partial class DirectiveAttributeCompletionItemProviderTest
{
    [Fact]
    public void GetCompletionItems_OnDirectiveAttributeParameter_ReturnsCompletions()
    {
        // Arrange
        var context = CreateRazorCompletionContext("<input @bind:f$$o  />");

        // Act
        var completions = _provider.GetCompletionItems(context);

        // Assert
        Assert.Equal(6, completions.Length);
        AssertContains(completions, "culture");
        AssertContains(completions, "event");
        AssertContains(completions, "format");
        AssertContains(completions, "get");
        AssertContains(completions, "set");
        AssertContains(completions, "after");
    }

    [Fact]
    public void GetAttributeParameterCompletions_NoDescriptorsForTag_ReturnsEmptyCollection()
    {
        // Arrange
        var documentContext = TagHelperDocumentContext.GetOrCreate(tagHelpers: []);
        var context = GetDefaultDirectiveAttributeCompletionContext("@bin");

        // Act
        var completions = DirectiveAttributeCompletionItemProvider.GetAttributeCompletions("foobarbaz", context, documentContext);

        // Assert
        Assert.Empty(completions);
    }

    [Fact]
    public void GetAttributeParameterCompletions_NoDirectiveAttributesForTag_ReturnsEmptyCollection()
    {
        // Arrange
        var descriptor = TagHelperDescriptorBuilder.CreateTagHelper("CatchAll", "TestAssembly");
        descriptor.BoundAttributeDescriptor(boundAttribute => boundAttribute.Name = "Test");
        descriptor.TagMatchingRule(rule => rule.RequireTagName("*"));
        var documentContext = TagHelperDocumentContext.GetOrCreate([descriptor.Build()]);

        var context = GetDefaultDirectiveAttributeCompletionContext("@bin");

        // Act
        var completions = DirectiveAttributeCompletionItemProvider.GetAttributeCompletions("input", context, documentContext);

        // Assert
        Assert.Empty(completions);
    }

    [Fact]
    public void GetAttributeParameterCompletions_SelectedDirectiveAttributeParameter_IsExcludedInCompletions()
    {
        // Arrange
        var context = GetDefaultDirectiveAttributeCompletionContext("@bin") with
        {
            ExistingAttributes = ["@bind"],
        };

        // Act
        var completions = DirectiveAttributeCompletionItemProvider.GetAttributeCompletions("input", context, _defaultTagHelperContext);

        // Assert
        AssertDoesNotContain(completions, "format");
    }

    [Fact]
    public void GetAttributeParameterCompletions_ReturnsCompletion()
    {
        // Arrange
        var context = GetDefaultDirectiveAttributeCompletionContext("@bind");

        // Act
        var completions = DirectiveAttributeCompletionItemProvider.GetAttributeCompletions("input", context, _defaultTagHelperContext);

        // Assert
        AssertContains(completions, "format");
    }

    [Fact]
    public void GetAttributeParameterCompletions_ReturnsSnippetCompletion()
    {
        // Arrange
        var context = GetDefaultDirectiveAttributeCompletionContext("@bind") with
        {
            UseSnippets = true,
        };

        // Act
        var completions = DirectiveAttributeCompletionItemProvider.GetAttributeCompletions("input", context, _defaultTagHelperContext);

        // Assert
        AssertContains(completions, "format=\"$0\"", "format");
    }

    [Fact]
    public void GetCompletionItems_OnDirectiveAttributeName_bind_ReturnsSameParameterCompletions()
    {
        // Arrange
        var contextAttributeName = CreateRazorCompletionContext("<input @$$  />");
        var contextParameterName = CreateRazorCompletionContext("<input @bind-value:$$  />");

        // Act
        var completionsAttributeName = _provider.GetCompletionItems(contextAttributeName);
        var completionsParameterName = _provider.GetCompletionItems(contextParameterName);

        // Assert
        var parameterNamesFromAttributeCompletions = completionsAttributeName
            .Where(c => c.DisplayText.StartsWith("@bind-value:"))
            .SelectAsArray(c => c.DisplayText["@bind-Value:".Length..]);
        var parameterNamesFromParameterCompletions = completionsParameterName
            .SelectAsArray(c => c.DisplayText);

        AssertEx.SequenceEqual(parameterNamesFromAttributeCompletions, parameterNamesFromParameterCompletions);
    }

    [Fact]
    public void GetAttributeParameterCompletions_BaseDirectiveAttributeAndParameterVariationsExist_ExcludesCompletion()
    {
        // Arrange
        var context = GetDefaultDirectiveAttributeCompletionContext("@bin") with
        {
            ExistingAttributes = ["@bind", "@bind:format", "@bind:event", "@"],
        };

        // Act
        var completions = DirectiveAttributeCompletionItemProvider.GetAttributeCompletions("input", context, _defaultTagHelperContext);

        // Assert
        AssertDoesNotContain(completions, "format");
    }

    private static void AssertContains(IReadOnlyList<RazorCompletionItem> completions, string insertText)
        => AssertContains(completions, insertText, insertText);

    private static void AssertContains(IReadOnlyList<RazorCompletionItem> completions, string insertText, string displayText)
    {
        Assert.Contains(completions, completion => insertText == completion.InsertText &&
                displayText == completion.DisplayText &&
                RazorCompletionItemKind.DirectiveAttributeParameter == completion.Kind);
    }

    private static void AssertDoesNotContain(IReadOnlyList<RazorCompletionItem> completions, string insertText)
    {
        Assert.DoesNotContain(completions, completion => insertText == completion.InsertText &&
               insertText == completion.DisplayText &&
               RazorCompletionItemKind.DirectiveAttributeParameter == completion.Kind);
    }

    private DirectiveAttributeCompletionContext GetDefaultDirectiveAttributeCompletionContext(string selectedAttributeName)
    {
        return new DirectiveAttributeCompletionContext()
        {
            SelectedAttributeName = selectedAttributeName,
            InAttributeName = false,
            InParameterName = true,
            UseSnippets = false,
            Options = _defaultRazorCompletionOptions
        };
    }
}
