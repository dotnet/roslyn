// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

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
            using var testLspServer = await CreateTestLspServerAsync(markup);
            var expected = new LSP.SignatureHelp()
            {
                ActiveParameter = 0,
                ActiveSignature = 0,
                Signatures = new LSP.SignatureInformation[] { CreateSignatureInformation("int A.M2(string a)", "M2 is a method.", "a", "") }
            };

            var results = await RunGetSignatureHelpAsync(testLspServer, testLspServer.GetLocations("caret").Single());
            AssertJsonEquals(expected, results);
        }

        private static async Task<LSP.SignatureHelp?> RunGetSignatureHelpAsync(TestLspServer testLspServer, LSP.Location caret)
        {
            return await testLspServer.ExecuteRequestAsync<LSP.TextDocumentPositionParams, LSP.SignatureHelp?>(
                LSP.Methods.TextDocumentSignatureHelpName,
                CreateTextDocumentPositionParams(caret), CancellationToken.None);
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
