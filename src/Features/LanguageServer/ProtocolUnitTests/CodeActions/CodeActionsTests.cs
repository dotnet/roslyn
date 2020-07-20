// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            using var workspace = CreateTestWorkspace(markup, out var locations);
            var expected = CreateCommand(CSharpAnalyzersResources.Use_implicit_type, locations["caret"].Single());
            var clientCapabilities = CreateClientCapabilitiesWithExperimentalValue("supportsWorkspaceEdits", JToken.FromObject(false));

            var results = await RunGetCodeActionsAsync(workspace.CurrentSolution, locations["caret"].Single(), clientCapabilities);
            var useImplicitTypeResult = results.Single(r => r.Title == CSharpAnalyzersResources.Use_implicit_type);
            AssertJsonEquals(expected, useImplicitTypeResult);
        }

        private static async Task<LSP.Command[]> RunGetCodeActionsAsync(Solution solution, LSP.Location caret, LSP.ClientCapabilities clientCapabilities = null)
        {
            var results = await GetLanguageServer(solution).ExecuteRequestAsync<LSP.CodeActionParams, LSP.SumType<LSP.Command, LSP.CodeAction>[]>(LSP.Methods.TextDocumentCodeActionName,
                CreateCodeActionParams(caret), clientCapabilities, null, CancellationToken.None);
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
