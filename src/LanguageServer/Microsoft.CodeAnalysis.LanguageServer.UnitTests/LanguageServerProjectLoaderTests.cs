// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
using Xunit.Abstractions;
using LSP = Roslyn.LanguageServer.Protocol;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

[UseExportProvider]
public sealed class LanguageServerProjectLoaderTests(ITestOutputHelper testOutputHelper) : AbstractLanguageServerHostTests(testOutputHelper)
{
    private protected override Task<ExportProvider> CreateExportProviderAsync(
        ServerConfiguration serverConfiguration,
        ILoggerFactory loggerFactory,
        ExtensionAssemblyManager extensionManager,
        IAssemblyLoader assemblyLoader)
        => Task.FromResult(LanguageServerTestComposition.GetSharedExportProvider(
            serverConfiguration, loggerFactory, typeof(TestProjectLoaderFactory)));

    [Fact]
    public async Task ConcurrentCallersShareHandleAndCompleteAfterWorkspaceCommit()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var evaluation = loader.QueueEvaluation();
        var projectPath = Path.Combine(TempRoot.Root, "Project.csproj");
        var equivalentPath = Path.Combine(TempRoot.Root, "directory", "..", "Project.csproj");

        var firstHandle = await loader.BeginLoadAsync(projectPath);
        var secondHandle = await loader.BeginLoadAsync(equivalentPath);

        Assert.Same(firstHandle, secondHandle);
        Assert.False(firstHandle.Completion.IsCompleted);
        await evaluation.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        Assert.Equal(1, loader.EvaluationCount);

