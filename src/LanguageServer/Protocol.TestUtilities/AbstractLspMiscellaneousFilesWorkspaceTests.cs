// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));

        // Open a file that is part of a registered workspace and verify it is not present in the misc workspace.
        var fileInWorkspaceUri = testLspServer.GetCurrentSolution().Projects.Single().Documents.Single().GetURI();
        await testLspServer.OpenDocumentAsync(fileInWorkspaceUri).ConfigureAwait(false);
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
        var project = testLspServer.GetCurrentSolution().Projects.Single();
        var documentInfo = DocumentInfo.Create(
                DocumentId.CreateNewId(project.Id),
                documentPath,
                sourceCodeKind: SourceCodeKind.Regular,
                loader: new TestTextLoader(source),
                filePath: documentPath);
        testLspServer.TestWorkspace.OnDocumentAdded(documentInfo);
        await WaitForWorkspaceOperationsAsync(testLspServer.TestWorkspace);

        Assert.Contains(documentPath, testLspServer.GetCurrentSolution().Projects.Single().Documents.Select(d => d.FilePath));

        // Verify that the manager returns the file that has been added to the main workspace.
        await AssertFileInMainWorkspaceAsync(testLspServer, looseFileUri).ConfigureAwait(false);

        // Make sure doc was removed from misc workspace.
        Assert.False(miscWorkspace.CurrentSolution.ContainsDocument(miscDocument.Id));
        Assert.Null(await GetMiscellaneousDocumentAsync(testLspServer));
    }

    [Theory, CombinatorialData]
    public async Task TestLooseFile_RazorFile(bool mutatingLspWorkspace)
    {
        // Create a server that supports LSP misc files and verify no misc files present.
        await using var testLspServer = await CreateTestLspServerAsync(string.Empty, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });
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

    private static async ValueTask<Document?> GetMiscellaneousDocumentAsync(TestLspServer testLspServer)
    {
        var documents = await testLspServer.GetManagerAccessor().GetMiscellaneousDocumentsAsync(static p => p.Documents).ToImmutableArrayAsync(CancellationToken.None);
        return documents.SingleOrDefault();
    }

    private static async ValueTask<TextDocument?> GetMiscellaneousAdditionalDocumentAsync(TestLspServer testLspServer)
    {
        var documents = await testLspServer.GetManagerAccessor().GetMiscellaneousDocumentsAsync(static p => p.AdditionalDocuments).ToImmutableArrayAsync(CancellationToken.None);
        return documents.SingleOrDefault();
    }

    private static async Task AssertFileInMiscWorkspaceAsync(TestLspServer testLspServer, DocumentUri fileUri)
    {
        var (_, _, document) = await testLspServer.GetManager().GetLspDocumentInfoAsync(new LSP.TextDocumentIdentifier { DocumentUri = fileUri }, CancellationToken.None);
        Assert.NotNull(document);
        Assert.True(await testLspServer.GetManagerAccessor().IsMiscellaneousFilesDocumentAsync(document));
    }

    private static async Task AssertFileInMainWorkspaceAsync(TestLspServer testLspServer, DocumentUri fileUri)
    {
        var (lspWorkspace, _, _) = await testLspServer.GetManager().GetLspDocumentInfoAsync(new LSP.TextDocumentIdentifier { DocumentUri = fileUri }, CancellationToken.None).ConfigureAwait(false);
        Assert.Equal(testLspServer.TestWorkspace, lspWorkspace);
    }

    private static async Task<LSP.Hover> RunGetHoverAsync(TestLspServer testLspServer, LSP.Location caret)
    {
        var result = await testLspServer.ExecuteRequestAsync<LSP.TextDocumentPositionParams, LSP.Hover>(LSP.Methods.TextDocumentHoverName,
            CreateTextDocumentPositionParams(caret), CancellationToken.None);
        Contract.ThrowIfNull(result);
        return result;
    }
}
