// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.LanguageServer.Logging;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;
using Roslyn.LanguageServer.Protocol;
using Roslyn.Test.Utilities;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests.Miscellaneous;

public abstract class AbstractLspMiscellaneousFilesWorkspaceTests : AbstractLanguageServerProtocolTests, IDisposable
{
    private readonly TestOutputLoggerProvider _loggerProvider;
    private readonly ILoggerFactory _loggerFactory;
    protected readonly TempRoot TempRoot;

    /// <summary>
    /// Projects created via <see cref="AddDocumentAsync"/>. These are created directly against the host project factory
    /// (bypassing the project loader that would normally dispose them on shutdown), so the test owns their lifetime and
    /// must remove them on teardown to release the file watches they hold.
    /// </summary>
    private readonly List<ProjectSystemProject> _projectsToRemoveOnDispose = [];

    /// <summary>
    /// Snapshot of the file watches active before this test ran. Used to verify that the server releases every file
    /// watch it created once it shuts down (see <see cref="FileWatcherReleaseTracker"/>).
    /// </summary>
    private readonly FileWatcherReleaseTracker _fileWatcherReleaseTracker;

    public AbstractLspMiscellaneousFilesWorkspaceTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _loggerProvider = new TestOutputLoggerProvider(testOutputHelper);
        _loggerFactory = new LoggerFactory([_loggerProvider]);
        TempRoot = new();
        _fileWatcherReleaseTracker = FileWatcherReleaseTracker.Capture();
    }

    public void Dispose()
    {
        // Projects created via AddDocumentAsync are created directly against the host project factory and bypass the
        // project loader, so nothing else removes them. Remove them now (while the host workspace is still alive) to
        // release the file watches they hold, mirroring what LoadedProject.Dispose does for loader-managed projects.
        foreach (var project in _projectsToRemoveOnDispose)
            project.RemoveFromWorkspace();

        TempRoot.Dispose();
        _loggerProvider.Dispose();
        _loggerFactory.Dispose();

        // The test's server(s) are disposed by the test body (via 'await using'), which releases their file watches on
        // shutdown. Verify that actually happened so a watch-leaking test fails here rather than leaking into a later test.
        _fileWatcherReleaseTracker.AssertWatchesReleased();
    }

    protected override ValueTask<ExportProvider> CreateExportProviderAsync()
    {
        AsynchronousOperationListenerProvider.Enable(enable: true);
        return new(LanguageServerTestComposition.GetSharedExportProvider(AbstractLanguageServerHostTests.DefaultServerConfiguration, _loggerFactory));
    }

    private protected Workspace GetHostWorkspace(TestLspServer testLspServer)
    {
        var provider = testLspServer.GetRequiredLspService<IHostWorkspaceProvider>();
        return provider.Workspace;
    }

    private protected async ValueTask<Document> AddDocumentAsync(TestLspServer testLspServer, string filePath)
    {
        // For the file-based programs, we want to put them in the real workspace via the real host service
        var workspaceFactory = testLspServer.GetRequiredLspService<LanguageServerWorkspaceFactory>();
        var project = await workspaceFactory.HostProjectFactory.CreateAndAddToWorkspaceAsync(
            Guid.NewGuid().ToString(),
            LanguageNames.CSharp,
            new ProjectSystemProjectCreationInfo { AssemblyName = Guid.NewGuid().ToString() },
            workspaceFactory.ProjectSystemHostInfo);

        project.AddSourceFile(filePath);

        // The test owns this project's lifetime; track it so it is removed (releasing its file watches) on teardown.
        _projectsToRemoveOnDispose.Add(project);

        return workspaceFactory.HostWorkspace.CurrentSolution.GetRequiredProject(project.Id).Documents.Single();
    }

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

    private protected static async ValueTask<TextDocument?> GetMiscellaneousAdditionalDocumentAsync(TestLspServer testLspServer)
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

    private protected async Task AssertFileInMainWorkspaceAsync(TestLspServer testLspServer, DocumentUri fileUri)
    {
        var (lspWorkspace, _, _) = await testLspServer.GetManager().GetLspDocumentInfoAsync(new LSP.TextDocumentIdentifier { DocumentUri = fileUri }, CancellationToken.None).ConfigureAwait(false);
        Assert.Equal(GetHostWorkspace(testLspServer), lspWorkspace);
    }

    private protected static async Task<LSP.Hover> RunGetHoverAsync(TestLspServer testLspServer, LSP.Location caret)
    {
        var result = await testLspServer.ExecuteRequestAsync<LSP.TextDocumentPositionParams, LSP.Hover>(LSP.Methods.TextDocumentHoverName,
            CreateTextDocumentPositionParams(caret), CancellationToken.None);
        Contract.ThrowIfNull(result);
        return result;
    }
}
