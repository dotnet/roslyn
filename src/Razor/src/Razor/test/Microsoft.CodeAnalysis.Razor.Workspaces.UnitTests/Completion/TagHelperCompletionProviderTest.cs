// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.Syntax;
using Microsoft.AspNetCore.Razor.Test.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.Completion;

public class TagHelperCompletionProviderTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    [Fact]
    public void GetCompletionItems_ParentTagConstraint_AttributeCompletedWhenParentMatches()
    {
        // Arrange - use SimpleTagHelpers which includes a "SomeChild" tag helper
        // that requires ParentTag = "test1" and has a bound attribute "attribute".
        // Legacy file kind requires @addTagHelper directive for tag helper resolution.
        var tagHelpers = SimpleTagHelpers.Default;

        var testCode = new TestCode("@addTagHelper *, TestAssembly\n<test1><SomeChild $$></SomeChild></test1>");
        var codeDocument = CreateCodeDocument(testCode.Text, tagHelpers);
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
            CompletionReason.Invoked,
            new RazorCompletionOptions(SnippetsSupported: true, AutoInsertAttributeQuotes: true, CommitElementsWithSpace: true, UseVsCodeCompletionCommitCharacters: false));

        var provider = new TagHelperCompletionProvider(new TagHelperCompletionService());

        // Act
        var completions = provider.GetCompletionItems(context);

        // Assert - The "attribute" bound attribute should appear in completions because
        // the SomeChild tag is inside test1, satisfying the ParentTag constraint
        var childAttrCompletion = completions.FirstOrDefault(c => c.DisplayText == "attribute");
        Assert.NotNull(childAttrCompletion);
    }

    [Fact]
    public void GetCompletionItems_ParentTagConstraint_AttributeNotCompletedWhenParentDoesNotMatch()
    {
        // Arrange - SomeChild requires ParentTag="test1", but we put it inside "div" instead
        var tagHelpers = SimpleTagHelpers.Default;

        var testCode = new TestCode("@addTagHelper *, TestAssembly\n<div><SomeChild $$></SomeChild></div>");
        var codeDocument = CreateCodeDocument(testCode.Text, tagHelpers);
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
            CompletionReason.Invoked,
            new RazorCompletionOptions(SnippetsSupported: true, AutoInsertAttributeQuotes: true, CommitElementsWithSpace: true, UseVsCodeCompletionCommitCharacters: false));

        var provider = new TagHelperCompletionProvider(new TagHelperCompletionService());

        // Act
        var completions = provider.GetCompletionItems(context);

        // Assert - The "attribute" should NOT appear because the parent is div, not test1
        var childAttrCompletion = completions.FirstOrDefault(c => c.DisplayText == "attribute");
        Assert.Null(childAttrCompletion);
    }

    private static RazorCodeDocument CreateCodeDocument(string text, TagHelperCollection tagHelpers)
    {
        var sourceDocument = TestRazorSourceDocument.Create(text);
        var projectEngine = RazorProjectEngine.Create(builder =>
        {
            builder.ConfigureParserOptions(builder =>
            {
                builder.UseRoslynTokenizer = true;
            });
        });

        return projectEngine.Process(sourceDocument, RazorFileKind.Legacy, importSources: default, tagHelpers);
    }
}
