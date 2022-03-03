// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.LanguageServer.UnitTests.Completion;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Workspaces;

public class LspWorkspaceManagerTests : AbstractLanguageServerProtocolTests
{
    [Fact]
    public async Task TestForksOnDidOpenAndDidCloseAsync()
    {
        var markup = "";
        using var testLspServer = await CreateTestLspServerAsync(markup);
        var documentUri = testLspServer.GetCurrentSolution().Projects.First().Documents.First().GetURI();

        // Verify that the workspace is registered with no lsp solution.
        Assert.Null(GetManagerWorkspaceState(testLspServer.TestWorkspace, testLspServer));

        await testLspServer.OpenDocumentAsync(documentUri, "LSP text");

        // Assert that the solution is forked with the new text on open.
        var forkedSolution = GetManagerWorkspaceState(testLspServer.TestWorkspace, testLspServer);
        AssertEx.NotNull(forkedSolution);
        Assert.NotEqual(testLspServer.TestWorkspace.CurrentSolution, forkedSolution);

        var lspDocument = GetLspDocument(documentUri, testLspServer);
        AssertEx.NotNull(lspDocument);
        Assert.Equal("LSP text", (await lspDocument.GetTextAsync(CancellationToken.None)).ToString());

        // Verify LSP text changes are reflected in the opened document.
        await testLspServer.InsertTextAsync(documentUri, (0, 0, "More text"));
        lspDocument = GetLspDocument(documentUri, testLspServer);
        AssertEx.NotNull(lspDocument);
        Assert.Equal("More textLSP text", (await lspDocument.GetTextAsync(CancellationToken.None)).ToString());

        // Close the document in LSP and verify all LSP tracked changes are now gone.
        // The document should be reset to the workspace's state.
        await testLspServer.CloseDocumentAsync(documentUri);
        var newSolution = GetManagerWorkspaceState(testLspServer.TestWorkspace, testLspServer);
        Assert.Null(newSolution);
        Assert.Equal(testLspServer.GetCurrentSolution(), GetLspDocument(documentUri, testLspServer)!.Project.Solution);
    }

    [Fact]
    public async Task TestDoesNotForkAllOpenDocumentsOnDidChangeAsync()
    {
        var markupOne = "One";
        var markupTwo = "Two";
        using var testLspServer = await CreateTestLspServerAsync(new string[] { markupOne, markupTwo });
        var firstDocumentUri = testLspServer.GetCurrentSolution().Projects.First().Documents.Single(d => d.FilePath!.Contains("test1")).GetURI();
        var secondDocumentUri = testLspServer.GetCurrentSolution().Projects.First().Documents.Single(d => d.FilePath!.Contains("test2")).GetURI();

        var firstDocument = await OpenDocumentAndVerifyLspTextAsync(firstDocumentUri, testLspServer);
        var secondDocument = await OpenDocumentAndVerifyLspTextAsync(secondDocumentUri, testLspServer);
        var firstDocumentInitialVersion = await firstDocument.GetSyntaxVersionAsync(CancellationToken.None);
        var secondDocumentInitialVersion = await secondDocument.GetSyntaxVersionAsync(CancellationToken.None);

        // Make a text change in one of the opened documents.
        await testLspServer.InsertTextAsync(firstDocumentUri, (0, 0, "Some more text"));

        var firstDocumentWithChange = GetLspDocument(firstDocumentUri, testLspServer);
        var secondDocumentUnchanged = GetLspDocument(secondDocumentUri, testLspServer);
        AssertEx.NotNull(firstDocumentWithChange);
        AssertEx.NotNull(secondDocumentUnchanged);

        // Verify that the document that we inserted text into had a version change.
        Assert.NotEqual(firstDocumentInitialVersion, await firstDocumentWithChange.GetSyntaxVersionAsync(CancellationToken.None));
        Assert.Equal("Some more textLSP text", (await firstDocumentWithChange.GetTextAsync(CancellationToken.None)).ToString());

        // Verify that the document that we did not change still has the same version.
        Assert.Equal(secondDocumentInitialVersion, await secondDocumentUnchanged.GetSyntaxVersionAsync(CancellationToken.None));
    }

