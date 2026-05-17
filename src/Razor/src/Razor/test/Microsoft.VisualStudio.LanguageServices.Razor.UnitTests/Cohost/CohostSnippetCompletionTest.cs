// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.LanguageServer;
using Microsoft.CodeAnalysis.Razor.Completion;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Telemetry;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Razor.Snippets;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public class CohostSnippetCompletionTest(ITestOutputHelper testOutputHelper) : CohostEndpointTestBase(testOutputHelper)
{
    [Fact]
    public async Task SnippetResolve()
    {
        TestCode input = """
                This is a Razor document.

                <$$div></div>

                The end.
                """;

        var document = CreateProjectAndRazorDocument(input.Text);
        var sourceText = await document.GetTextAsync(DisposalToken);

        const string InvalidLabel = "_INVALID_";

        var response = new RazorVSInternalCompletionList()
        {
            Items = [new VSInternalCompletionItem() { Label = InvalidLabel }],
            IsIncomplete = true
        };

        var requestInvoker = new TestHtmlRequestInvoker([(Methods.TextDocumentCompletionName, response)]);

        var snippetCompletionItemProvider = new TestSnippetCompletionItemProvider();

        var completionListCache = new CompletionListCache();
        var completionEndpoint = new CohostDocumentCompletionEndpoint(
            IncompatibleProjectService,
            RemoteServiceInvoker,
            ClientSettingsManager,
            ClientCapabilitiesService,
            snippetCompletionItemProvider,
            requestInvoker,
            completionListCache,
            NoOpTelemetryReporter.Instance,
            LoggerFactory);

        var request = new RazorVSInternalCompletionParams()
        {
            TextDocument = new TextDocumentIdentifier()
            {
                DocumentUri = document.GetURI()
            },
            Position = sourceText.GetPosition(input.Position),
            Context = new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Explicit,
                TriggerKind = CompletionTriggerKind.Invoked
            }
        };

        var result = await completionEndpoint.GetTestAccessor().HandleRequestAsync(request, document, DisposalToken);

        Assert.NotNull(result);

        var resolveEndpoint = new CohostDocumentCompletionResolveEndpoint(
            IncompatibleProjectService,
            completionListCache,
            RemoteServiceInvoker,
            requestInvoker,
            ClientCapabilitiesService,
            snippetCompletionItemProvider,
            LoggerFactory);

        // We clone the item first though, to ensure us setting the data doesn't hide a bug in our caching logic, around wrapping" the data.
        var itemToResolve = result.Items.First();
        itemToResolve = JsonSerializer.Deserialize<VSInternalCompletionItem>(JsonSerializer.SerializeToElement(itemToResolve, JsonHelpers.JsonSerializerOptions), JsonHelpers.JsonSerializerOptions)!;

        var tdi = resolveEndpoint.GetTestAccessor().GetRazorTextDocumentIdentifier(itemToResolve);
        Assert.NotNull(tdi);
        Assert.Equal(document.GetURI(), tdi.DocumentUri);

        var resolvedItem = await resolveEndpoint.GetTestAccessor().HandleRequestAsync(itemToResolve, document, DisposalToken);

        Assert.NotNull(resolvedItem);
        Assert.Equal(itemToResolve.Label, resolvedItem.Label);
        Assert.Equal("ResolvedSnippetItem", resolvedItem.InsertText);
    }

    [Fact]
    public async Task SnippetResolve_InStartTag_StripsLeadingOpenAngleBracket()
    {
        // When a snippet is resolved in start-tag context (after '<'), the leading '<'
        // from the snippet body should be stripped to avoid duplication.
        TestCode input = """
                This is a Razor document.

                <$$div></div>

                The end.
                """;

        var document = CreateProjectAndRazorDocument(input.Text);
        var sourceText = await document.GetTextAsync(DisposalToken);

        const string InvalidLabel = "_INVALID_";

        var response = new RazorVSInternalCompletionList()
        {
            Items = [new VSInternalCompletionItem() { Label = InvalidLabel }],
            IsIncomplete = true
        };

        var requestInvoker = new TestHtmlRequestInvoker([(Methods.TextDocumentCompletionName, response)]);

        var snippetCompletionItemProvider = new StartTagSnippetProvider();

        var completionListCache = new CompletionListCache();
        var completionEndpoint = new CohostDocumentCompletionEndpoint(
            IncompatibleProjectService,
            RemoteServiceInvoker,
            ClientSettingsManager,
            ClientCapabilitiesService,
            snippetCompletionItemProvider,
            requestInvoker,
            completionListCache,
            NoOpTelemetryReporter.Instance,
            LoggerFactory);

        var request = new RazorVSInternalCompletionParams()
        {
            TextDocument = new TextDocumentIdentifier()
            {
                DocumentUri = document.GetURI()
            },
            Position = sourceText.GetPosition(input.Position),
            Context = new VSInternalCompletionContext()
            {
                InvokeKind = VSInternalCompletionInvokeKind.Explicit,
                TriggerKind = CompletionTriggerKind.Invoked
            }
        };

        var result = await completionEndpoint.GetTestAccessor().HandleRequestAsync(request, document, DisposalToken);

        Assert.NotNull(result);

        var resolveEndpoint = new CohostDocumentCompletionResolveEndpoint(
            IncompatibleProjectService,
            completionListCache,
            RemoteServiceInvoker,
            requestInvoker,
            ClientCapabilitiesService,
            snippetCompletionItemProvider,
            LoggerFactory);

        var itemToResolve = result.Items.First();
        itemToResolve = JsonSerializer.Deserialize<VSInternalCompletionItem>(JsonSerializer.SerializeToElement(itemToResolve, JsonHelpers.JsonSerializerOptions), JsonHelpers.JsonSerializerOptions)!;

        var resolvedItem = await resolveEndpoint.GetTestAccessor().HandleRequestAsync(itemToResolve, document, DisposalToken);

        Assert.NotNull(resolvedItem);
        Assert.Equal("table", resolvedItem.Label);
        // The resolved snippet body starts with '<table>...' but in start-tag context
        // the '<' is already in the document, so it should be stripped.
        Assert.Equal("table>$0</table>", resolvedItem.InsertText);
    }

    private class TestSnippetCompletionItemProvider : ISnippetCompletionItemProvider
    {
        public void AddSnippetCompletions(ref PooledArrayBuilder<VSInternalCompletionItem> builder, RazorLanguageKind projectedKind, string? triggerCharacter, ICollection<string> validElementNames, RazorCompletionOptions options, bool isStartTagContext)
        {
            Assert.Equal(RazorLanguageKind.Html, projectedKind);
            builder.Add(new VSInternalCompletionItem()
            {
                Label = "SnippetItem",
                InsertText = "SnippetItem",
                InsertTextFormat = InsertTextFormat.Snippet,
                Data = new SnippetCompletionData("SnippetPath")
            });
        }

        public bool TryResolveInsertString(VSInternalCompletionItem completionItem, [NotNullWhen(true)] out string? insertString)
        {
            Assert.Equal("SnippetItem", completionItem.Label);
            Assert.Equal("SnippetItem", completionItem.InsertText);

            Assert.True(SnippetCompletionData.TryParse(completionItem.Data, out var snippetCompletionData));
            Assert.Equal("SnippetPath", snippetCompletionData.Path);

            insertString = "ResolvedSnippetItem";

            return true;
        }
    }

    /// <summary>
    /// Snippet provider that returns a snippet whose resolved body starts with '&lt;',
    /// to test that the leading '&lt;' is stripped in start-tag context.
    /// </summary>
    private class StartTagSnippetProvider : ISnippetCompletionItemProvider
    {
        public void AddSnippetCompletions(ref PooledArrayBuilder<VSInternalCompletionItem> builder, RazorLanguageKind projectedKind, string? triggerCharacter, ICollection<string> validElementNames, RazorCompletionOptions options, bool isStartTagContext)
        {
            builder.Add(new VSInternalCompletionItem()
            {
                Label = "table",
                InsertText = "table",
                InsertTextFormat = InsertTextFormat.Snippet,
                Kind = CompletionItemKind.Snippet,
                Data = new SnippetCompletionData("TableSnippetPath")
            });
        }

        public bool TryResolveInsertString(VSInternalCompletionItem completionItem, [NotNullWhen(true)] out string? insertString)
        {
            // Simulate a real snippet body that starts with '<'
            insertString = "<table>$0</table>";
            return true;
        }
    }
}
