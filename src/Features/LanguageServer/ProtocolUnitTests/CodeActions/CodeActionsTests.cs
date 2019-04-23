// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.LanguageServer.CustomProtocol;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests
{
    public class CodeActionsTests : LanguageServerProtocolTestsBase
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
            var expected = CreateCommand("Use implicit type", locations["caret"].First());

            var results = await RunCodeActionsAsync(solution, locations["caret"].First());
            AssertCodeActionCommandsEqual(expected, results.Single());
        }

        private static async Task<LSP.Command[]> RunCodeActionsAsync(Solution solution, LSP.Location caret)
            => await GetLanguageServer(solution).GetCodeActionsAsync(solution, CreateCodeActionParams(caret), new LSP.ClientCapabilities(), CancellationToken.None);

        private static void AssertCodeActionCommandsEqual(LSP.Command expected, LSP.Command actual)
        {
            Assert.Equal(expected.Title, actual.Title);
            Assert.Equal(expected.CommandIdentifier, actual.CommandIdentifier);
            Assert.Equal(expected.Arguments.Length, actual.Arguments.Length);

            for (var i = 0; i < expected.Arguments.Length; i++)
            {
                if (expected.Arguments[i] is LSP.Command)
                {
                    AssertCodeActionCommandsEqual((LSP.Command)expected.Arguments[i], (LSP.Command)actual.Arguments[i]);
                }
                else if (expected.Arguments[i] is RunCodeActionParams)
                {
                    AssertRunCodeActionParamsEqual((RunCodeActionParams)expected.Arguments[i], (RunCodeActionParams)actual.Arguments[i]);
                }
            }

            // local functions
            static void AssertRunCodeActionParamsEqual(RunCodeActionParams expected, RunCodeActionParams actual)
            {
                Assert.Equal(expected.Title, actual.Title);
                Assert.Equal(expected.TextDocument.Uri, actual.TextDocument.Uri);
                Assert.Equal(expected.Range, actual.Range);
            }
        }

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
                CommandIdentifier = "_liveshare.remotecommand.Roslyn",
                Arguments = new object[]
                {
                    new LSP.Command()
                    {
                        Title = title,
                        CommandIdentifier = "Roslyn.RunCodeAction",
                        Arguments = new object[]
                        {
                            CreateRunCodeActionParams(location, title)
                        }
                    }
                }
            };
    }
}