    [Fact]
    public async Task TestForksOnClosedDocumentChangesChangesAsync()
    {
        var markupOne = "One";
        var markupTwo = "Two";
        using var testLspServer = await CreateTestLspServerAsync(new string[] { markupOne, markupTwo });
        var firstDocumentUri = testLspServer.GetCurrentSolution().Projects.First().Documents.Single(d => d.FilePath!.Contains("test1")).GetURI();

        var secondDocument = testLspServer.GetCurrentSolution().Projects.First().Documents.Single(d => d.FilePath!.Contains("test2"));
        var secondDocumentUri = secondDocument.GetURI();

        // Open one of the documents via LSP and verify we have created our LSP solution.
        await OpenDocumentAndVerifyLspTextAsync(firstDocumentUri, testLspServer);

        // Modify a closed document via the workspace.
        await testLspServer.TestWorkspace.ChangeDocumentAsync(secondDocument.Id, SourceText.From("Two is now three!"));

        // Assert that the LSP incremental solution is cleared.
        var changedSolution = GetManagerWorkspaceState(testLspServer.TestWorkspace, testLspServer);
        Assert.Null(changedSolution);

        // Verify that the LSP solution has the LSP text from the open document.
        var openedDocument = GetLspDocument(firstDocumentUri, testLspServer);
        AssertEx.NotNull(openedDocument);
        Assert.Equal("LSP text", (await openedDocument.GetTextAsync(CancellationToken.None)).ToString());

        // Verify that the LSP solution has the workspace text in the closed document.
        secondDocument = GetLspDocument(secondDocumentUri, testLspServer);
        AssertEx.NotNull(secondDocument);
        Assert.Equal("Two is now three!", (await secondDocument.GetTextAsync()).ToString());
    }

    [Fact]
    public async Task TestForksOnProjectChangesAsync()
    {
        var markup = "One";
        using var testLspServer = await CreateTestLspServerAsync(markup);
        var documentUri = testLspServer.GetCurrentSolution().Projects.First().Documents.Single(d => d.FilePath!.Contains("test1")).GetURI();

        // Open the document via LSP and verify the initial project name.
        var openedDocument = await OpenDocumentAndVerifyLspTextAsync(documentUri, testLspServer);
        Assert.Equal("Test", openedDocument?.Project.AssemblyName);

        // Modify the project via the workspace.
        var newProject = testLspServer.TestWorkspace.CurrentSolution.Projects.First().WithAssemblyName("NewCSProj1");
        await testLspServer.TestWorkspace.ChangeProjectAsync(newProject.Id, newProject.Solution);

        // Assert that the LSP incremental solution is cleared.
        var changedSolution = GetManagerWorkspaceState(testLspServer.TestWorkspace, testLspServer);
        Assert.Null(changedSolution);

        // Verify that the new LSP solution has the updated project info.
        openedDocument = GetLspDocument(documentUri, testLspServer);
        AssertEx.NotNull(openedDocument);
        Assert.Equal("LSP text", (await openedDocument.GetTextAsync(CancellationToken.None)).ToString());
        Assert.Equal("NewCSProj1", openedDocument.Project.AssemblyName);
    }

    [Fact]
    public async Task TestDoesNotForkOnOpenDocumentWorkspaceEventAsync()
    {
        var markup = "One";
        using var testLspServer = await CreateTestLspServerAsync(markup);
        var firstDocumentUri = testLspServer.GetCurrentSolution().Projects.First().Documents.Single(d => d.FilePath!.Contains("test1")).GetURI();

        // Open the document via LSP to create the initial LSP solution.
        var openedDocument = await OpenDocumentAndVerifyLspTextAsync(firstDocumentUri, testLspServer);

        // Modify the currently open LSP document via the workspace.
        await testLspServer.TestWorkspace.ChangeDocumentAsync(openedDocument.Id, SourceText.From("New One"));

        // Assert that the LSP incremental solution is unchanged (with the original LSP text and not workspace text).
        var lspDocument = GetLspDocument(firstDocumentUri, testLspServer);
        Assert.Equal(openedDocument, lspDocument);
    }

