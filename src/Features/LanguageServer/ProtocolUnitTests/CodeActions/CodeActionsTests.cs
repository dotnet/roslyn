// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json.Linq;
using Roslyn.Test.Utilities;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.CodeActions
{
    public class CodeActionsTests : AbstractLanguageServerProtocolTests
    {
        [Fact]
        public async Task TestGetCodeActionsAsync()
        {
            var markup =
@"class A
{
    void M()
    {
        {|caret:|}int i = 1;
    }
}";
            var (solution, locations) = CreateTestSolution(markup);
            var expected = CreateCommand(CSharpFeaturesResources.Use_implicit_type, locations["caret"].Single());
            var clientCapabilities = CreateClientCapabilitiesWithExperimentalValue("supportsWorkspaceEdits", JToken.FromObject(false));

            var results = await RunGetCodeActionsAsync(solution, locations["caret"].Single(), clientCapabilities);
            var useImplicitTypeResult = results.Single(r => r.Title == CSharpFeaturesResources.Use_implicit_type);
            AssertJsonEquals(expected, useImplicitTypeResult);
        }

        private static async Task<LSP.Command[]> RunGetCodeActionsAsync(Solution solution, LSP.Location caret, LSP.ClientCapabilities clientCapabilities = null)
        {
            var results = await GetLanguageServer(solution).GetCodeActionsAsync(solution, CreateCodeActionParams(caret), clientCapabilities, CancellationToken.None);
            return results.Select(r => (LSP.Command)r).ToArray();
        }

        private static LSP.ClientCapabilities CreateClientCapabilitiesWithExperimentalValue(string experimentalProperty, JToken value)
            => new LSP.ClientCapabilities()
            {
                Experimental = new JObject
                {
                    { experimentalProperty, value }
                }
            };

        private static LSP.CodeActionParams CreateCodeActionParams(LSP.Location caret)
            => new LSP.CodeActionParams()
            {
                TextDocument = CreateTextDocumentIdentifier(caret.Uri),
                Range = caret.Range,
                Context = new LSP.CodeActionContext()
                {
                    // TODO - Code actions should respect context.
                }
            };

        private static LSP.Command CreateCommand(string title, LSP.Location location)
            => new LSP.Command()
            {
                Title = title,
                CommandIdentifier = "Roslyn.RunCodeAction",
                Arguments = new object[]
                {
                    CreateRunCodeActionParams(title, location)
                }
            };
    }
}
