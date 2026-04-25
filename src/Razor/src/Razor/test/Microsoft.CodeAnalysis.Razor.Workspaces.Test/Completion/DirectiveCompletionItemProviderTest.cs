// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Extensions;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Test.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.Completion;

public class DirectiveCompletionItemProviderTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    private static readonly Action<RazorCompletionItem>[] s_mvcDirectiveCollectionVerifiers = GetDirectiveVerifies(DirectiveCompletionItemProvider.MvcDefaultDirectives);
    private static readonly Action<RazorCompletionItem>[] s_componentDirectiveCollectionVerifiers = GetDirectiveVerifies(DirectiveCompletionItemProvider.ComponentDefaultDirectives);

    private static Action<RazorCompletionItem>[] GetDirectiveVerifies(ImmutableArray<DirectiveDescriptor> directiveDescriptors)
    {
        using var builder = new PooledArrayBuilder<Action<RazorCompletionItem>>(directiveDescriptors.Length * 2);

        foreach (var directive in directiveDescriptors)
        {
            builder.Add(item => AssertRazorCompletionItem(directive, item, isSnippet: false));
            builder.Add(item => AssertRazorCompletionItem(directive, item, isSnippet: true));
        }

        return builder.ToArray();
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor-tooling/issues/4547")]
    public void GetDirectiveCompletionItems_ReturnsDefaultDirectivesAsCompletionItems()
    {
        // Arrange
        var syntaxTree = CreateSyntaxTree("@addTag");

        // Act
        var completionItems = DirectiveCompletionItemProvider.GetDirectiveCompletionItems(syntaxTree);

        // Assert
        Assert.Collection(
            completionItems,
            s_mvcDirectiveCollectionVerifiers
        );
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor-tooling/issues/4547")]
    public void GetDirectiveCompletionItems_ReturnsCustomDirectivesAsCompletionItems()
    {
        // Arrange
        var customDirective = DirectiveDescriptor.CreateSingleLineDirective("custom", builder => builder.Description = "My Custom Directive.");
        var syntaxTree = CreateSyntaxTree("@addTag", customDirective);

        // Act
        var completionItems = DirectiveCompletionItemProvider.GetDirectiveCompletionItems(syntaxTree);

        // Assert
        Assert.Collection(
            completionItems,
            [
                item => AssertRazorCompletionItem(customDirective, item), ..
                s_mvcDirectiveCollectionVerifiers
            ]
        );
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor-tooling/issues/4547")]
    public void GetDirectiveCompletionItems_UsesDisplayNamesWhenNotNull()
    {
        // Arrange
        var customDirective = DirectiveDescriptor.CreateSingleLineDirective("custom", builder =>
        {
            builder.DisplayName = "different";
            builder.Description = "My Custom Directive.";
        });
        var syntaxTree = CreateSyntaxTree("@addTag", customDirective);

        // Act
        var completionItems = DirectiveCompletionItemProvider.GetDirectiveCompletionItems(syntaxTree);

        // Assert
        Assert.Collection(
            completionItems,
            [
                item => AssertRazorCompletionItem("different", customDirective, item), ..
                s_mvcDirectiveCollectionVerifiers
            ]
        );
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor-tooling/issues/4547")]
    public void GetDirectiveCompletionItems_CodeBlockCommitCharacters()
    {
        // Arrange
        var customDirective = DirectiveDescriptor.CreateCodeBlockDirective("custom", builder =>
        {
            builder.DisplayName = "code";
            builder.Description = "My Custom Code Block Directive.";
        });
        var syntaxTree = CreateSyntaxTree("@cod", customDirective);

        // Act
        var completionItems = DirectiveCompletionItemProvider.GetDirectiveCompletionItems(syntaxTree);

        // Assert
        Assert.Collection(
            completionItems,
            [
                item => AssertRazorCompletionItem("code", customDirective, item, DirectiveCompletionItemProvider.BlockDirectiveCommitCharacters), ..
                s_mvcDirectiveCollectionVerifiers
            ]
        );
    }

    [Fact]
    public void GetDirectiveCompletionItems_RazorBlockCommitCharacters()
    {
        // Arrange
        var customDirective = DirectiveDescriptor.CreateRazorBlockDirective("custom", builder =>
        {
            builder.DisplayName = "section";
            builder.Description = "My Custom Razor Block Directive.";
        });
        var syntaxTree = CreateSyntaxTree("@sec", customDirective);

        // Act
        var completionItems = DirectiveCompletionItemProvider.GetDirectiveCompletionItems(syntaxTree);

        // Assert
        Assert.Collection(
            completionItems,
            [
                item => AssertRazorCompletionItem("section", customDirective, item, DirectiveCompletionItemProvider.BlockDirectiveCommitCharacters), ..
                s_mvcDirectiveCollectionVerifiers
            ]
        );
    }

    [Theory]
    [WorkItem("https://github.com/dotnet/razor-tooling/issues/4547")]
    [InlineData("attribute")]
    [InlineData("implements")]
    [InlineData("inherits")]
    [InlineData("inject")]
    [InlineData("layout")]
    [InlineData("namespace")]
    [InlineData("page")]
    [InlineData("preservewhitespace")]
    [InlineData("typeparam")]
    public void GetDirectiveCompletionItems_ReturnsKnownDirectivesAsSnippets_SingleLine_Component(string knownDirective)
    {
        // Arrange
        var usingDirective = DirectiveCompletionItemProvider.ComponentDefaultDirectives.First();
        var customDirective = DirectiveDescriptor.CreateRazorBlockDirective(knownDirective, builder =>
        {
            builder.DisplayName = knownDirective;
            builder.Description = string.Empty; // Doesn't matter for this test. Just need to provide something to avoid ArgumentNullException
        });
        var syntaxTree = CreateSyntaxTree("@", RazorFileKind.Component, customDirective);

        // Act
        var completionItems = DirectiveCompletionItemProvider.GetDirectiveCompletionItems(syntaxTree);

        // Assert
        Assert.Collection(
            completionItems,
            item => AssertRazorCompletionItem(knownDirective, customDirective, item, commitCharacters: DirectiveCompletionItemProvider.BlockDirectiveCommitCharacters, isSnippet: false),
            item => AssertRazorCompletionItem(knownDirective + " directive ...", customDirective, item, commitCharacters: DirectiveCompletionItemProvider.BlockDirectiveCommitCharacters, isSnippet: true),
            item => AssertRazorCompletionItem(usingDirective.Directive, usingDirective, item, commitCharacters: DirectiveCompletionItemProvider.SingleLineDirectiveCommitCharacters, isSnippet: false),
            item => AssertRazorCompletionItem(usingDirective.Directive + " directive ...", usingDirective, item, commitCharacters: DirectiveCompletionItemProvider.SingleLineDirectiveCommitCharacters, isSnippet: true));
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor-tooling/issues/4547")]
    public void GetDirectiveCompletionItems_ReturnsKnownDirectivesAsSnippets_SingleLine_Legacy()
    {
        // Arrange
        var customDirective = DirectiveDescriptor.CreateRazorBlockDirective("model", builder =>
        {
            builder.DisplayName = "model"; // Currently "model" is the only cshtml-only single-line directive. "add(remove)TagHelper" and "tagHelperPrefix" are there by default
            builder.Description = string.Empty; // Doesn't matter for this test. Just need to provide something to avoid ArgumentNullException
        });
        var syntaxTree = CreateSyntaxTree("@", RazorFileKind.Legacy, customDirective);

        // Act
        var completionItems = DirectiveCompletionItemProvider.GetDirectiveCompletionItems(syntaxTree);

        // Assert
        Assert.Collection(
            completionItems,
            [
                item => AssertRazorCompletionItem("model", customDirective, item, commitCharacters: DirectiveCompletionItemProvider.BlockDirectiveCommitCharacters, isSnippet: false),
                item => AssertRazorCompletionItem("model directive ...", customDirective, item, commitCharacters: DirectiveCompletionItemProvider.BlockDirectiveCommitCharacters, isSnippet: true), ..
                s_mvcDirectiveCollectionVerifiers
            ]
        );
    }

    [Fact]
    public void GetDirectiveCompletionItems_ComponentDocument_ReturnsDefaultComponentDirectivesAsCompletionItems()
    {
        // Arrange
        var syntaxTree = CreateSyntaxTree("@addTag", RazorFileKind.Component);

        // Act
        var completionItems = DirectiveCompletionItemProvider.GetDirectiveCompletionItems(syntaxTree);

        // Assert
        // Assert
        Assert.Collection(
            completionItems,
            s_componentDirectiveCollectionVerifiers
        );
    }

    [Fact]
    public void ShouldProvideCompletions_ReturnsFalseWhenOwnerIsNotExpression()
    {
        // Arrange
        var context = CreateRazorCompletionContext("@$${");

        // Act
        var result = DirectiveCompletionItemProvider.ShouldProvideCompletions(context);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldProvideCompletions_ReturnsFalseWhenOwnerIsComplexExpression()
    {
        // Arrange
        var context = CreateRazorCompletionContext("@D$$ateTime.Now");

        // Act
        var result = DirectiveCompletionItemProvider.ShouldProvideCompletions(context);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldProvideCompletions_ReturnsFalseWhenOwnerIsExplicitExpression()
    {
        // Arrange
        var context = CreateRazorCompletionContext("@(so$$mething)");

        // Act
        var result = DirectiveCompletionItemProvider.ShouldProvideCompletions(context);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldProvideCompletions_ReturnsFalseWhenInsideStatement()
    {
        // Arrange
        var context = CreateRazorCompletionContext("@{ @$$ }");

        // Act
        var result = DirectiveCompletionItemProvider.ShouldProvideCompletions(context);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldProvideCompletions_ReturnsFalseWhenInsideMarkup()
    {
        // Arrange
        var context = CreateRazorCompletionContext("<p>@$$ </p>");

        // Act
        var result = DirectiveCompletionItemProvider.ShouldProvideCompletions(context);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldProvideCompletions_ReturnsFalseWhenInsideAttributeArea()
    {
        // Arrange
        var context = CreateRazorCompletionContext("<p @$$ >");

        // Act
        var result = DirectiveCompletionItemProvider.ShouldProvideCompletions(context);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldProvideCompletions_ReturnsFalseWhenInsideDirective()
    {
        // Arrange
        var context = CreateRazorCompletionContext("@functions { @$$  }", CompletionReason.Invoked, FunctionsDirective.Directive);

        // Act
        var result = DirectiveCompletionItemProvider.ShouldProvideCompletions(context);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldProvideCompletions_ReturnsTrueForSimpleImplicitExpressionsStartOfWord()
    {
        // Arrange
        var context = CreateRazorCompletionContext("@$$m");

        // Act
        var result = DirectiveCompletionItemProvider.ShouldProvideCompletions(context);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldProvideCompletions_ReturnsFalseForSimpleImplicitExpressions_WhenNotInvoked()
    {
        // Arrange
        var context = CreateRazorCompletionContext("@m$$od", CompletionReason.Typing);

        // Act
        var result = DirectiveCompletionItemProvider.ShouldProvideCompletions(context);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldProvideCompletions_ReturnsTrueForSimpleImplicitExpressions_WhenInvoked()
    {
        // Arrange
        var context = CreateRazorCompletionContext("@m$$od");

        // Act
        var result = DirectiveCompletionItemProvider.ShouldProvideCompletions(context);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsDirectiveCompletableToken_ReturnsTrueForCSharpKeywords()
    {
        // If you're typing `@inject` and stop at `@in` it will be parsed as a C# Keyword instead of an identifier, so we have to allow them too
        // Arrange
        var csharpToken = SyntaxFactory.Token(SyntaxKind.Keyword, "in");

        // Act
        var result = DirectiveCompletionItemProvider.IsDirectiveCompletableToken(csharpToken);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsDirectiveCompletableToken_ReturnsTrueForCSharpIdentifiers()
    {
        // Arrange
        var csharpToken = SyntaxFactory.Token(SyntaxKind.Identifier, "model");

        // Act
        var result = DirectiveCompletionItemProvider.IsDirectiveCompletableToken(csharpToken);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsDirectiveCompletableToken_ReturnsTrueForCSharpMarkerTokens()
    {
        // Arrange
        var csharpToken = SyntaxFactory.Token(SyntaxKind.Marker, string.Empty);

        // Act
        var result = DirectiveCompletionItemProvider.IsDirectiveCompletableToken(csharpToken);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsDirectiveCompletableToken_ReturnsFalseForNonCSharpTokens()
    {
        // Arrange
        var token = SyntaxFactory.Token(SyntaxKind.Text, string.Empty);

        // Act
        var result = DirectiveCompletionItemProvider.IsDirectiveCompletableToken(token);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsDirectiveCompletableToken_ReturnsFalseForInvalidCSharpTokens()
    {
        // Arrange
        var csharpToken = SyntaxFactory.Token(SyntaxKind.Tilde, "~");

        // Act
        var result = DirectiveCompletionItemProvider.IsDirectiveCompletableToken(csharpToken);

        // Assert
        Assert.False(result);
    }

    private static RazorCompletionContext CreateRazorCompletionContext(TestCode text, CompletionReason reason = CompletionReason.Invoked, params DirectiveDescriptor[] directives)
    {
        var syntaxTree = CreateSyntaxTree(text, directives);
        var absoluteIndex = text.Position;
        var sourceDocument = RazorSourceDocument.Create("", RazorSourceDocumentProperties.Default);
        var codeDocument = RazorCodeDocument.Create(sourceDocument);

        var tagHelperDocumentContext = TagHelperDocumentContext.GetOrCreate(tagHelpers: []);
        var owner = syntaxTree.Root.FindInnermostNode(absoluteIndex);
        owner = AbstractRazorCompletionFactsService.AdjustSyntaxNodeForWordBoundary(owner, absoluteIndex);
        return new RazorCompletionContext(codeDocument, absoluteIndex, owner, syntaxTree, tagHelperDocumentContext, reason);
    }

    private static void AssertRazorCompletionItem(string completionDisplayText, DirectiveDescriptor directive, RazorCompletionItem item, ImmutableArray<RazorCommitCharacter> commitCharacters = default, bool isSnippet = false)
    {
        Assert.Equal(item.DisplayText, completionDisplayText);
        var completionDescription = Assert.IsType<DirectiveCompletionDescription>(item.DescriptionInfo);

        if (isSnippet)
        {
            var (insertText, displayText) = DirectiveCompletionItemProvider.SingleLineDirectiveSnippets[directive.Directive];

            Assert.StartsWith(directive.Directive, item.InsertText);
            Assert.Equal(item.InsertText, insertText);
            Assert.StartsWith(displayText, completionDescription.Description.TrimStart('@'));
        }
        else
        {
            Assert.Equal(item.InsertText, directive.Directive);
            Assert.Equal(directive.Description, completionDescription.Description);
        }

        Assert.Equal(item.CommitCharacters, commitCharacters.IsDefault ? DirectiveCompletionItemProvider.SingleLineDirectiveCommitCharacters : commitCharacters);
    }

    private static void AssertRazorCompletionItem(DirectiveDescriptor directive, RazorCompletionItem item, bool isSnippet = false) =>
        AssertRazorCompletionItem(directive.Directive + (isSnippet ? " directive ..." : string.Empty), directive, item, isSnippet: isSnippet);

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
