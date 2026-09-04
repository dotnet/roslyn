// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias MSBuildWorkspacesContracts;

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.ProjectTelemetry;
using Microsoft.CodeAnalysis.LanguageServer.Services;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.CodeAnalysis.Workspaces.ProjectSystem;
using Microsoft.CommonLanguageServerProtocol.Framework;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Composition;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;
using ProjectFileInfo = MSBuildWorkspacesContracts::Microsoft.CodeAnalysis.MSBuild.ProjectFileInfo;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

[UseExportProvider]
public sealed class LanguageServerProjectLoaderTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerHostTests(testOutputHelper)
{
    private static Task<bool> WaitForLoadAsync(LoadedProject loadedProject)
        => loadedProject.WaitForLoadAsync(CancellationToken.None).AsTask();

    private protected override Task<ExportProvider> CreateExportProviderAsync(
        ServerConfiguration serverConfiguration,
        ILoggerFactory loggerFactory,
        ExtensionAssemblyManager extensionManager,
        IAssemblyLoader assemblyLoader)
        => Task.FromResult(LanguageServerTestComposition.GetSharedExportProvider(
            serverConfiguration, loggerFactory, typeof(TestProjectLoaderFactory)));

    [Fact]
    public async Task ConcurrentCallersShareLoadedProjectAndCompleteAfterWorkspaceCommit()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var evaluation = loader.QueueEvaluation();
        var projectPath = Path.Combine(TempRoot.Root, "Project.csproj");
        var equivalentPath = Path.Combine(TempRoot.Root, "directory", "..", "Project.csproj");

        var firstLoadedProject = await loader.BeginLoadAsync(projectPath);
        var secondLoadedProject = await loader.BeginLoadAsync(equivalentPath);

        Assert.Same(firstLoadedProject, secondLoadedProject);
        Assert.False(WaitForLoadAsync(firstLoadedProject).IsCompleted);
        await evaluation.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        Assert.Equal(1, loader.EvaluationCount);