    [Fact]
    public async Task TestForksEventuallyWithDelayedWorkspaceEventAsync()
    {
        var markup = "One";
        using var testLspServer = await CreateTestLspServerAsync(markup);
        var documentUri = testLspServer.GetCurrentSolution().Projects.First().Documents.Single(d => d.FilePath!.Contains("test1")).GetURI();

        // Open the document via LSP and ensure it has the original assembly name.
        var lspDocument = await OpenDocumentAndVerifyLspTextAsync(documentUri, testLspServer);
        Assert.Equal("Test", lspDocument?.Project.AssemblyName);

        // Modify the project via the workspace.
        var newProject = testLspServer.TestWorkspace.CurrentSolution.Projects.First().WithAssemblyName("NewCSProj1");
        testLspServer.TestWorkspace.TryApplyChanges(newProject.Solution);

        // Assert that the LSP incremental solution is still present since we have not heard the workspace event.
        lspDocument = GetLspDocument(documentUri, testLspServer);
        AssertEx.NotNull(lspDocument);
        Assert.Equal("Test", lspDocument.Project.AssemblyName);

        // Actually send the project changed event.
        await testLspServer.TestWorkspace.ChangeProjectAsync(newProject.Id, newProject.Solution);

        // Assert that the LSP incremental solution is cleared.
        Assert.Null(GetManagerWorkspaceState(testLspServer.TestWorkspace, testLspServer));

        // Verify that the project change event is reflected in LSP.
        lspDocument = GetLspDocument(documentUri, testLspServer);
        AssertEx.NotNull(lspDocument);
        Assert.Equal("NewCSProj1", lspDocument.Project.AssemblyName);
    }

    [Fact]
    public async Task TestDidOpenFindsAddedWorkspaceDocumentAsync()
    {
        var markup = "One";
        using var testLspServer = await CreateTestLspServerAsync(markup);
        var documentUri = testLspServer.GetCurrentSolution().Projects.First().Documents.Single(d => d.FilePath!.Contains("test1")).GetURI();

        // Open the document via LSP to create the initial LSP solution.
        await OpenDocumentAndVerifyLspTextAsync(documentUri, testLspServer);

        // Add a new document to the workspace, but do not send document added event.
        var newDocumentId = DocumentId.CreateNewId(testLspServer.TestWorkspace.CurrentSolution.ProjectIds[0]);
        var newSolution = testLspServer.TestWorkspace.CurrentSolution.AddDocument(newDocumentId, "NewDoc.cs", SourceText.From("New Doc"), filePath: @"C:\NewDoc.cs");
        var newDocumentUri = newSolution.GetRequiredDocument(newDocumentId).GetURI();
        testLspServer.TestWorkspace.TryApplyChanges(newSolution);

        // Verify that the lsp server forks again from the workspace and picks up the new document in the correct workspace on document open.
        await testLspServer.OpenDocumentAsync(newDocumentUri);
        var lspDocument = GetLspDocument(newDocumentUri, testLspServer);
        AssertEx.NotNull(lspDocument);
        Assert.Equal(testLspServer.TestWorkspace, lspDocument.Project.Solution.Workspace);
    }