        evaluation.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);
        var result = await firstHandle.Completion.WaitAsync(TestHelpers.HangMitigatingTimeout);

        Assert.Equal(LanguageServerProjectLoadStatus.Loaded, result.Status);
        Assert.NotEmpty(result.ProjectIds);
        Assert.All(result.ProjectIds, projectId => Assert.NotNull(loader.WorkspaceFactory.HostWorkspace.CurrentSolution.GetProject(projectId)));
    }

    [Fact]
    public async Task LoadedProjectReturnsCompletedHandleWithoutReevaluation()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var evaluation = loader.QueueEvaluation();
        var projectPath = Path.Combine(TempRoot.Root, "Project.csproj");

        var firstHandle = await loader.BeginLoadAsync(projectPath);
        await evaluation.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        evaluation.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);
        var firstResult = await firstHandle.Completion.WaitAsync(TestHelpers.HangMitigatingTimeout);

        var loadedHandle = await loader.BeginLoadAsync(projectPath);

        Assert.NotSame(firstHandle, loadedHandle);
        Assert.True(loadedHandle.Completion.IsCompletedSuccessfully);
        var loadedResult = await loadedHandle.Completion;
        Assert.Equal(firstResult.Status, loadedResult.Status);
        Assert.True(firstResult.ProjectIds.SequenceEqual(loadedResult.ProjectIds));
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

        var handle = await loader.BeginLoadAsync(projectPath);
        await firstEvaluation.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        firstEvaluation.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);
        await handle.Completion.WaitAsync(TestHelpers.HangMitigatingTimeout);

        var target = Assert.Single(LanguageServerProjectLoader.TestAccessor.GetLoadedProjectTargets(loader, projectPath));
        target.GetTestAccessor().RaiseNeedsReload();

        // A file-change-triggered reload after the initial load has committed must still reach the MSBuild host,
        // rather than being dropped because it carries the (already-completed) load operation from the initial request.
        await secondEvaluation.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        secondEvaluation.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);
        await loader.WaitForLoadsAsync();

        Assert.Equal(2, loader.EvaluationCount);
    }

    [Fact]
    public async Task FailedProjectRetriesOnNextBeginLoad()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var failedEvaluation = loader.QueueEvaluation();
        var retryEvaluation = loader.QueueEvaluation();
        var projectPath = Path.Combine(TempRoot.Root, "Project.csproj");

        var firstHandle = await loader.BeginLoadAsync(projectPath);
        await failedEvaluation.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        failedEvaluation.Fail(new InvalidOperationException("Expected test failure"));
        var firstResult = await firstHandle.Completion.WaitAsync(TestHelpers.HangMitigatingTimeout);
        Assert.Equal(LanguageServerProjectLoadStatus.Failed, firstResult.Status);

        var retryHandle = await loader.BeginLoadAsync(projectPath);
        Assert.NotSame(firstHandle, retryHandle);
        Assert.False(retryHandle.Completion.IsCompleted);

        await retryEvaluation.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        retryEvaluation.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);
        var retryResult = await retryHandle.Completion.WaitAsync(TestHelpers.HangMitigatingTimeout);

        Assert.Equal(LanguageServerProjectLoadStatus.Loaded, retryResult.Status);
        Assert.Equal(2, loader.EvaluationCount);
    }

    [Fact]
    public async Task FailureCompletesOnlyAffectedHandle()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var failedEvaluation = loader.QueueEvaluation();
        var successfulEvaluation = loader.QueueEvaluation();
        var failedPath = Path.Combine(TempRoot.Root, "Failed.csproj");
        var successfulPath = Path.Combine(TempRoot.Root, "Successful.csproj");

        var failedHandle = await loader.BeginLoadAsync(failedPath);
        var successfulHandle = await loader.BeginLoadAsync(successfulPath);
        await Task.WhenAll(failedEvaluation.Started.Task, successfulEvaluation.Started.Task).WaitAsync(TestHelpers.HangMitigatingTimeout);

        failedEvaluation.Fail(new InvalidOperationException("Expected test failure"));
        successfulEvaluation.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, successfulPath);

        var failedResult = await failedHandle.Completion.WaitAsync(TestHelpers.HangMitigatingTimeout);
        var successfulResult = await successfulHandle.Completion.WaitAsync(TestHelpers.HangMitigatingTimeout);
        Assert.Equal(LanguageServerProjectLoadStatus.Failed, failedResult.Status);
        Assert.Equal(LanguageServerProjectLoadStatus.Loaded, successfulResult.Status);
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

        var requestedHandle = await loader.BeginLoadAsync(requestedPath);
        var unrelatedHandle = await loader.BeginLoadAsync(unrelatedPath);
        await Task.WhenAll(firstEvaluation.Started.Task, secondEvaluation.Started.Task).WaitAsync(TestHelpers.HangMitigatingTimeout);

        var requestedEvaluation = firstEvaluation.Started.Task.Result == requestedPath ? firstEvaluation : secondEvaluation;
        var unrelatedEvaluation = firstEvaluation.Started.Task.Result == unrelatedPath ? firstEvaluation : secondEvaluation;
        var explicitLoad = loader.WaitForExplicitLoadsAsync([requestedHandle]);

        requestedEvaluation.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, requestedPath);
        await explicitLoad.WaitAsync(TestHelpers.HangMitigatingTimeout);
        Assert.False(unrelatedHandle.Completion.IsCompleted);

        unrelatedEvaluation.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, unrelatedPath);
        await unrelatedHandle.Completion.WaitAsync(TestHelpers.HangMitigatingTimeout);
    }

    [Fact]
    public async Task ExplicitLoadWaitsForAllRequestedHandlesDespiteFailure()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var firstEvaluation = loader.QueueEvaluation();
        var secondEvaluation = loader.QueueEvaluation();
        var firstPath = Path.Combine(TempRoot.Root, "First.csproj");
        var secondPath = Path.Combine(TempRoot.Root, "Second.csproj");

        var firstHandle = await loader.BeginLoadAsync(firstPath);
        var secondHandle = await loader.BeginLoadAsync(secondPath);
        await Task.WhenAll(firstEvaluation.Started.Task, secondEvaluation.Started.Task).WaitAsync(TestHelpers.HangMitigatingTimeout);

        var failedEvaluation = firstEvaluation.Started.Task.Result == firstPath ? firstEvaluation : secondEvaluation;
        var successfulEvaluation = firstEvaluation.Started.Task.Result == secondPath ? firstEvaluation : secondEvaluation;
        var explicitLoad = loader.WaitForExplicitLoadsAsync([firstHandle, secondHandle]);

        failedEvaluation.Fail(new InvalidOperationException("Expected test failure"));
        await firstHandle.Completion.WaitAsync(TestHelpers.HangMitigatingTimeout);
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

        var firstHandle = await loader.BeginLoadAsync(projectPath);
        var secondHandle = await loader.BeginLoadAsync(projectPath);
        Assert.Same(firstHandle, secondHandle);

        await using (var firstProgress = new LanguageServerProjectLoader.WorkDoneProgressTracker(firstReporter, totalItems: 1))
        await using (var secondProgress = new LanguageServerProjectLoader.WorkDoneProgressTracker(secondReporter, totalItems: 1))
        {
            var firstLoad = loader.WaitForExplicitLoadsAsync([firstHandle], firstProgress);
            var secondLoad = loader.WaitForExplicitLoadsAsync([secondHandle], secondProgress);

            await evaluation.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
            evaluation.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);
            await Task.WhenAll(firstLoad, secondLoad).WaitAsync(TestHelpers.HangMitigatingTimeout);
        }

        Assert.Contains(firstReporter.Reports, report => report is LSP.WorkDoneProgressReport { Percentage: 99 });
        Assert.Contains(secondReporter.Reports, report => report is LSP.WorkDoneProgressReport { Percentage: 99 });
        Assert.Equal(1, loader.EvaluationCount);
    }

    [Fact]
    public async Task UnsupportedProjectReturnsCanonicalCompletedResult()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var evaluation = loader.QueueEvaluation();
        var projectPath = Path.Combine(TempRoot.Root, "Unsupported.csproj");

        var handle = await loader.BeginLoadAsync(projectPath);
        await evaluation.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        evaluation.CompleteAsUnsupported();
        var result = await handle.Completion.WaitAsync(TestHelpers.HangMitigatingTimeout);

        var laterHandle = await loader.BeginLoadAsync(projectPath);
        Assert.Equal(LanguageServerProjectLoadStatus.Unsupported, result.Status);
        Assert.True(laterHandle.Completion.IsCompletedSuccessfully);
        Assert.Equal(result, await laterHandle.Completion);
        Assert.Equal(1, loader.EvaluationCount);
    }

    [Fact]
    public async Task UnloadCompletesHandleAndStaleEvaluationDoesNotCommit()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var evaluation = loader.QueueEvaluation();
        var projectPath = Path.Combine(TempRoot.Root, "Project.csproj");

        var handle = await loader.BeginLoadAsync(projectPath);
        await evaluation.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        Assert.True(await loader.UnloadAsync(projectPath));
        var result = await handle.Completion.WaitAsync(TestHelpers.HangMitigatingTimeout);
        Assert.Equal(LanguageServerProjectLoadStatus.Unloaded, result.Status);

        evaluation.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);
        await loader.WaitForLoadsAsync();
        Assert.Empty(loader.WorkspaceFactory.HostWorkspace.CurrentSolution.Projects);
    }

    [Fact]
    public async Task UnloadAndRequeueBeforeBatchDrainsPreservesNewOperation()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var currentEvaluation = loader.QueueEvaluation();
        var projectPath = Path.Combine(TempRoot.Root, "Project.csproj");

        var staleHandle = await loader.BeginLoadAsync(projectPath);
        Assert.True(await loader.UnloadAsync(projectPath));
        var currentHandle = await loader.BeginLoadAsync(projectPath);

        Assert.NotSame(staleHandle, currentHandle);
        Assert.Equal(LanguageServerProjectLoadStatus.Unloaded, (await staleHandle.Completion).Status);
        await currentEvaluation.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);

        currentEvaluation.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);
        var currentResult = await currentHandle.Completion.WaitAsync(TestHelpers.HangMitigatingTimeout);

        Assert.Equal(LanguageServerProjectLoadStatus.Loaded, currentResult.Status);
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

        var firstHandle = await loader.BeginLoadAsync(firstProjectPath);
        await firstEvaluation.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        var activeLoads = loader.WaitForActiveLoadsAsync();
        var secondHandle = await loader.BeginLoadAsync(secondProjectPath);

        Assert.False(activeLoads.IsCompleted);
        firstEvaluation.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, firstProjectPath);
        await activeLoads.WaitAsync(TestHelpers.HangMitigatingTimeout);
        Assert.False(secondHandle.Completion.IsCompleted);

        await secondEvaluation.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        secondEvaluation.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, secondProjectPath);
        await secondHandle.Completion.WaitAsync(TestHelpers.HangMitigatingTimeout);
        Assert.Equal(LanguageServerProjectLoadStatus.Loaded, (await firstHandle.Completion).Status);
    }

    [Fact]
    public async Task ShutdownCancelsOutstandingHandle()
    {
        var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var evaluation = loader.QueueEvaluation();
        var handle = await loader.BeginLoadAsync(Path.Combine(TempRoot.Root, "Project.csproj"));
        await evaluation.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);

        await server.DisposeAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => handle.Completion);
    }

    [Fact]
    public async Task ProjectPathIdentityIsCaseInsensitive()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        _ = loader.QueueEvaluation();

        var lowerCaseHandle = await loader.BeginLoadAsync(Path.Combine(TempRoot.Root, "project.csproj"));
        var upperCaseHandle = await loader.BeginLoadAsync(Path.Combine(TempRoot.Root, "PROJECT.csproj"));

        Assert.Same(lowerCaseHandle, upperCaseHandle);
    }

    [Fact]
    public void ProjectGuidCanBeEnrichedBeforeEvaluation()
    {
        var operation = new LanguageServerProjectLoader.ProjectLoadOperation(projectGuid: null);

        Assert.True(operation.TrySetProjectGuid("first-guid"));
        Assert.Equal("first-guid", operation.StartEvaluation());
    }

    [Fact]
    public void FirstProjectGuidWins()
    {
        var operation = new LanguageServerProjectLoader.ProjectLoadOperation("first-guid");

        Assert.False(operation.TrySetProjectGuid("conflicting-guid"));
        Assert.Equal("first-guid", operation.StartEvaluation());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PrimordialProjectCompletesHandleAfterCanonicalCommit(bool startDesignTimeBuildWithPrimordialProject)
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var evaluation = loader.QueueEvaluation();
        var projectPath = Path.Combine(TempRoot.Root, "Project.csproj");

        var primordialProject = await loader.CreatePrimordialProjectAsync(projectPath, doDesignTimeBuild: startDesignTimeBuildWithPrimordialProject);
        var handle = await loader.BeginLoadAsync(projectPath);
        await evaluation.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        evaluation.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);

        var result = await handle.Completion.WaitAsync(TestHelpers.HangMitigatingTimeout);
        Assert.Equal(LanguageServerProjectLoadStatus.Loaded, result.Status);
        Assert.Null(loader.WorkspaceFactory.MiscellaneousFilesWorkspaceProjectFactory.Workspace.CurrentSolution.GetProject(primordialProject.Id));
        Assert.DoesNotContain(primordialProject.Id, result.ProjectIds);
        Assert.All(result.ProjectIds, projectId => Assert.NotNull(loader.WorkspaceFactory.HostWorkspace.CurrentSolution.GetProject(projectId)));
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

        public Task<LanguageServerProjectLoadHandle> BeginLoadAsync(string projectPath, string? projectGuid = null)
            => BeginLoadingProjectAsync(projectPath, projectGuid);

        public Task WaitForLoadsAsync()
            => WaitForProjectsToFinishLoadingAsync();

        public Task WaitForActiveLoadsAsync(CancellationToken cancellationToken = default)
            => WaitForActiveProjectLoadsAsync(cancellationToken);

        public Task WaitForExplicitLoadsAsync(ImmutableArray<LanguageServerProjectLoadHandle> handles, WorkDoneProgressTracker? progressTracker = null)
            => WaitForProjectLoadsAsync(handles, progressTracker);

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
                doDesignTimeBuild))!;
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

        public void CompleteSuccessfully(ProjectSystemProjectFactory projectFactory, string projectPath)
            => Result.SetResult(LanguageServerProjectLoader.TestAccessor.CreateRemoteProjectLoadResult(projectFactory, projectPath));

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