        evaluation.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);
        var loadedSuccessfully = await WaitForLoadAsync(firstLoadedProject).WaitAsync(TestHelpers.HangMitigatingTimeout);

        Assert.True(loadedSuccessfully);
        Assert.NotEmpty(loader.WorkspaceFactory.HostWorkspace.CurrentSolution.Projects);
    }

    [Fact]
    public async Task LoadedProjectReturnsWithoutReevaluation()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var evaluation = loader.QueueEvaluation();
        var projectPath = Path.Combine(TempRoot.Root, "Project.csproj");

        var firstLoadedProject = await loader.BeginLoadAsync(projectPath);
        await evaluation.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        evaluation.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);
        var firstStatus = await WaitForLoadAsync(firstLoadedProject).WaitAsync(TestHelpers.HangMitigatingTimeout);

        var loadedProject = await loader.BeginLoadAsync(projectPath);

        Assert.Same(firstLoadedProject, loadedProject);
        Assert.Equal(firstStatus, await WaitForLoadAsync(loadedProject));
        Assert.Equal(1, loader.EvaluationCount);
    }

    [Fact]
    public async Task NeedsReloadTriggersReevaluationAfterInitialLoadCompletes()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var firstEvaluation = loader.QueueEvaluation();
        var secondEvaluation = loader.QueueEvaluation();
        var projectPath = Path.Combine(TempRoot.Root, "Project.csproj");

        var loadedProject = await loader.BeginLoadAsync(projectPath);
        await firstEvaluation.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        firstEvaluation.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);
        await WaitForLoadAsync(loadedProject).WaitAsync(TestHelpers.HangMitigatingTimeout);

        loadedProject.GetTestAccessor().RaiseNeedsReload();

        // A file-change-triggered reload after the initial load has committed must still reach the MSBuild host,
        // rather than being dropped because it carries the (already-completed) load operation from the initial request.
        await secondEvaluation.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        secondEvaluation.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);
        await loader.WaitForLoadsAsync();

        Assert.Equal(2, loader.EvaluationCount);
    }

    [Fact]
    public async Task SamePathLoadsInBatchAreDeduplicated()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var firstEvaluation = loader.QueueEvaluation();
        var projectPath = Path.Combine(TempRoot.Root, "Project.csproj");

        var requestedProject = await loader.BeginLoadAsync(projectPath);
        requestedProject.GetTestAccessor().RaiseNeedsReload();

        await firstEvaluation.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);

        firstEvaluation.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);
        await Task.WhenAll(WaitForLoadAsync(requestedProject), loader.WaitForLoadsAsync()).WaitAsync(TestHelpers.HangMitigatingTimeout);

        Assert.Equal(1, loader.EvaluationCount);
    }

    [Fact]
    public async Task FailedReloadPreservesLoadedStatus()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var successfulEvaluation = loader.QueueEvaluation();
        var failedEvaluation = loader.QueueEvaluation();
        var projectPath = Path.Combine(TempRoot.Root, "Project.csproj");

        var initialLoadedProject = await loader.BeginLoadAsync(projectPath);
        await successfulEvaluation.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        successfulEvaluation.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath, targetFramework: "net8.0");
        Assert.True(await WaitForLoadAsync(initialLoadedProject).WaitAsync(TestHelpers.HangMitigatingTimeout));

        initialLoadedProject.GetTestAccessor().RaiseNeedsReload();
        await failedEvaluation.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        failedEvaluation.Fail(new InvalidOperationException("Expected reload failure"));
        await loader.WaitForLoadsAsync().WaitAsync(TestHelpers.HangMitigatingTimeout);

        var loadedProjectAfterFailedReload = await loader.BeginLoadAsync(projectPath);
        Assert.Same(initialLoadedProject, loadedProjectAfterFailedReload);
        Assert.True(await WaitForLoadAsync(loadedProjectAfterFailedReload));
    }

    [Fact]
    public async Task FailedProjectOnlyRetriesForFileChange()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var failedEvaluation = loader.QueueEvaluation();
        var successfulReload = loader.QueueEvaluation();
        var projectPath = Path.Combine(TempRoot.Root, "Project.csproj");

        var firstLoadedProject = await loader.BeginLoadAsync(projectPath);
        await failedEvaluation.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        failedEvaluation.Fail(new InvalidOperationException("Expected test failure"));
        Assert.False(await WaitForLoadAsync(firstLoadedProject).WaitAsync(TestHelpers.HangMitigatingTimeout));
        await loader.WaitForLoadsAsync().WaitAsync(TestHelpers.HangMitigatingTimeout);

        var loadedProject = await loader.BeginLoadAsync(projectPath);

        Assert.Same(firstLoadedProject, loadedProject);
        Assert.False(await WaitForLoadAsync(loadedProject));
        Assert.Equal(1, loader.EvaluationCount);

        loadedProject.GetTestAccessor().RaiseNeedsReload();
        await successfulReload.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        successfulReload.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);
        await loader.WaitForLoadsAsync().WaitAsync(TestHelpers.HangMitigatingTimeout);

        Assert.True(await WaitForLoadAsync(loadedProject));
        Assert.Equal(2, loader.EvaluationCount);
    }

    [Fact]
    public async Task FailureCompletesOnlyAffectedProject()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var firstEvaluation = loader.QueueEvaluation();
        var secondEvaluation = loader.QueueEvaluation();
        var failedPath = Path.Combine(TempRoot.Root, "Failed.csproj");
        var successfulPath = Path.Combine(TempRoot.Root, "Successful.csproj");

        var failedProject = await loader.BeginLoadAsync(failedPath);
        var successfulProject = await loader.BeginLoadAsync(successfulPath);
        await Task.WhenAll(firstEvaluation.Started.Task, secondEvaluation.Started.Task).WaitAsync(TestHelpers.HangMitigatingTimeout);

        var failedEvaluation = firstEvaluation.Started.Task.Result == failedPath ? firstEvaluation : secondEvaluation;
        var successfulEvaluation = firstEvaluation.Started.Task.Result == successfulPath ? firstEvaluation : secondEvaluation;
        failedEvaluation.Fail(new InvalidOperationException("Expected test failure"));
        successfulEvaluation.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, successfulPath);

        Assert.False(await WaitForLoadAsync(failedProject).WaitAsync(TestHelpers.HangMitigatingTimeout));
        Assert.True(await WaitForLoadAsync(successfulProject).WaitAsync(TestHelpers.HangMitigatingTimeout));
    }

    [Fact]
    public async Task ExplicitLoadDoesNotWaitForUnrelatedQueuedWork()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var firstEvaluation = loader.QueueEvaluation();
        var secondEvaluation = loader.QueueEvaluation();
        var requestedPath = Path.Combine(TempRoot.Root, "Requested.csproj");
        var unrelatedPath = Path.Combine(TempRoot.Root, "Unrelated.csproj");

        var requestedProject = await loader.BeginLoadAsync(requestedPath);
        var unrelatedProject = await loader.BeginLoadAsync(unrelatedPath);
        await Task.WhenAll(firstEvaluation.Started.Task, secondEvaluation.Started.Task).WaitAsync(TestHelpers.HangMitigatingTimeout);

        var requestedEvaluation = firstEvaluation.Started.Task.Result == requestedPath ? firstEvaluation : secondEvaluation;
        var unrelatedEvaluation = firstEvaluation.Started.Task.Result == unrelatedPath ? firstEvaluation : secondEvaluation;
        var explicitLoad = loader.WaitForExplicitLoadsAsync([requestedProject]);

        requestedEvaluation.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, requestedPath);
        await explicitLoad.WaitAsync(TestHelpers.HangMitigatingTimeout);
        Assert.False(WaitForLoadAsync(unrelatedProject).IsCompleted);

        unrelatedEvaluation.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, unrelatedPath);
        await WaitForLoadAsync(unrelatedProject).WaitAsync(TestHelpers.HangMitigatingTimeout);
    }

    [Fact]
    public async Task ExplicitLoadWaitsForAllRequestedProjectsDespiteFailure()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var firstEvaluation = loader.QueueEvaluation();
        var secondEvaluation = loader.QueueEvaluation();
        var firstPath = Path.Combine(TempRoot.Root, "First.csproj");
        var secondPath = Path.Combine(TempRoot.Root, "Second.csproj");

        var firstProject = await loader.BeginLoadAsync(firstPath);
        var secondProject = await loader.BeginLoadAsync(secondPath);
        await Task.WhenAll(firstEvaluation.Started.Task, secondEvaluation.Started.Task).WaitAsync(TestHelpers.HangMitigatingTimeout);

        var failedEvaluation = firstEvaluation.Started.Task.Result == firstPath ? firstEvaluation : secondEvaluation;
        var successfulEvaluation = firstEvaluation.Started.Task.Result == secondPath ? firstEvaluation : secondEvaluation;
        var explicitLoad = loader.WaitForExplicitLoadsAsync([firstProject, secondProject]);

        failedEvaluation.Fail(new InvalidOperationException("Expected test failure"));
        await WaitForLoadAsync(firstProject).WaitAsync(TestHelpers.HangMitigatingTimeout);
        Assert.False(explicitLoad.IsCompleted);

        successfulEvaluation.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, secondPath);
        await explicitLoad.WaitAsync(TestHelpers.HangMitigatingTimeout);
    }

    [Fact]
    public async Task JoinedExplicitLoadsReportProgressIndependently()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var evaluation = loader.QueueEvaluation();
        var projectPath = Path.Combine(TempRoot.Root, "Project.csproj");
        var firstReporter = new TestProgressReporter();
        var secondReporter = new TestProgressReporter();

        var firstLoadedProject = await loader.BeginLoadAsync(projectPath);
        var secondLoadedProject = await loader.BeginLoadAsync(projectPath);
        Assert.Same(firstLoadedProject, secondLoadedProject);

        await using (var firstProgress = new LanguageServerProjectLoader.WorkDoneProgressTracker(firstReporter, totalItems: 1))
        await using (var secondProgress = new LanguageServerProjectLoader.WorkDoneProgressTracker(secondReporter, totalItems: 1))
        {
            var firstLoad = loader.WaitForExplicitLoadsAsync([firstLoadedProject], firstProgress);
            var secondLoad = loader.WaitForExplicitLoadsAsync([secondLoadedProject], secondProgress);

            await evaluation.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
            evaluation.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);
            await Task.WhenAll(firstLoad, secondLoad).WaitAsync(TestHelpers.HangMitigatingTimeout);
        }

        Assert.Contains(firstReporter.Reports, report => report is LSP.WorkDoneProgressReport { Percentage: 99 });
        Assert.Contains(secondReporter.Reports, report => report is LSP.WorkDoneProgressReport { Percentage: 99 });
        Assert.Equal(1, loader.EvaluationCount);
    }

    [Fact]
    public async Task UnsupportedProjectReturnsCanonicalCompletedStatus()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var evaluation = loader.QueueEvaluation();
        var projectPath = Path.Combine(TempRoot.Root, "Unsupported.csproj");

        var loadedProject = await loader.BeginLoadAsync(projectPath);
        await evaluation.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        evaluation.CompleteAsUnsupported();
        var loadedSuccessfully = await WaitForLoadAsync(loadedProject).WaitAsync(TestHelpers.HangMitigatingTimeout);

        var laterLoadedProject = await loader.BeginLoadAsync(projectPath);
        Assert.False(loadedSuccessfully);
        Assert.Same(loadedProject, laterLoadedProject);
        Assert.False(await WaitForLoadAsync(laterLoadedProject));
        Assert.Equal(1, loader.EvaluationCount);
    }

    [Fact]
    public async Task UnloadCompletesProjectAndStaleEvaluationDoesNotCommit()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var evaluation = loader.QueueEvaluation();
        var projectPath = Path.Combine(TempRoot.Root, "Project.csproj");

        var loadedProject = await loader.BeginLoadAsync(projectPath);
        await evaluation.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        Assert.True(await loader.UnloadAsync(projectPath));
        Assert.False(await loadedProject.TryBeginLoadAsync());
        Assert.False(await WaitForLoadAsync(loadedProject).WaitAsync(TestHelpers.HangMitigatingTimeout));

        evaluation.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);
        await loader.WaitForLoadsAsync();
        Assert.Empty(loader.WorkspaceFactory.HostWorkspace.CurrentSolution.Projects);
    }

    [Fact]
    public async Task UnloadAndRequeueBeforeBatchDrainsPreservesNewProject()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var currentEvaluation = loader.QueueEvaluation();
        var projectPath = Path.Combine(TempRoot.Root, "Project.csproj");

        var staleLoadedProject = await loader.BeginLoadAsync(projectPath);
        Assert.True(await loader.UnloadAsync(projectPath));
        var currentLoadedProject = await loader.BeginLoadAsync(projectPath);

        Assert.NotSame(staleLoadedProject, currentLoadedProject);
        Assert.False(await WaitForLoadAsync(staleLoadedProject));
        await currentEvaluation.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);

        currentEvaluation.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);
        var loadedSuccessfully = await WaitForLoadAsync(currentLoadedProject).WaitAsync(TestHelpers.HangMitigatingTimeout);

        Assert.True(loadedSuccessfully);
        Assert.Equal(1, loader.EvaluationCount);
        Assert.Single(loader.WorkspaceFactory.HostWorkspace.CurrentSolution.Projects);
    }

    [Fact]
    public async Task WaitForActiveProjectLoadsAsyncUsesCanonicalSnapshot()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var firstEvaluation = loader.QueueEvaluation();
        var secondEvaluation = loader.QueueEvaluation();
        var firstProjectPath = Path.Combine(TempRoot.Root, "First.csproj");
        var secondProjectPath = Path.Combine(TempRoot.Root, "Second.csproj");

        var firstLoadedProject = await loader.BeginLoadAsync(firstProjectPath);
        await firstEvaluation.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        var activeLoads = loader.WaitForActiveLoadsAsync();
        var secondLoadedProject = await loader.BeginLoadAsync(secondProjectPath);

        Assert.False(activeLoads.IsCompleted);
        firstEvaluation.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, firstProjectPath);
        await activeLoads.WaitAsync(TestHelpers.HangMitigatingTimeout);
        Assert.False(WaitForLoadAsync(secondLoadedProject).IsCompleted);

        await secondEvaluation.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        secondEvaluation.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, secondProjectPath);
        await WaitForLoadAsync(secondLoadedProject).WaitAsync(TestHelpers.HangMitigatingTimeout);
        Assert.True(await WaitForLoadAsync(firstLoadedProject));
    }

    [Fact]
    public async Task ShutdownCompletesOutstandingLoadAsUnloaded()
    {
        var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var evaluation = loader.QueueEvaluation();
        var loadedProject = await loader.BeginLoadAsync(Path.Combine(TempRoot.Root, "Project.csproj"));
        await evaluation.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);

        await server.DisposeAsync();

        Assert.False(await WaitForLoadAsync(loadedProject));
    }

    [Fact]
    public async Task ProjectPathIdentityUsesPlatformSemantics()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        _ = loader.QueueEvaluation();
        _ = loader.QueueEvaluation();

        var lowerCaseHandle = await loader.BeginLoadAsync(Path.Combine(TempRoot.Root, "project.csproj"));
        var upperCaseHandle = await loader.BeginLoadAsync(Path.Combine(TempRoot.Root, "PROJECT.csproj"));

        if (PathUtilities.IsUnixLikePlatform)
            Assert.NotSame(lowerCaseHandle, upperCaseHandle);
        else
            Assert.Same(lowerCaseHandle, upperCaseHandle);
    }

    [Fact]
    public async Task MalformedAbsoluteProjectPathDoesNotThrowDuringNormalization()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var projectPath = Path.GetPathRoot(TempRoot.Root) + "\0Invalid.csproj";

        Assert.False(await loader.UnloadAsync(projectPath));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PrimordialProjectCompletesAfterCanonicalCommit(bool startDesignTimeBuildWithPrimordialProject)
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var evaluation = loader.QueueEvaluation();
        var projectPath = Path.Combine(TempRoot.Root, "Project.csproj");

        var primordialProject = await loader.CreatePrimordialProjectAsync(projectPath, doDesignTimeBuild: startDesignTimeBuildWithPrimordialProject);
        if (!startDesignTimeBuildWithPrimordialProject)
            _ = await loader.CreatePrimordialProjectAsync(projectPath, doDesignTimeBuild: true);

        await evaluation.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        evaluation.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);
        await loader.WaitForLoadsAsync().WaitAsync(TestHelpers.HangMitigatingTimeout);

        Assert.Null(loader.WorkspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.Workspace.CurrentSolution.GetProject(primordialProject.Id));
        Assert.NotEmpty(loader.WorkspaceFactory.HostWorkspace.CurrentSolution.Projects);
    }

    [ExportCSharpVisualBasicLspServiceFactory(typeof(TestProjectLoader)), PartNotDiscoverable, Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class TestProjectLoaderFactory(
        IGlobalOptionService globalOptionService,
        IAsynchronousOperationListenerProvider listenerProvider,
        ServerConfigurationFactory serverConfigurationFactory) : ILspServiceFactory
    {
        public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
            => new TestProjectLoader(
                lspServices,
                globalOptionService,
                lspServices.GetRequiredService<ILoggerFactory>(),
                listenerProvider,
                serverConfigurationFactory,
                lspServices.GetRequiredService<IBinLogPathProvider>(),
                lspServices.GetRequiredService<DotnetCliHelper>());
    }

    internal sealed class TestProjectLoader : LanguageServerProjectLoader, ILspService
    {
        private readonly ConcurrentQueue<ScriptedEvaluation> _evaluations = new();
        private int _evaluationCount;

        public LanguageServerWorkspaceFactory WorkspaceFactory => _workspaceFactory;
        public int EvaluationCount => Volatile.Read(ref _evaluationCount);

        public TestProjectLoader(
            ILspServices lspServices,
            IGlobalOptionService globalOptionService,
            ILoggerFactory loggerFactory,
            IAsynchronousOperationListenerProvider listenerProvider,
            ServerConfigurationFactory serverConfigurationFactory,
            IBinLogPathProvider binLogPathProvider,
            DotnetCliHelper dotnetCliHelper)
            : base(lspServices, globalOptionService, loggerFactory, listenerProvider, serverConfigurationFactory, binLogPathProvider, dotnetCliHelper)
        {
        }

        public ScriptedEvaluation QueueEvaluation()
        {
            var evaluation = new ScriptedEvaluation();
            _evaluations.Enqueue(evaluation);
            return evaluation;
        }

        public Task<LoadedProject> BeginLoadAsync(string projectPath, string? projectGuid = null)
            => BeginLoadingProjectAsync(projectPath, projectGuid);

        public Task WaitForLoadsAsync()
            => WaitForProjectsToFinishLoadingAsync();

        public Task WaitForActiveLoadsAsync(CancellationToken cancellationToken = default)
            => WaitForActiveProjectLoadsAsync(cancellationToken);

        public Task WaitForExplicitLoadsAsync(ImmutableArray<LoadedProject> loadedProjects, WorkDoneProgressTracker? progressTracker = null)
            => WaitForProjectLoadsAsync(loadedProjects, progressTracker);

        public ValueTask<bool> UnloadAsync(string projectPath)
            => TryUnloadProjectAsync(projectPath);

        public async ValueTask<Project> CreatePrimordialProjectAsync(string projectPath, bool doDesignTimeBuild)
        {
            var projectFactory = WorkspaceFactory.MiscellaneousFilesWorkspaceProjectFactory;
            return (await GetOrLoadProjectAsync(
                projectPath,
                projectFactory,
                _ => ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Default,
                    name: "Primordial",
                    assemblyName: "Primordial",
                    LanguageNames.CSharp,
                    filePath: projectPath),
                doDesignTimeBuild)).Single();
        }

        protected override async Task<RemoteProjectLoadResult?> TryLoadProjectInMSBuildHostAsync(
            BuildHostProcessManager buildHostProcessManager, string projectPath, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _evaluationCount);
            Assert.True(_evaluations.TryDequeue(out var evaluation));
            evaluation.Started.TrySetResult(projectPath);
            return await evaluation.Result.Task.WaitAsync(cancellationToken);
        }
    }

    internal sealed class ScriptedEvaluation
    {
        public TaskCompletionSource<string> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<LanguageServerProjectLoader.RemoteProjectLoadResult?> Result { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void CompleteSuccessfully(ProjectSystemProjectFactory projectFactory, string projectPath, string? targetFramework = null)
            => Result.SetResult(new()
            {
                ProjectFileInfos = [ProjectFileInfo.CreateEmpty(LanguageNames.CSharp, projectPath) with { CommandLineArgs = ["/target:library"], TargetFramework = targetFramework }],
                DiagnosticLogItems = [],
                ProjectRestorePath = null,
                ProjectFactory = projectFactory,
                IsFileBasedProgram = false,
                IsMiscellaneousFile = false,
                HasFileBasedAppDirectives = false,
                HasAllInformation = true,
                PreferredBuildHostKind = BuildHostProcessKind.NetCore,
                ActualBuildHostKind = BuildHostProcessKind.NetCore,
            });

        public void CompleteAsUnsupported()
            => Result.SetResult(null);

        public void Fail(Exception exception)
            => Result.SetException(exception);
    }

    private sealed class TestProgressReporter : IProgress<LSP.WorkDoneProgress>
    {
        public ConcurrentQueue<LSP.WorkDoneProgress> Reports { get; } = new();

        public void Report(LSP.WorkDoneProgress value)
            => Reports.Enqueue(value);
    }
}
