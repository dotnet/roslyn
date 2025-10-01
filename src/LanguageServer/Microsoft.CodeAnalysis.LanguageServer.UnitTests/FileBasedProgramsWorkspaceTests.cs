// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.LanguageServer.UnitTests.Miscellaneous;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.UnitTests;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;
using Roslyn.Test.Utilities;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class FileBasedProgramsWorkspaceTests : AbstractLspMiscellaneousFilesWorkspaceTests, IDisposable
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly TestOutputLoggerProvider _loggerProvider;
    private readonly TempRoot _tempRoot;
    private readonly TempDirectory _mefCacheDirectory;

    public FileBasedProgramsWorkspaceTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _loggerProvider = new TestOutputLoggerProvider(testOutputHelper);
        _loggerFactory = new LoggerFactory([_loggerProvider]);
        _tempRoot = new();
        _mefCacheDirectory = _tempRoot.CreateDirectory();
    }

    public void Dispose()
    {
        _tempRoot.Dispose();
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

    private protected override async ValueTask<Document> AddDocumentAsync(TestLspServer testLspServer, string filePath, string content)
    {
        // For the file-based programs, we want to put them in the real workspace via the real host service
        var workspaceFactory = testLspServer.TestWorkspace.ExportProvider.GetExportedValue<LanguageServerWorkspaceFactory>();
        var project = await workspaceFactory.HostProjectFactory.CreateAndAddToWorkspaceAsync(
            Guid.NewGuid().ToString(),
            LanguageNames.CSharp,
            new ProjectSystemProjectCreationInfo { AssemblyName = Guid.NewGuid().ToString() },
            workspaceFactory.ProjectSystemHostInfo);

        project.AddSourceFile(filePath);

        return workspaceFactory.HostWorkspace.CurrentSolution.GetRequiredProject(project.Id).Documents.Single();
    }

    private protected override Workspace GetHostWorkspace(TestLspServer testLspServer)
    {
        var workspaceFactory = testLspServer.TestWorkspace.ExportProvider.GetExportedValue<LanguageServerWorkspaceFactory>();
        return workspaceFactory.HostWorkspace;
    }

    [Theory, CombinatorialData]
    public async Task TestLspTransfersFromFileBaedProgramToHostWorkspaceDocumentOrderingAsync(bool mutatingLspWorkspace)
    {
        var markup = "Console.WriteLine();";

        // Create a server that includes the LSP misc files workspace so we can test transfers to and from it.
        await using var testLspServer = await CreateTestLspServerAsync(markup, mutatingLspWorkspace, new InitializationOptions { ServerKind = WellKnownLspServerKinds.CSharpVisualBasicLspServer });

        // Include some Unicode characters to test URL handling.
        using var tempRoot = new TempRoot();
        var newDocumentFilePath = Path.Combine(tempRoot.CreateDirectory().Path, "Program.cs");
        File.WriteAllText(newDocumentFilePath, markup);

        var newDocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(newDocumentFilePath);

        // Ensure LSP has the text
        await testLspServer.OpenDocumentAsync(newDocumentUri, "Console.WriteLine();");

        // The first call should return a document in the misc files workspace.
        var (_, miscDocument) = await GetLspWorkspaceAndDocumentAsync(newDocumentUri, testLspServer).ConfigureAwait(false);
        Assert.NotNull(miscDocument);
        Assert.True(await testLspServer.GetManagerAccessor().IsMiscellaneousFilesDocumentAsync(miscDocument));
        Assert.Equal(WorkspaceKind.MiscellaneousFiles, miscDocument.Project.Solution.Workspace.Kind);

        // Wait for the project system to load the file as a FBP
        await testLspServer.TestWorkspace.GetService<AsynchronousOperationListenerProvider>().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();

        // The second call should return an FBP document in the host workspace.
        var (documentWorkspace, document) = await GetLspWorkspaceAndDocumentAsync(newDocumentUri, testLspServer).ConfigureAwait(false);
        Assert.NotNull(document);
        Assert.NotNull(documentWorkspace);
        Assert.Equal(WorkspaceKind.Host, documentWorkspace.Kind);
        // FBP project path is just the document file path.
        Assert.Equal(newDocumentFilePath, document.Project.FilePath);

        // Now, upadate the host workspace to also include the document (simulating it getting added by the project system later on).
        var workspaceFactory = testLspServer.TestWorkspace.ExportProvider.GetExportedValue<LanguageServerWorkspaceFactory>();
        var projectName = "MainSolutionProject";
        var project = await workspaceFactory.HostProjectFactory.CreateAndAddToWorkspaceAsync(
            Guid.NewGuid().ToString(),
            LanguageNames.CSharp,
            new ProjectSystemProjectCreationInfo { AssemblyName = projectName },
            workspaceFactory.ProjectSystemHostInfo);

        project.AddSourceFile(newDocumentFilePath);

        // Wait for the project system to load the host project
        await testLspServer.TestWorkspace.GetService<AsynchronousOperationListenerProvider>().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();

        // LSP should return the document from the main solution instead of the FBP.
        (documentWorkspace, document) = await GetLspWorkspaceAndDocumentAsync(newDocumentUri, testLspServer).ConfigureAwait(false);
        Assert.NotNull(document);
        Assert.NotNull(documentWorkspace);
        Assert.Equal(WorkspaceKind.Host, documentWorkspace.Kind);
        Assert.Equal(document.Project.AssemblyName, projectName);

        // LSP request context will have the main sln document, GetTextDocumentAsync should also return the FBP document (as the order is in the sln state).
        var documentFromAPI = document.Project.Solution.GetTextDocumentAsync(CreateTextDocumentIdentifier(newDocumentUri), CancellationToken.None);
        Assert.Equal(document, await documentFromAPI);
    }
}
