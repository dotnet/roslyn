// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Rename
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
            AssertJsonEquals(expectedEdits, ((TextDocumentEdit[])results.DocumentChanges).First().Edits);
        }

        [WpfFact]
        public async Task TestRename_WithLinkedFilesAsync()
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

            var workspaceXml =
$@"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj"" PreprocessorSymbols=""Proj1"">
        <Document FilePath = ""C:\C.cs""><![CDATA[{markup}]]></Document>
    </Project>
    <Project Language = ""C#"" CommonReferences=""true"" PreprocessorSymbols=""Proj2"">
        <Document IsLinkFile = ""true"" LinkAssemblyName=""CSProj"" LinkFilePath=""C:\C.cs""/>
    </Project>
</Workspace>";

            using var workspace = CreateXmlTestWorkspace(workspaceXml, out var locations);
            var renameLocation = locations["caret"].First();
            var renameValue = "RENAME";
            var expectedEdits = locations["renamed"].Select(location => new LSP.TextEdit() { NewText = renameValue, Range = location.Range });

            var results = await RunRenameAsync(workspace.CurrentSolution, renameLocation, renameValue);
            AssertJsonEquals(expectedEdits, ((TextDocumentEdit[])results.DocumentChanges).First().Edits);
        }

        [WpfFact]
        public async Task TestRename_WithLinkedFilesAndPreprocessorAsync()
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
    void M3()
    {
#if Proj1
        {|renamed:M|}()
#endif
    }
    void M4()
    {
#if Proj2
        {|renamed:M|}()
#endif
    }
}";

            var workspaceXml =
$@"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj"" PreprocessorSymbols=""Proj1"">
        <Document FilePath = ""C:\C.cs""><![CDATA[{markup}]]></Document>
    </Project>
    <Project Language = ""C#"" CommonReferences=""true"" PreprocessorSymbols=""Proj2"">
        <Document IsLinkFile = ""true"" LinkAssemblyName=""CSProj"" LinkFilePath=""C:\C.cs""/>
    </Project>
</Workspace>";

            using var workspace = CreateXmlTestWorkspace(workspaceXml, out var locations);
            var renameLocation = locations["caret"].First();
            var renameValue = "RENAME";
            var expectedEdits = locations["renamed"].Select(location => new LSP.TextEdit() { NewText = renameValue, Range = location.Range });

            var results = await RunRenameAsync(workspace.CurrentSolution, renameLocation, renameValue);
            AssertJsonEquals(expectedEdits, ((TextDocumentEdit[])results.DocumentChanges).First().Edits);
        }

        private static LSP.RenameParams CreateRenameParams(LSP.Location location, string newName)
            => new LSP.RenameParams()
            {
                NewName = newName,
                Position = location.Range.Start,
                TextDocument = CreateTextDocumentIdentifier(location.Uri)
            };

        private static async Task<WorkspaceEdit> RunRenameAsync(Solution solution, LSP.Location renameLocation, string renamevalue)
           => await GetLanguageServer(solution).ExecuteRequestAsync<LSP.RenameParams, LSP.WorkspaceEdit>(LSP.Methods.TextDocumentRenameName,
               CreateRenameParams(renameLocation, renamevalue), new LSP.ClientCapabilities(), null, CancellationToken.None);
    }
}
