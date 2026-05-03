// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Formatting;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Razor.Remote;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using WorkItemAttribute = Microsoft.AspNetCore.Razor.Test.Common.WorkItemAttribute;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

[Collection(HtmlFormattingCollection.Name)]
public class CohostRangeFormattingEndpointTest(HtmlFormattingFixture htmlFormattingFixture, ITestOutputHelper testOutputHelper)
    : CohostEndpointTestBase(testOutputHelper)
{
    [Fact]
    public Task RangeFormatting()
        => VerifyRangeFormattingAsync(
            input: """
            @preservewhitespace    true

                        <div></div>

            @{
            <p>
                    @{
                            var t = 1;
            if (true)
            {
            
                        }
                    }
                    </p>
            [|<div>
             @{
                <div>
            <div>
                    This is heavily nested
            </div>
             </div>
                }
                    </div>|]
            }

            @code {
                            private void M(string thisIsMyString)
                {
                    var x = 5;

                                var y = "Hello";

                    M("Hello");
                }
            }
            """,
            expected: """
            @preservewhitespace    true
            
                        <div></div>
            
            @{
            <p>
                    @{
                            var t = 1;
            if (true)
            {
            
                        }
                    }
                    </p>
                <div>
                    @{
                        <div>
                            <div>
                                This is heavily nested
                            </div>
                        </div>
                    }
                </div>
            }
            
            @code {
                            private void M(string thisIsMyString)
                {
                    var x = 5;
            
                                var y = "Hello";
            
                    M("Hello");
                }
            }
            """);

    [Fact]
    public async Task FormatOnPasteDisabled()
    {
        ClientSettingsManager.Update(ClientSettingsManager.GetClientSettings().AdvancedSettings with { FormatOnPaste = false });

        await VerifyRangeFormattingAsync(
            input: """
                <div>
                [|hello
                <div>
                </div>|]
                </div>
                """,
            expected: """
                <div>
                hello
                <div>
                </div>
                </div>
                """,
            otherOptions: new()
                {
                    { "fromPaste", true }
                });
    }

    [Fact]
    [WorkItem("https://github.com/dotnet/razor/issues/12805")]
    public async Task FormatOnPasteIgnoresFormattingErrors()
    {
        await VerifyRangeFormattingAsync(
            input: """
                @using System.Collections.Generic
                @{
                    var item = new List<string>();
                }

                <div class="d-flex">
                    <div>
                        @if [|ErrorWitnessPersonID|]
                    </div>
                    <div>
                        @(item.Count)x
                    </div>
                </div>
                """,
            expected: """
                @using System.Collections.Generic
                @{
                    var item = new List<string>();
                }

                <div class="d-flex">
                    <div>
                        @if ErrorWitnessPersonID
                    </div>
                    <div>
                        @(item.Count)x
                    </div>
                </div>
                """,
            otherOptions: new()
                {
                    { "fromPaste", true }
                });
    }

    private async Task VerifyRangeFormattingAsync(TestCode input, string expected, Dictionary<string, SumType<bool, int, string>>? otherOptions = null)
    {
        var document = CreateProjectAndRazorDocument(input.Text);
        var inputText = await document.GetTextAsync(DisposalToken);

        var generatedHtml = await RemoteServiceInvoker.TryInvokeAsync<IRemoteHtmlDocumentService, string?>(document.Project.Solution,
            (service, solutionInfo, ct) => service.GetHtmlDocumentTextAsync(solutionInfo, document.Id, ct),
            DisposalToken).ConfigureAwait(false);
        Assert.NotNull(generatedHtml);

        var uri = new Uri(document.CreateUri(), $"{document.FilePath}{LanguageServerConstants.HtmlVirtualDocumentSuffix}");
        var htmlEdits = await htmlFormattingFixture.Service.GetDocumentFormattingEditsAsync(LoggerFactory, uri, generatedHtml, insertSpaces: true, tabSize: 4);

        var formattingService = (RazorFormattingService)OOPExportProvider.GetExportedValue<IRazorFormattingService>();
        var accessor = formattingService.GetTestAccessor();
        accessor.SetFormattingLoggerFactory(new TestFormattingLoggerFactory(TestOutputHelper));

        var requestInvoker = new TestHtmlRequestInvoker([(Methods.TextDocumentFormattingName, htmlEdits)]);

        var endpoint = new CohostRangeFormattingEndpoint(IncompatibleProjectService, RemoteServiceInvoker, requestInvoker, ClientSettingsManager, LoggerFactory);

        var request = new DocumentRangeFormattingParams()
        {
            TextDocument = new TextDocumentIdentifier() { DocumentUri = document.CreateDocumentUri() },
            Options = new FormattingOptions()
            {
                TabSize = 4,
                InsertSpaces = true,
                OtherOptions = otherOptions
            },
            Range = inputText.GetRange(input.Span)
        };

        var edits = await endpoint.GetTestAccessor().HandleRequestAsync(request, document, DisposalToken);

        if (edits is null or [])
        {
            Assert.Equal(input.Text, expected);
            return;
        }

        var changes = edits.Select(inputText.GetTextChange);
        var finalText = inputText.WithChanges(changes);

        AssertEx.EqualOrDiff(expected, finalText.ToString());
    }
}
