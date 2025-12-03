// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Features.Workspaces;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Miscellaneous;

public abstract class AbstractLspMiscellaneousFilesWorkspaceTests : AbstractLanguageServerProtocolTests
{
    public AbstractLspMiscellaneousFilesWorkspaceTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
    }

    [Theory, CombinatorialData]
    public async Task TestLooseFile_Opened(bool mutatingLspWorkspace)
    {
        // Create a server that supports LSP misc files and verify no misc files present.
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));

        // Open an empty loose file and make a request to verify it gets added to the misc workspace.
        var looseFileUri = ProtocolConversions.CreateAbsoluteDocumentUri(@"C:\SomeFile.cs");
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

        var looseFileUri = ProtocolConversions.CreateAbsoluteDocumentUri(@"C:\SomeFile.cs");

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
        var looseFileUri = ProtocolConversions.CreateAbsoluteDocumentUri(@"C:\SomeFile.cs");
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
        var document = await AddDocumentAsync(testLspServer, "C:\\SomeFile.cs", markup).ConfigureAwait(false);
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
        var looseFileUri = ProtocolConversions.CreateAbsoluteDocumentUri("C:\\\ue25b\ud86d\udeac.cs");
        var looseFileTextDocumentIdentifier = new LSP.TextDocumentIdentifier { DocumentUri = looseFileUri };
        await testLspServer.OpenDocumentAsync(looseFileUri, source).ConfigureAwait(false);

        // Verify that the file returned by the manager is in the lsp misc files workspace.
        await AssertFileInMiscWorkspaceAsync(testLspServer, looseFileUri).ConfigureAwait(false);
        var (miscWorkspace, _, miscDocument) = await testLspServer.GetManager().GetLspDocumentInfoAsync(new LSP.TextDocumentIdentifier { DocumentUri = looseFileUri }, CancellationToken.None);
        Contract.ThrowIfNull(miscWorkspace);
        Contract.ThrowIfNull(miscDocument);
        Assert.True(miscWorkspace.CurrentSolution.ContainsDocument(miscDocument.Id));

        var documentPath = ProtocolConversions.GetDocumentFilePathFromUri(looseFileUri.GetRequiredParsedUri());

        // Update the workspace to contain the loose file.
        await AddDocumentAsync(testLspServer, documentPath, source);
        Assert.Contains(documentPath, GetHostWorkspace(testLspServer).CurrentSolution.Projects.Single().Documents.Select(d => d.FilePath));

        // Verify that the manager returns the file that has been added to the main workspace.
        await AssertFileInMainWorkspaceAsync(testLspServer, looseFileUri).ConfigureAwait(false);

        // Make sure doc was removed from misc workspace.
        Assert.False(miscWorkspace.CurrentSolution.ContainsDocument(miscDocument.Id));
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));
    }

    [Theory, CombinatorialData]
    public async Task TestLooseFile_RazorFile(bool mutatingLspWorkspace)
    {
        var composition = Composition.AddParts(typeof(TestRazorMiscellaneousProjectInfoService));

        // Create a server that supports LSP misc files and verify no misc files present.
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer }, composition);
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));
        Assert.Null(await GetMiscellaneousAdditionalDocumentAsync(testLspServer));

        // Open an empty loose file and make a request to verify it gets added to the misc workspace.
        var looseFileUri = ProtocolConversions.CreateAbsoluteDocumentUri(@"C:\SomeFile.razor");
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
    public async Task TestLooseFile_RequestedTwiceAndClosed(bool mutatingLspWorkspace)
    {
        // Create a server that supports LSP misc files and verify no misc files present.
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));

        // Open an empty loose file and make a request to verify it gets added to the misc workspace.
        var looseFileUri = ProtocolConversions.CreateAbsoluteDocumentUri(@"C:\SomeFile.cs");
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
        AssertEx.NotNull(miscDoc);
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
        var result = await testLspServer.ExecuteRequestAsync<LSP.TextDocumentPositionParams, LSP.Location[]>(LSP.Methods.TextDocumentDefinitionName,
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
        AssertEx.NotNull(miscDoc);
        Assert.Equal(LanguageNames.CSharp, miscDoc.Project.Language);

        // Verify GTD request succeeded.
        AssertEx.NotNull(result);
        Assert.Equal(0, result.Single().Range.Start.Line);
        Assert.Equal(6, result.Single().Range.Start.Character);
        Assert.Equal(0, result.Single().Range.End.Line);
        Assert.Equal(7, result.Single().Range.End.Character);
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
        AssertEx.NotNull(miscDocument);
        var miscText = await miscDocument.GetTextAsync(CancellationToken.None);
        Assert.Equal("More LSP textLSP text", miscText.ToString());
        Assert.True(await testLspServer.GetManagerAccessor().IsMiscellaneousFilesDocumentAsync(miscDocument));

        // Update the registered workspace with the new document.
        var newDocumentId = (await AddDocumentAsync(testLspServer, newDocumentFilePath, "New Doc")).Id;

        // Verify that the newly added document in the registered workspace is returned.
        var (documentWorkspace, document) = await GetLspWorkspaceAndDocumentAsync(newDocumentUri, testLspServer).ConfigureAwait(false);
        AssertEx.NotNull(document);
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

    private protected abstract ValueTask<Document> AddDocumentAsync(TestLspServer testLspServer, string filePath, string content);
    private protected abstract Workspace GetHostWorkspace(TestLspServer testLspServer);

    private protected static async Task<(Workspace? workspace, Document? document)> GetLspWorkspaceAndDocumentAsync(DocumentUri uri, TestLspServer testLspServer)
    {
        var (workspace, _, document) = await testLspServer.GetManager().GetLspDocumentInfoAsync(CreateTextDocumentIdentifier(uri), CancellationToken.None).ConfigureAwait(false);
        return (workspace, document as Document);
    }

    private protected static async Task<(Workspace workspace, Document document)> GetRequiredLspWorkspaceAndDocumentAsync(DocumentUri uri, TestLspServer testLspServer)
    {
        var (workspace, document) = await GetLspWorkspaceAndDocumentAsync(uri, testLspServer);
        Assert.NotNull(workspace);
        Assert.NotNull(document);
        return (workspace, document);
    }

    private protected static async ValueTask<Document?> GetMiscellaneousDocumentAsync(TestLspServer testLspServer)
    {
        var documents = await testLspServer.GetManagerAccessor().GetMiscellaneousDocumentsAsync(static p => p.Documents).ToImmutableArrayAsync(CancellationToken.None);
        return documents.SingleOrDefault();
    }

    private static async ValueTask<TextDocument?> GetMiscellaneousAdditionalDocumentAsync(TestLspServer testLspServer)
    {
        var documents = await testLspServer.GetManagerAccessor().GetMiscellaneousDocumentsAsync(static p => p.AdditionalDocuments).ToImmutableArrayAsync(CancellationToken.None);
        return documents.SingleOrDefault();
    }

    private protected static async Task AssertFileInMiscWorkspaceAsync(TestLspServer testLspServer, DocumentUri fileUri)
    {
        var (_, _, document) = await testLspServer.GetManager().GetLspDocumentInfoAsync(new LSP.TextDocumentIdentifier { DocumentUri = fileUri }, CancellationToken.None);
        Assert.NotNull(document);
        Assert.True(await testLspServer.GetManagerAccessor().IsMiscellaneousFilesDocumentAsync(document));
    }

    private async Task AssertFileInMainWorkspaceAsync(TestLspServer testLspServer, DocumentUri fileUri)
    {
        var (lspWorkspace, _, _) = await testLspServer.GetManager().GetLspDocumentInfoAsync(new LSP.TextDocumentIdentifier { DocumentUri = fileUri }, CancellationToken.None).ConfigureAwait(false);
        Assert.Equal(GetHostWorkspace(testLspServer), lspWorkspace);
    }

    private static async Task<LSP.Hover> RunGetHoverAsync(TestLspServer testLspServer, LSP.Location caret)
    {
        var result = await testLspServer.ExecuteRequestAsync<LSP.TextDocumentPositionParams, LSP.Hover>(LSP.Methods.TextDocumentHoverName,
            CreateTextDocumentPositionParams(caret), CancellationToken.None);
        Contract.ThrowIfNull(result);
        return result;
    }

    // This is a test version of the real service which lives in the Razor EA, which is not referenced here
    [PartNotDiscoverable]
    [ExportLanguageService(typeof(IMiscellaneousProjectInfoService), "Razor"), Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    private sealed class TestRazorMiscellaneousProjectInfoService() : IMiscellaneousProjectInfoService
    {
        public string ProjectLanguageOverride => LanguageNames.CSharp;
        public bool AddAsAdditionalDocument => true;

        public IEnumerable<AnalyzerReference>? GetAnalyzerReferences(SolutionServices services) => null;
    }
}
