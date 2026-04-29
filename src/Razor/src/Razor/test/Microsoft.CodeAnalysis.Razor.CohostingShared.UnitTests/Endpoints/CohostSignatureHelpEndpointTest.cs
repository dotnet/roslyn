// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Razor;
using Microsoft.CodeAnalysis.ExternalAccess.Razor;
using Microsoft.CodeAnalysis.Razor.Settings;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.LanguageServices.Razor.LanguageClient.Cohost;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.VisualStudio.Razor.LanguageClient.Cohost;

public class CohostSignatureHelpEndpointTest(ITestOutputHelper testOutputHelper) : CohostEndpointTestBase(testOutputHelper)
{
    [Fact]
    public async Task CSharpMethodCSharp()
    {
        var input = """
                <div></div>

                @{
                    string M1(int i) => throw new NotImplementedException();

                    void Act()
                    {
                        M1($$);
                    }
                }
                """;

        await VerifySignatureHelpAsync(input, "string M1(int i)");
    }

    [Fact]
    public async Task CSharpMethodInRazor()
    {
        var input = """
                <div>@GetDiv($$)</div>

                @{
                    string GetDiv() => "";
                }
                """;

        await VerifySignatureHelpAsync(input, "string GetDiv()");
    }

    [Fact]
    public async Task AutoListParamsOff_Invoked_ReturnsResult()
    {
        var input = """
                <div></div>

                @{
                    string M1(int i) => throw new NotImplementedException();

                    void Act()
                    {
                        M1($$);
                    }
                }
                """;

        await VerifySignatureHelpAsync(input, "string M1(int i)", autoListParams: false, triggerKind: SignatureHelpTriggerKind.Invoked);
    }

    [Fact]
    public async Task AutoListParamsOff_NotInvoked_ReturnsNoResult()
    {
        var input = """
                <div></div>

                @{
                    string M1(int i) => throw new NotImplementedException();

                    void Act()
                    {
                        M1($$);
                    }
                }
                """;

        await VerifySignatureHelpAsync(input, "", autoListParams: false, triggerKind: SignatureHelpTriggerKind.ContentChange);
    }

    private async Task VerifySignatureHelpAsync(string input, string expected, bool autoListParams = true, SignatureHelpTriggerKind? triggerKind = null)
    {
        TestFileMarkupParser.GetPosition(input, out input, out var cursorPosition);
        var document = CreateProjectAndRazorDocument(input);
        var sourceText = await document.GetTextAsync(DisposalToken);

        ClientSettingsManager.Update(ClientCompletionSettings.Default with { AutoListParams = autoListParams });

        var requestInvoker = new TestHtmlRequestInvoker([(Methods.TextDocumentSignatureHelpName, (object?)null)]);

        var endpoint = new CohostSignatureHelpEndpoint(IncompatibleProjectService, RemoteServiceInvoker, ClientSettingsManager, requestInvoker);

        var signatureHelpContext = new SignatureHelpContext()
        {
            TriggerKind = triggerKind ?? SignatureHelpTriggerKind.Invoked
        };

        var request = new SignatureHelpParams()
        {
            TextDocument = new TextDocumentIdentifier()
            {
                DocumentUri = document.CreateDocumentUri()
            },
            Position = sourceText.GetPosition(cursorPosition),
            Context = signatureHelpContext
        };

        var result = await endpoint.GetTestAccessor().HandleRequestAsync(request, document, DisposalToken);

        // Assert
        if (expected.Length == 0)
        {
            Assert.Null(result);
            return;
        }

        var actual = Assert.Single(result.AssumeNotNull().Signatures);
        Assert.Equal(expected, actual.Label);
    }
}
