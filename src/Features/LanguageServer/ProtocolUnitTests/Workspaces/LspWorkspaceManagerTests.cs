// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Workspaces;

public class LspWorkspaceManagerTests : AbstractLanguageServerProtocolTests
{
    public LspWorkspaceManagerTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Theory, CombinatorialData]
    public async Task TestUsesLspTextOnOpenCloseAsync(bool mutatingLspWorkspace)
    {
        var markup = "";
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var documentUri = testLspServer.GetCurrentSolution().Projects.First().Documents.First().GetURI();

        await testLspServer.OpenDocumentAsync(documentUri, "LSP text");

        var (_, lspDocument) = await GetLspWorkspaceAndDocumentAsync(documentUri, testLspServer).ConfigureAwait(false);
        AssertEx.NotNull(lspDocument);
        Assert.Equal("LSP text", (await lspDocument.GetTextAsync(CancellationToken.None)).ToString());

        // Verify LSP text changes are reflected in the opened document.
        await testLspServer.InsertTextAsync(documentUri, (0, 0, "More text"));
        (_, lspDocument) = await GetLspWorkspaceAndDocumentAsync(documentUri, testLspServer).ConfigureAwait(false);
        AssertEx.NotNull(lspDocument);
        Assert.Equal("More textLSP text", (await lspDocument.GetTextAsync(CancellationToken.None)).ToString());

        // Close the document in LSP and verify all LSP tracked changes are now gone.
        // The document should be reset to the workspace's state.
        await testLspServer.CloseDocumentAsync(documentUri);
        var (_, closedDocument) = await GetLspWorkspaceAndDocumentAsync(documentUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(testLspServer.GetCurrentSolution(), closedDocument!.Project.Solution);
    }

    [Theory, CombinatorialData]
    public async Task TestLspUsesWorkspaceInstanceOnChangesAsync(bool mutatingLspWorkspace)
    {
        var markupOne = "One";
        var markupTwo = "Two";
        await using var testLspServer = await CreateTestLspServerAsync(new string[] { markupOne, markupTwo }, mutatingLspWorkspace);
        var firstDocumentUri = testLspServer.GetCurrentSolution().Projects.First().Documents.Single(d => d.FilePath!.Contains("test1")).GetURI();
        var secondDocumentUri = testLspServer.GetCurrentSolution().Projects.First().Documents.Single(d => d.FilePath!.Contains("test2")).GetURI();

        var firstDocument = await OpenDocumentAndVerifyLspTextAsync(firstDocumentUri, testLspServer, markupOne);
        var secondDocument = await OpenDocumentAndVerifyLspTextAsync(secondDocumentUri, testLspServer, markupTwo);
        var firstDocumentInitialVersion = await firstDocument.GetSyntaxVersionAsync(CancellationToken.None);
        var secondDocumentInitialVersion = await secondDocument.GetSyntaxVersionAsync(CancellationToken.None);

        if (mutatingLspWorkspace)
        {
            // In the mutating case, adding/opening the second document will cause the workspace solution to actually
            // change.  So it will point to a different document instance for firstDocument.   The underlying state will
            // be the same though.
            Assert.NotSame(testLspServer.TestWorkspace.CurrentSolution.GetDocument(firstDocument.Id), firstDocument);
            Assert.Same(testLspServer.TestWorkspace.CurrentSolution.GetDocument(firstDocument.Id)?.State, firstDocument?.State);
        }
        else
        {
            // Verify the LSP documents are the same instance as the workspaces documents.
            Assert.Same(testLspServer.TestWorkspace.CurrentSolution.GetDocument(firstDocument.Id), firstDocument);
        }

        Assert.Same(testLspServer.TestWorkspace.CurrentSolution.GetDocument(secondDocument.Id), secondDocument);

        // Make a text change in one of the opened documents in both LSP and the workspace.
        await testLspServer.InsertTextAsync(firstDocumentUri, (0, 0, "Some more text"));
        AssertEx.NotNull(firstDocument);
        await testLspServer.TestWorkspace.ChangeDocumentAsync(firstDocument.Id, SourceText.From($"Some more text{markupOne}", System.Text.Encoding.UTF8, SourceHashAlgorithms.Default));

        var (_, firstDocumentWithChange) = await GetLspWorkspaceAndDocumentAsync(firstDocumentUri, testLspServer).ConfigureAwait(false);
        var (_, secondDocumentUnchanged) = await GetLspWorkspaceAndDocumentAsync(secondDocumentUri, testLspServer).ConfigureAwait(false);
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

    [Theory, CombinatorialData]
    public async Task TestLspHasClosedDocumentChangesAsync(bool mutatingLspWorkspace)
    {
        var markupOne = "One";
        var markupTwo = "Two";
        await using var testLspServer = await CreateTestLspServerAsync(new string[] { markupOne, markupTwo }, mutatingLspWorkspace);
        var firstDocumentUri = testLspServer.GetCurrentSolution().Projects.First().Documents.Single(d => d.FilePath!.Contains("test1")).GetURI();

        var secondDocument = testLspServer.GetCurrentSolution().Projects.First().Documents.Single(d => d.FilePath!.Contains("test2"));
        var secondDocumentUri = secondDocument.GetURI();

        // Open one of the documents via LSP and verify we have created our LSP solution.
        await OpenDocumentAndVerifyLspTextAsync(firstDocumentUri, testLspServer);

        // Modify a closed document via the workspace.
        await testLspServer.TestWorkspace.ChangeDocumentAsync(secondDocument.Id, SourceText.From("Two is now three!", System.Text.Encoding.UTF8, SourceHashAlgorithms.Default));

        // Verify that the LSP solution has the LSP text from the open document.
        var (_, openedDocument) = await GetLspWorkspaceAndDocumentAsync(firstDocumentUri, testLspServer).ConfigureAwait(false);
        AssertEx.NotNull(openedDocument);
        Assert.Equal("LSP text", (await openedDocument.GetTextAsync(CancellationToken.None)).ToString());

        // Verify that the LSP solution has the workspace text in the closed document.
        (_, secondDocument) = await GetLspWorkspaceAndDocumentAsync(secondDocumentUri, testLspServer).ConfigureAwait(false);
        AssertEx.NotNull(secondDocument);
        Assert.Equal("Two is now three!", (await secondDocument.GetTextAsync()).ToString());

        if (mutatingLspWorkspace)
        {
            // In the mutating case, opening the second doc pushes its changes through to the underlying workspace.  So
            // the documents will be the same.
            Assert.Equal(testLspServer.TestWorkspace.CurrentSolution.GetDocument(secondDocument.Id), secondDocument);
        }
        else
        {
            Assert.NotEqual(testLspServer.TestWorkspace.CurrentSolution.GetDocument(secondDocument.Id), secondDocument);
        }
    }

    [Theory, CombinatorialData]
    public async Task TestLspHasProjectChangesAsync(bool mutatingLspWorkspace)
    {
        var markup = "One";
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var documentUri = testLspServer.GetCurrentSolution().Projects.First().Documents.Single(d => d.FilePath!.Contains("test1")).GetURI();

        // Open the document via LSP and verify the initial project name.
        var openedDocument = await OpenDocumentAndVerifyLspTextAsync(documentUri, testLspServer, markup);
        Assert.Equal("Test", openedDocument?.Project.AssemblyName);
        Assert.Equal(testLspServer.TestWorkspace.CurrentSolution, openedDocument!.Project.Solution);

        // Modify the project via the workspace.
        var newProject = testLspServer.TestWorkspace.CurrentSolution.Projects.First().WithAssemblyName("NewCSProj1");
        await testLspServer.TestWorkspace.ChangeProjectAsync(newProject.Id, newProject.Solution);

        // Verify that the new LSP solution has the updated project info.
        (_, openedDocument) = await GetLspWorkspaceAndDocumentAsync(documentUri, testLspServer).ConfigureAwait(false);
        AssertEx.NotNull(openedDocument);
        Assert.Equal(markup, (await openedDocument.GetTextAsync(CancellationToken.None)).ToString());
        Assert.Equal("NewCSProj1", openedDocument.Project.AssemblyName);
        Assert.Equal(testLspServer.TestWorkspace.CurrentSolution, openedDocument.Project.Solution);
    }

    [Theory, CombinatorialData]
    public async Task TestLspHasProjectChangesWithForkedTextAsync(bool mutatingLspWorkspace)
    {
        var markup = "One";
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var documentUri = testLspServer.GetCurrentSolution().Projects.First().Documents.Single(d => d.FilePath!.Contains("test1")).GetURI();

        // Open the document via LSP with different text from the workspace and verify the initial project name.
        var openedDocument = await OpenDocumentAndVerifyLspTextAsync(documentUri, testLspServer);
        Assert.Equal("Test", openedDocument?.Project.AssemblyName);

        // In the mutating case, the changes are pushed through such that the solutions are the same.
        if (mutatingLspWorkspace)
        {
            Assert.Equal(testLspServer.TestWorkspace.CurrentSolution, openedDocument!.Project.Solution);
        }
        else
        {
            Assert.NotEqual(testLspServer.TestWorkspace.CurrentSolution, openedDocument!.Project.Solution);
        }

        // Modify the project via the workspace.
        var newProject = testLspServer.TestWorkspace.CurrentSolution.Projects.First().WithAssemblyName("NewCSProj1");
        await testLspServer.TestWorkspace.ChangeProjectAsync(newProject.Id, newProject.Solution);

        // Verify that the new LSP solution has the updated project info.
        (_, openedDocument) = await GetLspWorkspaceAndDocumentAsync(documentUri, testLspServer).ConfigureAwait(false);
        AssertEx.NotNull(openedDocument);
        Assert.Equal("LSP text", (await openedDocument.GetTextAsync(CancellationToken.None)).ToString());
        Assert.Equal("NewCSProj1", openedDocument.Project.AssemblyName);

        // In the mutating case, the changes are pushed through such that the solutions are the same.
        if (mutatingLspWorkspace)
        {
            Assert.Equal(testLspServer.TestWorkspace.CurrentSolution, openedDocument.Project.Solution);
        }
        else
        {
            Assert.NotEqual(testLspServer.TestWorkspace.CurrentSolution, openedDocument.Project.Solution);
        }
    }

    [Theory, CombinatorialData]
    public async Task TestLspFindsNewDocumentAsync(bool mutatingLspWorkspace)
    {
        var markup = "One";
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var documentUri = testLspServer.GetCurrentSolution().Projects.First().Documents.Single(d => d.FilePath!.Contains("test1")).GetURI();

        // Open the document via LSP to create the initial LSP solution.
        await OpenDocumentAndVerifyLspTextAsync(documentUri, testLspServer, markup);

        // Add a new document to the workspace
        var newDocumentId = DocumentId.CreateNewId(testLspServer.TestWorkspace.CurrentSolution.ProjectIds[0]);
        var newSolution = testLspServer.TestWorkspace.CurrentSolution.AddDocument(newDocumentId, "NewDoc.cs", SourceText.From("New Doc", System.Text.Encoding.UTF8, SourceHashAlgorithms.Default), filePath: @"C:\NewDoc.cs");
        var newDocumentUri = newSolution.GetRequiredDocument(newDocumentId).GetURI();
        await testLspServer.TestWorkspace.ChangeSolutionAsync(newSolution);

        // Verify that the lsp server sees the workspace change and picks up the document in the correct workspace.
        await testLspServer.OpenDocumentAsync(newDocumentUri);
        var (_, lspDocument) = await GetLspWorkspaceAndDocumentAsync(newDocumentUri, testLspServer).ConfigureAwait(false);
        AssertEx.NotNull(lspDocument);
        Assert.Equal(testLspServer.TestWorkspace.CurrentSolution, lspDocument.Project.Solution);
    }

    [Theory, CombinatorialData]
    public async Task TestLspTransfersDocumentToNewWorkspaceAsync(bool mutatingLspWorkspace)
    {
        var markup = "One";

        // Create a server that includes the LSP misc files workspace so we can test transfers to and from it.
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });

        // Create a new document, but do not update the workspace solution yet.
        var newDocumentId = DocumentId.CreateNewId(testLspServer.TestWorkspace.CurrentSolution.ProjectIds[0]);

        // Include some Unicode characters to test URL handling.
        var newDocumentFilePath = "C:\\NewDoc\\\ue25b\ud86d\udeac.cs";
        var newDocumentInfo = DocumentInfo.Create(newDocumentId, "NewDoc.cs", filePath: newDocumentFilePath, loader: new TestTextLoader("New Doc"));
        var newDocumentUri = ProtocolConversions.CreateAbsoluteUri(newDocumentFilePath);

        // Open the document via LSP before the workspace sees it.
        await testLspServer.OpenDocumentAsync(newDocumentUri, "LSP text");

        // Verify it is in the lsp misc workspace.
        var (miscWorkspace, miscDocument) = await GetLspWorkspaceAndDocumentAsync(newDocumentUri, testLspServer).ConfigureAwait(false);
        AssertEx.NotNull(miscDocument);
        Assert.Equal(testLspServer.GetManagerAccessor().GetLspMiscellaneousFilesWorkspace(), miscWorkspace);
        Assert.Equal("LSP text", (await miscDocument.GetTextAsync(CancellationToken.None)).ToString());

        // Make a change and verify the misc document is updated.
        await testLspServer.InsertTextAsync(newDocumentUri, (0, 0, "More LSP text"));
        (_, miscDocument) = await GetLspWorkspaceAndDocumentAsync(newDocumentUri, testLspServer).ConfigureAwait(false);
        AssertEx.NotNull(miscDocument);
        var miscText = await miscDocument.GetTextAsync(CancellationToken.None);
        Assert.Equal("More LSP textLSP text", miscText.ToString());

        // Update the registered workspace with the new document.
        await testLspServer.TestWorkspace.AddDocumentAsync(newDocumentInfo);

        // Verify that the newly added document in the registered workspace is returned.
        var (documentWorkspace, document) = await GetLspWorkspaceAndDocumentAsync(newDocumentUri, testLspServer).ConfigureAwait(false);
        AssertEx.NotNull(document);
        Assert.Equal(testLspServer.TestWorkspace, documentWorkspace);
        Assert.Equal(newDocumentId, document.Id);
        // Verify we still are using the tracked LSP text for the document.
        var documentText = await document.GetTextAsync(CancellationToken.None);
        Assert.Equal("More LSP textLSP text", documentText.ToString());
    }

    [Theory, CombinatorialData]
    public async Task TestUsesRegisteredHostWorkspace(bool mutatingLspWorkspace)
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

        await using var testLspServer = await CreateXmlTestLspServerAsync(firstWorkspaceXml, mutatingLspWorkspace);
        // Verify 1 workspace registered to start with.
        Assert.True(IsWorkspaceRegistered(testLspServer.TestWorkspace, testLspServer));

        using var testWorkspaceTwo = TestWorkspace.Create(
            XElement.Parse(secondWorkspaceXml),
            workspaceKind: "OtherWorkspaceKind",
            composition: testLspServer.TestWorkspace.Composition);

        // Wait for workspace creation operations for the second workspace to complete.
        await WaitForWorkspaceOperationsAsync(testWorkspaceTwo);

        // Manually register the workspace since the workspace listener does not listen for this workspace kind.
        var workspaceRegistrationService = testLspServer.TestWorkspace.GetService<LspWorkspaceRegistrationService>();
        workspaceRegistrationService.Register(testWorkspaceTwo);

        // Verify both workspaces registered.
        Assert.True(IsWorkspaceRegistered(testLspServer.TestWorkspace, testLspServer));
        Assert.True(IsWorkspaceRegistered(testWorkspaceTwo, testLspServer));

        // Verify the host workspace returned is the workspace with kind host.
        var (_, hostSolution) = await GetLspHostWorkspaceAndSolutionAsync(testLspServer).ConfigureAwait(false);
        AssertEx.NotNull(hostSolution);
        Assert.Equal("FirstWorkspaceProject", hostSolution.Projects.First().Name);
    }

    [Theory, CombinatorialData]
    public async Task TestWorkspaceRequestFailsWhenHostWorkspaceMissing(bool mutatingLspWorkspace)
    {
        var firstWorkspaceXml =
@$"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""FirstWorkspaceProject"">
        <Document FilePath=""C:\FirstWorkspace.cs"">FirstWorkspace</Document>
    </Project>
</Workspace>";

        await using var testLspServer = await CreateXmlTestLspServerAsync(firstWorkspaceXml, mutatingLspWorkspace, workspaceKind: WorkspaceKind.MiscellaneousFiles);
        var exportProvider = testLspServer.TestWorkspace.ExportProvider;

        var workspaceRegistrationService = exportProvider.GetExport<LspWorkspaceRegistrationService>();

        // Verify the workspace is registered.
        Assert.True(IsWorkspaceRegistered(testLspServer.TestWorkspace, testLspServer));

        // Verify there is not workspace matching the host workspace kind.
        var (_, solution) = await GetLspHostWorkspaceAndSolutionAsync(testLspServer).ConfigureAwait(false);
        Assert.Null(solution);
    }

    [Theory, CombinatorialData]
    public async Task TestLspUpdatesCorrectWorkspaceWithMultipleWorkspacesAsync(bool mutatingLspWorkspace)
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

        await using var testLspServer = await CreateXmlTestLspServerAsync(firstWorkspaceXml, mutatingLspWorkspace);

        using var testWorkspaceTwo = CreateWorkspace(options: null, WorkspaceKind.MSBuild, mutatingLspWorkspace);
        testWorkspaceTwo.InitializeDocuments(XElement.Parse(secondWorkspaceXml));

        // Wait for workspace creation operations to complete for the second workspace.
        await WaitForWorkspaceOperationsAsync(testWorkspaceTwo);

        // Verify both workspaces registered.
        Assert.True(IsWorkspaceRegistered(testLspServer.TestWorkspace, testLspServer));
        Assert.True(IsWorkspaceRegistered(testWorkspaceTwo, testLspServer));

        var firstWorkspaceDocumentUri = ProtocolConversions.CreateAbsoluteUri(@"C:\FirstWorkspace.cs");
        var secondWorkspaceDocumentUri = ProtocolConversions.CreateAbsoluteUri(@"C:\SecondWorkspace.cs");
        await testLspServer.OpenDocumentAsync(firstWorkspaceDocumentUri);

        // Verify we can get both documents from their respective workspaces.
        var (firstWorkspace, firstDocument) = await GetLspWorkspaceAndDocumentAsync(firstWorkspaceDocumentUri, testLspServer).ConfigureAwait(false);
        AssertEx.NotNull(firstDocument);
        Assert.Equal(firstWorkspaceDocumentUri, firstDocument.GetURI());
        Assert.Equal(testLspServer.TestWorkspace, firstWorkspace);

        var (secondWorkspace, secondDocument) = await GetLspWorkspaceAndDocumentAsync(secondWorkspaceDocumentUri, testLspServer).ConfigureAwait(false);
        AssertEx.NotNull(secondDocument);
        Assert.Equal(secondWorkspaceDocumentUri, secondDocument.GetURI());
        Assert.Equal(testWorkspaceTwo, secondWorkspace);

        // Verify making an LSP change only changes the respective workspace and document.
        await testLspServer.InsertTextAsync(firstWorkspaceDocumentUri, (0, 0, "Change in first workspace"));

        // The first document should now different text.
        var (_, changedFirstDocument) = await GetLspWorkspaceAndDocumentAsync(firstWorkspaceDocumentUri, testLspServer).ConfigureAwait(false);
        AssertEx.NotNull(changedFirstDocument);
        var changedFirstDocumentText = await changedFirstDocument.GetTextAsync(CancellationToken.None);
        var firstDocumentText = await firstDocument.GetTextAsync(CancellationToken.None);
        Assert.NotEqual(firstDocumentText, changedFirstDocumentText);

        // The second document should return the same document instance since it was not changed.
        var (_, unchangedSecondDocument) = await GetLspWorkspaceAndDocumentAsync(secondWorkspaceDocumentUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(secondDocument, unchangedSecondDocument);
    }

    [Theory, CombinatorialData]
    public async Task TestWorkspaceEventUpdatesCorrectWorkspaceWithMultipleWorkspacesAsync(bool mutatingLspWorkspace)
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

        await using var testLspServer = await CreateXmlTestLspServerAsync(firstWorkspaceXml, mutatingLspWorkspace);

        using var testWorkspaceTwo = CreateWorkspace(options: null, workspaceKind: WorkspaceKind.MSBuild, mutatingLspWorkspace);
        testWorkspaceTwo.InitializeDocuments(XElement.Parse(secondWorkspaceXml));

        // Wait for workspace operations to complete for the second workspace.
        await WaitForWorkspaceOperationsAsync(testWorkspaceTwo);

        var firstWorkspaceDocumentUri = ProtocolConversions.CreateAbsoluteUri(@"C:\FirstWorkspace.cs");
        var secondWorkspaceDocumentUri = ProtocolConversions.CreateAbsoluteUri(@"C:\SecondWorkspace.cs");
        await testLspServer.OpenDocumentAsync(firstWorkspaceDocumentUri);

        // Verify we can get both documents from their respective workspaces.
        var (firstWorkspace, firstDocument) = await GetLspWorkspaceAndDocumentAsync(firstWorkspaceDocumentUri, testLspServer).ConfigureAwait(false);
        AssertEx.NotNull(firstDocument);
        Assert.Equal(firstWorkspaceDocumentUri, firstDocument.GetURI());
        Assert.Equal(testLspServer.TestWorkspace, firstWorkspace);

        var (secondWorkspace, secondDocument) = await GetLspWorkspaceAndDocumentAsync(secondWorkspaceDocumentUri, testLspServer).ConfigureAwait(false);
        AssertEx.NotNull(secondDocument);
        Assert.Equal(secondWorkspaceDocumentUri, secondDocument.GetURI());
        Assert.Equal(testWorkspaceTwo, secondWorkspace);

        // Verify making a workspace change only changes the respective workspace.
        var newProjectWorkspaceTwo = testWorkspaceTwo.CurrentSolution.Projects.First().WithAssemblyName("NewCSProj1");
        await testWorkspaceTwo.ChangeProjectAsync(newProjectWorkspaceTwo.Id, newProjectWorkspaceTwo.Solution);

        // The second document should have an updated project assembly name.
        var (_, secondDocumentChangedProject) = await GetLspWorkspaceAndDocumentAsync(secondWorkspaceDocumentUri, testLspServer).ConfigureAwait(false);
        AssertEx.NotNull(secondDocumentChangedProject);
        Assert.Equal("NewCSProj1", secondDocumentChangedProject.Project.AssemblyName);
        Assert.NotEqual(secondDocument, secondDocumentChangedProject);

        // The first document should be the same document as the last one since that workspace was not changed.
        var (_, lspDocument) = await GetLspWorkspaceAndDocumentAsync(firstWorkspaceDocumentUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(firstDocument, lspDocument);
    }

    [Theory, CombinatorialData]
    public async Task TestSeparateWorkspaceManagerPerServerAsync(bool mutatingLspWorkspace)
    {
        var workspaceXml =
@$"<Workspace>
    <Project Language=""C#"" CommonReferences=""true"" AssemblyName=""CSProj1"">
        <Document FilePath=""C:\test1.cs"">Original text</Document>
    </Project>
</Workspace>";

        using var testWorkspace = CreateWorkspace(options: null, workspaceKind: null, mutatingLspWorkspace);
        testWorkspace.InitializeDocuments(XElement.Parse(workspaceXml));

        // Wait for workspace creation operations to complete.
        await WaitForWorkspaceOperationsAsync(testWorkspace);

        var documentUri = testWorkspace.CurrentSolution.Projects.First().Documents.First().GetURI();

        await using var testLspServerOne = await TestLspServer.CreateAsync(testWorkspace, new InitializationOptions(), TestOutputLspLogger);
        await using var testLspServerTwo = await TestLspServer.CreateAsync(testWorkspace, new InitializationOptions(), TestOutputLspLogger);

        Assert.NotEqual(testLspServerOne.GetManager(), testLspServerTwo.GetManager());

        // Verify workspace is registered with both servers.
        Assert.True(IsWorkspaceRegistered(testWorkspace, testLspServerOne));
        Assert.True(IsWorkspaceRegistered(testWorkspace, testLspServerTwo));

        // Verify that the LSP solution uses the correct text for each server.
        var documentServerOne = await OpenDocumentAndVerifyLspTextAsync(documentUri, testLspServerOne, "Server one text");

        var (_, documentServerTwo) = await GetLspWorkspaceAndDocumentAsync(documentUri, testLspServerTwo).ConfigureAwait(false);
        AssertEx.NotNull(documentServerTwo);

        if (mutatingLspWorkspace)
        {
            // If the underlying workspace is getting mutated, then given that the second lsp-server hasn't heard
            // anything about doc-1 yet, it will see the change pushed through by the first lsp-server.
            Assert.Equal("Server one text", (await documentServerTwo.GetTextAsync(CancellationToken.None)).ToString());
        }
        else
        {
            Assert.Equal("Original text", (await documentServerTwo.GetTextAsync(CancellationToken.None)).ToString());
        }

        // Verify workspace updates are reflected in both servers.
        var newAssemblyName = "NewCSProj1";
        var newProject = testWorkspace.CurrentSolution.Projects.First().WithAssemblyName(newAssemblyName);
        await testWorkspace.ChangeProjectAsync(newProject.Id, newProject.Solution);

        // Verify LSP solution has the project changes.
        (_, documentServerOne) = await GetLspWorkspaceAndDocumentAsync(documentUri, testLspServerOne).ConfigureAwait(false);
        AssertEx.NotNull(documentServerOne);
        Assert.Equal(newAssemblyName, documentServerOne.Project.AssemblyName);
        (_, documentServerTwo) = await GetLspWorkspaceAndDocumentAsync(documentUri, testLspServerTwo).ConfigureAwait(false);
        AssertEx.NotNull(documentServerTwo);
        Assert.Equal(newAssemblyName, documentServerTwo.Project.AssemblyName);
    }

    [Theory, CombinatorialData]
    public async Task TestDoesNotForkWhenDocumentTextBufferOpenedAsync(bool mutatingLspWorkspace)
    {
        var markup = "Text";
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace);
        var documentUri = testLspServer.GetCurrentSolution().Projects.First().Documents.First().GetURI();

        // Calling get text buffer opens the document in the workspace.
        testLspServer.TestWorkspace.Documents.Single().GetTextBuffer();

        await testLspServer.OpenDocumentAsync(documentUri, "Text");

        var (_, lspDocument) = await GetLspWorkspaceAndDocumentAsync(documentUri, testLspServer).ConfigureAwait(false);
        AssertEx.NotNull(lspDocument);
        Assert.Equal("Text", (await lspDocument.GetTextAsync(CancellationToken.None)).ToString());

        Assert.Same(testLspServer.TestWorkspace.CurrentSolution, lspDocument.Project.Solution);
    }

    [Fact]
    public async Task TestLspDocumentPreferredOverProjectSystemDocumentAddInMutatingWorkspace()
    {
        // This verifies that we will still see the lsp view of the document, even though it found out about a document
        // prior to a project system even telling it about a file, and even if the project system removes the file.

        // Start with an empty workspace.
        await using var testLspServer = await CreateTestLspServerAsync(
            Array.Empty<string>(), mutatingLspWorkspace: true, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });

        // Open the doc
        var filePath = "c:\\\ue25b\ud86d\udeac.cs";
        var documentUri = ProtocolConversions.CreateAbsoluteUri(filePath);
        await testLspServer.OpenDocumentAsync(documentUri, "Text");

        // Initially the doc will be in the lsp misc workspace.
        var (workspace1, document1) = await GetLspWorkspaceAndDocumentAsync(documentUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace1?.Kind);

        AssertEx.NotNull(document1);
        Assert.Equal("Text", (await document1.GetTextAsync(CancellationToken.None)).ToString());

        // Now, add a project to the actual workspace containing Test1.cs with different content.
        await testLspServer.TestWorkspace.ChangeSolutionAsync(
            testLspServer.TestWorkspace.CurrentSolution.Projects.Single()
                .AddDocument(filePath, SourceText.From("ProjectSystemText"), filePath: filePath)
                .Project.Solution);

        // if we were to directly query the workspace right now, the contents won't match what lsp thinks it is:
        Assert.NotEqual(
            (await document1.GetTextAsync(CancellationToken.None)).ToString(),
            (await testLspServer.TestWorkspace.CurrentSolution.Projects.Single().Documents.Single().GetTextAsync()).ToString());

        // However, if we retrieve from lsp now, we'll see the document moved into the real workspace and we'll see the
        // real workspace contents changed.
        (workspace1, document1) = await GetLspWorkspaceAndDocumentAsync(documentUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.Host, workspace1?.Kind);

        AssertEx.NotNull(document1);
        Assert.Equal("Text", (await document1.GetTextAsync(CancellationToken.None)).ToString());

        // Retrieving the document from lsp should push its content into the workspace.
        Assert.Equal(
            (await document1.GetTextAsync(CancellationToken.None)).ToString(),
            (await testLspServer.TestWorkspace.CurrentSolution.Projects.Single().Documents.Single().GetTextAsync()).ToString());

        // The file should not be in the misc workspace.
        Assert.Empty(testLspServer.GetManagerAccessor().GetLspMiscellaneousFilesWorkspace()!.CurrentSolution.Projects);

        // Now, if the project system removes the file, we will still see the lsp version of, but back to the misc workspace.
        await testLspServer.TestWorkspace.ChangeSolutionAsync(
            testLspServer.TestWorkspace.CurrentSolution.Projects.Single().RemoveDocument(document1.Id).Solution);

        (workspace1, document1) = await GetLspWorkspaceAndDocumentAsync(documentUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, workspace1?.Kind);

        AssertEx.NotNull(document1);
        Assert.Equal("Text", (await document1.GetTextAsync(CancellationToken.None)).ToString());
    }

    [Fact]
    public async Task TestLspDocumentPreferredOverProjectSystemDocumentChangeInMutatingWorkspace()
    {
        // This verifies that we will still see the lsp view of the document, even if the project system feeds in
        // external information about the contents of the file.

        // Start with an empty workspace.
        await using var testLspServer = await CreateTestLspServerAsync(
            "Initial Disk Contents", mutatingLspWorkspace: true, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });

        var documentUri = testLspServer.TestWorkspace.CurrentSolution.Projects.Single().Documents.Single().GetURI();
        await testLspServer.OpenDocumentAsync(documentUri, "Text");

        var (workspace, document) = await GetLspWorkspaceAndDocumentAsync(documentUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.Host, workspace?.Kind);

        // We should see the contents lsp told us about.
        AssertEx.NotNull(document);
        Assert.Equal("Text", (await document.GetTextAsync(CancellationToken.None)).ToString());

        // Now, explicitly update the workspace, simulating the project system externally changing it.
        await testLspServer.TestWorkspace.ChangeSolutionAsync(
            testLspServer.TestWorkspace.CurrentSolution.WithDocumentText(document.Id, SourceText.From("New Disk Contents")));

        // Querying the workspace directly, prior to an LSP call will show the new contents.
        document = testLspServer.TestWorkspace.CurrentSolution.Projects.Single().Documents.Single();
        Assert.Equal("New Disk Contents", (await document.GetTextAsync()).ToString());

        // However, once we retrieve from LSP, the lsp contents should be pushed back into the workspace.
        (workspace, document) = await GetLspWorkspaceAndDocumentAsync(documentUri, testLspServer).ConfigureAwait(false);

        AssertEx.NotNull(document);
        Assert.Equal("Text", (await document.GetTextAsync(CancellationToken.None)).ToString());

        document = testLspServer.TestWorkspace.CurrentSolution.Projects.Single().Documents.Single();
        Assert.Equal("Text", (await document.GetTextAsync()).ToString());
    }

    [Fact]
    public async Task TestLspDocumentChangedInMutatingWorkspaceIncrementallyParses()
    {
        // This verifies that we will still see the lsp view of the document, even if the project system feeds in
        // external information about the contents of the file.

        // Start with an empty workspace.
        await using var testLspServer = await CreateTestLspServerAsync(
            "Initial Disk Contents", mutatingLspWorkspace: true, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });

        var initialContents = "namespace N { class C1 { void X() { } } class C2 { void Y() { } } class C3 { void Z() { } } }";
        var documentUri = testLspServer.TestWorkspace.CurrentSolution.Projects.Single().Documents.Single().GetURI();
        await testLspServer.OpenDocumentAsync(documentUri, initialContents);

        var (workspace, originalDocument) = await GetLspWorkspaceAndDocumentAsync(documentUri, testLspServer).ConfigureAwait(false);
        Assert.Equal(WorkspaceKind.Host, workspace?.Kind);

        // We should see the contents lsp told us about.
        AssertEx.NotNull(originalDocument);
        var originalSourceText = await originalDocument.GetTextAsync(CancellationToken.None);
        var originalRoot = await originalDocument.GetRequiredSyntaxRootAsync(CancellationToken.None);
        Assert.Equal(initialContents, originalSourceText.ToString());

        // Change "C3 => D3"
        await testLspServer.ReplaceTextAsync(documentUri,
            (ProtocolConversions.TextSpanToRange(new TextSpan(initialContents.IndexOf("C3"), 1), originalSourceText), "D"));

        var (_, newDocument) = await GetLspWorkspaceAndDocumentAsync(documentUri, testLspServer).ConfigureAwait(false);

        AssertEx.NotNull(newDocument);
        var newSourceText = await newDocument.GetTextAsync(CancellationToken.None);
        var newRoot = await newDocument.GetRequiredSyntaxRootAsync(CancellationToken.None);
        Assert.Equal("namespace N { class C1 { void X() { } } class C2 { void Y() { } } class D3 { void Z() { } } }", newSourceText.ToString());

        // Demonstrate that incremental parsing is working by showing that the first class is reused across edits, while
        // the last class is not.  We don't test the second as that one may or may not be reused depending on how good
        // incremental parsing may be.  For example, sometimes it will conservatively not reuse a node because that node
        // touches a node that is getting recreated.
        var syntaxFacts = originalDocument.GetRequiredLanguageService<ISyntaxFactsService>();
        var oldClassDeclarations = originalRoot.DescendantNodes().Where(n => syntaxFacts.IsClassDeclaration(n)).ToImmutableArray();
        var newClassDeclarations = newRoot.DescendantNodes().Where(n => syntaxFacts.IsClassDeclaration(n)).ToImmutableArray();

        Assert.Equal(oldClassDeclarations.Length, newClassDeclarations.Length);
        Assert.Equal(3, oldClassDeclarations.Length);

        Assert.True(oldClassDeclarations[0].IsIncrementallyIdenticalTo(newClassDeclarations[0]));
        Assert.False(oldClassDeclarations[2].IsIncrementallyIdenticalTo(newClassDeclarations[2]));

        // All the methods will get reused.
        var oldMethodDeclarations = originalRoot.DescendantNodes().Where(n => syntaxFacts.IsMethodLevelMember(n)).ToImmutableArray();
        var newMethodDeclarations = newRoot.DescendantNodes().Where(n => syntaxFacts.IsMethodLevelMember(n)).ToImmutableArray();

        Assert.Equal(oldMethodDeclarations.Length, newMethodDeclarations.Length);
        Assert.Equal(3, oldMethodDeclarations.Length);

        Assert.True(oldMethodDeclarations[0].IsIncrementallyIdenticalTo(newMethodDeclarations[0]));
        Assert.True(oldMethodDeclarations[1].IsIncrementallyIdenticalTo(newMethodDeclarations[1]));
        Assert.True(oldMethodDeclarations[2].IsIncrementallyIdenticalTo(newMethodDeclarations[2]));
    }

    private static async Task<Document> OpenDocumentAndVerifyLspTextAsync(Uri documentUri, TestLspServer testLspServer, string openText = "LSP text")
    {
        await testLspServer.OpenDocumentAsync(documentUri, openText);

        // Verify we can find the document with correct text in the new LSP solution.
        var (_, lspDocument) = await GetLspWorkspaceAndDocumentAsync(documentUri, testLspServer).ConfigureAwait(false);
        AssertEx.NotNull(lspDocument);
        Assert.Equal(openText, (await lspDocument.GetTextAsync(CancellationToken.None)).ToString());
        return lspDocument;
    }

    private static bool IsWorkspaceRegistered(Workspace workspace, TestLspServer testLspServer)
    {
        return testLspServer.GetManagerAccessor().IsWorkspaceRegistered(workspace);
    }

    private static async Task<(Workspace? workspace, Document? document)> GetLspWorkspaceAndDocumentAsync(Uri uri, TestLspServer testLspServer)
    {
        var (workspace, _, document) = await testLspServer.GetManager().GetLspDocumentInfoAsync(CreateTextDocumentIdentifier(uri), CancellationToken.None).ConfigureAwait(false);
        return (workspace, document);
    }

    private static Task<(Workspace?, Solution?)> GetLspHostWorkspaceAndSolutionAsync(TestLspServer testLspServer)
    {
        return testLspServer.GetManager().GetLspSolutionInfoAsync(CancellationToken.None);
    }
}
