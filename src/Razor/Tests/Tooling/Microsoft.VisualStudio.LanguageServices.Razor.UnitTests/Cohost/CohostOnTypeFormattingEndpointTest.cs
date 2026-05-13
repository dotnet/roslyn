// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Cohost;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Remote;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

[Collection(HtmlFormattingCollection.Name)]
public class CohostOnTypeFormattingEndpointTest(HtmlFormattingFixture htmlFormattingFixture, ITestOutputHelper testOutputHelper)
    : CohostEndpointTestBase(testOutputHelper)
{
    [Fact]
    public async Task InvalidTrigger()
    {
        await VerifyOnTypeFormattingAsync(
            input: """
                    @{
                     if(true){}$$
                    }
                    """,
            expected: """
                    @{
                     if(true){}
                    }
                    """,
            triggerCharacter: 'h');
    }

    [Fact]
    public async Task CSharp_InvalidTrigger()
    {
        await VerifyOnTypeFormattingAsync(
            input: """
                    @{
                     if(true){}$$
                    }
                    """,
            expected: """
                    @{
                     if(true){}
                    }
                    """,
            triggerCharacter: '\n');
    }

    [Fact]
    public async Task CSharp()
    {
        await VerifyOnTypeFormattingAsync(
            input: """
                    @{
                     if(true){}$$
                    }
                    """,
            expected: """
                    @{
                        if (true) { }
                    }
                    """,
            triggerCharacter: '}');
    }

    [Fact]
    public async Task FormatsSimpleHtmlTag_OnType()
    {
        await VerifyOnTypeFormattingAsync(
            input: """
                    <html>
                    <head>
                        <title>Hello</title>
                            <script>
                                var x = 2;$$
                            </script>
                    </head>
                    </html>
                    """,
            expected: """
                    <html>
                    <head>
                        <title>Hello</title>
                        <script>
                            var x = 2;
                        </script>
                    </head>
                    </html>
                    """,
            triggerCharacter: ';',
            html: true);
    }

    [Fact]
    public async Task FormattingDisabled()
    {
        ClientSettingsManager.Update(ClientSettingsManager.GetClientSettings().AdvancedSettings with { FormatOnType = false });

        await VerifyOnTypeFormattingAsync(
            input: """
                @{
                    if(true){}$$
                }
                """,
            expected: """
                @{
                    if(true){}
                }
                """,
            triggerCharacter: '}');
    }

    private async Task VerifyOnTypeFormattingAsync(TestCode input, string expected, char triggerCharacter, bool html = false)
    {
        var document = CreateProjectAndRazorDocument(input.Text);
        var inputText = await document.GetTextAsync(DisposalToken);
        var position = inputText.GetPosition(input.Position);

        IHtmlRequestInvoker requestInvoker;
        if (html)
        {
            var generatedHtml = await RemoteServiceInvoker.TryInvokeAsync<IRemoteHtmlDocumentService, string?>(document.Project.Solution,
                (service, solutionInfo, ct) => service.GetHtmlDocumentTextAsync(solutionInfo, document.Id, ct),
                DisposalToken).ConfigureAwait(false);
            Assert.NotNull(generatedHtml);

            var uri = new Uri(document.CreateUri(), $"{document.FilePath}{LanguageServerConstants.HtmlVirtualDocumentSuffix}");
            var htmlEdits = await htmlFormattingFixture.Service.GetOnTypeFormattingEditsAsync(LoggerFactory, uri, generatedHtml, position, insertSpaces: true, tabSize: 4);

            requestInvoker = new TestHtmlRequestInvoker([(Methods.TextDocumentOnTypeFormattingName, htmlEdits)]);
        }
        else
        {
            // We use a mock here so that it will throw if called
            requestInvoker = StrictMock.Of<IHtmlRequestInvoker>();
        }

        var endpoint = new CohostOnTypeFormattingEndpoint(IncompatibleProjectService, RemoteServiceInvoker, requestInvoker, ClientSettingsManager, LoggerFactory);

        var request = new DocumentOnTypeFormattingParams()
        {
            TextDocument = new TextDocumentIdentifier() { DocumentUri = document.CreateDocumentUri() },
            Options = new FormattingOptions()
            {
                TabSize = 4,
                InsertSpaces = true
            },
            Character = triggerCharacter.ToString(),
            Position = position
        };

        var edits = await endpoint.GetTestAccessor().HandleRequestAsync(request, document, DisposalToken);

        if (edits is null)
        {
            Assert.Equal(expected, input.Text);
            return;
        }

        var changes = edits.Select(inputText.GetTextChange);
        var finalText = inputText.WithChanges(changes);

        AssertEx.EqualOrDiff(expected, finalText.ToString());
    }
}
