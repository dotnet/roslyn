﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;
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
            using var testLspServer = CreateTestLspServer(markup, out var locations);
            var renameLocation = locations["caret"].First();
            var renameValue = "RENAME";
            var expectedEdits = locations["renamed"].Select(location => new LSP.TextEdit() { NewText = renameValue, Range = location.Range });

            var results = await RunRenameAsync(testLspServer, CreateRenameParams(renameLocation, renameValue));
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

            using var testLspServer = CreateXmlTestLspServer(workspaceXml, out var locations);
            var renameLocation = locations["caret"].First();
            var renameValue = "RENAME";
            var expectedEdits = locations["renamed"].Select(location => new LSP.TextEdit() { NewText = renameValue, Range = location.Range });

            var results = await RunRenameAsync(testLspServer, CreateRenameParams(renameLocation, renameValue));
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

            using var testLspServer = CreateXmlTestLspServer(workspaceXml, out var locations);
            var renameLocation = locations["caret"].First();
            var renameValue = "RENAME";
            var expectedEdits = locations["renamed"].Select(location => new LSP.TextEdit() { NewText = renameValue, Range = location.Range });

            var results = await RunRenameAsync(testLspServer, CreateRenameParams(renameLocation, renameValue));
            AssertJsonEquals(expectedEdits, ((TextDocumentEdit[])results.DocumentChanges).First().Edits);
        }

        [WpfFact]
        public async Task TestRename_WithMappedFileAsync()
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
            using var testLspServer = CreateTestLspServer(string.Empty, out _);

            AddMappedDocument(testLspServer.TestWorkspace, markup);

            var startPosition = new LSP.Position { Line = 2, Character = 9 };
            var endPosition = new LSP.Position { Line = 2, Character = 10 };
            var renameText = "RENAME";
            var renameParams = CreateRenameParams(new LSP.Location
            {
                Uri = new Uri($"C:\\{TestSpanMapper.GeneratedFileName}"),
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
            return await testLspServer.ExecuteRequestAsync<LSP.RenameParams, LSP.WorkspaceEdit>(LSP.Methods.TextDocumentRenameName,
                          renameParams, new LSP.ClientCapabilities(), null, CancellationToken.None);
        }
    }
}
