// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor.Test.Common;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Protocol;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Razor.LanguageClient.Cohost;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudioCode.RazorExtension.Test.Endpoints;

public class CohostCodeLensEndpointTest(ITestOutputHelper testOutputHelper) : CohostEndpointTestBase(testOutputHelper)
{
    [Fact]
    public Task OneMethod()
    {
        return VerifyCodeLensAsync("""
            <div></div>

            @code {
                public void [|{|Position0:|}Method|]()
                {
                    // This is a method
                }
            }
            """,
            expectedTitles: ["0 references"]);
    }

    [Fact]
    public Task TwoMethods()
    {
        return VerifyCodeLensAsync("""
            <div></div>

            @code {
                public void [|{|Position0:|}Method|]()
                {
                    Method2();
                }

                public void [|{|Position1:|}Method2|]()
                {
                    // This is another method
                }
            }
            """,
            expectedTitles: ["0 references", "1 reference"]);
    }

    [Fact]
    public Task UsageInRazor()
    {
        return VerifyCodeLensAsync("""
            <div></div>

            @Method()

            @code {
                public string [|{|Position0:|}Method|]()
                {
                    return "Hello, World!";
                }
            }
            """,
            expectedTitles: ["1 reference"]);
    }

    private async Task VerifyCodeLensAsync(TestCode input, string[] expectedTitles)
    {
        var document = CreateProjectAndRazorDocument(input.Text);
        var inputText = SourceText.From(input.Text);

        var endpoint = new CohostCodeLensEndpoint(IncompatibleProjectService, RemoteServiceInvoker);
        var resolveEndpoint = new CohostResolveCodeLensEndpoint(IncompatibleProjectService, RemoteServiceInvoker);

        var request = new CodeLensParams()
        {
            TextDocument = new TextDocumentIdentifier() { DocumentUri = document.CreateDocumentUri() },
        };

        var result = await endpoint.GetTestAccessor().HandleRequestAsync(request, document, DisposalToken);

        Assert.NotNull(result);
        foreach (var (codeLens, i) in result.Select((l, i) => (l, i)))
        {
            Assert.Contains(inputText.GetTextSpan(codeLens.Range), input.Spans);

            // Resolve expects a JsonElement
            codeLens.Data = JsonSerializer.SerializeToElement(codeLens.Data, JsonHelpers.JsonSerializerOptions);

            var tdi = resolveEndpoint.GetTestAccessor().GetRazorTextDocumentIdentifier(codeLens);
            Assert.NotNull(tdi);
            Assert.Equal(document.CreateUri(), tdi.Value.Uri);

            var resolved = await resolveEndpoint.GetTestAccessor().HandleRequestAsync(codeLens, document, DisposalToken);

            Assert.NotNull(resolved);
            Assert.NotNull(resolved.Command);
            Assert.NotNull(resolved.Command.Arguments);
            Assert.Equal(resolved.Command.Title, expectedTitles[i]);
            Assert.Equal("roslyn.client.peekReferences", resolved.Command.CommandIdentifier);

            var documentUri = Assert.IsType<DocumentUri>(resolved.Command.Arguments[0]);
            Assert.Equal(document.CreateDocumentUri(), documentUri);

            var position = Assert.IsType<Position>(resolved.Command.Arguments[1]);
            Assert.Equal(input.NamedSpans[$"Position{i}"].Single(), inputText.GetTextSpan(position.ToZeroWidthRange()));
        }
    }
}
