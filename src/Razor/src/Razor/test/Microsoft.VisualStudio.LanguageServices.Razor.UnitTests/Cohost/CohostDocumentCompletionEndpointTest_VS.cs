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
        // Snippets should not appear in plain text content — they're only relevant
        // when the user is actively writing a tag name (after typing '<').
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
            expectedItemLabels: [],
            unexpectedItemLabels: ["snippet1", "snippet2"],
            snippetLabels: ["snippet1", "snippet2"]);
    }

    [Fact]
    public async Task HtmlSnippetsCompletion_NotInEmptyDocument()
    {
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
            expectedItemLabels: [],
            unexpectedItemLabels: ["snippet1", "snippet2"],
            snippetLabels: ["snippet1", "snippet2"]);
    }

    [Fact]
    public async Task HtmlSnippetsCompletion_NotInWhitespaceOnlyDocument1()
    {
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
            expectedItemLabels: [],
            unexpectedItemLabels: ["snippet1", "snippet2"],
            snippetLabels: ["snippet1", "snippet2"]);
    }

    [Fact]
    public async Task HtmlSnippetsCompletion_NotInWhitespaceOnlyDocument2()
    {
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
            expectedItemLabels: [],
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
    public async Task HtmlSnippetsCompletion_NotAfterCompleteEndTag()
    {
        // Cursor is in text content after a complete end tag — no snippets here.
        await VerifyCompletionListAsync(
            input: """
                <div></div>$$
                """,
            completionContext: new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Explicit,
                TriggerKind = CompletionTriggerKind.Invoked
            },
            expectedItemLabels: [],
            unexpectedItemLabels: ["snippet1", "snippet2"],
            snippetLabels: ["snippet1", "snippet2"]);
    }
}
