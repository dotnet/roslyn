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
            htmlItemLabels: ["style", "dir"],
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
            htmlItemLabels: ["style"],
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
            expectedItemLabels: ["data-enhance", "data-enhance-nav", "data-permanent", "dir", "@..."],
            htmlItemLabels: ["dir"]);
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
            unexpectedItemLabels: ["data-enhance"],
            htmlItemLabels: ["dir"]);
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
            unexpectedItemLabels: ["data-enhance"],
            htmlItemLabels: ["dir"]);
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
            unexpectedItemLabels: ["data-enhance"],
            htmlItemLabels: ["dir"]);
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
            expectedItemLabels: ["style", "dir", "@..."],
            htmlItemLabels: ["style", "dir"]);
    }

    [Fact]
    public async Task HtmlSnippetsCompletion()
    {
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
            htmlItemLabels: [],
            snippetLabels: ["snippet1", "snippet2"]);
    }

    [Fact]
    public async Task HtmlSnippetsCompletion_EmptyDocument()
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
            expectedItemLabels: ["snippet1", "snippet2"],
            htmlItemLabels: [],
            snippetLabels: ["snippet1", "snippet2"]);
    }

    [Fact]
    public async Task HtmlSnippetsCompletion_WhitespaceOnlyDocument1()
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
            expectedItemLabels: ["snippet1", "snippet2"],
            htmlItemLabels: [],
            snippetLabels: ["snippet1", "snippet2"]);
    }

    [Fact]
    public async Task HtmlSnippetsCompletion_WhitespaceOnlyDocument2()
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
            expectedItemLabels: ["snippet1", "snippet2"],
            htmlItemLabels: [],
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
            htmlItemLabels: ["style", "dir"],
            snippetLabels: ["snippet1", "snippet2"]);
    }
}