    [Fact]
    public async Task TestDocumentOpenedBeforeAddedToWorkspaceAsync()
    {
        var markup = "One";
        using var testLspServer = await CreateTestLspServerAsync(markup);

        // Create a new document, but do not update the workspace solution yet.
        var newDocumentId = DocumentId.CreateNewId(testLspServer.TestWorkspace.CurrentSolution.ProjectIds[0]);
        var newDocumentFilePath = @"C:/NewDoc.cs";
        var newDocumentInfo = DocumentInfo.Create(newDocumentId, "NewDoc.cs", filePath: newDocumentFilePath, loader: new TestTextLoader("New Doc"));
        var newDocumentUri = ProtocolConversions.GetUriFromFilePath(newDocumentFilePath);

        // Open the document via LSP before the workspace sees it.
        await testLspServer.OpenDocumentAsync(newDocumentUri, "LSP text");

        // Verify it is in the lsp misc workspace.
        var miscDocument = GetLspDocument(newDocumentUri, testLspServer);
        AssertEx.NotNull(miscDocument);
        Assert.Equal(testLspServer.GetManagerAccessor().GetLspMiscellaneousFilesWorkspace(), miscDocument.Project.Solution.Workspace);
        Assert.Equal("LSP text", (await miscDocument.GetTextAsync(CancellationToken.None)).ToString());

        // Make a change and verify the misc document is updated.
        await testLspServer.InsertTextAsync(newDocumentUri, (0, 0, "More LSP text"));
        miscDocument = GetLspDocument(newDocumentUri, testLspServer);
        AssertEx.NotNull(miscDocument);
        var miscText = await miscDocument.GetTextAsync(CancellationToken.None);
        Assert.Equal("More LSP textLSP text", miscText.ToString());

        // Update the registered workspace with the new document.
        await testLspServer.TestWorkspace.AddDocumentAsync(newDocumentInfo);

        // Verify a fork was triggered.
        Assert.Null(GetManagerWorkspaceState(testLspServer.TestWorkspace, testLspServer));

        // Verify that the newly added document in the registered workspace is returned.
        var document = GetLspDocument(newDocumentUri, testLspServer);
        AssertEx.NotNull(document);
        Assert.Equal(testLspServer.TestWorkspace, document.Project.Solution.Workspace);
        Assert.Equal(newDocumentId, document.Id);
        // Verify we still are using the tracked LSP text for the document.
        var documentText = await document.GetTextAsync(CancellationToken.None);
        Assert.Equal("More LSP textLSP text", documentText.ToString());
    }

    [Fact]
    public async Task TestUsesRegisteredHostWorkspace()
    {
        var firstWorkspaceXml =
@$"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""FirstWorkspaceProject"">
        <Document FilePath=""C:\FirstWorkspace.cs"">FirstWorkspace</Document>
    </Project>
</Workspace>";

        var secondWorkspaceXml =
@$"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""SecondWorkspaceProject"">
        <Document FilePath=""C:\SecondWorkspace.cs"">SecondWorkspace</Document>
    </Project>
</Workspace>";

        using var testLspServer = await CreateXmlTestLspServerAsync(firstWorkspaceXml);
        // Verify 1 workspace registered to start with.
        Assert.Equal(1, testLspServer.GetManagerAccessor().GetWorkspaceState().Count);

        var exportProvider = testLspServer.TestWorkspace.ExportProvider;

        using var testWorkspaceTwo = TestWorkspace.Create(
            XElement.Parse(secondWorkspaceXml),
            workspaceKind: "OtherWorkspaceKind",
            exportProvider: exportProvider);

        // Wait for workspace creation operations for the second workspace to complete.
        await WaitForWorkspaceOperationsAsync(testWorkspaceTwo);

        // Manually register the workspace since the workspace listener does not listen for this workspace kind.
        var workspaceRegistrationService = exportProvider.GetExport<LspWorkspaceRegistrationService>();
        workspaceRegistrationService.Value.Register(testWorkspaceTwo);

        // Verify both workspaces registered.
        Assert.Null(GetManagerWorkspaceState(testLspServer.TestWorkspace, testLspServer));
        Assert.Null(GetManagerWorkspaceState(testWorkspaceTwo, testLspServer));

        // Verify the host workspace returned is the workspace with kind host.
        var hostSolution = GetLspHostSolution(testLspServer);
        AssertEx.NotNull(hostSolution);
        Assert.Equal("FirstWorkspaceProject", hostSolution.Projects.First().Name);
    }

