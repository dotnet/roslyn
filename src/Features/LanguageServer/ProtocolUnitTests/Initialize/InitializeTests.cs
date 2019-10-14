// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Initialize
{
    public class InitializeTests : AbstractLanguageServerProtocolTests
    {
        [Fact]
        public async Task TestInitializeAsync()
        {
            var (solution, _) = CreateTestSolution(string.Empty);
            var results = await RunInitializeAsync(solution, new LSP.InitializeParams());

            AssertServerCapabilities(results.Capabilities);
        }

        private static async Task<LSP.InitializeResult> RunInitializeAsync(Solution solution, LSP.InitializeParams request)
            => await GetLanguageServer(solution).InitializeAsync(solution, request, new LSP.ClientCapabilities(), CancellationToken.None);

        private static void AssertServerCapabilities(LSP.ServerCapabilities actual)
        {
            Assert.True(actual.DefinitionProvider);
            Assert.True(actual.ReferencesProvider);
            Assert.True(actual.ImplementationProvider);
            Assert.True(actual.HoverProvider);
            Assert.True(actual.CodeActionProvider);
            Assert.True(actual.DocumentSymbolProvider);
            Assert.True(actual.WorkspaceSymbolProvider);
            Assert.True(actual.DocumentFormattingProvider);
            Assert.True(actual.DocumentRangeFormattingProvider);
            Assert.True(actual.DocumentHighlightProvider);
            Assert.True(actual.RenameProvider);

            Assert.True(actual.CompletionProvider.ResolveProvider);
            Assert.Equal(new[] { "." }, actual.CompletionProvider.TriggerCharacters);

            Assert.Equal(new[] { "(", "," }, actual.SignatureHelpProvider.TriggerCharacters);

            Assert.Equal("}", actual.DocumentOnTypeFormattingProvider.FirstTriggerCharacter);
            Assert.Equal(new[] { ";", "\n" }, actual.DocumentOnTypeFormattingProvider.MoreTriggerCharacter);

            Assert.NotNull(actual.ExecuteCommandProvider);
        }
    }
}
