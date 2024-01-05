// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Rename
{
    public class RenameTests : AbstractLanguageServerProtocolTests
    {
        public RenameTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [WpfTheory, CombinatorialData]
        public async Task TestRenameAsync(bool mutatingLspWorkspace)
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
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
            var renameLocation = testLspServer.GetLocations("caret").First();
            var renameValue = "RENAME";
            var expectedEdits = testLspServer.GetLocations("renamed").Select(location => new LSP.TextEdit() { NewText = renameValue, Range = location.Range });

            var results = await RunRenameAsync(testLspServer, CreateRenameParams(renameLocation, renameValue));
            AssertJsonEquals(expectedEdits, ((TextDocumentEdit[])results.DocumentChanges).First().Edits);
        }

        [WpfTheory, CombinatorialData]
        public async Task TestRename_InvalidIdentifierAsync(bool mutatingLspWorkspace)
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
            await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
            var renameLocation = testLspServer.GetLocations("caret").First();
            var renameValue = "$RENAMED$";

            var results = await RunRenameAsync(testLspServer, CreateRenameParams(renameLocation, renameValue));
            Assert.Null(results);
        }

        [WpfTheory, CombinatorialData]
        public async Task TestRename_WithLinkedFilesAsync(bool mutatingLspWorkspace)
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

            await using var testLspServer = await CreateXmlTestLspServerAsync(workspaceXml, mutatingLspWorkspace);
            var renameLocation = testLspServer.GetLocations("caret").First();
            var renameValue = "RENAME";
            var expectedEdits = testLspServer.GetLocations("renamed").Select(location => new LSP.TextEdit() { NewText = renameValue, Range = location.Range });

            var results = await RunRenameAsync(testLspServer, CreateRenameParams(renameLocation, renameValue));
            AssertJsonEquals(expectedEdits, ((TextDocumentEdit[])results.DocumentChanges).First().Edits);
        }

        [WpfTheory, CombinatorialData]
        public async Task TestRename_WithLinkedFilesAndPreprocessorAsync(bool mutatingLspWorkspace)
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

            await using var testLspServer = await CreateXmlTestLspServerAsync(workspaceXml, mutatingLspWorkspace);
            var renameLocation = testLspServer.GetLocations("caret").First();
            var renameValue = "RENAME";
            var expectedEdits = testLspServer.GetLocations("renamed").Select(location => new LSP.TextEdit() { NewText = renameValue, Range = location.Range });

            var results = await RunRenameAsync(testLspServer, CreateRenameParams(renameLocation, renameValue));
            AssertJsonEquals(expectedEdits, ((TextDocumentEdit[])results.DocumentChanges).First().Edits);
        }

        [WpfTheory, CombinatorialData]
        public async Task TestRename_WithMappedFileAsync(bool mutatingLspWorkspace)
        {
            var markup =
@"class A
{
    void M()
    {
    }
    void M2()
    {
        M()
    }
}";
            await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace);

            AddMappedDocument(testLspServer.TestWorkspace, markup);

            var startPosition = new LSP.Position { Line = 2, Character = 9 };
            var endPosition = new LSP.Position { Line = 2, Character = 10 };
            var renameText = "RENAME";
            var renameParams = CreateRenameParams(new LSP.Location
            {
                Uri = ProtocolConversions.CreateAbsoluteUri($"C:\\{TestSpanMapper.GeneratedFileName}"),
                Range = new LSP.Range { Start = startPosition, End = endPosition }
            }, "RENAME");

            var results = await RunRenameAsync(testLspServer, renameParams);

            // There are two rename locations, so we expect two mapped locations.
            var expectedMappedRanges = ImmutableArray.Create(TestSpanMapper.MappedFileLocation.Range, TestSpanMapper.MappedFileLocation.Range);
            var expectedMappedDocument = TestSpanMapper.MappedFileLocation.Uri;

            var documentEdit = results.DocumentChanges.Value.First.Single();
            Assert.Equal(expectedMappedDocument, documentEdit.TextDocument.Uri);
            Assert.Equal(expectedMappedRanges, documentEdit.Edits.Select(edit => edit.Range));
            Assert.True(documentEdit.Edits.All(edit => edit.NewText == renameText));
        }

        private static LSP.RenameParams CreateRenameParams(LSP.Location location, string newName)
            => new LSP.RenameParams()
            {
                NewName = newName,
                Position = location.Range.Start,
                TextDocument = CreateTextDocumentIdentifier(location.Uri)
            };

        private static async Task<WorkspaceEdit> RunRenameAsync(TestLspServer testLspServer, LSP.RenameParams renameParams)
        {
            return await testLspServer.ExecuteRequestAsync<LSP.RenameParams, LSP.WorkspaceEdit>(LSP.Methods.TextDocumentRenameName, renameParams, CancellationToken.None);
        }
    }
}
