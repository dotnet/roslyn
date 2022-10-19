// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Miscellaneous;

public class LspMiscellaneousFilesWorkspaceTests : AbstractLanguageServerProtocolTests
{
    [Fact]
    public async Task TestLooseFile_Opened()
    {
        var source =
@"class A
{
    void M()
    {
    }
}";

        // Create the server and verify no misc files present.
        using var testLspServer = await CreateTestLspServerAsync(string.Empty);
        Assert.Null(GetMiscellaneousDocument(testLspServer));

        // Open a loose file and verify it gets added to the misc workspace.
        var looseFileUri = new Uri(@"C:\SomeFile.cs");
        await testLspServer.OpenDocumentAsync(looseFileUri, source).ConfigureAwait(false);
        Assert.Equal(looseFileUri.AbsolutePath, GetMiscellaneousDocument(testLspServer)!.FilePath);

        // Verify requests succeed against the loose file.
        var caret = new LSP.Location
        {
            Range = new LSP.Range { Start = new LSP.Position { Character = 6, Line = 0 }, End = new LSP.Position { Character = 7, Line = 0 } },
            Uri = looseFileUri,
        };
        var hover = await RunGetHoverAsync(testLspServer, caret).ConfigureAwait(false);
        Assert.Contains("class A", hover.Contents.Third.Value);
    }

    [Fact]
    public async Task TestLooseFile_Changed()
    {
        var source =
@"class A
{
    void M()
    {
    }
}";

        using var testLspServer = await CreateTestLspServerAsync(string.Empty);
        Assert.Null(GetMiscellaneousDocument(testLspServer));

        var looseFileUri = new Uri(@"C:\SomeFile.cs");

        // Open an empty loose file and verify it gets added to the misc workspace.
        await testLspServer.OpenDocumentAsync(looseFileUri, string.Empty).ConfigureAwait(false);
        var docText = await GetMiscellaneousDocument(testLspServer)!.GetTextAsync(CancellationToken.None).ConfigureAwait(false);
        Assert.Equal(string.Empty, docText.ToString());

        // Make a text change to the loose file and verify requests appropriately reflect the changes.
        await testLspServer.InsertTextAsync(looseFileUri, (0, 0, source)).ConfigureAwait(false);
        var caret = new LSP.Location
        {
            Range = new LSP.Range { Start = new LSP.Position { Character = 6, Line = 0 }, End = new LSP.Position { Character = 7, Line = 0 } },
            Uri = looseFileUri,
        };
        var hover = await RunGetHoverAsync(testLspServer, caret).ConfigureAwait(false);
        Assert.Contains("class A", hover.Contents.Third.Value);
    }

    [Fact]
    public async Task TestLooseFile_Closed()
    {
        var source =
@"class A
{
    void M()
    {
    }
}";

        // Create the server and verify no misc files present.
        using var testLspServer = await CreateTestLspServerAsync(string.Empty);
        Assert.Null(GetMiscellaneousDocument(testLspServer));

        // Open a loose file and verify it gets added to the misc workspace.
        var looseFileUri = new Uri(@"C:\SomeFile.cs");
        await testLspServer.OpenDocumentAsync(looseFileUri, source).ConfigureAwait(false);
        Assert.Equal(looseFileUri.AbsolutePath, GetMiscellaneousDocument(testLspServer)!.FilePath);

        // Verify the loose file is removed from the misc workspace on close.
        await testLspServer.CloseDocumentAsync(looseFileUri).ConfigureAwait(false);
        Assert.Null(GetMiscellaneousDocument(testLspServer));
    }

    [Fact]
    public async Task TestFileInRegisteredWorkspace_Opened()
    {
        var markup =
@"class A
{
    void M()
    {
    }
}";

        // Create the server and verify no misc files present.
        using var testLspServer = await CreateTestLspServerAsync(markup);
        Assert.Null(GetMiscellaneousDocument(testLspServer));

        // Open a file that is part of a registered workspace and verify it is not present in the misc workspace.
        var fileInWorkspaceUri = new Uri(testLspServer.GetCurrentSolution().Projects.Single().Documents.Single().FilePath);
        await testLspServer.OpenDocumentAsync(fileInWorkspaceUri).ConfigureAwait(false);
        Assert.Null(GetMiscellaneousDocument(testLspServer));
    }

    [Fact]
    public async Task TestLooseFile_MovedToRegisteredWorkspace()
    {
        var source =
@"class A
{
    void M()
    {
    }
}";

        // Create the server and verify no misc files present.
        using var testLspServer = await CreateTestLspServerAsync(string.Empty);
        Assert.Null(GetMiscellaneousDocument(testLspServer));

        // Open a loose file and verify it gets added to the misc workspace.
        var looseFileUri = new Uri(@"C:\SomeFile.cs");
        await testLspServer.OpenDocumentAsync(looseFileUri, source).ConfigureAwait(false);
        Assert.Equal(looseFileUri.AbsolutePath, GetMiscellaneousDocument(testLspServer)!.FilePath);

        // Verify requests succeed against the loose file.
        var caret = new LSP.Location
        {
            Range = new LSP.Range { Start = new LSP.Position { Character = 6, Line = 0 }, End = new LSP.Position { Character = 7, Line = 0 } },
            Uri = looseFileUri,
        };
        var hover = await RunGetHoverAsync(testLspServer, caret).ConfigureAwait(false);
        Assert.Contains("class A", hover.Contents.Third.Value);

        // Update the workspace to contain the loose file.
        var project = testLspServer.GetCurrentSolution().Projects.Single();
        var documentInfo = DocumentInfo.Create(
                DocumentId.CreateNewId(project.Id),
                looseFileUri.AbsolutePath,
                sourceCodeKind: SourceCodeKind.Regular,
                loader: new TestTextLoader(source),
                filePath: looseFileUri.AbsolutePath);
        testLspServer.TestWorkspace.OnDocumentAdded(documentInfo);
        await WaitForWorkspaceOperationsAsync(testLspServer.TestWorkspace);

        Assert.Contains(looseFileUri.AbsolutePath, testLspServer.GetCurrentSolution().Projects.Single().Documents.Select(d => d.FilePath));

        // Make a change and verify that the file is no longer present in the misc workspace.
        await testLspServer.InsertTextAsync(looseFileUri, (0, 6, "B")).ConfigureAwait(false);
        Assert.Null(GetMiscellaneousDocument(testLspServer));

        // Verify the change is reflected in subsequent requests.
        caret = new LSP.Location
        {
            Range = new LSP.Range { Start = new LSP.Position { Character = 6, Line = 0 }, End = new LSP.Position { Character = 8, Line = 0 } },
            Uri = looseFileUri,
        };
        hover = await RunGetHoverAsync(testLspServer, caret).ConfigureAwait(false);
        Assert.Contains("class BA", hover.Contents.Third.Value);
    }

    private static Document? GetMiscellaneousDocument(TestLspServer testLspServer)
    {
        return testLspServer.GetManagerAccessor().GetLspMiscellaneousFilesWorkspace()!.CurrentSolution.Projects.SingleOrDefault()?.Documents.Single();
    }

    private static async Task<LSP.Hover> RunGetHoverAsync(TestLspServer testLspServer, LSP.Location caret)
    {
        var result = await testLspServer.ExecuteRequestAsync<LSP.TextDocumentPositionParams, LSP.Hover>(LSP.Methods.TextDocumentHoverName,
            CreateTextDocumentPositionParams(caret), CancellationToken.None);
        Contract.ThrowIfNull(result);
        return result;
    }
}
