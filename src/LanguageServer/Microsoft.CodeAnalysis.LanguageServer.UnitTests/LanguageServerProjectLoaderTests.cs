// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias MSBuildWorkspacesContracts;

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.LanguageServer.Services;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Options;
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
using ProjectFileReference = MSBuildWorkspacesContracts::Microsoft.CodeAnalysis.MSBuild.ProjectFileReference;

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
    public async Task ConcurrentCallersShareLoadedProjectAndCompleteAfterWorkspaceCommit()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var projectPath = Path.Combine(TempRoot.Root, "Project.csproj");
        var equivalentPath = Path.Combine(TempRoot.Root, "directory", "..", "Project.csproj");

        var (firstLoadedProject, designTimeBuild) = await loader.ExpectDesignTimeBuildAndBeginLoadingProjectAsync(projectPath);
        var secondLoadedProject = await loader.BeginLoadingProjectAsync(equivalentPath);

        Assert.Same(firstLoadedProject, secondLoadedProject);
        Assert.False(firstLoadedProject.WaitForLoadAsync(CancellationToken.None).AsTask().IsCompleted);
        await designTimeBuild.Started.Task;
        Assert.Equal(1, loader.DesignTimeBuildCount);

        designTimeBuild.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);
        var loadedSuccessfully = await firstLoadedProject.WaitForLoadAsync(CancellationToken.None);

        Assert.True(loadedSuccessfully);
        Assert.NotEmpty(loader.WorkspaceFactory.HostWorkspace.CurrentSolution.Projects);
    }

    [Fact]
    public async Task LoadedProjectReturnsWithoutAnotherDesignTimeBuild()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var projectPath = Path.Combine(TempRoot.Root, "Project.csproj");

        var (firstLoadedProject, designTimeBuild) = await loader.ExpectDesignTimeBuildAndBeginLoadingProjectAsync(projectPath);
        await designTimeBuild.Started.Task;
        designTimeBuild.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);
        var firstStatus = await firstLoadedProject.WaitForLoadAsync(CancellationToken.None);

        var loadedProject = await loader.BeginLoadingProjectAsync(projectPath);

        Assert.Same(firstLoadedProject, loadedProject);
        Assert.Equal(firstStatus, await loadedProject.WaitForLoadAsync(CancellationToken.None));
        Assert.Equal(1, loader.DesignTimeBuildCount);
    }

    [Fact]
    public async Task NeedsReloadTriggersAnotherDesignTimeBuildAfterInitialLoadCompletes()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var projectPath = Path.Combine(TempRoot.Root, "Project.csproj");

        var (loadedProject, firstDesignTimeBuild) = await loader.ExpectDesignTimeBuildAndBeginLoadingProjectAsync(projectPath);
        await firstDesignTimeBuild.Started.Task;
        firstDesignTimeBuild.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);
        await loadedProject.WaitForLoadAsync(CancellationToken.None);

        var secondDesignTimeBuild = loader.ExpectDesignTimeBuild(projectPath);
        loadedProject.GetTestAccessor().RaiseNeedsReload();

        // A file-change-triggered reload after the initial load has committed must still reach the MSBuild host,
        // rather than being dropped because it carries the (already-completed) load operation from the initial request.
        await secondDesignTimeBuild.Started.Task;
        secondDesignTimeBuild.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);

        Assert.Equal(2, loader.DesignTimeBuildCount);
    }

    [Fact]
    public async Task FailedProjectOnlyRetriesForFileChange()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var projectPath = Path.Combine(TempRoot.Root, "Project.csproj");

        var (firstLoadedProject, failedDesignTimeBuild) = await loader.ExpectDesignTimeBuildAndBeginLoadingProjectAsync(projectPath);
        await failedDesignTimeBuild.Started.Task;
        failedDesignTimeBuild.Fail(new InvalidOperationException("Expected test failure"));
        Assert.False(await firstLoadedProject.WaitForLoadAsync(CancellationToken.None));

        var loadedProject = await loader.BeginLoadingProjectAsync(projectPath);

        Assert.Same(firstLoadedProject, loadedProject);
        Assert.False(await loadedProject.WaitForLoadAsync(CancellationToken.None));
        Assert.Equal(1, loader.DesignTimeBuildCount);

        var successfulReload = loader.ExpectDesignTimeBuild(projectPath);
        loadedProject.GetTestAccessor().RaiseNeedsReload();
        await successfulReload.Started.Task;
        successfulReload.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);
    }

    [Fact]
    public async Task FailureCompletesOnlyAffectedProject()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var failedPath = Path.Combine(TempRoot.Root, "Failed.csproj");
        var successfulPath = Path.Combine(TempRoot.Root, "Successful.csproj");

        var (failedProject, failedDesignTimeBuild) = await loader.ExpectDesignTimeBuildAndBeginLoadingProjectAsync(failedPath);
        var (successfulProject, successfulDesignTimeBuild) = await loader.ExpectDesignTimeBuildAndBeginLoadingProjectAsync(successfulPath);
        await Task.WhenAll(failedDesignTimeBuild.Started.Task, successfulDesignTimeBuild.Started.Task);

        failedDesignTimeBuild.Fail(new InvalidOperationException("Expected test failure"));
        successfulDesignTimeBuild.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, successfulPath);

        Assert.False(await failedProject.WaitForLoadAsync(CancellationToken.None));
        Assert.True(await successfulProject.WaitForLoadAsync(CancellationToken.None));
    }

    [Fact]
    public async Task ExplicitLoadDoesNotWaitForUnrelatedQueuedWork()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var requestedPath = Path.Combine(TempRoot.Root, "Requested.csproj");
        var unrelatedPath = Path.Combine(TempRoot.Root, "Unrelated.csproj");

        var (requestedProject, requestedDesignTimeBuild) = await loader.ExpectDesignTimeBuildAndBeginLoadingProjectAsync(requestedPath);
        var (unrelatedProject, unrelatedDesignTimeBuild) = await loader.ExpectDesignTimeBuildAndBeginLoadingProjectAsync(unrelatedPath);
        await Task.WhenAll(requestedDesignTimeBuild.Started.Task, unrelatedDesignTimeBuild.Started.Task);

        var explicitLoad = LanguageServerProjectLoader.WaitForProjectLoadsAsync([requestedProject]);

        requestedDesignTimeBuild.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, requestedPath);
        await explicitLoad;
        Assert.False(unrelatedProject.WaitForLoadAsync(CancellationToken.None).AsTask().IsCompleted);

        unrelatedDesignTimeBuild.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, unrelatedPath);
        await unrelatedProject.WaitForLoadAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ExplicitLoadWaitsForAllRequestedProjectsDespiteFailure()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var firstPath = Path.Combine(TempRoot.Root, "First.csproj");
        var secondPath = Path.Combine(TempRoot.Root, "Second.csproj");

        var (firstProject, failedDesignTimeBuild) = await loader.ExpectDesignTimeBuildAndBeginLoadingProjectAsync(firstPath);
        var (secondProject, successfulDesignTimeBuild) = await loader.ExpectDesignTimeBuildAndBeginLoadingProjectAsync(secondPath);
        await Task.WhenAll(failedDesignTimeBuild.Started.Task, successfulDesignTimeBuild.Started.Task);

        var explicitLoad = LanguageServerProjectLoader.WaitForProjectLoadsAsync([firstProject, secondProject]);

        failedDesignTimeBuild.Fail(new InvalidOperationException("Expected test failure"));
        await firstProject.WaitForLoadAsync(CancellationToken.None);
        Assert.False(explicitLoad.IsCompleted);

        successfulDesignTimeBuild.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, secondPath);
        await explicitLoad;
    }

    [Fact]
    public async Task JoinedExplicitLoadsReportProgressIndependently()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var projectPath = Path.Combine(TempRoot.Root, "Project.csproj");
        var firstReporter = new TestProgressReporter();
        var secondReporter = new TestProgressReporter();

        var (firstLoadedProject, designTimeBuild) = await loader.ExpectDesignTimeBuildAndBeginLoadingProjectAsync(projectPath);
        var secondLoadedProject = await loader.BeginLoadingProjectAsync(projectPath);
        Assert.Same(firstLoadedProject, secondLoadedProject);

        await using (var firstProgress = new LanguageServerProjectLoader.WorkDoneProgressTracker(firstReporter, totalItems: 1))
        await using (var secondProgress = new LanguageServerProjectLoader.WorkDoneProgressTracker(secondReporter, totalItems: 1))
        {
            var firstLoad = LanguageServerProjectLoader.WaitForProjectLoadsAsync([firstLoadedProject], firstProgress);
            var secondLoad = LanguageServerProjectLoader.WaitForProjectLoadsAsync([secondLoadedProject], secondProgress);

            await designTimeBuild.Started.Task;
            designTimeBuild.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);
            await Task.WhenAll(firstLoad, secondLoad);
        }

        // Disposing the trackers ensures their asynchronous progress queues have finished reporting.
        Assert.Contains(firstReporter.Reports, report => report is LSP.WorkDoneProgressReport { Percentage: 99 });
        Assert.Contains(secondReporter.Reports, report => report is LSP.WorkDoneProgressReport { Percentage: 99 });
        Assert.Equal(1, loader.DesignTimeBuildCount);
    }

    [Fact]
    public async Task UnsupportedProjectReturnsCanonicalCompletedStatus()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var projectPath = Path.Combine(TempRoot.Root, "Unsupported.csproj");

        var (loadedProject, designTimeBuild) = await loader.ExpectDesignTimeBuildAndBeginLoadingProjectAsync(projectPath);
        await designTimeBuild.Started.Task;
        designTimeBuild.CompleteAsUnsupported();
        var loadedSuccessfully = await loadedProject.WaitForLoadAsync(CancellationToken.None);

        var laterLoadedProject = await loader.BeginLoadingProjectAsync(projectPath);
        Assert.False(loadedSuccessfully);
        Assert.Same(loadedProject, laterLoadedProject);
        Assert.False(await laterLoadedProject.WaitForLoadAsync(CancellationToken.None));
        Assert.Equal(1, loader.DesignTimeBuildCount);
    }

    [Fact]
    public async Task UnloadCompletesProjectAndStaleDesignTimeBuildDoesNotCommit()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var projectPath = Path.Combine(TempRoot.Root, "Project.csproj");

        var (loadedProject, designTimeBuild) = await loader.ExpectDesignTimeBuildAndBeginLoadingProjectAsync(projectPath);
        await designTimeBuild.Started.Task;
        Assert.True(await loader.TryUnloadProjectAsync(projectPath));
        Assert.False(await loadedProject.WaitForLoadAsync(CancellationToken.None));

        designTimeBuild.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);

        // Wait until all processing has completed
        await server.ExportProvider.GetExportedValue<AsynchronousOperationListenerProvider>().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();
        Assert.Empty(loader.WorkspaceFactory.HostWorkspace.CurrentSolution.Projects);
    }

    [Fact]
    public async Task UnloadAndRequeueBeforeBatchDrainsPreservesNewProject()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var projectPath = Path.Combine(TempRoot.Root, "Project.csproj");

        var (staleLoadedProject, currentDesignTimeBuild) = await loader.ExpectDesignTimeBuildAndBeginLoadingProjectAsync(projectPath);
        Assert.True(await loader.TryUnloadProjectAsync(projectPath));
        var currentLoadedProject = await loader.BeginLoadingProjectAsync(projectPath);

        Assert.NotSame(staleLoadedProject, currentLoadedProject);
        Assert.False(await staleLoadedProject.WaitForLoadAsync(CancellationToken.None));
        await currentDesignTimeBuild.Started.Task;

        currentDesignTimeBuild.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);
        var loadedSuccessfully = await currentLoadedProject.WaitForLoadAsync(CancellationToken.None);

        Assert.True(loadedSuccessfully);
        Assert.Equal(1, loader.DesignTimeBuildCount);
        Assert.Single(loader.WorkspaceFactory.HostWorkspace.CurrentSolution.Projects);
    }

    [Fact]
    public async Task CachedProjectHasProjectDataWhileDesignTimeBuildIsPending()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var projectPath = Path.Combine(TempRoot.Root, "Cached.csproj");
        loader.CachedProjects.Add(projectPath, [ProjectFileInfo.CreateEmpty(LanguageNames.CSharp, projectPath) with { CommandLineArgs = ["/target:library", "/define:CACHED_PROJECT"] }]);

        var (loadedProject, designTimeBuild) = await loader.ExpectDesignTimeBuildAndBeginLoadingProjectAsync(projectPath);
        await designTimeBuild.Started.Task;

        Assert.True(await loadedProject.WaitForLoadAsync(CancellationToken.None));
        var project = Assert.Single(loader.WorkspaceFactory.HostWorkspace.CurrentSolution.Projects);
        Assert.Contains("CACHED_PROJECT", project.ParseOptions!.PreprocessorSymbolNames);
        Assert.False(designTimeBuild.Result.Task.IsCompleted);

        designTimeBuild.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);
    }

    [Fact]
    public async Task WaitForAllProjectLoadsAsyncUsesCanonicalSnapshot()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var firstProjectPath = Path.Combine(TempRoot.Root, "First.csproj");
        var secondProjectPath = Path.Combine(TempRoot.Root, "Second.csproj");

        var (firstLoadedProject, firstDesignTimeBuild) = await loader.ExpectDesignTimeBuildAndBeginLoadingProjectAsync(firstProjectPath);
        await firstDesignTimeBuild.Started.Task;
        var allLoads = loader.WaitForCurrentProjectLoadsAsync(CancellationToken.None);
        var (secondLoadedProject, secondDesignTimeBuild) = await loader.ExpectDesignTimeBuildAndBeginLoadingProjectAsync(secondProjectPath);

        Assert.False(allLoads.IsCompleted);
        firstDesignTimeBuild.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, firstProjectPath);
        await allLoads;
        Assert.False(secondLoadedProject.WaitForLoadAsync(CancellationToken.None).AsTask().IsCompleted);

        await secondDesignTimeBuild.Started.Task;
        secondDesignTimeBuild.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, secondProjectPath);
        await secondLoadedProject.WaitForLoadAsync(CancellationToken.None);
        Assert.True(await firstLoadedProject.WaitForLoadAsync(CancellationToken.None));
    }

    [Fact]
    public async Task ShutdownCompletesOutstandingLoadAsUnloaded()
    {
        var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var projectPath = Path.Combine(TempRoot.Root, "Project.csproj");
        var (loadedProject, designTimeBuild) = await loader.ExpectDesignTimeBuildAndBeginLoadingProjectAsync(projectPath);
        await designTimeBuild.Started.Task;

        await server.DisposeAsync();

        Assert.False(await loadedProject.WaitForLoadAsync(CancellationToken.None));
    }

    [Fact]
    public async Task ProjectPathIdentityUsesPlatformSemantics()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var lowerCasePath = Path.Combine(TempRoot.Root, "project.csproj");
        var upperCasePath = Path.Combine(TempRoot.Root, "PROJECT.csproj");

        _ = loader.ExpectDesignTimeBuild(lowerCasePath);
        // If we're on a Unix platform, then we expect the request for PROJECT.csproj will be
        // different, so we'll need to create a separate expectation for that.
        if (PathUtilities.IsUnixLikePlatform)
            _ = loader.ExpectDesignTimeBuild(upperCasePath);

        var lowerCaseLoadedProject = await loader.BeginLoadingProjectAsync(lowerCasePath);
        var upperCaseLoadedProject = await loader.BeginLoadingProjectAsync(upperCasePath);

        if (PathUtilities.IsUnixLikePlatform)
            Assert.NotSame(lowerCaseLoadedProject, upperCaseLoadedProject);
        else
            Assert.Same(lowerCaseLoadedProject, upperCaseLoadedProject);
    }

    [Fact]
    public async Task MalformedAbsoluteProjectPathDoesNotThrowDuringNormalization()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var projectPath = Path.GetPathRoot(TempRoot.Root) + "\0Invalid.csproj";

        Assert.False(await loader.TryUnloadProjectAsync(projectPath));
    }

    [Fact]
    public async Task PrimordialProjectWithoutDesignTimeBuildIsNotUpgraded()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var projectPath = Path.Combine(TempRoot.Root, "Project.csproj");

        var primordialProject = await loader.CreatePrimordialProjectAsync(projectPath, doDesignTimeBuild: false);
        var sameProject = await loader.CreatePrimordialProjectAsync(projectPath, doDesignTimeBuild: true);

        Assert.Equal(primordialProject.Id, sameProject.Id);
        Assert.Equal(0, loader.DesignTimeBuildCount);
    }

    [Fact]
    public async Task PrimordialProjectUsesNormalizedPath()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var projectPath = Path.Combine(TempRoot.Root, "Project.csproj");
        var nonCanonicalProjectPath = Path.Combine(TempRoot.Root, "directory", "..", "Project.csproj");

        var project = await loader.CreatePrimordialProjectAsync(nonCanonicalProjectPath, doDesignTimeBuild: false);
        var projectFromCanonicalPath = await loader.CreatePrimordialProjectAsync(projectPath, doDesignTimeBuild: false);

        Assert.Equal(projectPath, project.FilePath);
        Assert.Equal(project.Id, projectFromCanonicalPath.Id);
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

    /// <summary>
    /// A project loader whose design-time builds are supplied by the test so load ordering and results are deterministic.
    /// </summary>
    internal sealed class TestProjectLoader : LanguageServerProjectLoader, ILspService
    {
        private readonly ConcurrentDictionary<string, ExpectedDesignTimeBuild> _expectedDesignTimeBuilds = new(PathUtilities.Comparer);
        private int _designTimeBuildCount;

        public LanguageServerWorkspaceFactory WorkspaceFactory => _workspaceFactory;
        public int DesignTimeBuildCount => Volatile.Read(ref _designTimeBuildCount);
        public Dictionary<string, ImmutableArray<ProjectFileInfo>> CachedProjects { get; } = new(PathUtilities.Comparer);

        // Tests wait for two design-time builds to start before completing either, so the worker count
        // must not fall back to one on machines with fewer processors.
        protected override int MaxNodeCount => 2;

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

        public ExpectedDesignTimeBuild ExpectDesignTimeBuild(string projectPath)
        {
            projectPath = NormalizeProjectPath(projectPath);
            var designTimeBuild = new ExpectedDesignTimeBuild();
            Assert.True(_expectedDesignTimeBuilds.TryAdd(projectPath, designTimeBuild));
            return designTimeBuild;
        }

        public Task<LoadedProject> BeginLoadingProjectAsync(string projectPath)
            => base.BeginLoadingProjectAsync(projectPath, ProjectReloadPriority.Medium);

        public async Task<(LoadedProject LoadedProject, ExpectedDesignTimeBuild DesignTimeBuild)> ExpectDesignTimeBuildAndBeginLoadingProjectAsync(string projectPath)
        {
            var designTimeBuild = ExpectDesignTimeBuild(projectPath);
            var loadedProject = await BeginLoadingProjectAsync(projectPath);
            return (loadedProject, designTimeBuild);
        }

        public async ValueTask<Project> CreatePrimordialProjectAsync(string projectPath, bool doDesignTimeBuild)
        {
            var projectFactory = WorkspaceFactory.MiscellaneousFilesWorkspaceProjectFactory;
            return (await GetOrLoadProjectAsync(
                projectPath,
                projectFactory,
                (_, normalizedProjectPath) => ProjectInfo.Create(
                    ProjectId.CreateNewId(),
                    VersionStamp.Default,
                    name: "Primordial",
                    assemblyName: "Primordial",
                    LanguageNames.CSharp,
                    filePath: normalizedProjectPath),
                doDesignTimeBuild)).Single();
        }

        protected override async Task<(ImmutableArray<ProjectFileInfo>, ProjectSystemProjectFactory)?> TryLoadProjectFromCacheAsync(
            string projectPath, CancellationToken cancellationToken)
            => CachedProjects.TryGetValue(projectPath, out var projectFileInfos)
                ? (projectFileInfos, WorkspaceFactory.HostProjectFactory)
                : null;

        protected override async Task<RemoteProjectLoadResult?> TryLoadProjectInMSBuildHostAsync(
            BuildHostProcessManager buildHostProcessManager, string projectPath, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _designTimeBuildCount);
            Assert.True(_expectedDesignTimeBuilds.TryGetValue(projectPath, out var designTimeBuild));
            designTimeBuild.Started.TrySetResult();
            try
            {
                return await designTimeBuild.Result.Task.WaitAsync(cancellationToken);
            }
            finally
            {
                Assert.True(_expectedDesignTimeBuilds.TryRemove(new(projectPath, designTimeBuild)));
            }
        }
    }

    /// <summary>Controls the result of one expected design-time build.</summary>
    internal sealed class ExpectedDesignTimeBuild
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<LanguageServerProjectLoader.RemoteProjectLoadResult?> Result { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void CompleteSuccessfully(
            ProjectSystemProjectFactory projectFactory, string projectPath, string? targetFramework = null, string[]? projectReferences = null)
            => Result.SetResult(new()
            {
                ProjectFileInfos = [ProjectFileInfo.CreateEmpty(LanguageNames.CSharp, projectPath) with
                {
                    CommandLineArgs = ["/target:library"],
                    TargetFramework = targetFramework,
                    ProjectReferences = projectReferences is null ? [] : [.. projectReferences.Select(path => new ProjectFileReference(path, [], referenceOutputAssembly: true))],
                }],
                DiagnosticLogItems = [],
                ProjectRestorePath = projectPath,
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
