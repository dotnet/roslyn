// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias MSBuildWorkspacesContracts;

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.Handler.Diagnostics;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.LanguageServer.Services;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;
using Roslyn.Test.Utilities;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;
using DiagnosticLogItem = MSBuildWorkspacesContracts::Microsoft.CodeAnalysis.MSBuild.DiagnosticLogItem;
using DiagnosticLogItemKind = MSBuildWorkspacesContracts::Microsoft.CodeAnalysis.MSBuild.DiagnosticLogItemKind;
using DocumentFileInfo = MSBuildWorkspacesContracts::Microsoft.CodeAnalysis.MSBuild.DocumentFileInfo;
using ProjectFileInfo = MSBuildWorkspacesContracts::Microsoft.CodeAnalysis.MSBuild.ProjectFileInfo;
using TestProjectLoader = Microsoft.CodeAnalysis.LanguageServer.UnitTests.LanguageServerProjectLoaderTests.TestProjectLoader;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

[UseExportProvider]
public sealed class ProjectLoadDiagnosticSourceProviderTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerHostTests(testOutputHelper)
{
    private protected override Task<ExportProvider> CreateExportProviderAsync(
        ServerConfiguration serverConfiguration,
        ILoggerFactory loggerFactory,
        ExtensionAssemblyManager extensionManager,
        IAssemblyLoader assemblyLoader)
        => Task.FromResult(LanguageServerTestComposition.GetSharedExportProvider(
            serverConfiguration, loggerFactory, typeof(LanguageServerProjectLoaderTests.TestProjectLoaderFactory), typeof(TestProjectLoaderDiagnosticSourceProvider)));

    [Fact]
    public async Task LoadErrorsAreReportedAsWorkspaceDiagnosticsUntilNextSuccessfulLoad()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var projectPath = Path.Combine(TempRoot.Root, "Project.csproj");
        File.WriteAllText(projectPath, "");

        var (loadedProject, failedDesignTimeBuild) = await loader.ExpectDesignTimeBuildAndBeginLoadingProjectAsync(projectPath);
        await failedDesignTimeBuild.Started.Task;
        failedDesignTimeBuild.CompleteWithError(loader.WorkspaceFactory.HostProjectFactory, new DiagnosticLogItem(DiagnosticLogItemKind.Error, "Expected load error", projectPath));
        Assert.True(await loadedProject.WaitForLoadAsync(CancellationToken.None));

        // Pull twice and assert twice to make sure we don't lose existing results
        var client = new WorkspaceDiagnosticsClient(server);
        for (var pull = 0; pull < 2; pull++)
        {
            await client.PullAsync();
            var diagnostic = Assert.Single(client.GetDiagnostics(projectPath));
            Assert.Equal(LSP.DiagnosticSeverity.Error, diagnostic.Severity);
            Assert.Equal("Expected load error", diagnostic.Message);
        }

