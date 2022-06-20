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
    public async Task TestUsesLspTextOnOpenCloseAsync()
    {
        var markup = "";
        using var testLspServer = await CreateTestLspServerAsync(markup);
        var documentUri = testLspServer.GetCurrentSolution().Projects.First().Documents.First().GetURI();

        await testLspServer.OpenDocumentAsync(documentUri, "LSP text");

        var lspDocument = await GetLspDocumentAsync(documentUri, testLspServer).ConfigureAwait(false);
        AssertEx.NotNull(lspDocument);
        Assert.Equal("LSP text", (await lspDocument.GetTextAsync(CancellationToken.None)).ToString());

        // Verify LSP text changes are reflected in the opened document.
        await testLspServer.InsertTextAsync(documentUri, (0, 0, "More text"));
        lspDocument = await GetLspDocumentAsync(documentUri, testLspServer).ConfigureAwait(false);
        AssertEx.NotNull(lspDocument);
        Assert.Equal("More textLSP text", (await lspDocument.GetTextAsync(CancellationToken.None)).ToString());

        // Close the document in LSP and verify all LSP tracked changes are now gone.
        // The document should be reset to the workspace's state.
        await testLspServer.CloseDocumentAsync(documentUri);
        var closedDocument = await GetLspDocumentAsync(documentUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(testLspServer.GetCurrentSolution(), closedDocument!.Project.Solution);
    }

    [Fact]
    public async Task TestLspUsesWorkspaceInstanceOnChangesAsync()
    {
        var markupOne = "One";
        var markupTwo = "Two";
        using var testLspServer = await CreateTestLspServerAsync(new string[] { markupOne, markupTwo });
        var firstDocumentUri = testLspServer.GetCurrentSolution().Projects.First().Documents.Single(d => d.FilePath!.Contains("test1")).GetURI();
        var secondDocumentUri = testLspServer.GetCurrentSolution().Projects.First().Documents.Single(d => d.FilePath!.Contains("test2")).GetURI();

        var firstDocument = await OpenDocumentAndVerifyLspTextAsync(firstDocumentUri, testLspServer, markupOne);
        var secondDocument = await OpenDocumentAndVerifyLspTextAsync(secondDocumentUri, testLspServer, markupTwo);
        var firstDocumentInitialVersion = await firstDocument.GetSyntaxVersionAsync(CancellationToken.None);
        var secondDocumentInitialVersion = await secondDocument.GetSyntaxVersionAsync(CancellationToken.None);

        // Verify the LSP documents are the same instance as the workspaces documents.
        Assert.Equal(testLspServer.TestWorkspace.CurrentSolution.GetDocument(firstDocument.Id), firstDocument);
        Assert.Equal(testLspServer.TestWorkspace.CurrentSolution.GetDocument(secondDocument.Id), secondDocument);

        // Make a text change in one of the opened documents in both LSP and the workspace.
        await testLspServer.InsertTextAsync(firstDocumentUri, (0, 0, "Some more text"));
        await testLspServer.TestWorkspace.ChangeDocumentAsync(firstDocument.Id, SourceText.From($"Some more text{markupOne}", System.Text.Encoding.UTF8));

        var firstDocumentWithChange = await GetLspDocumentAsync(firstDocumentUri, testLspServer).ConfigureAwait(false);
        var secondDocumentUnchanged = await GetLspDocumentAsync(secondDocumentUri, testLspServer).ConfigureAwait(false);
        AssertEx.NotNull(firstDocumentWithChange);
        AssertEx.NotNull(secondDocumentUnchanged);

        // Verify that the document that we inserted text into had a version change.
        Assert.NotEqual(firstDocumentInitialVersion, await firstDocumentWithChange.GetSyntaxVersionAsync(CancellationToken.None));
        Assert.Equal($"Some more text{markupOne}", (await firstDocumentWithChange.GetTextAsync(CancellationToken.None)).ToString());

        // Verify that the document that we did not change still has the same version.
        Assert.Equal(secondDocumentInitialVersion, await secondDocumentUnchanged.GetSyntaxVersionAsync(CancellationToken.None));

        // Verify the LSP documents are the same instance as the workspaces documents.
        Assert.Equal(testLspServer.TestWorkspace.CurrentSolution.GetDocument(firstDocumentWithChange.Id), firstDocumentWithChange);
        Assert.Equal(testLspServer.TestWorkspace.CurrentSolution.GetDocument(secondDocumentUnchanged.Id), secondDocumentUnchanged);
    }

    [Fact]
    public async Task TestLspHasClosedDocumentChangesAsync()
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
        await testLspServer.TestWorkspace.ChangeDocumentAsync(secondDocument.Id, SourceText.From("Two is now three!", System.Text.Encoding.UTF8));

        // Verify that the LSP solution has the LSP text from the open document.
        var openedDocument = await GetLspDocumentAsync(firstDocumentUri, testLspServer).ConfigureAwait(false);
        AssertEx.NotNull(openedDocument);
        Assert.Equal("LSP text", (await openedDocument.GetTextAsync(CancellationToken.None)).ToString());

        // Verify that the LSP solution has the workspace text in the closed document.
        secondDocument = await GetLspDocumentAsync(secondDocumentUri, testLspServer).ConfigureAwait(false);
        AssertEx.NotNull(secondDocument);
        Assert.Equal("Two is now three!", (await secondDocument.GetTextAsync()).ToString());
        Assert.NotEqual(testLspServer.TestWorkspace.CurrentSolution.GetDocument(secondDocument.Id), secondDocument);
    }

    [Fact]
    public async Task TestLspHasProjectChangesAsync()
    {
        var markup = "One";
        using var testLspServer = await CreateTestLspServerAsync(markup);
        var documentUri = testLspServer.GetCurrentSolution().Projects.First().Documents.Single(d => d.FilePath!.Contains("test1")).GetURI();

        // Open the document via LSP and verify the initial project name.
        var openedDocument = await OpenDocumentAndVerifyLspTextAsync(documentUri, testLspServer, markup);
        Assert.Equal("Test", openedDocument?.Project.AssemblyName);
        Assert.Equal(testLspServer.TestWorkspace.CurrentSolution, openedDocument!.Project.Solution);

        // Modify the project via the workspace.
        var newProject = testLspServer.TestWorkspace.CurrentSolution.Projects.First().WithAssemblyName("NewCSProj1");
        await testLspServer.TestWorkspace.ChangeProjectAsync(newProject.Id, newProject.Solution);

        // Verify that the new LSP solution has the updated project info.
        openedDocument = await GetLspDocumentAsync(documentUri, testLspServer).ConfigureAwait(false);
        AssertEx.NotNull(openedDocument);
        Assert.Equal(markup, (await openedDocument.GetTextAsync(CancellationToken.None)).ToString());
        Assert.Equal("NewCSProj1", openedDocument.Project.AssemblyName);
        Assert.Equal(testLspServer.TestWorkspace.CurrentSolution, openedDocument.Project.Solution);
    }

    [Fact]
    public async Task TestLspHasProjectChangesWithForkedTextAsync()
    {
        var markup = "One";
        using var testLspServer = await CreateTestLspServerAsync(markup);
        var documentUri = testLspServer.GetCurrentSolution().Projects.First().Documents.Single(d => d.FilePath!.Contains("test1")).GetURI();

        // Open the document via LSP with different text from the workspace and verify the initial project name.
        var openedDocument = await OpenDocumentAndVerifyLspTextAsync(documentUri, testLspServer);
        Assert.Equal("Test", openedDocument?.Project.AssemblyName);
        Assert.NotEqual(testLspServer.TestWorkspace.CurrentSolution, openedDocument!.Project.Solution);

        // Modify the project via the workspace.
        var newProject = testLspServer.TestWorkspace.CurrentSolution.Projects.First().WithAssemblyName("NewCSProj1");
        await testLspServer.TestWorkspace.ChangeProjectAsync(newProject.Id, newProject.Solution);

        // Verify that the new LSP solution has the updated project info.
        openedDocument = await GetLspDocumentAsync(documentUri, testLspServer).ConfigureAwait(false);
        AssertEx.NotNull(openedDocument);
        Assert.Equal("LSP text", (await openedDocument.GetTextAsync(CancellationToken.None)).ToString());
        Assert.Equal("NewCSProj1", openedDocument.Project.AssemblyName);
        Assert.NotEqual(testLspServer.TestWorkspace.CurrentSolution, openedDocument.Project.Solution);
    }

    [Fact]
    public async Task TestLspFindsNewDocumentAsync()
    {
        var markup = "One";
        using var testLspServer = await CreateTestLspServerAsync(markup);
        var documentUri = testLspServer.GetCurrentSolution().Projects.First().Documents.Single(d => d.FilePath!.Contains("test1")).GetURI();

        // Open the document via LSP to create the initial LSP solution.
        await OpenDocumentAndVerifyLspTextAsync(documentUri, testLspServer, markup);

        // Add a new document to the workspace
        var newDocumentId = DocumentId.CreateNewId(testLspServer.TestWorkspace.CurrentSolution.ProjectIds[0]);
        var newSolution = testLspServer.TestWorkspace.CurrentSolution.AddDocument(newDocumentId, "NewDoc.cs", SourceText.From("New Doc", System.Text.Encoding.UTF8), filePath: @"C:\NewDoc.cs");
        var newDocumentUri = newSolution.GetRequiredDocument(newDocumentId).GetURI();
        await testLspServer.TestWorkspace.ChangeSolutionAsync(newSolution);

        // Verify that the lsp server sees the workspace change and picks up the document in the correct workspace.
        await testLspServer.OpenDocumentAsync(newDocumentUri);
        var lspDocument = await GetLspDocumentAsync(newDocumentUri, testLspServer).ConfigureAwait(false);
        AssertEx.NotNull(lspDocument);
        Assert.Equal(testLspServer.TestWorkspace.CurrentSolution, lspDocument.Project.Solution);
    }

    [Fact]
    public async Task TestLspTransfersDocumentToNewWorkspaceAsync()
    {
        var markup = "One";

        // Create a server that includes the LSP misc files workspace so we can test transfers to and from it.
        using var testLspServer = await CreateTestLspServerAsync(markup, new ClientCapabilities(), serverKind: WellKnownLspServerKinds.CSharpVisualBasicLspServer);

        // Create a new document, but do not update the workspace solution yet.
        var newDocumentId = DocumentId.CreateNewId(testLspServer.TestWorkspace.CurrentSolution.ProjectIds[0]);
        var newDocumentFilePath = @"C:/NewDoc.cs";
        var newDocumentInfo = DocumentInfo.Create(newDocumentId, "NewDoc.cs", filePath: newDocumentFilePath, loader: new TestTextLoader("New Doc"));
        var newDocumentUri = ProtocolConversions.GetUriFromFilePath(newDocumentFilePath);

        // Open the document via LSP before the workspace sees it.
        await testLspServer.OpenDocumentAsync(newDocumentUri, "LSP text");

        // Verify it is in the lsp misc workspace.
        var miscDocument = await GetLspDocumentAsync(newDocumentUri, testLspServer).ConfigureAwait(false);
        AssertEx.NotNull(miscDocument);
        Assert.Equal(testLspServer.GetManagerAccessor().GetLspMiscellaneousFilesWorkspace(), miscDocument.Project.Solution.Workspace);
        Assert.Equal("LSP text", (await miscDocument.GetTextAsync(CancellationToken.None)).ToString());

        // Make a change and verify the misc document is updated.
        await testLspServer.InsertTextAsync(newDocumentUri, (0, 0, "More LSP text"));
        miscDocument = await GetLspDocumentAsync(newDocumentUri, testLspServer).ConfigureAwait(false);
        AssertEx.NotNull(miscDocument);
        var miscText = await miscDocument.GetTextAsync(CancellationToken.None);
        Assert.Equal("More LSP textLSP text", miscText.ToString());

        // Update the registered workspace with the new document.
        await testLspServer.TestWorkspace.AddDocumentAsync(newDocumentInfo);

        // Verify that the newly added document in the registered workspace is returned.
        var document = await GetLspDocumentAsync(newDocumentUri, testLspServer).ConfigureAwait(false);
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
        Assert.True(IsWorkspaceRegistered(testLspServer.TestWorkspace, testLspServer));

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
        Assert.True(IsWorkspaceRegistered(testLspServer.TestWorkspace, testLspServer));
        Assert.True(IsWorkspaceRegistered(testWorkspaceTwo, testLspServer));

        // Verify the host workspace returned is the workspace with kind host.
        var hostSolution = await GetLspHostSolutionAsync(testLspServer).ConfigureAwait(false);
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
        Assert.True(IsWorkspaceRegistered(testLspServer.TestWorkspace, testLspServer));

        // Verify there is not workspace matching the host workspace kind.
        Assert.Null(await GetLspHostSolutionAsync(testLspServer).ConfigureAwait(false));
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
        Assert.True(IsWorkspaceRegistered(testLspServer.TestWorkspace, testLspServer));
        Assert.True(IsWorkspaceRegistered(testWorkspaceTwo, testLspServer));

        var firstWorkspaceDocumentUri = ProtocolConversions.GetUriFromFilePath(@"C:\FirstWorkspace.cs");
        var secondWorkspaceDocumentUri = ProtocolConversions.GetUriFromFilePath(@"C:\SecondWorkspace.cs");
        await testLspServer.OpenDocumentAsync(firstWorkspaceDocumentUri);

        // Verify we can get both documents from their respective workspaces.
        var firstDocument = await GetLspDocumentAsync(firstWorkspaceDocumentUri, testLspServer).ConfigureAwait(false);
        AssertEx.NotNull(firstDocument);
        Assert.Equal(firstWorkspaceDocumentUri, firstDocument.GetURI());
        Assert.Equal(testLspServer.TestWorkspace, firstDocument.Project.Solution.Workspace);

        var secondDocument = await GetLspDocumentAsync(secondWorkspaceDocumentUri, testLspServer).ConfigureAwait(false);
        AssertEx.NotNull(secondDocument);
        Assert.Equal(secondWorkspaceDocumentUri, secondDocument.GetURI());
        Assert.Equal(testWorkspaceTwo, secondDocument.Project.Solution.Workspace);

        // Verify making an LSP change only changes the respective workspace and document.
        await testLspServer.InsertTextAsync(firstWorkspaceDocumentUri, (0, 0, "Change in first workspace"));

        // The first document should now different text.
        var changedFirstDocument = await GetLspDocumentAsync(firstWorkspaceDocumentUri, testLspServer).ConfigureAwait(false);
        AssertEx.NotNull(changedFirstDocument);
        var changedFirstDocumentText = await changedFirstDocument.GetTextAsync(CancellationToken.None);
        var firstDocumentText = await firstDocument.GetTextAsync(CancellationToken.None);
        Assert.NotEqual(firstDocumentText, changedFirstDocumentText);

        // The second document should return the same document instance since it was not changed.
        var unchangedSecondDocument = await GetLspDocumentAsync(secondWorkspaceDocumentUri, testLspServer).ConfigureAwait(false);
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
        var firstDocument = await GetLspDocumentAsync(firstWorkspaceDocumentUri, testLspServer).ConfigureAwait(false);
        AssertEx.NotNull(firstDocument);
        Assert.Equal(firstWorkspaceDocumentUri, firstDocument.GetURI());
        Assert.Equal(testLspServer.TestWorkspace, firstDocument.Project.Solution.Workspace);

        var secondDocument = await GetLspDocumentAsync(secondWorkspaceDocumentUri, testLspServer).ConfigureAwait(false);
        AssertEx.NotNull(secondDocument);
        Assert.Equal(secondWorkspaceDocumentUri, secondDocument.GetURI());
        Assert.Equal(testWorkspaceTwo, secondDocument.Project.Solution.Workspace);

        // Verify making a workspace change only changes the respective workspace.
        var newProjectWorkspaceTwo = testWorkspaceTwo.CurrentSolution.Projects.First().WithAssemblyName("NewCSProj1");
        await testWorkspaceTwo.ChangeProjectAsync(newProjectWorkspaceTwo.Id, newProjectWorkspaceTwo.Solution);

        // The second document should have an updated project assembly name.
        var secondDocumentChangedProject = await GetLspDocumentAsync(secondWorkspaceDocumentUri, testLspServer).ConfigureAwait(false);
        AssertEx.NotNull(secondDocumentChangedProject);
        Assert.Equal("NewCSProj1", secondDocumentChangedProject.Project.AssemblyName);
        Assert.NotEqual(secondDocument, secondDocumentChangedProject);

        // The first document should be the same document as the last one since that workspace was not changed.
        Assert.Equal(firstDocument, await GetLspDocumentAsync(firstWorkspaceDocumentUri, testLspServer).ConfigureAwait(false));
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
        Assert.True(IsWorkspaceRegistered(testWorkspace, testLspServerOne));
        Assert.True(IsWorkspaceRegistered(testWorkspace, testLspServerTwo));

        // Verify that the LSP solution uses the correct text for each server.
        var documentServerOne = await OpenDocumentAndVerifyLspTextAsync(documentUri, testLspServerOne, "Server one text");

        var documentServerTwo = await GetLspDocumentAsync(documentUri, testLspServerTwo).ConfigureAwait(false);
        AssertEx.NotNull(documentServerTwo);
        Assert.Equal("Original text", (await documentServerTwo.GetTextAsync(CancellationToken.None)).ToString());

        // Verify workspace updates are reflected in both servers.
        var newAssemblyName = "NewCSProj1";
        var newProject = testWorkspace.CurrentSolution.Projects.First().WithAssemblyName(newAssemblyName);
        await testWorkspace.ChangeProjectAsync(newProject.Id, newProject.Solution);

        // Verify LSP solution has the project changes.
        documentServerOne = await GetLspDocumentAsync(documentUri, testLspServerOne).ConfigureAwait(false);
        AssertEx.NotNull(documentServerOne);
        Assert.Equal(newAssemblyName, documentServerOne.Project.AssemblyName);
        documentServerTwo = await GetLspDocumentAsync(documentUri, testLspServerTwo).ConfigureAwait(false);
        AssertEx.NotNull(documentServerTwo);
        Assert.Equal(newAssemblyName, documentServerTwo.Project.AssemblyName);
    }

    private static async Task<Document> OpenDocumentAndVerifyLspTextAsync(Uri documentUri, TestLspServer testLspServer, string openText = "LSP text")
    {
        await testLspServer.OpenDocumentAsync(documentUri, openText);

        // Verify we can find the document with correct text in the new LSP solution.
        var lspDocument = await GetLspDocumentAsync(documentUri, testLspServer).ConfigureAwait(false);
        AssertEx.NotNull(lspDocument);
        Assert.Equal(openText, (await lspDocument.GetTextAsync(CancellationToken.None)).ToString());
        return lspDocument;
    }

    private static bool IsWorkspaceRegistered(Workspace workspace, TestLspServer testLspServer)
    {
        return testLspServer.GetManagerAccessor().IsWorkspaceRegistered(workspace);
    }

    private static Task<Document?> GetLspDocumentAsync(Uri uri, TestLspServer testLspServer)
    {
        return testLspServer.GetManager().GetLspDocumentAsync(CreateTextDocumentIdentifier(uri), CancellationToken.None);
    }

    private static Task<Solution?> GetLspHostSolutionAsync(TestLspServer testLspServer)
    {
        return testLspServer.GetManager().TryGetHostLspSolutionAsync(CancellationToken.None);
    }
}
