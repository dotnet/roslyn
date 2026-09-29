// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Test.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.Razor.Completion.Delegation;

public class DelegatedCompletionHelperTest(ITestOutputHelper testOutput) : ToolingTestBase(testOutput)
{
    [Fact]
    public void ShouldIncludeSnippets_InTextContentAfterCompleteEndTag_ReturnsTrue()
    {
        // The caret is at the true end of the document, immediately after a complete "</div>".
        // This is equivalent to being in (empty) text content, so snippets should be offered.
        TestCode code = "<div></div>$$";
        var codeDocument = CreateCodeDocument(code);

        var result = DelegatedCompletionHelper.ShouldIncludeSnippets(codeDocument, code.Position, out var isStartTagContext);

        Assert.True(result);
        Assert.False(isStartTagContext);
    }

    [Fact]
    public void ShouldIncludeSnippets_AfterIncompleteEndTag_ReturnsFalse()
    {
        // The caret is at the true end of the document, immediately after an *incomplete* end tag
        // (missing '>'). Unlike a complete end tag, the caret is still inside markup, not past it,
        // so snippets must not be offered.
        TestCode code = "<div></di$$";
        var codeDocument = CreateCodeDocument(code);

        var result = DelegatedCompletionHelper.ShouldIncludeSnippets(codeDocument, code.Position, out var isStartTagContext);

        Assert.False(result);
        Assert.False(isStartTagContext);
    }

    [Fact]
    public void ShouldIncludeSnippets_InTextContentAfterCompleteScriptBlock_ReturnsTrue()
    {
        // The caret is at the true end of the document, immediately after a complete "</script>".
        // The caret is past the script block (not inside it), so snippets should be offered.
        TestCode code = "<script></script>$$";
        var codeDocument = CreateCodeDocument(code);

        var result = DelegatedCompletionHelper.ShouldIncludeSnippets(codeDocument, code.Position, out var isStartTagContext);

        Assert.True(result);
        Assert.False(isStartTagContext);
    }

    [Fact]
    public void ShouldIncludeSnippets_InsideScriptBlock_ReturnsFalse()
    {
        // The caret is inside a <script> block's text content, which contains JavaScript, not
        // HTML element markup, so snippets must not be offered.
        TestCode code = """
            <script>
            $$
            </script>
            """;
        var codeDocument = CreateCodeDocument(code);

        var result = DelegatedCompletionHelper.ShouldIncludeSnippets(codeDocument, code.Position, out var isStartTagContext);

        Assert.False(result);
        Assert.False(isStartTagContext);
    }

    [Fact]
    public void ShouldIncludeSnippets_InsideStyleBlock_ReturnsFalse()
    {
        // The caret is inside a <style> block's text content, which contains CSS, not
        // HTML element markup, so snippets must not be offered.
        TestCode code = """
            <style>
            $$
            </style>
            """;
        var codeDocument = CreateCodeDocument(code);

        var result = DelegatedCompletionHelper.ShouldIncludeSnippets(codeDocument, code.Position, out var isStartTagContext);

        Assert.False(result);
        Assert.False(isStartTagContext);
    }

    [Fact]
    public void ShouldIncludeSnippets_InTextContent_ReturnsTrue()
    {
        TestCode code = "<div>ab$$cd</div>";
        var codeDocument = CreateCodeDocument(code);

        var result = DelegatedCompletionHelper.ShouldIncludeSnippets(codeDocument, code.Position, out var isStartTagContext);

        Assert.True(result);
        Assert.False(isStartTagContext);
    }

    [Fact]
    public void ShouldIncludeSnippets_InStartTagName_ReturnsTrueAndIsStartTagContext()
    {
        TestCode code = "<di$$v></div>";
        var codeDocument = CreateCodeDocument(code);

        var result = DelegatedCompletionHelper.ShouldIncludeSnippets(codeDocument, code.Position, out var isStartTagContext);

        Assert.True(result);
        Assert.True(isStartTagContext);
    }

    [Fact]
    public void ShouldIncludeSnippets_InStartTagAttributeArea_ReturnsFalse()
    {
        TestCode code = "<div $$></div>";
        var codeDocument = CreateCodeDocument(code);

        var result = DelegatedCompletionHelper.ShouldIncludeSnippets(codeDocument, code.Position, out var isStartTagContext);

        Assert.False(result);
        Assert.False(isStartTagContext);
    }

    private static RazorCodeDocument CreateCodeDocument(TestCode code)
    {
        var sourceDocument = TestRazorSourceDocument.Create(code.Text);
        var projectEngine = RazorProjectEngine.Create(builder =>
        {
            builder.ConfigureParserOptions(builder =>
            {
                builder.UseRoslynTokenizer = true;
            });
        });

        return projectEngine.Process(sourceDocument, RazorFileKind.Legacy, importSources: default, tagHelpers: []);
    }
}
