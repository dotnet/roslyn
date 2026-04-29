// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Completion;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public class CohostDocumentCompletionResolveEndpointTest(ITestOutputHelper testOutputHelper) : CohostEndpointTestBase(testOutputHelper)
{
    [Fact]
    public async Task HtmlResolve()
    {
        await VerifyCompletionItemResolveAsync(
            input: """
                This is a Razor document.

                <div st$$></div>

                The end.
                """);
    }

    [Fact]
    public async Task SnippetResolve()
    {
        await VerifyCompletionItemResolveAsync(
            input: """
                This is a Razor document.

                $$

                The end.
                """);
    }

    private async Task VerifyCompletionItemResolveAsync(TestCode input)
    {
        var document = CreateProjectAndRazorDocument(input.Text);

        var response = new VSInternalCompletionItem()
        {
            Label = "ResolvedItem"
        };
        var requestInvoker = new TestHtmlRequestInvoker([(Methods.TextDocumentCompletionResolveName, response)]);

        var completionListCache = new CompletionListCache();
        var endpoint = new CohostDocumentCompletionResolveEndpoint(
            IncompatibleProjectService,
            completionListCache,
            RemoteServiceInvoker,
            requestInvoker,
            ClientCapabilitiesService,
            new ThrowingSnippetCompletionItemResolveProvider(),
            LoggerFactory);

        var context = new DelegatedCompletionResolutionContext(
            OriginalCompletionListData: null,
            ProjectedKind: RazorLanguageKind.Html,
            ProvisionalTextEdit: null);

        var list = new RazorVSInternalCompletionList
        {
#if VSCODE
            ItemDefaults = new()
            {
                Data = JsonSerializer.SerializeToElement(context),
            },
#else
            Data = JsonSerializer.SerializeToElement(context),
#endif
            Items = [new VSInternalCompletionItem()
            {
                Label = "TestItem"
            }]
        };

        var clientCapabilities = ClientCapabilitiesService.ClientCapabilities;

        var resultId = completionListCache.Add(list, context);
        list.SetResultId(resultId, clientCapabilities);
        RazorCompletionResolveData.Wrap(list, new TextDocumentIdentifier { DocumentUri = document.CreateDocumentUri() }, clientCapabilities);

        var request = list.Items[0];
        // Simulate the LSP client, which would receive all of the items and the list data, and send the item back to us with
        // data filled in.
        request.Data = JsonSerializer.SerializeToElement(list.Data ?? list.ItemDefaults?.Data, JsonHelpers.JsonSerializerOptions);

        var tdi = endpoint.GetTestAccessor().GetRazorTextDocumentIdentifier(request);
        Assert.NotNull(tdi);
        Assert.Equal(document.CreateUri(), tdi.Value.Uri);

        var result = await endpoint.GetTestAccessor().HandleRequestAsync(request, document, DisposalToken);

        Assert.NotNull(result);
#if VSCODE
        // We don't support Html resolve in VS Code, so original item should have been returned
        Assert.Same(result, request);
        Assert.NotEqual(response.Label, result.Label);
#else
        Assert.NotSame(result, request);
        Assert.Equal(response.Label, result.Label);
#endif
    }
}
