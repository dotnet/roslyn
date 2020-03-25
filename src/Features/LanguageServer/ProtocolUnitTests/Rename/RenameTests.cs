// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Definitions
{
    public class RenameTests : AbstractLanguageServerProtocolTests
    {
        [WpfFact]
        public async Task TestRenameAsync()
        {
            var markup =
@"class A
{
    void {|caret:|}{|renamed:M|}()
    {
    }
    void M2()
    {
        {|renamed:M|}()
    }
}";
            using var workspace = CreateTestWorkspace(markup, out var locations);
            var renameLocation = locations["caret"].First();
            var renameValue = "RENAME";
            var expectedEdits = locations["renamed"].Select(location => new LSP.TextEdit() { NewText = renameValue, Range = location.Range });

            var results = await RunRenameAsync(workspace.CurrentSolution, renameLocation, renameValue);
            AssertJsonEquals(expectedEdits, results.DocumentChanges.First().Edits);
        }

        private static LSP.RenameParams CreateRenameParams(LSP.Location location, string newName)
            => new LSP.RenameParams()
            {
                NewName = newName,
                Position = location.Range.Start,
                TextDocument = CreateTextDocumentIdentifier(location.Uri)
            };

        private static async Task<WorkspaceEdit> RunRenameAsync(Solution solution, LSP.Location renameLocation, string renamevalue)
           => await GetLanguageServer(solution).RenameAsync(solution, CreateRenameParams(renameLocation, renamevalue), new LSP.ClientCapabilities(), CancellationToken.None);
    }
}
