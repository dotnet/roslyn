// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Legacy;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.Test.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.Completion;

public class MarkupTransitionCompletionItemProviderTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    private readonly MarkupTransitionCompletionItemProvider _provider = new();

    [Fact]
    public void GetCompletionItems_ReturnsEmptyCompletionItemInUnopenedMarkupContext()
    {
        // Arrange
        var razorCompletionContext = CreateRazorCompletionContext("<div>$$");

        // Act
        var completionItems = _provider.GetCompletionItems(razorCompletionContext);

        // Assert
        Assert.Empty(completionItems);
    }

    [Fact]
    public void GetCompletionItems_ReturnsEmptyCompletionItemInSimpleMarkupContext()
    {
        // Arrange
        var razorCompletionContext = CreateRazorCompletionContext("<div><$$");

        // Act
        var completionItems = _provider.GetCompletionItems(razorCompletionContext);

        // Assert
        Assert.Empty(completionItems);
    }

    [Fact]
    public void GetCompletionItems_ReturnsEmptyCompletionItemInNestedMarkupContext()
    {
        // Arrange
        var razorCompletionContext = CreateRazorCompletionContext("<div><span><p></p><p><$$ </p></span></div>");

        // Act
        var completionItems = _provider.GetCompletionItems(razorCompletionContext);

        // Assert
        Assert.Empty(completionItems);
    }

    [Fact]
    public void GetCompletionItems_ReturnsMarkupTransitionCompletionItemInCodeBlockStartingTag()
    {
        // Arrange
        var razorCompletionContext = CreateRazorCompletionContext("@{<$$");

        // Act
        var completionItems = _provider.GetCompletionItems(razorCompletionContext);

        // Assert
        Assert.Collection(completionItems, AssertRazorCompletionItem);
    }

    [Fact]
    public void GetCompletionItems_ReturnsMarkupTransitionCompletionItemInCodeBlockPartialCompletion()
    {
        // Arrange
        var razorCompletionContext = CreateRazorCompletionContext("@{<te$$");

        // Act
        var completionItems = _provider.GetCompletionItems(razorCompletionContext);

        // Assert
        Assert.Collection(completionItems, AssertRazorCompletionItem);
    }

    [Fact]
    public void GetCompletionItems_ReturnsMarkupTransitionCompletionItemInIfConditional()
    {
        // Arrange
        var razorCompletionContext = CreateRazorCompletionContext("@if (true) {<$$ }");

        // Act
        var completionItems = _provider.GetCompletionItems(razorCompletionContext);

        // Assert
        Assert.Collection(completionItems, AssertRazorCompletionItem);
    }

    [Fact]
    public void GetCompletionItems_ReturnsMarkupTransitionCompletionItemInFunctionDirective()
    {
        // Arrange
        var razorCompletionContext = CreateRazorCompletionContext("@functions {public string GetHello(){<$$ return \"pi\";}}", FunctionsDirective.Directive);

        // Act
        var completionItems = _provider.GetCompletionItems(razorCompletionContext);

        // Assert
        Assert.Collection(completionItems, AssertRazorCompletionItem);
    }

    [Fact]
    public void GetCompletionItems_ReturnsEmptyCompletionItemInExpression()
    {
        // Arrange
        var testCode = @"@{
    SomeFunctionAcceptingMethod(() =>
    {
        string foo = ""bar"";
    });
}

