// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Composition;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Features.Workspaces;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.UnitTests.Miscellaneous;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.MiscellaneousFiles;

/// <summary>
/// This class runs all the tests in <see cref="AbstractLspMiscellaneousFilesWorkspaceTests"/> against the base implementation.
/// </summary>
public sealed partial class LspMiscellaneousFilesWorkspaceProviderTests : AbstractLspMiscellaneousFilesWorkspaceTests
{
    public LspMiscellaneousFilesWorkspaceProviderTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Theory, CombinatorialData]
    public async Task TestLooseFile_Opened(bool mutatingLspWorkspace)
    {
        // Create a server that supports LSP misc files and verify no misc files present.
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));

        // Open an empty loose file and make a request to verify it gets added to the misc workspace.
        var looseFileUri = CreateAbsoluteDocumentUri("SomeFile.cs");
        await testLspServer.OpenDocumentAsync(looseFileUri, """
            class A
            {
                void M()
                {
                }
            }
            """).ConfigureAwait(false);

        // Verify file is added to the misc file workspace.
        await AssertFileInMiscWorkspaceAsync(testLspServer, looseFileUri).ConfigureAwait(false);
    }

    [Theory, CombinatorialData]
    public async Task TestLooseFile_Changed(bool mutatingLspWorkspace)
    {
        // Create a server that supports LSP misc files and verify no misc files present.
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });

        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));

        var looseFileUri = CreateAbsoluteDocumentUri("SomeFile.cs");

        // Open an empty loose file and make a request to verify it gets added to the misc workspace.
        await testLspServer.OpenDocumentAsync(looseFileUri, string.Empty).ConfigureAwait(false);
        await AssertFileInMiscWorkspaceAsync(testLspServer, looseFileUri).ConfigureAwait(false);

        // Assert that the misc workspace contains the initial document.
        var miscWorkspaceText = await (await GetMiscellaneousDocumentAsync(testLspServer))!.GetTextAsync(CancellationToken.None);
        Assert.Empty(miscWorkspaceText.ToString());

        // Make a text change to the loose file and verify requests appropriately reflect the changes.
        await testLspServer.InsertTextAsync(looseFileUri, (0, 0, """
            class A
            {
                void M()
                {
                }
            }
            """)).ConfigureAwait(false);
        var caret = new LSP.Location { Range = new() { Start = new(0, 6), End = new(0, 7) }, DocumentUri = looseFileUri };
        var hover = await RunGetHoverAsync(testLspServer, caret).ConfigureAwait(false);
        Assert.Contains("class A", hover.Contents.Fourth.Value);
        await AssertFileInMiscWorkspaceAsync(testLspServer, looseFileUri).ConfigureAwait(false);

        // Assert that the misc workspace contains the updated document.
        miscWorkspaceText = await (await GetMiscellaneousDocumentAsync(testLspServer))!.GetTextAsync(CancellationToken.None);
        Assert.Contains("class A", miscWorkspaceText.ToString());
    }

    [Theory, CombinatorialData]
    public async Task TestLooseFile_Closed(bool mutatingLspWorkspace)
    {
        // Create a server that supports LSP misc files and verify no misc files present.
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));

        // Open an empty loose file and make a request to verify it gets added to the misc workspace.
        var looseFileUri = CreateAbsoluteDocumentUri("SomeFile.cs");
        await testLspServer.OpenDocumentAsync(looseFileUri, """
            class A
            {
                void M()
                {
                }
            }
            """).ConfigureAwait(false);
        await AssertFileInMiscWorkspaceAsync(testLspServer, looseFileUri).ConfigureAwait(false);

        // Verify the loose file is removed from the misc workspace on close.
        await testLspServer.CloseDocumentAsync(looseFileUri).ConfigureAwait(false);
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));
    }

    [Theory, CombinatorialData]
    public async Task TestFileInRegisteredWorkspace_Opened(bool mutatingLspWorkspace)
    {
        var markup =
            """
            class A
            {
                void M()
                {
                }
            }
            """;

        // Create a server that supports LSP misc files and verify no misc files present.
        await using var testLspServer = await CreateTestLspServerAsync("", mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));

        // Open a file that is part of a registered workspace and verify it is not present in the misc workspace.
        var document = await AddDocumentAsync(testLspServer, TestHelpers.CreateAbsolutePath("SomeFile.cs")).ConfigureAwait(false);
        var fileInWorkspaceUri = document.GetURI();
        await testLspServer.OpenDocumentAsync(fileInWorkspaceUri, markup).ConfigureAwait(false);
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));
    }

    [Theory, CombinatorialData]
    public async Task TestLooseFile_MovedToRegisteredWorkspace(bool mutatingLspWorkspace)
    {
        var source =
            """
            class A
            {
                void M()
                {
                }
            }
            """;

        // Create a server that supports LSP misc files and verify no misc files present.
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));

        // Open an empty loose file and make a request to verify it gets added to the misc workspace.
        // Include some Unicode characters to test URL handling.
        var looseFileUri = CreateAbsoluteDocumentUri("\ue25b\ud86d\udeac.cs");
        var looseFileTextDocumentIdentifier = new LSP.TextDocumentIdentifier { DocumentUri = looseFileUri };
        await testLspServer.OpenDocumentAsync(looseFileUri, source).ConfigureAwait(false);

        // Verify that the file returned by the manager is in the lsp misc files workspace.
        await AssertFileInMiscWorkspaceAsync(testLspServer, looseFileUri).ConfigureAwait(false);
        var (miscWorkspace, _, miscDocument) = await testLspServer.GetManager().GetLspDocumentInfoAsync(new LSP.TextDocumentIdentifier { DocumentUri = looseFileUri }, CancellationToken.None);
        Contract.ThrowIfNull(miscWorkspace);
        Contract.ThrowIfNull(miscDocument);
        Assert.True(miscWorkspace.CurrentSolution.ContainsDocument(miscDocument.Id));

        var documentPath = looseFileUri.GetDocumentFilePathFromUri();

        // Update the workspace to contain the loose file.
        await AddDocumentAsync(testLspServer, documentPath);
        Assert.Contains(documentPath, GetHostWorkspace(testLspServer).CurrentSolution.Projects.Single().Documents.Select(d => d.FilePath));

        // Verify that the manager returns the file that has been added to the main workspace.
        await AssertFileInMainWorkspaceAsync(testLspServer, looseFileUri).ConfigureAwait(false);

        // Make sure doc was removed from misc workspace.
        Assert.False(miscWorkspace.CurrentSolution.ContainsDocument(miscDocument.Id));
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));
    }

    [Theory, CombinatorialData]
    public async Task TestLooseFile_RequestedTwiceAndClosed(bool mutatingLspWorkspace)
    {
        // Create a server that supports LSP misc files and verify no misc files present.
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));

        // Open an empty loose file and make a request to verify it gets added to the misc workspace.
        var looseFileUri = CreateAbsoluteDocumentUri("SomeFile.cs");
        await testLspServer.OpenDocumentAsync(looseFileUri, """
            class A
            {
                void M()
                {
                }
            }
            """).ConfigureAwait(false);

        // Trigger a request and assert we got a file in the misc workspace.
        await AssertFileInMiscWorkspaceAsync(testLspServer, looseFileUri).ConfigureAwait(false);

        // Trigger another request and assert we got a file in the misc workspace.
        await AssertFileInMiscWorkspaceAsync(testLspServer, looseFileUri).ConfigureAwait(false);

        await testLspServer.CloseDocumentAsync(looseFileUri).ConfigureAwait(false);
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));
    }

    [Theory, CombinatorialData]
    public async Task TestLspTransfersFromMiscellaneousFilesToHostWorkspaceAsync(bool mutatingLspWorkspace, bool waitForWorkspace, bool fileBasedProgramContent)
    {
        var markup = fileBasedProgramContent ? "Console.WriteLine();" : "class C { }";

        // Create a server that includes the LSP misc files workspace so we can test transfers to and from it.
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });

        // Include some Unicode characters to test URL handling.
        using var tempRoot = new TempRoot();
        var newDocumentFilePath = Path.Combine(tempRoot.CreateDirectory().Path, "ue25b\ud86d\udeac.cs");

        // If this is file based, we're going to be inspecting the actual content on disk as a part of a dotnet run-api invocation
        if (fileBasedProgramContent)
            File.WriteAllText(newDocumentFilePath, markup);
        var newDocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(newDocumentFilePath);

        // Open the document via LSP before the workspace sees it.
        await testLspServer.OpenDocumentAsync(newDocumentUri, "LSP text");

        // Verify it is a miscellaneous document of some kind
        var (_, miscDocument) = await GetLspWorkspaceAndDocumentAsync(newDocumentUri, testLspServer).ConfigureAwait(false);
        Assert.NotNull(miscDocument);
        Assert.True(await testLspServer.GetManagerAccessor().IsMiscellaneousFilesDocumentAsync(miscDocument));
        Assert.Equal("LSP text", (await miscDocument.GetTextAsync(CancellationToken.None)).ToString());

        if (waitForWorkspace)
        {
            // Optionally wait for the workspace so we can test what happens if we're seeing if it's a file-based program; otherwise we can test what
            // happens if that analysis is still happening while we're loading real solutions.
            await testLspServer.TestWorkspace.GetService<AsynchronousOperationListenerProvider>().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();
        }

        // Make a change and verify the misc document is updated.
        await testLspServer.InsertTextAsync(newDocumentUri, (0, 0, "More LSP text"));
        (_, miscDocument) = await GetLspWorkspaceAndDocumentAsync(newDocumentUri, testLspServer).ConfigureAwait(false);
        Assert.NotNull(miscDocument);
        var miscText = await miscDocument.GetTextAsync(CancellationToken.None);
        Assert.Equal("More LSP textLSP text", miscText.ToString());
        Assert.True(await testLspServer.GetManagerAccessor().IsMiscellaneousFilesDocumentAsync(miscDocument));

        // Update the registered workspace with the new document.
        var newDocumentId = (await AddDocumentAsync(testLspServer, newDocumentFilePath)).Id;

        // Verify that the newly added document in the registered workspace is returned.
        var (documentWorkspace, document) = await GetLspWorkspaceAndDocumentAsync(newDocumentUri, testLspServer).ConfigureAwait(false);
        Assert.NotNull(document);
        Assert.Equal(GetHostWorkspace(testLspServer), documentWorkspace);
        Assert.False(await testLspServer.GetManagerAccessor().IsMiscellaneousFilesDocumentAsync(document));
        Assert.Equal(newDocumentId, document.Id);
        // Verify we still are using the tracked LSP text for the document.
        var documentText = await document.GetTextAsync(CancellationToken.None);
        Assert.Equal("More LSP textLSP text", documentText.ToString());

        // There should not be any other misc document in the solution anymore.
        var matchingDocuments = await document.Project.Solution.GetTextDocumentsAsync(newDocumentUri, CancellationToken.None);
        Assert.Single(matchingDocuments);
    }

    [Theory, CombinatorialData]
    public async Task TestLooseFile_OpenedWithLanguageId(bool mutatingLspWorkspace)
    {
        // Create a server that supports LSP misc files and verify no misc files present.
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));

        // Open an empty loose file that hasn't been saved with a name.

        var looseFileUri = new DocumentUri("untitled:untitledFile");

        await testLspServer.OpenDocumentAsync(looseFileUri, """
            class A
            {
                void M()
                {
                }
            }
            """, languageId: "csharp").ConfigureAwait(false);

        // Verify file is added to the misc file workspace.
        await AssertFileInMiscWorkspaceAsync(testLspServer, looseFileUri).ConfigureAwait(false);
        var miscDoc = await GetMiscellaneousDocumentAsync(testLspServer);
        Assert.NotNull(miscDoc);
        Assert.Equal(LanguageNames.CSharp, miscDoc.Project.Language);
    }

    [Theory, CombinatorialData]
    public async Task TestLooseFile_OpenedWithLanguageIdWithSubsequentRequest(bool mutatingLspWorkspace)
    {
        // Create a server that supports LSP misc files and verify no misc files present.
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));

        // Open an empty loose file that hasn't been saved with a name.

        var looseFileUri = new DocumentUri("untitled:untitledFile");

        await testLspServer.OpenDocumentAsync(looseFileUri, """
            class A
            {
                void M()
                {
                    A a = new A();
                }
            }
            """, languageId: "csharp").ConfigureAwait(false);
        // Make an immediate followup request as soon as we queue the didOpen.
        // This should succeed and use the language from the didOpen.
        var result = await testLspServer.ExecuteRequestAsync<TextDocumentPositionParams, LSP.Location[]>(Methods.TextDocumentDefinitionName,
            CreateTextDocumentPositionParams(new LSP.Location
            {
                DocumentUri = looseFileUri,
                Range = new LSP.Range
                {
                    Start = new(4, 8)
                }
            }), CancellationToken.None);

        // Verify file was added to the misc file workspace.
        await AssertFileInMiscWorkspaceAsync(testLspServer, looseFileUri).ConfigureAwait(false);
        var miscDoc = await GetMiscellaneousDocumentAsync(testLspServer);
        Assert.NotNull(miscDoc);
        Assert.Equal(LanguageNames.CSharp, miscDoc.Project.Language);

        // Verify GTD request succeeded.
        Assert.NotNull(result);
        Assert.Equal(0, result.Single().Range.Start.Line);
        Assert.Equal(6, result.Single().Range.Start.Character);
        Assert.Equal(0, result.Single().Range.End.Line);
        Assert.Equal(7, result.Single().Range.End.Character);
    }

    [Theory, CombinatorialData]
    public async Task TestLooseFile_RazorFile(bool mutatingLspWorkspace)
    {
        // Create a server that supports LSP misc files and verify no misc files present.
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));
        Assert.Null(await GetMiscellaneousAdditionalDocumentAsync(testLspServer));

        // Open an empty loose file and make a request to verify it gets added to the misc workspace.
        var looseFileUri = CreateAbsoluteDocumentUri("SomeFile.razor");
        await testLspServer.OpenDocumentAsync(looseFileUri, "<div></div>").ConfigureAwait(false);

        // Trigger a request and assert we got a file in the misc workspace.
        await AssertFileInMiscWorkspaceAsync(testLspServer, looseFileUri).ConfigureAwait(false);
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));
        Assert.NotNull(await GetMiscellaneousAdditionalDocumentAsync(testLspServer));

        // Trigger another request and assert we got a file in the misc workspace.
        await AssertFileInMiscWorkspaceAsync(testLspServer, looseFileUri).ConfigureAwait(false);
        Assert.NotNull(await GetMiscellaneousAdditionalDocumentAsync(testLspServer));

        await testLspServer.CloseDocumentAsync(looseFileUri).ConfigureAwait(false);
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));
        Assert.Null(await GetMiscellaneousAdditionalDocumentAsync(testLspServer));
    }

    [Theory, CombinatorialData]
    public async Task TestLooseFilesInCanonicalProject(bool mutatingLspWorkspace)
    {
        // Create a server that supports LSP misc files and verify no misc files present.
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));

        var looseFileUriOne = CreateAbsoluteDocumentUri("SomeFile.cs");
        await testLspServer.OpenDocumentAsync(looseFileUriOne, """
            class A
            {
                void M()
                {
                }
            }
            """).ConfigureAwait(false);

        // Document should be initially found in a primordial misc files project
        var (_, looseDocumentOne) = await GetLspWorkspaceAndDocumentAsync(looseFileUriOne, testLspServer).ConfigureAwait(false);
        Assert.NotNull(looseDocumentOne);
        Assert.Equal(1, looseDocumentOne.Project.Documents.Count());
        Assert.Empty(looseDocumentOne.Project.MetadataReferences);

        // Wait for the canonical project to finish loading.
        await testLspServer.TestWorkspace.GetService<AsynchronousOperationListenerProvider>().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();

        // Verify the document is found in a forked canonical project.
        var (_, canonicalDocumentOne) = await GetLspWorkspaceAndDocumentAsync(looseFileUriOne, testLspServer).ConfigureAwait(false);
        Assert.NotNull(canonicalDocumentOne);
        Assert.NotEqual(looseDocumentOne, canonicalDocumentOne);
        // Should have the appropriate generated files now that we ran a design time build
        Assert.Contains(canonicalDocumentOne.Project.Documents, d => d.Name == "Canonical.AssemblyInfo.cs");

        // Add another loose virtual document and verify it goes into a forked canonical project.
        var looseFileUriTwo = ProtocolConversions.CreateAbsoluteDocumentUri(@"vscode-notebook-cell://dev-container/test.cs");
        await testLspServer.OpenDocumentAsync(looseFileUriTwo, """
            class Other
            {
                void OtherMethod()
                {
                }
            }
            """).ConfigureAwait(false);

        var (_, canonicalDocumentTwo) = await GetLspWorkspaceAndDocumentAsync(looseFileUriTwo, testLspServer).ConfigureAwait(false);

        // Wait for the canonical project to finish loading.
        await testLspServer.TestWorkspace.GetService<AsynchronousOperationListenerProvider>().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();
        (_, canonicalDocumentTwo) = await GetLspWorkspaceAndDocumentAsync(looseFileUriTwo, testLspServer).ConfigureAwait(false);

        Assert.NotNull(canonicalDocumentTwo);
        Assert.NotEqual(canonicalDocumentOne.Project.Id, canonicalDocumentTwo.Project.Id);
        Assert.DoesNotContain(canonicalDocumentTwo.Project.Documents, d => d.Name == looseDocumentOne.Name);
        // Semantic diagnostics are not expected due to absence of top-level statements
        Assert.False(canonicalDocumentTwo.Project.State.HasAllInformation);
        // Should have the appropriate generated files from the base misc files project now that we ran a design time build
        Assert.Contains(canonicalDocumentTwo.Project.Documents, d => d.Name == "Canonical.AssemblyInfo.cs");
    }
}

