// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
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
    private readonly TempDirectory _mefCacheDirectory;
    protected readonly TempRoot TempRoot;

    public AbstractLspMiscellaneousFilesWorkspaceTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _loggerProvider = new TestOutputLoggerProvider(testOutputHelper);
        _loggerFactory = new LoggerFactory([_loggerProvider]);
        TempRoot = new();
        _mefCacheDirectory = TempRoot.CreateDirectory();
    }

    public void Dispose()
    {
        TempRoot.Dispose();
        _loggerProvider.Dispose();
    }

    protected override async ValueTask<ExportProvider> CreateExportProviderAsync()
    {
        AsynchronousOperationListenerProvider.Enable(enable: true);

        var (exportProvider, _) = await LanguageServerTestComposition.CreateExportProviderAsync(
            _loggerFactory,
            includeDevKitComponents: false,
            cacheDirectory: _mefCacheDirectory.Path,
            extensionPaths: []);

        return exportProvider;
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