    [Fact]
    public async Task TestWorkspaceRequestFailsWhenHostWorkspaceMissing()
    {
        var firstWorkspaceXml =
@$"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""FirstWorkspaceProject"">
        <Document FilePath=""C:\FirstWorkspace.cs"">FirstWorkspace</Document>
    </Project>
</Workspace>";

        using var testLspServer = await CreateXmlTestLspServerAsync(firstWorkspaceXml, workspaceKind: WorkspaceKind.MiscellaneousFiles);
        var exportProvider = testLspServer.TestWorkspace.ExportProvider;

        var workspaceRegistrationService = exportProvider.GetExport<LspWorkspaceRegistrationService>();
        Assert.Equal(WorkspaceKind.Host, workspaceRegistrationService.Value.GetHostWorkspaceKind());

        // Verify the workspace is registered.
        Assert.Null(GetManagerWorkspaceState(testLspServer.TestWorkspace, testLspServer));

        // Verify there is not workspace matching the host workspace kind.
        Assert.Null(GetLspHostSolution(testLspServer));
    }

    [Fact]
    public async Task TestLspUpdatesCorrectWorkspaceWithMultipleWorkspacesAsync()
    {
        var firstWorkspaceXml =
@$"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""FirstWorkspaceProject"">
        <Document FilePath=""C:\FirstWorkspace.cs"">FirstWorkspace</Document>
    </Project>
</Workspace>";

        var secondWorkspaceXml =
@$"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""SecondWorkspaceProject"">
        <Document FilePath=""C:\SecondWorkspace.cs"">SecondWorkspace</Document>
    </Project>
</Workspace>";

        using var testLspServer = await CreateXmlTestLspServerAsync(firstWorkspaceXml);
        var exportProvider = testLspServer.TestWorkspace.ExportProvider;

        using var testWorkspaceTwo = TestWorkspace.Create(
            XElement.Parse(secondWorkspaceXml),
            workspaceKind: WorkspaceKind.MSBuild,
            exportProvider: exportProvider);

        // Wait for workspace creation operations to complete for the second workspace.
        await WaitForWorkspaceOperationsAsync(testWorkspaceTwo);

        // Verify both workspaces registered.
        Assert.Null(GetManagerWorkspaceState(testLspServer.TestWorkspace, testLspServer));
        Assert.Null(GetManagerWorkspaceState(testWorkspaceTwo, testLspServer));

        var firstWorkspaceDocumentUri = ProtocolConversions.GetUriFromFilePath(@"C:\FirstWorkspace.cs");
        var secondWorkspaceDocumentUri = ProtocolConversions.GetUriFromFilePath(@"C:\SecondWorkspace.cs");
        await testLspServer.OpenDocumentAsync(firstWorkspaceDocumentUri);

        // Verify both workspaces forked on document open.
        Assert.NotNull(GetManagerWorkspaceState(testLspServer.TestWorkspace, testLspServer));
        Assert.NotNull(GetManagerWorkspaceState(testWorkspaceTwo, testLspServer));

        // Verify we can get both documents from their respective workspaces.
        var firstDocument = GetLspDocument(firstWorkspaceDocumentUri, testLspServer);
        AssertEx.NotNull(firstDocument);
        Assert.Equal(firstWorkspaceDocumentUri, firstDocument.GetURI());
        Assert.Equal(testLspServer.TestWorkspace, firstDocument.Project.Solution.Workspace);

        var secondDocument = GetLspDocument(secondWorkspaceDocumentUri, testLspServer);
        AssertEx.NotNull(secondDocument);
        Assert.Equal(secondWorkspaceDocumentUri, secondDocument.GetURI());
        Assert.Equal(testWorkspaceTwo, secondDocument.Project.Solution.Workspace);

        // Verify making an LSP change only changes the respective workspace and document.
        await testLspServer.InsertTextAsync(firstWorkspaceDocumentUri, (0, 0, "Change in first workspace"));

        // The first document should now different text.
        var changedFirstDocument = GetLspDocument(firstWorkspaceDocumentUri, testLspServer);
        AssertEx.NotNull(changedFirstDocument);
        var changedFirstDocumentText = await changedFirstDocument.GetTextAsync(CancellationToken.None);
        var firstDocumentText = await firstDocument.GetTextAsync(CancellationToken.None);
        Assert.NotEqual(firstDocumentText, changedFirstDocumentText);

        // The second document should return the same document instance since it was not changed.
        var unchangedSecondDocument = GetLspDocument(secondWorkspaceDocumentUri, testLspServer);
        Assert.Equal(secondDocument, unchangedSecondDocument);
    }

