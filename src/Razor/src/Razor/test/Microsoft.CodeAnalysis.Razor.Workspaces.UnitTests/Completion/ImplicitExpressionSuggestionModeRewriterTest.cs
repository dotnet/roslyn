// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis.Razor.Completion.Delegation;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.Completion;

public class ImplicitExpressionSuggestionModeRewriterTest
{
    private readonly ImplicitExpressionSuggestionModeRewriter _rewriter = new();

    [Theory]
    [InlineData("<div>@h$$</div>")]                                        // Simple identifier
    [InlineData("<div>@items.Where(x => x.Name).First()$$</div>")]         // Balanced parens after expression
    [InlineData("<div>@items[0].N$$</div>")]                               // Brackets don't affect paren depth
    public void TopLevel_ClearsSuggestionMode(string markup)
    {
        Assert.False(GetRewrittenSuggestionMode(markup));
    }

    [Theory]
    [InlineData("<div>@items.Where(x$$)</div>")]                           // Inside parens
    [InlineData("<div>@items.Where(x => x.Select(y$$))</div>")]            // Inside nested parens
    [InlineData("<div>@{ h$$ }</div>")]                                    // Code block, not implicit expression
    public void Nested_PreservesSuggestionMode(string markup)
    {
        Assert.True(GetRewrittenSuggestionMode(markup));
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

    [Theory]
    [InlineData("<div>@$$</div>", RazorFileKind.Component)]      // Component file
    [InlineData("<div>@$$</div>", RazorFileKind.Legacy)]          // CSHTML file
    [InlineData("@$$", RazorFileKind.Component)]                  // Bare @ at start
    public void AtTransition_SetsIsIncompleteAndClearsCommitChars(string markup, RazorFileKind fileKind)
    {
        // When cursor is immediately after @ with no identifier typed, commit characters must be
        // cleared to prevent '{' from committing a completion item when the user intends @{ (code block).
        // IsIncomplete must also be set so the client re-queries on the next keystroke, allowing
        // normal commit characters to be restored once an identifier character is typed.
        var (codeDocument, cursorIndex) = Parse(markup, fileKind);

        var completionList = CreateCompletionList(suggestionMode: false);
        completionList.CommitCharacters = new string[] { "{", "(" };
        completionList.ItemDefaults = new() { CommitCharacters = new string[] { "{", "." } };
        completionList.Items[0].CommitCharacters = new string[] { "{" };
        completionList.Items[0].VsCommitCharacters = new VSInternalCommitCharacter[] { new() { Character = "{" } };

        var result = _rewriter.Rewrite(completionList, codeDocument, cursorIndex, new Position(), new RazorCompletionOptions());

        Assert.True(result.IsIncomplete);
        Assert.False(result.SuggestionMode); // SuggestionMode is not modified
        Assert.Null(result.CommitCharacters);
        Assert.Null(result.ItemDefaults!.CommitCharacters);
        Assert.Null(result.Items[0].CommitCharacters);
        Assert.Null(result.Items[0].VsCommitCharacters);
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

    private static (RazorCodeDocument CodeDocument, int CursorIndex) Parse(string textWithMarker, RazorFileKind fileKind = RazorFileKind.Component)
    {
        var cursorIndex = textWithMarker.IndexOf("$$");
        Assert.True(cursorIndex >= 0, "Test input must contain $$ cursor marker");

        var text = textWithMarker.Remove(cursorIndex, 2);

        var sourceDocument = TestRazorSourceDocument.Create(text);
        var options = new RazorParserOptions.Builder(RazorLanguageVersion.Latest, fileKind).ToOptions();
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
