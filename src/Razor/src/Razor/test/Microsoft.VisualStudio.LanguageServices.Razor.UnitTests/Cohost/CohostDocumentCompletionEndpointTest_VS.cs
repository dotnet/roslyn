// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public partial class CohostDocumentCompletionEndpointTest
{
    [Fact]
    public async Task HtmlAttributeNamesAndTagHelpersCompletion_TriggerWithSpace()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                <EditForm $$></EditForm>

                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerCharacter = " ",
                TriggerKind = CompletionTriggerKind.TriggerCharacter
            },
            expectedItemLabels: ["style", "dir", "FormName", "OnValidSubmit", "@..."],
            itemToResolve: "FormName",
            expectedResolvedItemDescription: "string Microsoft.AspNetCore.Components.Forms.EditForm.FormName");
    }

    [Fact]
    public async Task TagHelperAttributes_NoAutoInsertQuotes_Completion_TriggerWithSpace()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                <EditForm $$></EditForm>

                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerCharacter = " ",
                TriggerKind = CompletionTriggerKind.TriggerCharacter
            },
            expectedItemLabels: ["FormName", "OnValidSubmit", "@...", "style"],
            autoInsertAttributeQuotes: false);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/9378")]
    public async Task BlazorDataEnhanceAttributeCompletion_OnFormElement_TriggerWithSpace()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                <form $$></form>

                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerCharacter = " ",
                TriggerKind = CompletionTriggerKind.TriggerCharacter
            },
            expectedItemLabels: ["data-enhance", "data-enhance-nav", "data-permanent", "dir", "@..."]);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/9378")]
    public async Task BlazorDataEnhanceNavAttributeCompletion_OnAnyElement_TriggerWithSpace()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                <div $$></div>

                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerCharacter = " ",
                TriggerKind = CompletionTriggerKind.TriggerCharacter
            },
            expectedItemLabels: ["data-enhance-nav", "data-permanent", "dir", "@..."],
            unexpectedItemLabels: ["data-enhance"]);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/9378")]
    public async Task BlazorDataPermanentAttributeCompletion_OnAnchorElement_TriggerWithSpace()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                <a $$></a>

                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerCharacter = " ",
                TriggerKind = CompletionTriggerKind.TriggerCharacter
            },
            expectedItemLabels: ["data-enhance-nav", "data-permanent", "dir", "@..."],
            unexpectedItemLabels: ["data-enhance"]);
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/9378")]
    public async Task BlazorDataAttributeCompletion_DoesNotDuplicateExistingAttribute_TriggerWithSpace()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                <form data-enhance $$></form>

                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerCharacter = " ",
                TriggerKind = CompletionTriggerKind.TriggerCharacter
            },
            expectedItemLabels: ["data-enhance-nav", "data-permanent", "dir", "@..."],
            unexpectedItemLabels: ["data-enhance"]);
    }

    // Tests HTML attributes and DirectiveAttributeTransitionCompletionItemProvider
    [Fact]
    public async Task HtmlAndDirectiveAttributeTransitionNamesCompletion_TriggerWithSpace()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                <div $$></div>

                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerCharacter = " ",
                TriggerKind = CompletionTriggerKind.TriggerCharacter
            },
            expectedItemLabels: ["style", "dir", "@..."]);
    }

    [Fact]
    public async Task HtmlSnippetsCompletion_NotInTextContent()
    {
        // Typing 'a' in plain text content does not trigger completion at all.
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                $$

                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerCharacter = "a",
                TriggerKind = CompletionTriggerKind.TriggerCharacter
            },
            expectedItemLabels: null,
            snippetLabels: ["snippet1", "snippet2"]);
    }

    [Fact]
    public async Task HtmlSnippetsCompletion_InEmptyDocument_OnExplicitInvocation()
    {
        // Empty document with explicit invocation — snippets should appear.
        await VerifyCompletionListAsync(
            input: """
                $$
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Explicit,
                TriggerCharacter = null,
                TriggerKind = CompletionTriggerKind.Invoked
            },
            expectedItemLabels: ["snippet1", "snippet2"],
            snippetLabels: ["snippet1", "snippet2"]);
    }

    [Fact]
    public async Task HtmlSnippetsCompletion_NotInWhitespaceOnlyDocument1()
    {
        // Typing 'a' in whitespace-only document does not trigger completion at all.
        await VerifyCompletionListAsync(
            input: """

                $$
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerCharacter = "a",
                TriggerKind = CompletionTriggerKind.TriggerCharacter
            },
            expectedItemLabels: null,
            snippetLabels: ["snippet1", "snippet2"]);
    }

    [Fact]
    public async Task HtmlSnippetsCompletion_NotInWhitespaceOnlyDocument2()
    {
        // Typing 'a' in whitespace-only document does not trigger completion at all.
        await VerifyCompletionListAsync(
            input: """
                $$

                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerCharacter = "a",
                TriggerKind = CompletionTriggerKind.TriggerCharacter
            },
            expectedItemLabels: null,
            snippetLabels: ["snippet1", "snippet2"]);
    }

    [Fact]
    public async Task HtmlSnippetsCompletion_InTextContent_OnExplicitInvocation()
    {
        // Explicit invocation (Ctrl+Space) in text content should show snippets.
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                $$

                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Explicit,
                TriggerCharacter = null,
                TriggerKind = CompletionTriggerKind.Invoked
            },
            expectedItemLabels: ["snippet1", "snippet2"],
            snippetLabels: ["snippet1", "snippet2"]);
    }

    [Fact]
    public async Task HtmlSnippetsCompletion_InWhitespaceDocument_OnExplicitInvocation()
    {
        // Explicit invocation (Ctrl+Space) in whitespace-only document should show snippets.
        await VerifyCompletionListAsync(
            input: """

                $$
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Explicit,
                TriggerCharacter = null,
                TriggerKind = CompletionTriggerKind.Invoked
            },
            expectedItemLabels: ["snippet1", "snippet2"],
            snippetLabels: ["snippet1", "snippet2"]);
    }

    [Fact]
    public async Task HtmlSnippetsCompletion_NotInAttributeValue()
    {
        // Snippets should not appear in attribute values, even on explicit invocation.
        // HTML delegation still happens (attribute value completions come from the HTML server),
        // so we supply htmlItemLabels to avoid the _INVALID_ sentinel.
        await VerifyCompletionListAsync(
            input: """
                <div class="$$"></div>
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Explicit,
                TriggerCharacter = null,
                TriggerKind = CompletionTriggerKind.Invoked
            },
            htmlItemLabels: ["bold"],
            expectedItemLabels: ["bold"],
            unexpectedItemLabels: ["snippet1", "snippet2"],
            snippetLabels: ["snippet1", "snippet2"]);
    }

    [Fact]
    public async Task HtmlSnippetsCompletion_NotInScriptBlock()
    {
        // Snippets should not appear inside <script> blocks, even on explicit invocation,
        // because the content is JavaScript, not HTML element markup.
        // htmlItemLabels must be supplied because the local provider returns null for script
        // content, causing delegation to the (mock) external HTML server.
        await VerifyCompletionListAsync(
            input: """
                <script>
                $$
                </script>
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Explicit,
                TriggerCharacter = null,
                TriggerKind = CompletionTriggerKind.Invoked
            },
            htmlItemLabels: ["js-completion"],
            expectedItemLabels: ["js-completion"],
            unexpectedItemLabels: ["snippet1", "snippet2"],
            snippetLabels: ["snippet1", "snippet2"]);
    }

    [Fact]
    public async Task HtmlSnippetsCompletion_NotInStyleBlock()
    {
        // Snippets should not appear inside <style> blocks, even on explicit invocation,
        // because the content is CSS, not HTML element markup.
        // htmlItemLabels must be supplied because the local provider returns null for style
        // content, causing delegation to the (mock) external HTML server.
        await VerifyCompletionListAsync(
            input: """
                <style>
                $$
                </style>
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Explicit,
                TriggerCharacter = null,
                TriggerKind = CompletionTriggerKind.Invoked
            },
            htmlItemLabels: ["css-completion"],
            expectedItemLabels: ["css-completion"],
            unexpectedItemLabels: ["snippet1", "snippet2"],
            snippetLabels: ["snippet1", "snippet2"]);
    }

    [Fact]
    public async Task HtmlSnippetsCompletion_NotInStartTag()
    {
        await VerifyCompletionListAsync(
            input: """
                This is a Razor document.

                <div $$></div>

                The end.
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerCharacter = " ",
                TriggerKind = CompletionTriggerKind.TriggerCharacter
            },
            expectedItemLabels: ["style", "dir"],
            unexpectedItemLabels: ["snippet1", "snippet2"],
            snippetLabels: ["snippet1", "snippet2"]);
    }

    [Fact]
    public async Task HtmlSnippetsCompletion_NotInEndTag()
    {
        await VerifyCompletionListAsync(
            input: """
                <div></$$
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerCharacter = "/",
                TriggerKind = CompletionTriggerKind.TriggerCharacter
            },
            expectedItemLabels: ["/div>"],
            unexpectedItemLabels: ["snippet1", "snippet2"],
            snippetLabels: ["snippet1", "snippet2"]);
    }

    [Fact]
    public async Task HtmlSnippetsCompletion_NotInEndTag_FollowedByContent()
    {
        await VerifyCompletionListAsync(
            input: """
                <div></$$</div>
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerCharacter = "/",
                TriggerKind = CompletionTriggerKind.TriggerCharacter
            },
            expectedItemLabels: [],
            unexpectedItemLabels: ["</div>", "snippet1", "snippet2"],
            snippetLabels: ["snippet1", "snippet2"]);
    }

    [Fact]
    public async Task TagHelperElementCompletion_NotInEndTag()
    {
        await VerifyCompletionListAsync(
            input: """
                <div></d$$iv>
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Explicit,
                TriggerKind = CompletionTriggerKind.Invoked
            },
            expectedItemLabels: [],
            unexpectedItemLabels: ["EditForm"],
            htmlItemLabels: [],
            snippetLabels: []);
    }

    [Fact]
    public async Task HtmlSnippetsCompletion_InTextContentAfterEndTag_OnExplicitInvocation()
    {
        // Cursor is in text content after a complete end tag — snippets appear on explicit invocation.
        await VerifyCompletionListAsync(
            input: """
                <div></div>$$
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Explicit,
                TriggerKind = CompletionTriggerKind.Invoked
            },
            expectedItemLabels: ["snippet1", "snippet2"],
            snippetLabels: ["snippet1", "snippet2"]);
    }

    [Fact]
    public async Task HtmlSnippetsCompletion_ContextFiltering_OnlyValidChildren()
    {
        // Inside <ul>, only snippets whose root element is a valid child should appear.
        // "ul" produces a <ul> element (valid inside <ul>? No — but "li" is).
        // We test with "li" (valid child of <ul>) and "div" (not valid).
        var result = await VerifyCompletionListAsync(
            input: """
                <ul>
                <$$
                </ul>
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerCharacter = "<",
                TriggerKind = CompletionTriggerKind.TriggerCharacter
            },
            expectedItemLabels: ["li"],
            unexpectedItemLabels: ["div"],
            snippetLabels: ["li", "div", "table"]);

        Assert.NotNull(result);

        // "li" snippet should be present (valid child of <ul>)
        Assert.Contains(result.Items, static item => item.Label == "li" && item.Kind == CompletionItemKind.Snippet);
        // "div" and "table" snippets should be filtered out (not valid children of <ul>)
        Assert.DoesNotContain(result.Items, static item => item.Label == "div" && item.Kind == CompletionItemKind.Snippet);
        Assert.DoesNotContain(result.Items, static item => item.Label == "table" && item.Kind == CompletionItemKind.Snippet);
    }

    [Fact]
    public async Task HtmlSnippetsCompletion_RazorExcludedSnippets_NotShown()
    {
        // "scriptr" and "scriptr2" are ASP.NET Web Forms snippets excluded in Razor files.
        await VerifyCompletionListAsync(
            input: """
                <$$
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerCharacter = "<",
                TriggerKind = CompletionTriggerKind.TriggerCharacter
            },
            expectedItemLabels: ["div"],
            unexpectedItemLabels: ["scriptr", "scriptr2"],
            snippetLabels: ["div", "scriptr", "scriptr2"]);
    }

    [Fact]
    public async Task HtmlSnippetsCompletion_SortText_SnippetsSortAfterElements()
    {
        // Snippets should have SortText = shortcut + " " so they sort after
        // element completions with the same label.
        var result = await VerifyCompletionListAsync(
            input: """
                <$$
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Typing,
                TriggerCharacter = "<",
                TriggerKind = CompletionTriggerKind.TriggerCharacter
            },
            expectedItemLabels: ["div"],
            snippetLabels: ["div"]);

        Assert.NotNull(result);
        var snippetItem = Assert.Single(result.Items, static item => item.Label == "div" && item.Kind == CompletionItemKind.Snippet);
        // Trailing space causes snippet to sort after the element with the same name
        Assert.Equal("div ", snippetItem.SortText);
    }
}
