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

        // Create a server that supports LSP misc files and verify no misc files present.
        using var testLspServer = await CreateTestLspServerAsync(string.Empty, new LSP.ClientCapabilities(), WellKnownLspServerKinds.CSharpVisualBasicLspServer);
        Assert.Null(GetMiscellaneousDocument(testLspServer));

        // Open an empty loose file and make a request to verify it gets added to the misc workspace.
        var looseFileUri = new Uri(@"C:\SomeFile.cs");
        await testLspServer.OpenDocumentAsync(looseFileUri, source).ConfigureAwait(false);

        // Verify file is added to the misc file workspace.
        await AssertFileInMiscWorkspaceAsync(testLspServer, looseFileUri).ConfigureAwait(false);
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

        // Create a server that supports LSP misc files and verify no misc files present.
        using var testLspServer = await CreateTestLspServerAsync(string.Empty, new LSP.ClientCapabilities(), WellKnownLspServerKinds.CSharpVisualBasicLspServer);
        Assert.Null(GetMiscellaneousDocument(testLspServer));

        var looseFileUri = new Uri(@"C:\SomeFile.cs");

        // Open an empty loose file and make a request to verify it gets added to the misc workspace.
        await testLspServer.OpenDocumentAsync(looseFileUri, string.Empty).ConfigureAwait(false);
        await AssertFileInMiscWorkspaceAsync(testLspServer, looseFileUri).ConfigureAwait(false);

        // Make a text change to the loose file and verify requests appropriately reflect the changes.
        await testLspServer.InsertTextAsync(looseFileUri, (0, 0, source)).ConfigureAwait(false);
        var caret = new LSP.Location { Range = new() { Start = new(0, 6), End = new(0, 7) }, Uri = looseFileUri };
        var hover = await RunGetHoverAsync(testLspServer, caret).ConfigureAwait(false);
        Assert.Contains("class A", hover.Contents.Third.Value);
        await AssertFileInMiscWorkspaceAsync(testLspServer, looseFileUri).ConfigureAwait(false);
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

        // Create a server that supports LSP misc files and verify no misc files present.
        using var testLspServer = await CreateTestLspServerAsync(string.Empty, new LSP.ClientCapabilities(), WellKnownLspServerKinds.CSharpVisualBasicLspServer);
        Assert.Null(GetMiscellaneousDocument(testLspServer));

        // Open an empty loose file and make a request to verify it gets added to the misc workspace.
        var looseFileUri = new Uri(@"C:\SomeFile.cs");
        await testLspServer.OpenDocumentAsync(looseFileUri, source).ConfigureAwait(false);
        await AssertFileInMiscWorkspaceAsync(testLspServer, looseFileUri).ConfigureAwait(false);

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

        // Create a server that supports LSP misc files and verify no misc files present.
        using var testLspServer = await CreateTestLspServerAsync(markup, new LSP.ClientCapabilities(), WellKnownLspServerKinds.CSharpVisualBasicLspServer);
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

        // Create a server that supports LSP misc files and verify no misc files present.
        using var testLspServer = await CreateTestLspServerAsync(string.Empty, new LSP.ClientCapabilities(), WellKnownLspServerKinds.CSharpVisualBasicLspServer);
        Assert.Null(GetMiscellaneousDocument(testLspServer));

        // Open an empty loose file and make a request to verify it gets added to the misc workspace.
        var looseFileUri = new Uri(@"C:\SomeFile.cs");
        var looseFileTextDocumentIdentifier = new LSP.TextDocumentIdentifier { Uri = looseFileUri };
        await testLspServer.OpenDocumentAsync(looseFileUri, source).ConfigureAwait(false);

        // Verify that the file returned by the manager is in the lsp misc files workspace.
        await AssertFileInMiscWorkspaceAsync(testLspServer, looseFileUri).ConfigureAwait(false);

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

        // Verify that the manager returns the file that has been added to the main workspace.
        await AssertFileInMainWorkspaceAsync(testLspServer, looseFileUri).ConfigureAwait(false);
    }

    [Fact]
    public async Task TestLooseFile_DoesNotSupportLspMiscFiles()
    {
        var source =
@"class A
{
    void M()
    {
    }
}";

        // Create a server that doesn't use the LSP misc files workspace.
        using var testLspServer = await CreateTestLspServerAsync(string.Empty, new LSP.ClientCapabilities(), WellKnownLspServerKinds.AlwaysActiveVSLspServer);
        Assert.Null(testLspServer.GetManagerAccessor().GetLspMiscellaneousFilesWorkspace());

        // Open an empty loose file and make a request to verify it gets added to the misc workspace.
        var looseFileUri = new Uri(@"C:\SomeFile.cs");
        await testLspServer.OpenDocumentAsync(looseFileUri, source).ConfigureAwait(false);

        // Verify the request on the loose file fails.
        var lspDocument = await testLspServer.GetManager().GetLspDocumentAsync(new LSP.TextDocumentIdentifier { Uri = looseFileUri }, CancellationToken.None).ConfigureAwait(false);
        Assert.Null(lspDocument);
    }

    private static async Task AssertFileInMiscWorkspaceAsync(TestLspServer testLspServer, Uri fileUri)
    {
        var lspDocument = await testLspServer.GetManager().GetLspDocumentAsync(new LSP.TextDocumentIdentifier { Uri = fileUri }, CancellationToken.None);
        Assert.Equal(testLspServer.GetManagerAccessor().GetLspMiscellaneousFilesWorkspace(), lspDocument!.Project.Solution.Workspace);
    }

    private static async Task AssertFileInMainWorkspaceAsync(TestLspServer testLspServer, Uri fileUri)
    {
        var lspDocument = await testLspServer.GetManager().GetLspDocumentAsync(new LSP.TextDocumentIdentifier { Uri = fileUri }, CancellationToken.None).ConfigureAwait(false);
        Assert.Equal(testLspServer.TestWorkspace, lspDocument!.Project.Solution.Workspace);

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
