// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.LanguageServer.CustomProtocol;
using Newtonsoft.Json.Linq;
using Roslyn.Test.Utilities;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.CodeActions
{
    public class RunCodeActionsTests : AbstractLanguageServerProtocolTests
    {
        [WpfFact]
        public async Task TestRunCodeActionsAsync()
        {
            var markup =
@"class A
{
    void M()
    {
        {|caret:|}int i = 1;
    }
}";
            var (solution, ranges) = CreateTestSolution(markup);
            var codeActionLocation = ranges["caret"].First();

            var results = await RunExecuteWorkspaceCommand(solution, codeActionLocation, CSharpFeaturesResources.Use_implicit_type);
            Assert.True(results);
        }

        private static async Task<bool> RunExecuteWorkspaceCommand(Solution solution, LSP.Location caret, string title, LSP.ClientCapabilities clientCapabilities = null)
            => (bool)await GetLanguageServer(solution).ExecuteWorkspaceCommandAsync(solution, CreateExecuteCommandParams(caret, title),
                clientCapabilities, CancellationToken.None);

        private static LSP.ExecuteCommandParams CreateExecuteCommandParams(LSP.Location location, string title)
            => new LSP.ExecuteCommandParams()
            {
                Command = "Roslyn.RunCodeAction",
                Arguments = new object[]
                {
                    JObject.FromObject(CreateRunCodeActionParams(title, location))
                },
            };
    }
}