        var successfulDesignTimeBuild = loader.ExpectDesignTimeBuild(projectPath);
        loadedProject.GetTestAccessor().RaiseNeedsReload();
        await successfulDesignTimeBuild.Started.Task;
        successfulDesignTimeBuild.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);

        await client.PullAsync();
        Assert.Empty(client.GetDiagnostics(projectPath));
        Assert.Single(loader.WorkspaceFactory.HostWorkspace.CurrentSolution.Projects);
    }

    [Fact]
    public async Task LoadWarningInOnlyOneTargetSurvivesPullingAgain()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var projectPath = Path.Combine(TempRoot.Root, "Project.csproj");
        File.WriteAllText(projectPath, "");

        var (loadedProject, designTimeBuild) = await loader.ExpectDesignTimeBuildAndBeginLoadingProjectAsync(projectPath);
        await designTimeBuild.Started.Task;
        designTimeBuild.Complete(
            loader.WorkspaceFactory.HostProjectFactory,
            projectPath,
            [
                ProjectFileInfo.CreateEmpty(LanguageNames.CSharp, projectPath) with { TargetFramework = "net8.0" },
                ProjectFileInfo.CreateEmpty(LanguageNames.CSharp, projectPath) with
                {
                    TargetFramework = "net9.0",
                    Diagnostics = [new DiagnosticLogItem(DiagnosticLogItemKind.Warning, "Expected target warning", projectPath)],
                },
            ],
            evaluationDiagnostics: []);
        Assert.True(await loadedProject.WaitForLoadAsync(CancellationToken.None));

        // Pull twice and assert twice to make sure we don't lose existing results
        var client = new WorkspaceDiagnosticsClient(server);
        for (var pull = 0; pull < 2; pull++)
        {
            await client.PullAsync();
            Assert.Equal("Expected target warning", Assert.Single(client.GetDiagnostics(projectPath)).Message);
        }
    }

    [Fact]
    public async Task LoadDiagnosticsOfAllTargetsAreVisibleToTheClientAcrossPulls()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var projectPath = Path.Combine(TempRoot.Root, "Project.csproj");
        File.WriteAllText(projectPath, "");

        var (loadedProject, designTimeBuild) = await loader.ExpectDesignTimeBuildAndBeginLoadingProjectAsync(projectPath);
        await designTimeBuild.Started.Task;
        designTimeBuild.Complete(
            loader.WorkspaceFactory.HostProjectFactory,
            projectPath,
            [
                ProjectFileInfo.CreateEmpty(LanguageNames.CSharp, projectPath) with
                {
                    TargetFramework = "net8.0",
                    Diagnostics = [new DiagnosticLogItem(DiagnosticLogItemKind.Warning, "Expected net8.0 warning", projectPath)],
                },
                ProjectFileInfo.CreateEmpty(LanguageNames.CSharp, projectPath) with
                {
                    TargetFramework = "net9.0",
                    Diagnostics = [new DiagnosticLogItem(DiagnosticLogItemKind.Warning, "Expected net9.0 warning", projectPath)],
                },
            ],
            evaluationDiagnostics: []);
        Assert.True(await loadedProject.WaitForLoadAsync(CancellationToken.None));

        // Pull twice and assert twice to make sure we don't lose existing results
        var client = new WorkspaceDiagnosticsClient(server);
        for (var pull = 0; pull < 2; pull++)
        {
            await client.PullAsync();
            AssertEx.SetEqual(["Expected net8.0 warning", "Expected net9.0 warning"], client.GetDiagnostics(projectPath).Select(diagnostic => diagnostic.Message));
        }
    }

    [Fact]
    public async Task EvaluationDiagnosticsAreReportedOnceAlongWithTargetDiagnostics()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var projectPath = Path.Combine(TempRoot.Root, "Project.csproj");
        File.WriteAllText(projectPath, "");

        var (loadedProject, designTimeBuild) = await loader.ExpectDesignTimeBuildAndBeginLoadingProjectAsync(projectPath);
        await designTimeBuild.Started.Task;
        designTimeBuild.Complete(
            loader.WorkspaceFactory.HostProjectFactory,
            projectPath,
            [
                ProjectFileInfo.CreateEmpty(LanguageNames.CSharp, projectPath) with
                {
                    TargetFramework = "net8.0",
                    Diagnostics = [new DiagnosticLogItem(DiagnosticLogItemKind.Warning, "Expected target warning", projectPath)],
                },
                ProjectFileInfo.CreateEmpty(LanguageNames.CSharp, projectPath) with { TargetFramework = "net9.0" },
            ],
            evaluationDiagnostics: [new DiagnosticLogItem(DiagnosticLogItemKind.Warning, "Expected evaluation warning", projectPath)]);
        Assert.True(await loadedProject.WaitForLoadAsync(CancellationToken.None));
        Assert.Equal(2, loader.WorkspaceFactory.HostWorkspace.CurrentSolution.ProjectIds.Count);

        // The evaluation warning applies to both targets, so it is shown once alongside the warning from just one of them.
        // Pull twice and assert twice to make sure we don't lose existing results
        var client = new WorkspaceDiagnosticsClient(server);
        for (var pull = 0; pull < 2; pull++)
        {
            await client.PullAsync();
            Assert.Collection(
                client.GetDiagnostics(projectPath),
                diagnostic =>
                {
                    Assert.Equal("Expected evaluation warning", diagnostic.Message);
                    Assert.Equal(LSP.DiagnosticSeverity.Warning, diagnostic.Severity);
                },
                diagnostic =>
                {
                    Assert.Equal("Expected target warning", diagnostic.Message);
                    Assert.Equal(LSP.DiagnosticSeverity.Warning, diagnostic.Severity);
                });
        }
    }

    [Fact]
    public async Task LoadErrorInProjectFileIsReportedAtItsPosition()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var projectPath = Path.Combine(TempRoot.Root, "Project.csproj");
        File.WriteAllText(projectPath, "");

        var (loadedProject, designTimeBuild) = await loader.ExpectDesignTimeBuildAndBeginLoadingProjectAsync(projectPath);
        await designTimeBuild.Started.Task;
        designTimeBuild.CompleteWithError(loader.WorkspaceFactory.HostProjectFactory, new DiagnosticLogItem(DiagnosticLogItemKind.Error, "Expected load error", projectPath, projectPath, LineNumber: 3, ColumnNumber: 5, Code: "MSB1234"));
        await loadedProject.WaitForLoadAsync(CancellationToken.None);

        // Pull twice and assert twice to make sure we don't lose existing results
        var client = new WorkspaceDiagnosticsClient(server);
        for (var pull = 0; pull < 2; pull++)
        {
            await client.PullAsync();
            var diagnostic = Assert.Single(client.GetDiagnostics(projectPath));
            Assert.Equal("Expected load error", diagnostic.Message);
            Assert.Equal("MSB1234", diagnostic.Code?.Value);
            Assert.Equal(new LSP.Position { Line = 2, Character = 4 }, diagnostic.Range.Start);
        }
    }

    [Fact]
    public async Task LoadErrorInImportedFileIsReportedOnThatFileUntilNextSuccessfulLoad()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var projectPath = Path.Combine(TempRoot.Root, "Project.csproj");
        var importedPath = Path.Combine(TempRoot.Root, "Imported.targets");
        File.WriteAllText(projectPath, "");

        var (loadedProject, designTimeBuild) = await loader.ExpectDesignTimeBuildAndBeginLoadingProjectAsync(projectPath);
        await designTimeBuild.Started.Task;
        designTimeBuild.CompleteWithError(loader.WorkspaceFactory.HostProjectFactory, new DiagnosticLogItem(DiagnosticLogItemKind.Error, "Expected load error", projectPath, importedPath, LineNumber: 30, ColumnNumber: 5));
        await loadedProject.WaitForLoadAsync(CancellationToken.None);

        // Pull twice and assert twice to make sure we don't lose existing results
        var client = new WorkspaceDiagnosticsClient(server);
        for (var pull = 0; pull < 2; pull++)
        {
            await client.PullAsync();
            var diagnostic = Assert.Single(client.GetDiagnostics(importedPath));
            Assert.Equal("Expected load error", diagnostic.Message);
            Assert.Equal(new LSP.Position { Line = 29, Character = 4 }, diagnostic.Range.Start);
            Assert.Empty(client.GetDiagnostics(projectPath));
        }

        var successfulDesignTimeBuild = loader.ExpectDesignTimeBuild(projectPath);
        loadedProject.GetTestAccessor().RaiseNeedsReload();
        await successfulDesignTimeBuild.Started.Task;
        successfulDesignTimeBuild.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);

        await client.PullAsync();
        Assert.Empty(client.GetDiagnostics(importedPath));
    }

    [Fact]
    public async Task LoadErrorInFileImportedByTwoProjectsIsReportedOnce()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var firstProjectPath = Path.Combine(TempRoot.Root, "First.csproj");
        var secondProjectPath = Path.Combine(TempRoot.Root, "Second.csproj");
        var importedPath = Path.Combine(TempRoot.Root, "Directory.Build.props");
        File.WriteAllText(firstProjectPath, "");
        File.WriteAllText(secondProjectPath, "");

        foreach (var projectPath in new[] { firstProjectPath, secondProjectPath })
        {
            var (loadedProject, designTimeBuild) = await loader.ExpectDesignTimeBuildAndBeginLoadingProjectAsync(projectPath);
            await designTimeBuild.Started.Task;
            designTimeBuild.CompleteWithError(loader.WorkspaceFactory.HostProjectFactory, new DiagnosticLogItem(DiagnosticLogItemKind.Error, "Expected load error", projectPath, importedPath, LineNumber: 30, ColumnNumber: 5));
            await loadedProject.WaitForLoadAsync(CancellationToken.None);
        }

        // Pull twice and assert twice to make sure we don't lose existing results
        var client = new WorkspaceDiagnosticsClient(server);
        for (var pull = 0; pull < 2; pull++)
        {
            await client.PullAsync();
            Assert.Equal("Expected load error", Assert.Single(client.GetDiagnostics(importedPath)).Message);
            Assert.Empty(client.GetDiagnostics(firstProjectPath));
            Assert.Empty(client.GetDiagnostics(secondProjectPath));
        }
    }

    [Fact]
    public async Task LoadErrorOfProjectWhoseFileIsAlsoItsDocumentSurvivesPullingAgain()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();

        // Like a file-based program, the project's file is the one document in it
        var projectPath = Path.Combine(TempRoot.Root, "Program.cs");
        File.WriteAllText(projectPath, "");

        var (loadedProject, designTimeBuild) = await loader.ExpectDesignTimeBuildAndBeginLoadingProjectAsync(projectPath);
        await designTimeBuild.Started.Task;
        designTimeBuild.Complete(
            loader.WorkspaceFactory.HostProjectFactory,
            projectPath,
            [ProjectFileInfo.CreateEmpty(LanguageNames.CSharp, projectPath) with { Documents = [new DocumentFileInfo(projectPath, "Program.cs", isLinked: false, isGenerated: false, folders: [])] }],
            evaluationDiagnostics: [new DiagnosticLogItem(DiagnosticLogItemKind.Warning, "Expected load warning", projectPath)]);
        Assert.True(await loadedProject.WaitForLoadAsync(CancellationToken.None));

        // Pull twice and assert twice to make sure we don't lose existing results
        var client = new WorkspaceDiagnosticsClient(server);
        for (var pull = 0; pull < 2; pull++)
        {
            await client.PullAsync();
            Assert.Equal("Expected load warning", Assert.Single(client.GetDiagnostics(projectPath)).Message);
        }
    }

    [Fact]
    public async Task UnchangedLoadDiagnosticsAreNotReportedAgain()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var projectPath = Path.Combine(TempRoot.Root, "Project.csproj");
        var importedPath = Path.Combine(TempRoot.Root, "Imported.targets");
        File.WriteAllText(projectPath, "");

        var (loadedProject, designTimeBuild) = await loader.ExpectDesignTimeBuildAndBeginLoadingProjectAsync(projectPath);
        await designTimeBuild.Started.Task;
        designTimeBuild.Complete(
            loader.WorkspaceFactory.HostProjectFactory,
            projectPath,
            [ProjectFileInfo.CreateEmpty(LanguageNames.CSharp, projectPath)],
            evaluationDiagnostics:
            [
                new DiagnosticLogItem(DiagnosticLogItemKind.Warning, "Expected project warning", projectPath),
                new DiagnosticLogItem(DiagnosticLogItemKind.Warning, "Expected imported warning", projectPath, importedPath),
            ]);
        Assert.True(await loadedProject.WaitForLoadAsync(CancellationToken.None));

        var client = new WorkspaceDiagnosticsClient(server);
        await client.PullAsync();
        Assert.True(client.WasReportedByLastPull(projectPath));
        Assert.True(client.WasReportedByLastPull(importedPath));

        await client.PullAsync();
        Assert.False(client.WasReportedByLastPull(projectPath));
        Assert.False(client.WasReportedByLastPull(importedPath));
        Assert.Single(client.GetDiagnostics(projectPath));
        Assert.Single(client.GetDiagnostics(importedPath));
    }

    /// <summary>
    /// Plays the part of a client that keeps the most recent report for each URI and gives them back as the previous result ids on its next pull.
    /// </summary>
    private sealed class WorkspaceDiagnosticsClient(SingleServerTestLspServer server)
    {
        private readonly Dictionary<string, (string ResultId, LSP.Diagnostic[] Diagnostics)> _reportsByUri = [];
        private ImmutableHashSet<string> _urisReportedByLastPull = [];

        public async Task PullAsync()
        {
            await server.ExportProvider.GetExportedValue<AsynchronousOperationListenerProvider>().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();

            var previousResultIds = _reportsByUri.Select(pair => new LSP.PreviousResultId { Uri = new LSP.DocumentUri(pair.Key), Value = pair.Value.ResultId }).ToArray();
            var reportTask = server.ExecuteRequestAsync<LSP.WorkspaceDiagnosticParams, LSP.WorkspaceDiagnosticReport>(
                LSP.Methods.WorkspaceDiagnosticName,
                new LSP.WorkspaceDiagnosticParams { Identifier = TestProjectLoaderDiagnosticSourceProvider.ProviderName, PreviousResultId = previousResultIds },
                CancellationToken.None);

            // Workspace diagnostics wait for a change before closing the connection, so tell it to close to let the pull finish
            server.GetRequiredLspService<WorkspacePullDiagnosticsHandler>().GetTestAccessor().TriggerConnectionClose();

            var report = await reportTask;
            Assert.NotNull(report);

            // Reports are applied in order, so if more than one is for the same URI the last one wins
            foreach (var item in report.Items)
            {
                var fullReport = (LSP.WorkspaceFullDocumentDiagnosticReport)item.Value!;
                if (fullReport.ResultId is null)
                    _reportsByUri.Remove(fullReport.Uri.UriString);
                else
                    _reportsByUri[fullReport.Uri.UriString] = (fullReport.ResultId, fullReport.Items);
            }

            _urisReportedByLastPull = [.. report.Items.Select(item => ((LSP.WorkspaceFullDocumentDiagnosticReport)item.Value!).Uri.UriString)];
        }

        public ImmutableArray<LSP.Diagnostic> GetDiagnostics(string filePath)
            => _reportsByUri.TryGetValue(ProtocolConversions.CreateAbsoluteDocumentUri(filePath).UriString, out var report) ? [.. report.Diagnostics] : [];

        public bool WasReportedByLastPull(string filePath)
            => _urisReportedByLastPull.Contains(ProtocolConversions.CreateAbsoluteDocumentUri(filePath).UriString);
    }

    [Export(typeof(IDiagnosticSourceProvider)), PartNotDiscoverable, Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class TestProjectLoaderDiagnosticSourceProvider()
        : AbstractProjectLoadDiagnosticSourceProvider(ProviderName)
    {
        public const string ProviderName = "TestProjectLoad";

        protected override LanguageServerProjectLoader? GetProjectLoader(RequestContext context)
            => context.GetService<TestProjectLoader>();
    }
}
