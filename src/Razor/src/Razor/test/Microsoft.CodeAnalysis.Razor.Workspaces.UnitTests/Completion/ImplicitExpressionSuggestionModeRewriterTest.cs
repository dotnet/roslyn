// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.Completion.Delegation;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.Completion;

public class ImplicitExpressionSuggestionModeRewriterTest
{
    private readonly ImplicitExpressionSuggestionModeRewriter _rewriter = new();

    [Fact]
    public void TopLevel_SimpleIdentifier_ClearsSuggestionMode()
    {
        // @h$$
        Assert.False(GetRewrittenSuggestionMode("<div>@h$$</div>"));
    }

    [Fact]
    public void TopLevel_BalancedParens_ClearsSuggestionMode()
    {
        // @items.Where(x => x.Name).First()$$
        Assert.False(GetRewrittenSuggestionMode("<div>@items.Where(x => x.Name).First()$$</div>"));
    }

    [Fact]
    public void TopLevel_Brackets_ClearsSuggestionMode()
    {
        // Brackets are not tracked — only parentheses affect nesting depth
        // @items[0].N$$
        Assert.False(GetRewrittenSuggestionMode("<div>@items[0].N$$</div>"));
    }

    [Fact]
    public void Nested_InsideParens_PreservesSuggestionMode()
    {
        // @items.Where(x$$)
        Assert.True(GetRewrittenSuggestionMode("<div>@items.Where(x$$)</div>"));
    }

    [Fact]
    public void Nested_InsideNestedParens_PreservesSuggestionMode()
    {
        // @items.Where(x => x.Select(y$$))
        Assert.True(GetRewrittenSuggestionMode("<div>@items.Where(x => x.Select(y$$))</div>"));
    }

    [Fact]
    public void NotImplicitExpression_CodeBlock_PreservesSuggestionMode()
    {
        // @{ h$$ } — code block, not implicit expression; rewriter should not touch it
        Assert.True(GetRewrittenSuggestionMode("<div>@{ h$$ }</div>"));
    }

    [Fact]
    public void SuggestionModeAlreadyFalse_RemainsUnchanged()
    {
        // When SuggestionMode is already false, the rewriter should leave it alone
        var (codeDocument, cursorIndex) = Parse("<div>@h$$</div>");

        var completionList = CreateCompletionList(suggestionMode: false);
        var result = _rewriter.Rewrite(completionList, codeDocument, cursorIndex, new Position(), new RazorCompletionOptions());

        Assert.False(result.SuggestionMode);
    }

    /// <summary>
    /// Parses the input, invokes the rewriter with SuggestionMode=true, and returns the resulting SuggestionMode value.
    /// </summary>
    private bool GetRewrittenSuggestionMode(string textWithMarker)
    {
        var (codeDocument, cursorIndex) = Parse(textWithMarker);

        var completionList = CreateCompletionList(suggestionMode: true);
        var result = _rewriter.Rewrite(completionList, codeDocument, cursorIndex, new Position(), new RazorCompletionOptions());

        return result.SuggestionMode;
    }

    private static (RazorCodeDocument CodeDocument, int CursorIndex) Parse(string textWithMarker)
    {
        var cursorIndex = textWithMarker.IndexOf("$$");
        Assert.True(cursorIndex >= 0, "Test input must contain $$ cursor marker");

        var text = textWithMarker.Remove(cursorIndex, 2);

        var sourceDocument = TestRazorSourceDocument.Create(text);
        var options = new RazorParserOptions.Builder(RazorLanguageVersion.Latest, RazorFileKind.Component).ToOptions();
        var syntaxTree = RazorSyntaxTree.Parse(sourceDocument, options);

        var codeDocument = RazorCodeDocument.Create(sourceDocument);
        codeDocument = codeDocument.WithTagHelperRewrittenSyntaxTree(syntaxTree);

        return (codeDocument, cursorIndex);
    }

    private static RazorVSInternalCompletionList CreateCompletionList(bool suggestionMode)
    {
        return new RazorVSInternalCompletionList()
        {
            Items = [new VSInternalCompletionItem() { Label = "test" }],
            SuggestionMode = suggestionMode
        };
    }
}
