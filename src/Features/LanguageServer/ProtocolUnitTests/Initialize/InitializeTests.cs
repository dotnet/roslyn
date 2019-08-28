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
            Assert.Equal(true, actual.DefinitionProvider);
            Assert.Equal(true, actual.ReferencesProvider);
            Assert.Equal(true, actual.ImplementationProvider);
            Assert.Equal(true, actual.HoverProvider);
            Assert.Equal(true, actual.CodeActionProvider);
            Assert.Equal(true, actual.DocumentSymbolProvider);
            Assert.Equal(true, actual.WorkspaceSymbolProvider);
            Assert.Equal(true, actual.DocumentFormattingProvider);
            Assert.Equal(true, actual.DocumentRangeFormattingProvider);
            Assert.Equal(true, actual.DocumentHighlightProvider);
            Assert.Equal(true, actual.RenameProvider);

            Assert.Equal(true, actual.CompletionProvider.ResolveProvider);
            Assert.Equal(new[] { "." }, actual.CompletionProvider.TriggerCharacters);

            Assert.Equal(new[] { "(", "," }, actual.SignatureHelpProvider.TriggerCharacters);

            Assert.Equal("}", actual.DocumentOnTypeFormattingProvider.FirstTriggerCharacter);
            Assert.Equal(new[] { ";", "\n" }, actual.DocumentOnTypeFormattingProvider.MoreTriggerCharacter);

            Assert.NotNull(actual.ExecuteCommandProvider);
        }
    }
}
