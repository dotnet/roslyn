// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.SignatureHelp
{
    public class SignatureHelpTests : AbstractLanguageServerProtocolTests
    {
        [Fact]
        public async Task TestGetSignatureHelpAsync()
        {
            var markup =
@"class A
{
    void M()
    {
        M2({|caret:|}'a');
    }
    /// <summary>
    /// M2 is a method.
    /// </summary>
    int M2(string a)
    {
        return 1;
    }

}";
            using var workspace = CreateTestWorkspace(markup, out var locations);
            var expected = new LSP.SignatureHelp()
            {
                ActiveParameter = 0,
                ActiveSignature = 0,
                Signatures = new LSP.SignatureInformation[] { CreateSignatureInformation("int A.M2(string a)", "M2 is a method.", "a", "") }
            };

            var results = await RunGetSignatureHelpAsync(workspace.CurrentSolution, locations["caret"].Single());
            AssertJsonEquals(expected, results);
        }

        private static async Task<LSP.SignatureHelp> RunGetSignatureHelpAsync(Solution solution, LSP.Location caret)
        {
            var queue = CreateRequestQueue(solution);
            return await GetLanguageServer(solution).ExecuteRequestAsync<LSP.TextDocumentPositionParams, LSP.SignatureHelp>(queue, LSP.Methods.TextDocumentSignatureHelpName,
                           CreateTextDocumentPositionParams(caret), new LSP.ClientCapabilities(), null, CancellationToken.None);
        }

        private static LSP.SignatureInformation CreateSignatureInformation(string methodLabal, string methodDocumentation, string parameterLabel, string parameterDocumentation)
            => new LSP.SignatureInformation()
            {
                Documentation = CreateMarkupContent(LSP.MarkupKind.PlainText, methodDocumentation),
                Label = methodLabal,
                Parameters = new LSP.ParameterInformation[]
                {
                    CreateParameterInformation(parameterLabel, parameterDocumentation)
                }
            };

        private static LSP.ParameterInformation CreateParameterInformation(string parameter, string documentation)
            => new LSP.ParameterInformation()
            {
                Documentation = CreateMarkupContent(LSP.MarkupKind.PlainText, documentation),
                Label = parameter
            };
    }
}