@SomeFunctionAcceptingMethod(() =>
{
    <$$
})";
        var razorCompletionContext = CreateRazorCompletionContext(testCode);

        // Act
        var completionItems = _provider.GetCompletionItems(razorCompletionContext);

        // Assert
        Assert.Empty(completionItems);
    }

    [Fact]
    public void GetCompletionItems_ReturnsEmptyCompletionItemInSingleLineTransitions()
    {
        // Arrange
        var testCode = @"@{
    @* @: Here's some Markup $$| <-- You shouldn't get a <text> tag completion here. *@
    @: Here's some markup <
}";
        var razorCompletionContext = CreateRazorCompletionContext(testCode);

        // Act
        var completionItems = _provider.GetCompletionItems(razorCompletionContext);

        // Assert
        Assert.Empty(completionItems);
    }

    [Fact]
    public void GetCompletionItems_ReturnsMarkupTransitionCompletionItemInNestedCSharpBlock()
    {
        // Arrange
        var testCode = @"<div>
@if (true)
{
  <$$ @* Should get text completion here *@
}
</div>";
        var razorCompletionContext = CreateRazorCompletionContext(testCode);

        // Act
        var completionItems = _provider.GetCompletionItems(razorCompletionContext);

        // Assert
        Assert.Collection(completionItems, AssertRazorCompletionItem);
    }

    [Fact]
    public void GetCompletionItems_ReturnsEmptyCompletionItemInNestedMarkupBlock()
    {
        // Arrange
        var testCode = @"@if (true)
{
<div>
  <$$ @* Shouldn't get text completion here *@
</div>
}";
        var razorCompletionContext = CreateRazorCompletionContext(testCode);

        // Act
        var completionItems = _provider.GetCompletionItems(razorCompletionContext);

        // Assert
        Assert.Empty(completionItems);
    }

    [Fact]
    public void GetCompletionItems_ReturnsMarkupTransitionCompletionItemWithUnrelatedClosingAngleBracket()
    {
        // Arrange
        var testCode = @"@functions {
    public void SomeOtherMethod()
    {
        <$$
    }

    private bool _collapseNavMenu => true;
}";
        var razorCompletionContext = CreateRazorCompletionContext(testCode, FunctionsDirective.Directive);

        // Act
        var completionItems = _provider.GetCompletionItems(razorCompletionContext);

        // Assert
        Assert.Collection(completionItems, AssertRazorCompletionItem);
    }

    [Fact]
    public void GetCompletionItems_ReturnsMarkupTransitionCompletionItemWithUnrelatedClosingTag()
    {
        // Arrange
        var razorCompletionContext = CreateRazorCompletionContext("@{<$$></>");

        // Act
        var completionItems = _provider.GetCompletionItems(razorCompletionContext);

        // Assert
        Assert.Collection(completionItems, AssertRazorCompletionItem);
    }

    [Fact]
    public void GetCompletionItems_ReturnsEmptyCompletionItemWhenOwnerIsComplexExpression()
    {
        // Arrange
        var razorCompletionContext = CreateRazorCompletionContext("@DateTime.Now<$$");

        // Act
        var completionItems = _provider.GetCompletionItems(razorCompletionContext);

        // Assert
        Assert.Empty(completionItems);
    }

    [Fact]
    public void GetCompletionItems_ReturnsEmptyCompletionItemWhenOwnerIsExplicitExpression()
    {
        // Arrange
        var razorCompletionContext = CreateRazorCompletionContext("@(something)<$$");

        // Act
        var completionItems = _provider.GetCompletionItems(razorCompletionContext);

        // Assert
        Assert.Empty(completionItems);
    }

    [Fact]
    public void GetCompletionItems_ReturnsEmptyCompletionItemWithSpaceAfterStartTag()
    {
        // Arrange
        var razorCompletionContext = CreateRazorCompletionContext("@{< $$");

        // Act
        var completionItems = _provider.GetCompletionItems(razorCompletionContext);

        // Assert
        Assert.Empty(completionItems);
    }

    [Fact]
    public void GetCompletionItems_ReturnsEmptyCompletionItemWithSpaceAfterStartTagAndAttribute()
    {
        // Arrange
        var razorCompletionContext = CreateRazorCompletionContext("@{< te$$=\"\"");

        // Act
        var completionItems = _provider.GetCompletionItems(razorCompletionContext);

        // Assert
        Assert.Empty(completionItems);
    }

    [Fact]
    public void GetCompletionItems_ReturnsEmptyCompletionItemWhenInsideAttributeArea()
    {
        // Arrange
        var razorCompletionContext = CreateRazorCompletionContext("<p <$$ >");

        // Act
        var completionItems = _provider.GetCompletionItems(razorCompletionContext);

        // Assert
        Assert.Empty(completionItems);
    }

    private static void AssertRazorCompletionItem(RazorCompletionItem item)
    {
        Assert.Equal(SyntaxConstants.TextTagName, item.DisplayText);
        Assert.Equal(SyntaxConstants.TextTagName, item.InsertText);
        var completionDescription = Assert.IsType<MarkupTransitionCompletionDescription>(item.DescriptionInfo);
        Assert.Equal(CodeAnalysisResources.MarkupTransition_Description, completionDescription.Description);
    }

    private static RazorCompletionContext CreateRazorCompletionContext(TestCode text, params DirectiveDescriptor[] directives)
    {
        var syntaxTree = CreateSyntaxTree(text, directives);
        var absoluteIndex = text.Position;
        var sourceDocument = RazorSourceDocument.Create("", RazorSourceDocumentProperties.Default);
        var codeDocument = RazorCodeDocument.Create(sourceDocument);

        var tagHelperDocumentContext = TagHelperDocumentContext.GetOrCreate(tagHelpers: []);

        var owner = syntaxTree.Root.FindInnermostNode(absoluteIndex, includeWhitespace: true, walkMarkersBack: true);
        owner = AbstractRazorCompletionFactsService.AdjustSyntaxNodeForWordBoundary(owner, absoluteIndex);
        return new RazorCompletionContext(codeDocument, absoluteIndex, owner, syntaxTree, tagHelperDocumentContext);
    }

    private static RazorSyntaxTree CreateSyntaxTree(TestCode text, params DirectiveDescriptor[] directives)
    {
        return CreateSyntaxTree(text, RazorFileKind.Legacy, directives);
    }

    private static RazorSyntaxTree CreateSyntaxTree(TestCode text, RazorFileKind fileKind, params DirectiveDescriptor[] directives)
    {
        var sourceDocument = TestRazorSourceDocument.Create(text.Text);

        var builder = new RazorParserOptions.Builder(RazorLanguageVersion.Latest, fileKind)
        {
            Directives = [.. directives]
        };

        var options = builder.ToOptions();

        var syntaxTree = RazorSyntaxTree.Parse(sourceDocument, options);
        return syntaxTree;
    }
}