    [Fact]
    public async Task TestWorkspaceEventUpdatesCorrectWorkspaceWithMultipleWorkspacesAsync()
    {
        var firstWorkspaceXml =
@$"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""FirstWorkspaceProject"">
        <Document FilePath=""C:\FirstWorkspace.cs"">FirstWorkspace</Document>
    </Project>
</Workspace>";

        var secondWorkspaceXml =
@$"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""SecondWorkspaceProject"">
        <Document FilePath=""C:\SecondWorkspace.cs"">SecondWorkspace</Document>
    </Project>
</Workspace>";

        using var testLspServer = await CreateXmlTestLspServerAsync(firstWorkspaceXml);

        var exportProvider = testLspServer.TestWorkspace.ExportProvider;

        using var testWorkspaceTwo = TestWorkspace.Create(
            XElement.Parse(secondWorkspaceXml),
            workspaceKind: WorkspaceKind.MSBuild,
            exportProvider: exportProvider);

        // Wait for workspace operations to complete for the second workspace.
        await WaitForWorkspaceOperationsAsync(testWorkspaceTwo);

        var firstWorkspaceDocumentUri = ProtocolConversions.GetUriFromFilePath(@"C:\FirstWorkspace.cs");
        var secondWorkspaceDocumentUri = ProtocolConversions.GetUriFromFilePath(@"C:\SecondWorkspace.cs");
        await testLspServer.OpenDocumentAsync(firstWorkspaceDocumentUri);

        // Verify we can get both documents from their respective workspaces.
        var firstDocument = GetLspDocument(firstWorkspaceDocumentUri, testLspServer);
        AssertEx.NotNull(firstDocument);
        Assert.Equal(firstWorkspaceDocumentUri, firstDocument.GetURI());
        Assert.Equal(testLspServer.TestWorkspace, firstDocument.Project.Solution.Workspace);

        var secondDocument = GetLspDocument(secondWorkspaceDocumentUri, testLspServer);
        AssertEx.NotNull(secondDocument);
        Assert.Equal(secondWorkspaceDocumentUri, secondDocument.GetURI());
        Assert.Equal(testWorkspaceTwo, secondDocument.Project.Solution.Workspace);

        // Verify making a workspace change only changes the respective workspace.
        var newProjectWorkspaceTwo = testWorkspaceTwo.CurrentSolution.Projects.First().WithAssemblyName("NewCSProj1");
        await testWorkspaceTwo.ChangeProjectAsync(newProjectWorkspaceTwo.Id, newProjectWorkspaceTwo.Solution);

        // The second document should have an updated project assembly name.
        var secondDocumentChangedProject = GetLspDocument(secondWorkspaceDocumentUri, testLspServer);
        AssertEx.NotNull(secondDocumentChangedProject);
        Assert.Equal("NewCSProj1", secondDocumentChangedProject.Project.AssemblyName);
        Assert.NotEqual(secondDocument, secondDocumentChangedProject);

        // The first document should be the same document as the last one since that workspace was not changed.
        Assert.Equal(firstDocument, GetLspDocument(firstWorkspaceDocumentUri, testLspServer));
    }

