﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            var (solution, locations) = CreateTestSolution(markup);
            var expected = new LSP.SignatureHelp()
            {
                ActiveParameter = 0,
                ActiveSignature = 0,
                Signatures = new LSP.SignatureInformation[] { CreateSignatureInformation("int A.M2(string a)", "M2 is a method.", "a", "") }
            };

            var results = await RunGetSignatureHelpAsync(solution, locations["caret"].First());
            AssertSignatureHelpEquals(expected, results);
        }

        private static async Task<LSP.SignatureHelp> RunGetSignatureHelpAsync(Solution solution, LSP.Location caret)
            => await GetLanguageServer(solution).GetSignatureHelpAsync(solution, CreateTextDocumentPositionParams(caret), new LSP.ClientCapabilities(), CancellationToken.None);

        private static void AssertSignatureHelpEquals(LSP.SignatureHelp expected, LSP.SignatureHelp actual)
        {
            Assert.Equal(expected.ActiveParameter, actual.ActiveParameter);
            Assert.Equal(expected.ActiveSignature, actual.ActiveSignature);
            AssertCollectionsEqual(expected.Signatures, actual.Signatures, AssertSignatureInformationsEqual);
        }

        private static void AssertSignatureInformationsEqual(LSP.SignatureInformation expected, LSP.SignatureInformation actual)
        {
            Assert.Equal(expected.Label, actual.Label);
            AssertMarkupContentsEqual(expected.Documentation, actual.Documentation);
            AssertCollectionsEqual(expected.Parameters, actual.Parameters, AssertParameterInformationsEqual);
        }

        private static void AssertParameterInformationsEqual(LSP.ParameterInformation expected, LSP.ParameterInformation actual)
        {
            Assert.Equal(expected.Label, actual.Label);
            AssertMarkupContentsEqual(expected.Documentation, actual.Documentation);
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
