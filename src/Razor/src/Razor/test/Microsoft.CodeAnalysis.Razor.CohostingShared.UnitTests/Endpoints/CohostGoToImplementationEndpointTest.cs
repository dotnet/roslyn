// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Remote.Razor;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public class CohostGoToImplementationEndpointTest(ITestOutputHelper testOutputHelper) : CohostEndpointTestBase(testOutputHelper)
{
    [Fact]
    public async Task CSharp_Method()
    {
        var input = """
            <div></div>

            @{
                var x = Ge$$tX();
            }

            @code
            {
                int [||]GetX()
                {
                    return 4;
                }
            }
            """;

        await VerifyGoToImplementationAsync(input);
    }

    [Fact]
    public async Task CSharp_Field()
    {
        var input = """
            <div></div>

            @{
                var x = GetX();
            }

            @code
            {
                private string [||]_name;

                string GetX()
                {
                    return _na$$me;
                }
            }
            """;

        await VerifyGoToImplementationAsync(input);
    }

    [Fact]
    public async Task CSharp_Multiple()
    {
        var input = """
            <div></div>

            @code
            {
                class Base { }
                class [||]Derived1 : Base { }
                class [||]Derived2 : Base { }

                void M(Ba$$se b)
                {
                }
            }
            """;

        await VerifyGoToImplementationAsync(input);
    }

    [Fact]
    public async Task Html()
    {
        // This really just validates Uri remapping, the actual response is largely arbitrary

        TestCode input = """
            <div></div>

            <script>
                function [|foo|]() {
                    f$$oo();
                }
            </script>
            """;

        var document = CreateProjectAndRazorDocument(input.Text);
        var inputText = await document.GetTextAsync(DisposalToken);

        var htmlResponse = new LspLocation
        {
            DocumentUri = new(new Uri(document.CreateUri(), document.Name + LanguageServerConstants.HtmlVirtualDocumentSuffix)),
            Range = inputText.GetRange(input.Span),
        };

        await VerifyGoToImplementationAsync(input, document, htmlResponse);
    }

    [Fact]
    public async Task Component_FromCSharp()
    {
        TestCode input = """
            <SurveyPrompt Title="InputValue" />

            @typeof(Surv$$eyPrompt).ToString()
            """;

        TestCode surveyPrompt = """
            [||]@namespace SomeProject

            <div></div>

            @code
            {
                [Parameter]
                public string Title { get; set; }
            }
            """;

        var result = await GetGoToImplementationResultAsync(input,
            additionalFiles: (FilePath("SurveyPrompt.razor"), surveyPrompt.Text));

        Assert.NotNull(result);
        var locations = result.Value.First;
        var location = Assert.Single(locations);

        Assert.Equal(FileUri("SurveyPrompt.razor"), location.DocumentUri.GetRequiredParsedUri());
        var text = SourceText.From(surveyPrompt.Text);
        var range = text.GetRange(surveyPrompt.Span);
        Assert.Equal(range, location.Range);
    }

    private async Task VerifyGoToImplementationAsync(TestCode input, TextDocument? document = null, LspLocation? htmlResponse = null)
    {
        document ??= CreateProjectAndRazorDocument(input.Text);
        var result = await GetGoToImplementationResultCoreAsync(input, document, htmlResponse);

        Assert.NotNull(result);

        var inputText = SourceText.From(input.Text);
        if (result.Value.TryGetFirst(out var roslynLocations))
        {
            var expected = input.Spans.Select(s => inputText.GetRange(s).ToLinePositionSpan()).OrderBy(r => r.Start.Line).ToArray();
            var actual = roslynLocations.Select(l => l.Range.ToLinePositionSpan()).OrderBy(r => r.Start.Line).ToArray();
            Assert.Equal(expected, actual);

            Assert.All(roslynLocations, l => l.DocumentUri.GetRequiredParsedUri().Equals(document.CreateUri()));
        }
        else
        {
            Assert.Fail($"Unsupported result type: {result.Value.GetType()}");
        }
    }

    private async Task<SumType<LspLocation[], VSInternalReferenceItem[]>?> GetGoToImplementationResultAsync(
        TestCode input,
        LspLocation? htmlResponse = null,
        params (string fileName, string contents)[]? additionalFiles)
    {
        var document = CreateProjectAndRazorDocument(input.Text, additionalFiles: additionalFiles);
        return await GetGoToImplementationResultCoreAsync(input, document, htmlResponse);
    }

    private async Task<SumType<LspLocation[], VSInternalReferenceItem[]>?> GetGoToImplementationResultCoreAsync(TestCode input, TextDocument document, LspLocation? htmlResponse)
    {
        var requestInvoker = htmlResponse is null
            ? new TestHtmlRequestInvoker()
            : new TestHtmlRequestInvoker([(Methods.TextDocumentImplementationName, new SumType<LspLocation[], VSInternalReferenceItem[]>?(new LspLocation[] { htmlResponse }))]);

        var inputText = await document.GetTextAsync(DisposalToken);

        var filePathService = new RemoteFilePathService();
        var endpoint = new CohostGoToImplementationEndpoint(IncompatibleProjectService, RemoteServiceInvoker, requestInvoker, filePathService);

        var position = inputText.GetPosition(input.Position);
        var textDocumentPositionParams = new TextDocumentPositionParams
        {
            Position = position,
            TextDocument = new TextDocumentIdentifier { DocumentUri = document.CreateDocumentUri() },
        };

        return await endpoint.GetTestAccessor().HandleRequestAsync(textDocumentPositionParams, document, DisposalToken);
    }
}