    [Fact]
    public async Task TestSeparateWorkspaceManagerPerServerAsync()
    {
        var workspaceXml =
@$"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj1"">
        <Document FilePath=""C:\test1.cs"">Original text</Document>
    </Project>
</Workspace>";

        using var testWorkspace = TestWorkspace.Create(XElement.Parse(workspaceXml), composition: Composition);

        // Wait for workspace creation operations to complete.
        await WaitForWorkspaceOperationsAsync(testWorkspace);

        var documentUri = testWorkspace.CurrentSolution.Projects.First().Documents.First().GetURI();

        using var testLspServerOne = await TestLspServer.CreateAsync(testWorkspace, clientCapabilities: new(), WellKnownLspServerKinds.AlwaysActiveVSLspServer);
        using var testLspServerTwo = await TestLspServer.CreateAsync(testWorkspace, clientCapabilities: new(), WellKnownLspServerKinds.AlwaysActiveVSLspServer);

        Assert.NotEqual(testLspServerOne.GetManager(), testLspServerTwo.GetManager());

        // Verify workspace is registered with both servers.
        Assert.Null(GetManagerWorkspaceState(testWorkspace, testLspServerOne));
        Assert.Null(GetManagerWorkspaceState(testWorkspace, testLspServerTwo));

        // Verify that the LSP solution uses the correct text for each server.
        var documentServerOne = await OpenDocumentAndVerifyLspTextAsync(documentUri, testLspServerOne, "Server one text");

        var documentServerTwo = GetLspDocument(documentUri, testLspServerTwo);
        AssertEx.NotNull(documentServerTwo);
        Assert.Equal("Original text", (await documentServerTwo.GetTextAsync(CancellationToken.None)).ToString());

        // Verify workspace updates are reflected in both servers.
        var newAssemblyName = "NewCSProj1";
        var newProject = testWorkspace.CurrentSolution.Projects.First().WithAssemblyName(newAssemblyName);
        await testWorkspace.ChangeProjectAsync(newProject.Id, newProject.Solution);
        // Verify lsp incremental solution cleared.
        Assert.Null(GetManagerWorkspaceState(testWorkspace, testLspServerOne));
        Assert.Null(GetManagerWorkspaceState(testWorkspace, testLspServerTwo));

        // Verify LSP solution has the project changes.
        documentServerOne = GetLspDocument(documentUri, testLspServerOne);
        AssertEx.NotNull(documentServerOne);
        Assert.Equal(newAssemblyName, documentServerOne.Project.AssemblyName);
        documentServerTwo = GetLspDocument(documentUri, testLspServerTwo);
        AssertEx.NotNull(documentServerTwo);
        Assert.Equal(newAssemblyName, documentServerTwo.Project.AssemblyName);
    }

    private static async Task<Document> OpenDocumentAndVerifyLspTextAsync(Uri documentUri, TestLspServer testLspServer, string openText = "LSP text")
    {
        await testLspServer.OpenDocumentAsync(documentUri, openText);

        // Verify doc open created an LSP solution from the workspace.
        Assert.NotNull(GetManagerWorkspaceState(testLspServer.TestWorkspace, testLspServer));

        // Verify we can find the document with correct text in the new LSP solution.
        var lspDocument = GetLspDocument(documentUri, testLspServer);
        AssertEx.NotNull(lspDocument);
        Assert.Equal(openText, (await lspDocument.GetTextAsync(CancellationToken.None)).ToString());
        return lspDocument;
    }

    private static Solution? GetManagerWorkspaceState(Workspace workspace, TestLspServer testLspServer)
    {
        return testLspServer.GetManagerAccessor().GetWorkspaceState()[workspace];
    }

    private static Document? GetLspDocument(Uri uri, TestLspServer testLspServer)
    {
        return testLspServer.GetManager().GetLspDocument(CreateTextDocumentIdentifier(uri), clientName: null);
    }

    private static Solution? GetLspHostSolution(TestLspServer testLspServer)
    {
        return testLspServer.GetManager().TryGetHostLspSolution();
    }
}
