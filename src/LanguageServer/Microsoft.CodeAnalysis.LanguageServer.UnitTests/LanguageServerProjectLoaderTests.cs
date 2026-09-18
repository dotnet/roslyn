// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias MSBuildWorkspacesContracts;

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.ErrorReporting;
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
    private protected override Task<ExportProvider> CreateExportProviderAsync(
        ServerConfiguration serverConfiguration,
        ILoggerFactory loggerFactory,
        ExtensionAssemblyManager extensionManager,
        IAssemblyLoader assemblyLoader)
        => Task.FromResult(LanguageServerTestComposition.GetSharedExportProvider(
            serverConfiguration, loggerFactory, typeof(TestProjectLoaderFactory), typeof(TestOnDemandProjectLoaderFactory)));

    [Fact]
    public async Task ConcurrentCallersShareLoadedProjectAndCompleteAfterWorkspaceCommit()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var designTimeBuild = loader.QueueDesignTimeBuild();
        var projectPath = Path.Combine(TempRoot.Root, "Project.csproj");
        var equivalentPath = Path.Combine(TempRoot.Root, "directory", "..", "Project.csproj");

        var firstLoadedProject = await loader.BeginLoadAsync(projectPath);
        var secondLoadedProject = await loader.BeginLoadAsync(equivalentPath);

        Assert.Same(firstLoadedProject, secondLoadedProject);
        Assert.False(firstLoadedProject.WaitForLoadAsync(CancellationToken.None).AsTask().IsCompleted);
        await designTimeBuild.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        Assert.Equal(1, loader.DesignTimeBuildCount);

        designTimeBuild.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);
        var loadedSuccessfully = await firstLoadedProject.WaitForLoadAsync(CancellationToken.None).AsTask().WaitAsync(TestHelpers.HangMitigatingTimeout);

        Assert.True(loadedSuccessfully);
        Assert.NotEmpty(loader.WorkspaceFactory.HostWorkspace.CurrentSolution.Projects);
        Assert.Equal(1, loader.CacheLoadCount);
    }

    [Theory, CombinatorialData]
    public async Task ConcurrentCallersDoNotRepeatCacheInitialization(bool highPriority)
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var designTimeBuild = loader.QueueDesignTimeBuild();
        var projectPath = Path.Combine(TempRoot.Root, "Project.csproj");
        var equivalentPath = Path.Combine(TempRoot.Root, "directory", "..", "Project.csproj");
        var enteredCache = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseCache = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        loader.BeforeCacheLoadAsync = async (_, cancellationToken) =>
        {
            enteredCache.TrySetResult();
            await releaseCache.Task.WaitAsync(cancellationToken);
        };

        var firstLoad = loader.BeginLoadAsync(projectPath);
        await enteredCache.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        await designTimeBuild.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        var secondLoad = highPriority
            ? loader.BeginHighPriorityLoadAsync(equivalentPath)
            : loader.BeginLoadAsync(equivalentPath);

        try
        {
            Assert.Equal(1, loader.CacheLoadCount);
            var joinedProject = await secondLoad.WaitAsync(TestHelpers.HangMitigatingTimeout);
            Assert.False(firstLoad.IsCompleted);
            Assert.False(joinedProject.WaitForLoadAsync(CancellationToken.None).AsTask().IsCompleted);
        }
        finally
        {
            releaseCache.SetResult();
            await Task.WhenAll(firstLoad, secondLoad).WaitAsync(TestHelpers.HangMitigatingTimeout);
        }

        var loadedProject = await firstLoad;
        Assert.Same(loadedProject, await secondLoad);
        designTimeBuild.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);
        Assert.True(await loadedProject.WaitForLoadAsync(CancellationToken.None).AsTask().WaitAsync(TestHelpers.HangMitigatingTimeout));
        Assert.Equal(1, loader.CacheLoadCount);
        Assert.Equal(1, loader.DesignTimeBuildCount);
    }

    [Fact]
    public async Task LoadedProjectReturnsWithoutAnotherDesignTimeBuild()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var designTimeBuild = loader.QueueDesignTimeBuild();
        var projectPath = Path.Combine(TempRoot.Root, "Project.csproj");

        var firstLoadedProject = await loader.BeginLoadAsync(projectPath);
        await designTimeBuild.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        designTimeBuild.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);
        var firstResult = await firstLoadedProject.WaitForLoadAsync(CancellationToken.None).AsTask().WaitAsync(TestHelpers.HangMitigatingTimeout);

        var loadedProject = await loader.BeginLoadAsync(projectPath);

        Assert.Same(firstLoadedProject, loadedProject);
        Assert.Equal(firstResult, await loadedProject.WaitForLoadAsync(CancellationToken.None));
        Assert.Equal(1, loader.DesignTimeBuildCount);
        Assert.Equal(1, loader.CacheLoadCount);
    }

    [Fact]
    public async Task HighPriorityLoadPromotesAlreadyQueuedProject()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        loader.MaxNodeCountForTest = 1;
        var blockingBuild = loader.QueueDesignTimeBuild();
        var blockingPath = Path.Combine(TempRoot.Root, "Blocking.csproj");
        await loader.BeginLoadAsync(blockingPath);
        await blockingBuild.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        var firstDesignTimeBuild = loader.QueueDesignTimeBuild();
        var secondDesignTimeBuild = loader.QueueDesignTimeBuild();
        var normalPriorityPath = Path.Combine(TempRoot.Root, "Normal.csproj");
        var highPriorityPath = Path.Combine(TempRoot.Root, "High.csproj");

        var normalPriorityProject = await loader.BeginLoadAsync(normalPriorityPath);
        var highPriorityProject = await loader.BeginLoadAsync(highPriorityPath);
        Assert.Same(highPriorityProject, await loader.BeginHighPriorityLoadAsync(highPriorityPath));
        Assert.Same(highPriorityProject, await loader.BeginLoadAsync(highPriorityPath));
        blockingBuild.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, blockingPath);

        Assert.Equal(
            highPriorityPath,
            await firstDesignTimeBuild.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout));
        firstDesignTimeBuild.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, highPriorityPath);
        Assert.True(await highPriorityProject.WaitForLoadAsync(CancellationToken.None));

        Assert.Equal(
            normalPriorityPath,
            await secondDesignTimeBuild.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout));
        secondDesignTimeBuild.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, normalPriorityPath);
        Assert.True(await normalPriorityProject.WaitForLoadAsync(CancellationToken.None));
    }

    [Fact]
    public async Task HighPriorityLoadDoesNotQueueAnotherInFlightBuild()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var build = loader.QueueDesignTimeBuild();
        var path = Path.Combine(TempRoot.Root, "Project.csproj");
        var project = await loader.BeginLoadAsync(path);
        await build.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);

        Assert.Same(project, await loader.BeginHighPriorityLoadAsync(path));
        build.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, path);
        await server.ExportProvider.GetExportedValue<AsynchronousOperationListenerProvider>().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();
        Assert.Equal(1, loader.DesignTimeBuildCount);
        Assert.Equal(1, loader.CacheLoadCount);
    }

    [Fact]
    public async Task ProjectReferencePathsAreResolvedAndDeduplicated()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var directory = TempRoot.CreateDirectory();
        var projectPath = directory.CreateFile("Root.csproj").Path;
        var dependencyPath = directory.CreateFile("Dependency.csproj").Path;
        var otherDependencyPath = directory.CreateFile("Other.csproj").Path;
        var build = loader.QueueDesignTimeBuild();
        var project = await loader.BeginLoadAsync(projectPath);
        await build.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        build.CompleteSuccessfully(
            loader.WorkspaceFactory.HostProjectFactory, projectPath,
            references: ["Dependency.csproj", "Dependency.csproj", dependencyPath, "Other.csproj"]);
        Assert.True(await project.WaitForLoadAsync(CancellationToken.None).AsTask().WaitAsync(TestHelpers.HangMitigatingTimeout));

        var references = await project.GetProjectReferencePathsAsync(CancellationToken.None);
        Assert.Equal(2, references.Length);
        AssertEx.SetEqual([dependencyPath, otherDependencyPath], references);
    }

    [Fact]
    public async Task OnDemandRequestsJoinClosureAfterRootLoads()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var projectLoader = server.GetRequiredLspService<TestProjectLoader>();
        var loader = server.GetRequiredLspService<IOnDemandProjectLoader>();
        var directory = TempRoot.CreateDirectory();
        var rootPath = directory.CreateFile("Root.csproj").Path;
        var documentPath = directory.CreateDirectory("src").CreateFile("Program.cs").Path;
        var otherDocumentPath = directory.CreateDirectory("linked").CreateFile("Other.cs").Path;
        var dependencyPath = directory.CreateDirectory("dependency").CreateFile("Dependency.csproj").Path;
        var uri = ProtocolConversions.CreateAbsoluteDocumentUri(documentPath);
        server.GetRequiredLspService<IWorkspaceFolderTracker>().Update(
            [new() { DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(directory.Path), Name = "Workspace" }],
            removedFolders: null);
        var rootBuild = projectLoader.QueueDesignTimeBuild();
        var dependencyBuild = projectLoader.QueueDesignTimeBuild();

        var loadTask = loader.StartLoadingAsync(uri);
        Assert.Same(loadTask, loader.StartLoadingAsync(uri));
        var workspaceLoadSnapshot = await loader.CaptureWorkspaceLoadSnapshotAsync();
        var workspaceLoadTask = workspaceLoadSnapshot.Completion;
        Assert.False(workspaceLoadTask.IsCompleted);
        Assert.Equal(rootPath, await rootBuild.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout));
        rootBuild.CompleteSuccessfully(
            projectLoader.WorkspaceFactory.HostProjectFactory, rootPath,
            documents: [documentPath, otherDocumentPath], references: [dependencyPath]);
        Assert.Equal(dependencyPath, await dependencyBuild.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout));

        Assert.NotEmpty(projectLoader.WorkspaceFactory.HostWorkspace.CurrentSolution.GetDocumentIdsWithFilePath(documentPath));
        Assert.Same(loadTask, loader.StartLoadingAsync(uri));
        var otherLoadTask = loader.StartLoadingAsync(ProtocolConversions.CreateAbsoluteDocumentUri(otherDocumentPath));
        Assert.False(otherLoadTask.IsCompleted);
        Assert.False(workspaceLoadTask.IsCompleted);
        dependencyBuild.CompleteSuccessfully(projectLoader.WorkspaceFactory.HostProjectFactory, dependencyPath);
        await Task.WhenAll(loadTask, otherLoadTask, workspaceLoadTask).WaitAsync(TestHelpers.HangMitigatingTimeout);
        Assert.Equal(2, projectLoader.DesignTimeBuildCount);
    }

    [Fact]
    public async Task OnDemandDependenciesTakePriorityOverQueuedProjects()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var projectLoader = server.GetRequiredLspService<TestProjectLoader>();
        projectLoader.MaxNodeCountForTest = 1;
        var directory = TempRoot.CreateDirectory();
        var rootPath = directory.CreateFile("Root.csproj").Path;
        var dependencyPath = directory.CreateDirectory("dependency").CreateFile("Dependency.csproj").Path;
        var normalPath = directory.CreateDirectory("normal").CreateFile("Normal.csproj").Path;
        var blockingPath = directory.CreateDirectory("blocking").CreateFile("Blocking.csproj").Path;
        var uri = ProtocolConversions.CreateAbsoluteDocumentUri(directory.CreateFile("Program.cs").Path);
        server.GetRequiredLspService<IWorkspaceFolderTracker>().Update(
            [new() { DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(directory.Path), Name = "Workspace" }],
            removedFolders: null);
        var releaseRoot = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var dependencyQueued = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        projectLoader.BeforeCacheLoadAsync = async (path, cancellationToken) =>
        {
            if (path == rootPath)
                await releaseRoot.Task.WaitAsync(cancellationToken);
            else if (path == dependencyPath)
                dependencyQueued.SetResult();
        };
        var rootBuild = projectLoader.QueueDesignTimeBuild();
        var blockingBuild = projectLoader.QueueDesignTimeBuild();
        var loadTask = server.GetRequiredLspService<IOnDemandProjectLoader>().StartLoadingAsync(uri);
        await rootBuild.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        await projectLoader.BeginLoadAsync(blockingPath);
        rootBuild.CompleteSuccessfully(projectLoader.WorkspaceFactory.HostProjectFactory, rootPath, references: [dependencyPath]);
        await blockingBuild.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);

        var dependencyBuild = projectLoader.QueueDesignTimeBuild();
        var normalBuild = projectLoader.QueueDesignTimeBuild();
        var normalProject = await projectLoader.BeginLoadAsync(normalPath);
        releaseRoot.SetResult();
        await dependencyQueued.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        blockingBuild.CompleteSuccessfully(projectLoader.WorkspaceFactory.HostProjectFactory, blockingPath);

        Assert.Equal(dependencyPath, await dependencyBuild.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout));
        dependencyBuild.CompleteSuccessfully(projectLoader.WorkspaceFactory.HostProjectFactory, dependencyPath);
        await loadTask.WaitAsync(TestHelpers.HangMitigatingTimeout);
        Assert.Equal(normalPath, await normalBuild.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout));
        normalBuild.CompleteSuccessfully(projectLoader.WorkspaceFactory.HostProjectFactory, normalPath);
        Assert.True(await normalProject.WaitForLoadAsync(CancellationToken.None));
    }

    [Fact]
    public async Task OnDemandShutdownDoesNotSuppressUnexpectedTraversalFailure()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var projectLoader = server.GetRequiredLspService<TestProjectLoader>();
        var directory = TempRoot.CreateDirectory();
        directory.CreateFile("A.csproj");
        directory.CreateFile("B.csproj");
        var uri = ProtocolConversions.CreateAbsoluteDocumentUri(directory.CreateFile("Program.cs").Path);
        var tracker = server.GetRequiredLspService<IWorkspaceFolderTracker>();
        tracker.Update(
            [new() { DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(directory.Path), Name = "Workspace" }],
            removedFolders: null);
        var cancellationObserved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseCache = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        projectLoader.BeforeCacheLoadAsync = async (_, cancellationToken) =>
        {
            using var registration = cancellationToken.Register(() => cancellationObserved.TrySetResult());
            await releaseCache.Task;
            cancellationToken.ThrowIfCancellationRequested();
        };
        projectLoader.QueueDesignTimeBuild();
        var expected = new InvalidOperationException("Unexpected traversal failure");
        var reported = new ConcurrentQueue<Exception>();
        FatalError.OverwriteHandler((exception, _, _) => reported.Enqueue(exception));
        using var logger = new FaultingLoadLogger(expected);
        await using var loader = new OnDemandProjectLoader(
            new OnDemandProjectLoader.ProjectDiscovery([".csproj"], LoggerFactory),
            tracker, projectLoader, projectLoader.WorkspaceFactory,
            server.ExportProvider.GetExportedValue<IGlobalOptionService>(),
            server.ExportProvider.GetExportedValue<AsynchronousOperationListenerProvider>().GetListener(FeatureAttribute.Workspace),
            logger);
        logger.OnSecondLoad = () => _ = loader.ShutdownAsync();

        var loadTask = loader.StartLoadingAsync(uri);
        try
        {
            await cancellationObserved.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
            Assert.False(loadTask.IsCompleted);
        }
        finally
        {
            releaseCache.SetResult();
        }

        await loader.ShutdownAsync().WaitAsync(TestHelpers.HangMitigatingTimeout);
        Assert.Contains(expected, reported);
    }

    [Fact]
    public async Task OnDemandShutdownDrainsChildrenStillEnteringProjectSystem()
    {
        var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var projectLoader = server.GetRequiredLspService<TestProjectLoader>();
        var loader = server.GetRequiredLspService<IOnDemandProjectLoader>();
        var directory = TempRoot.CreateDirectory();
        directory.CreateFile("A.csproj");
        var blockedPath = directory.CreateFile("B.csproj").Path;
        var uri = ProtocolConversions.CreateAbsoluteDocumentUri(directory.CreateFile("Program.cs").Path);
        server.GetRequiredLspService<IWorkspaceFolderTracker>().Update(
            [new() { DocumentUri = ProtocolConversions.CreateAbsoluteDocumentUri(directory.Path), Name = "Workspace" }],
            removedFolders: null);
        var enteredCache = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancellationObserved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseCache = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        projectLoader.BeforeCacheLoadAsync = async (path, cancellationToken) =>
        {
            if (path != blockedPath)
                return;

            using var registration = cancellationToken.Register(() => cancellationObserved.TrySetResult());
            enteredCache.SetResult();
            await releaseCache.Task;
            cancellationToken.ThrowIfCancellationRequested();
        };
        projectLoader.QueueDesignTimeBuild();
        projectLoader.QueueDesignTimeBuild();
        var loadTask = loader.StartLoadingAsync(uri);
        await enteredCache.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);

        var shutdownTask = server.DisposeAsync().AsTask();
        try
        {
            await cancellationObserved.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
            Assert.False(shutdownTask.IsCompleted);
            Assert.False(loadTask.IsCompleted);
            var shutdownHook = server.GetRequiredLspService<OnDemandProjectLoader>();
            Assert.Same(shutdownHook.ShutdownAsync(), shutdownHook.ShutdownAsync());
        }
        finally
        {
            releaseCache.SetResult();
        }

        await shutdownTask.WaitAsync(TestHelpers.HangMitigatingTimeout);
        await loadTask.WaitAsync(TestHelpers.HangMitigatingTimeout);
    }

    [Fact]
    public async Task NeedsReloadTriggersAnotherDesignTimeBuildAfterInitialLoadCompletes()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var firstDesignTimeBuild = loader.QueueDesignTimeBuild();
        var secondDesignTimeBuild = loader.QueueDesignTimeBuild();
        var projectPath = Path.Combine(TempRoot.Root, "Project.csproj");

        var loadedProject = await loader.BeginLoadAsync(projectPath);
        await firstDesignTimeBuild.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        firstDesignTimeBuild.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);
        await loadedProject.WaitForLoadAsync(CancellationToken.None).AsTask().WaitAsync(TestHelpers.HangMitigatingTimeout);

        loadedProject.GetTestAccessor().RaiseNeedsReload();

        // A file-change-triggered reload after the initial load has committed must still reach the MSBuild host,
        // rather than being dropped because it carries the (already-completed) load operation from the initial request.
        await secondDesignTimeBuild.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        secondDesignTimeBuild.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);

        Assert.Equal(2, loader.DesignTimeBuildCount);
    }

    [Fact]
    public async Task UnsupportedReloadPreservesPreviouslyLoadedProject()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var initialDesignTimeBuild = loader.QueueDesignTimeBuild();
        var unsupportedReload = loader.QueueDesignTimeBuild();
        var projectPath = Path.Combine(TempRoot.Root, "Project.csproj");

        var loadedProject = await loader.BeginLoadAsync(projectPath);
        await initialDesignTimeBuild.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        initialDesignTimeBuild.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);
        Assert.True(await loadedProject.WaitForLoadAsync(CancellationToken.None).AsTask().WaitAsync(TestHelpers.HangMitigatingTimeout));
        var projectId = loader.WorkspaceFactory.HostWorkspace.CurrentSolution.Projects.Single().Id;

        loadedProject.GetTestAccessor().RaiseNeedsReload();
        await unsupportedReload.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        unsupportedReload.CompleteAsUnsupported();
        await server.ExportProvider.GetExportedValue<AsynchronousOperationListenerProvider>().GetWaiter(FeatureAttribute.Workspace).ExpeditedWaitAsync();

        Assert.True(await loadedProject.WaitForLoadAsync(CancellationToken.None));
        Assert.Equal(projectId, loader.WorkspaceFactory.HostWorkspace.CurrentSolution.Projects.Single().Id);
        Assert.Equal(2, loader.DesignTimeBuildCount);
    }

    [Fact]
    public async Task FailedProjectOnlyRetriesForFileChange()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var failedDesignTimeBuild = loader.QueueDesignTimeBuild();
        var projectPath = Path.Combine(TempRoot.Root, "Project.csproj");

        var firstLoadedProject = await loader.BeginLoadAsync(projectPath);
        await failedDesignTimeBuild.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        failedDesignTimeBuild.Fail(new InvalidOperationException("Expected test failure"));
        Assert.False(await firstLoadedProject.WaitForLoadAsync(CancellationToken.None).AsTask().WaitAsync(TestHelpers.HangMitigatingTimeout));

        var loadedProject = await loader.BeginLoadAsync(projectPath);

        Assert.Same(firstLoadedProject, loadedProject);
        Assert.False(await loadedProject.WaitForLoadAsync(CancellationToken.None));
        Assert.Equal(1, loader.DesignTimeBuildCount);

        var successfulReload = loader.QueueDesignTimeBuild();
        loadedProject.GetTestAccessor().RaiseNeedsReload();
        await successfulReload.Started.Task;
        successfulReload.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);
    }

    [Fact]
    public async Task FailureCompletesOnlyAffectedProject()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var firstDesignTimeBuild = loader.QueueDesignTimeBuild();
        var secondDesignTimeBuild = loader.QueueDesignTimeBuild();
        var failedPath = Path.Combine(TempRoot.Root, "Failed.csproj");
        var successfulPath = Path.Combine(TempRoot.Root, "Successful.csproj");

        var failedProject = await loader.BeginLoadAsync(failedPath);
        var successfulProject = await loader.BeginLoadAsync(successfulPath);
        await Task.WhenAll(firstDesignTimeBuild.Started.Task, secondDesignTimeBuild.Started.Task).WaitAsync(TestHelpers.HangMitigatingTimeout);

        var failedDesignTimeBuild = firstDesignTimeBuild.Started.Task.Result == failedPath ? firstDesignTimeBuild : secondDesignTimeBuild;
        var successfulDesignTimeBuild = firstDesignTimeBuild.Started.Task.Result == successfulPath ? firstDesignTimeBuild : secondDesignTimeBuild;
        failedDesignTimeBuild.Fail(new InvalidOperationException("Expected test failure"));
        successfulDesignTimeBuild.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, successfulPath);

        Assert.False(await failedProject.WaitForLoadAsync(CancellationToken.None).AsTask().WaitAsync(TestHelpers.HangMitigatingTimeout));
        Assert.True(await successfulProject.WaitForLoadAsync(CancellationToken.None).AsTask().WaitAsync(TestHelpers.HangMitigatingTimeout));
    }

    [Fact]
    public async Task ExplicitLoadDoesNotWaitForUnrelatedQueuedWork()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var firstDesignTimeBuild = loader.QueueDesignTimeBuild();
        var secondDesignTimeBuild = loader.QueueDesignTimeBuild();
        var requestedPath = Path.Combine(TempRoot.Root, "Requested.csproj");
        var unrelatedPath = Path.Combine(TempRoot.Root, "Unrelated.csproj");

        var requestedProject = await loader.BeginLoadAsync(requestedPath);
        var unrelatedProject = await loader.BeginLoadAsync(unrelatedPath);
        await Task.WhenAll(firstDesignTimeBuild.Started.Task, secondDesignTimeBuild.Started.Task).WaitAsync(TestHelpers.HangMitigatingTimeout);

        var requestedDesignTimeBuild = firstDesignTimeBuild.Started.Task.Result == requestedPath ? firstDesignTimeBuild : secondDesignTimeBuild;
        var unrelatedDesignTimeBuild = firstDesignTimeBuild.Started.Task.Result == unrelatedPath ? firstDesignTimeBuild : secondDesignTimeBuild;
        var explicitLoad = loader.WaitForExplicitLoadsAsync([requestedProject]);

        requestedDesignTimeBuild.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, requestedPath);
        await explicitLoad.WaitAsync(TestHelpers.HangMitigatingTimeout);
        Assert.False(unrelatedProject.WaitForLoadAsync(CancellationToken.None).AsTask().IsCompleted);

        unrelatedDesignTimeBuild.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, unrelatedPath);
        await unrelatedProject.WaitForLoadAsync(CancellationToken.None).AsTask().WaitAsync(TestHelpers.HangMitigatingTimeout);
    }

    [Fact]
    public async Task ExplicitLoadWaitsForAllRequestedProjectsDespiteFailure()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var firstDesignTimeBuild = loader.QueueDesignTimeBuild();
        var secondDesignTimeBuild = loader.QueueDesignTimeBuild();
        var firstPath = Path.Combine(TempRoot.Root, "First.csproj");
        var secondPath = Path.Combine(TempRoot.Root, "Second.csproj");

        var firstProject = await loader.BeginLoadAsync(firstPath);
        var secondProject = await loader.BeginLoadAsync(secondPath);
        await Task.WhenAll(firstDesignTimeBuild.Started.Task, secondDesignTimeBuild.Started.Task).WaitAsync(TestHelpers.HangMitigatingTimeout);

        var failedDesignTimeBuild = firstDesignTimeBuild.Started.Task.Result == firstPath ? firstDesignTimeBuild : secondDesignTimeBuild;
        var successfulDesignTimeBuild = firstDesignTimeBuild.Started.Task.Result == secondPath ? firstDesignTimeBuild : secondDesignTimeBuild;
        var explicitLoad = loader.WaitForExplicitLoadsAsync([firstProject, secondProject]);

        failedDesignTimeBuild.Fail(new InvalidOperationException("Expected test failure"));
        await firstProject.WaitForLoadAsync(CancellationToken.None).AsTask().WaitAsync(TestHelpers.HangMitigatingTimeout);
        Assert.False(explicitLoad.IsCompleted);

        successfulDesignTimeBuild.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, secondPath);
        await explicitLoad.WaitAsync(TestHelpers.HangMitigatingTimeout);
    }

    [Fact]
    public async Task JoinedExplicitLoadsReportProgressIndependently()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var designTimeBuild = loader.QueueDesignTimeBuild();
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

            await designTimeBuild.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
            designTimeBuild.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);
            await Task.WhenAll(firstLoad, secondLoad).WaitAsync(TestHelpers.HangMitigatingTimeout);
        }

        // Disposing the trackers ensures their asynchronous progress queues have finished reporting.
        Assert.Contains(firstReporter.Reports, report => report is LSP.WorkDoneProgressReport { Percentage: 99 });
        Assert.Contains(secondReporter.Reports, report => report is LSP.WorkDoneProgressReport { Percentage: 99 });
        Assert.Equal(1, loader.DesignTimeBuildCount);
    }

    [Fact]
    public async Task UnsupportedProjectReturnsCanonicalCompletedResult()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var designTimeBuild = loader.QueueDesignTimeBuild();
        var projectPath = Path.Combine(TempRoot.Root, "Unsupported.csproj");

        var loadedProject = await loader.BeginLoadAsync(projectPath);
        await designTimeBuild.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        designTimeBuild.CompleteAsUnsupported();
        var loadedSuccessfully = await loadedProject.WaitForLoadAsync(CancellationToken.None).AsTask().WaitAsync(TestHelpers.HangMitigatingTimeout);

        var laterLoadedProject = await loader.BeginLoadAsync(projectPath);
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
        var designTimeBuild = loader.QueueDesignTimeBuild();
        var projectPath = Path.Combine(TempRoot.Root, "Project.csproj");

        var loadedProject = await loader.BeginLoadAsync(projectPath);
        await designTimeBuild.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        Assert.True(await loader.UnloadAsync(projectPath));
        Assert.False(await loadedProject.WaitForLoadAsync(CancellationToken.None).AsTask().WaitAsync(TestHelpers.HangMitigatingTimeout));

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
        var currentDesignTimeBuild = loader.QueueDesignTimeBuild();
        var projectPath = Path.Combine(TempRoot.Root, "Project.csproj");

        var staleLoadedProject = await loader.BeginLoadAsync(projectPath);
        Assert.True(await loader.UnloadAsync(projectPath));
        var currentLoadedProject = await loader.BeginLoadAsync(projectPath);

        Assert.NotSame(staleLoadedProject, currentLoadedProject);
        Assert.Equal(2, loader.CacheLoadCount);
        Assert.False(await staleLoadedProject.WaitForLoadAsync(CancellationToken.None));
        await currentDesignTimeBuild.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);

        currentDesignTimeBuild.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);
        var loadedSuccessfully = await currentLoadedProject.WaitForLoadAsync(CancellationToken.None).AsTask().WaitAsync(TestHelpers.HangMitigatingTimeout);

        Assert.True(loadedSuccessfully);
        Assert.Equal(1, loader.DesignTimeBuildCount);
        Assert.Single(loader.WorkspaceFactory.HostWorkspace.CurrentSolution.Projects);
    }

    [Fact]
    public async Task CachedProjectHasProjectDataWhileDesignTimeBuildIsPending()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var designTimeBuild = loader.QueueDesignTimeBuild();
        var projectPath = Path.Combine(TempRoot.Root, "Cached.csproj");
        loader.CachedProjects.Add(projectPath, [ProjectFileInfo.CreateEmpty(LanguageNames.CSharp, projectPath) with { CommandLineArgs = ["/target:library", "/define:CACHED_PROJECT"] }]);

        var loadedProject = await loader.BeginLoadAsync(projectPath);
        await designTimeBuild.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);

        Assert.True(await loadedProject.WaitForLoadAsync(CancellationToken.None).AsTask().WaitAsync(TestHelpers.HangMitigatingTimeout));
        var project = Assert.Single(loader.WorkspaceFactory.HostWorkspace.CurrentSolution.Projects);
        Assert.Contains("CACHED_PROJECT", project.ParseOptions!.PreprocessorSymbolNames);
        Assert.False(designTimeBuild.Result.Task.IsCompleted);
        Assert.Same(loadedProject, await loader.BeginLoadAsync(projectPath));
        Assert.Equal(1, loader.CacheLoadCount);

        designTimeBuild.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath);
    }

    [Fact]
    public async Task WorkspaceLoadSnapshotIncludesExplicitLoadsButNotLaterLoads()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var projectLoader = server.GetRequiredLspService<TestProjectLoader>();
        var loader = server.GetRequiredLspService<IOnDemandProjectLoader>();
        var firstBuild = projectLoader.QueueDesignTimeBuild();
        var laterBuild = projectLoader.QueueDesignTimeBuild();
        var firstPath = Path.Combine(TempRoot.Root, "First.csproj");
        var laterPath = Path.Combine(TempRoot.Root, "Later.csproj");
        await projectLoader.BeginLoadAsync(firstPath);
        await firstBuild.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);

        var snapshot = await loader.CaptureWorkspaceLoadSnapshotAsync().AsTask().WaitAsync(TestHelpers.HangMitigatingTimeout);
        Assert.False(snapshot.Completion.IsCompleted);

        var laterProject = await projectLoader.BeginLoadAsync(laterPath);
        firstBuild.CompleteSuccessfully(projectLoader.WorkspaceFactory.HostProjectFactory, firstPath);
        await snapshot.Completion.WaitAsync(TestHelpers.HangMitigatingTimeout);

        Assert.False(laterProject.WaitForLoadAsync(CancellationToken.None).AsTask().IsCompleted);
        await laterBuild.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        laterBuild.CompleteSuccessfully(projectLoader.WorkspaceFactory.HostProjectFactory, laterPath);
        Assert.True(await laterProject.WaitForLoadAsync(CancellationToken.None).AsTask().WaitAsync(TestHelpers.HangMitigatingTimeout));
    }

    [Fact]
    public async Task WaitForAllProjectLoadsAsyncUsesCanonicalSnapshot()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var firstDesignTimeBuild = loader.QueueDesignTimeBuild();
        var secondDesignTimeBuild = loader.QueueDesignTimeBuild();
        var firstProjectPath = Path.Combine(TempRoot.Root, "First.csproj");
        var secondProjectPath = Path.Combine(TempRoot.Root, "Second.csproj");

        var firstLoadedProject = await loader.BeginLoadAsync(firstProjectPath);
        await firstDesignTimeBuild.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        var allLoads = loader.WaitForAllTrackedProjectLoadsAsync();
        var secondLoadedProject = await loader.BeginLoadAsync(secondProjectPath);

        Assert.False(allLoads.IsCompleted);
        firstDesignTimeBuild.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, firstProjectPath);
        await allLoads.WaitAsync(TestHelpers.HangMitigatingTimeout);
        Assert.False(secondLoadedProject.WaitForLoadAsync(CancellationToken.None).AsTask().IsCompleted);

        await secondDesignTimeBuild.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);
        secondDesignTimeBuild.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, secondProjectPath);
        await secondLoadedProject.WaitForLoadAsync(CancellationToken.None).AsTask().WaitAsync(TestHelpers.HangMitigatingTimeout);
        Assert.True(await firstLoadedProject.WaitForLoadAsync(CancellationToken.None));
    }

    [Fact]
    public async Task ShutdownCompletesOutstandingLoadAsUnloaded()
    {
        var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var designTimeBuild = loader.QueueDesignTimeBuild();
        var loadedProject = await loader.BeginLoadAsync(Path.Combine(TempRoot.Root, "Project.csproj"));
        await designTimeBuild.Started.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);

        await server.DisposeAsync();

        Assert.False(await loadedProject.WaitForLoadAsync(CancellationToken.None));
    }

    [Fact]
    public async Task ProjectPathIdentityUsesPlatformSemantics()
    {
        await using var server = await CreateLanguageServerAsync(serverConfiguration: ServerConfigurationWithoutDevKit);
        var loader = server.GetRequiredLspService<TestProjectLoader>();
        _ = loader.QueueDesignTimeBuild();
        _ = loader.QueueDesignTimeBuild();

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

    [ExportCSharpVisualBasicLspServiceFactory(typeof(OnDemandProjectLoader), WellKnownLspServerKinds.CSharpVisualBasicLspServer), PartNotDiscoverable, Shared]
    [method: ImportingConstructor]
    [method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    internal sealed class TestOnDemandProjectLoaderFactory(
        IGlobalOptionService globalOptionService,
        IAsynchronousOperationListenerProvider listenerProvider) : ILspServiceFactory
    {
        public ILspService CreateILspService(LspServices lspServices, WellKnownLspServerKinds serverKind)
        {
            var loggerFactory = lspServices.GetRequiredService<ILoggerFactory>();
            return new OnDemandProjectLoader(
                new OnDemandProjectLoader.ProjectDiscovery(
                    lspServices.GetRequiredService<LanguageServerProjectSystem>().GetSupportedProjectFileExtensions(), loggerFactory),
                lspServices.GetRequiredService<IWorkspaceFolderTracker>(),
                lspServices.GetRequiredService<TestProjectLoader>(),
                lspServices.GetRequiredService<LanguageServerWorkspaceFactory>(),
                globalOptionService,
                listenerProvider.GetListener(FeatureAttribute.Workspace),
                loggerFactory);
        }
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
        private readonly ConcurrentQueue<ExpectedDesignTimeBuild> _expectedDesignTimeBuilds = new();
        private int _designTimeBuildCount;
        private int _cacheLoadCount;

        public LanguageServerWorkspaceFactory WorkspaceFactory => _workspaceFactory;
        public int DesignTimeBuildCount => Volatile.Read(ref _designTimeBuildCount);
        public int CacheLoadCount => Volatile.Read(ref _cacheLoadCount);
        public Dictionary<string, ImmutableArray<ProjectFileInfo>> CachedProjects { get; } = new(PathUtilities.Comparer);
        public int MaxNodeCountForTest { get; set; } = 2;
        public Func<string, CancellationToken, Task>? BeforeCacheLoadAsync { get; set; }

        // Tests wait for two design-time builds to start before completing either, so the worker count
        // must not fall back to one on machines with fewer processors.
        protected override int MaxNodeCount => MaxNodeCountForTest;

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

        public ExpectedDesignTimeBuild QueueDesignTimeBuild()
        {
            var designTimeBuild = new ExpectedDesignTimeBuild();
            _expectedDesignTimeBuilds.Enqueue(designTimeBuild);
            return designTimeBuild;
        }

        public Task<LoadedProject> BeginLoadAsync(string projectPath)
            => BeginLoadingProjectAsync(projectPath);

        public Task<LoadedProject> BeginHighPriorityLoadAsync(string projectPath)
            => BeginLoadingProjectAsync(projectPath, ProjectReloadPriority.High);

        public Task WaitForAllTrackedProjectLoadsAsync(CancellationToken cancellationToken = default)
            => WaitForAllProjectLoadsAsync(cancellationToken);

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
        {
            Interlocked.Increment(ref _cacheLoadCount);
            if (BeforeCacheLoadAsync is { } beforeCacheLoad)
                await beforeCacheLoad(projectPath, cancellationToken);

            return CachedProjects.TryGetValue(projectPath, out var projectFileInfos)
                ? (projectFileInfos, WorkspaceFactory.HostProjectFactory)
                : null;
        }

        protected override async Task<RemoteProjectLoadResult?> TryLoadProjectInMSBuildHostAsync(
            BuildHostProcessManager buildHostProcessManager, string projectPath, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _designTimeBuildCount);
            Assert.True(_expectedDesignTimeBuilds.TryDequeue(out var designTimeBuild));
            designTimeBuild.Started.TrySetResult(projectPath);
            return await designTimeBuild.Result.Task.WaitAsync(cancellationToken);
        }
    }

    /// <summary>Controls the result of one expected design-time build.</summary>
    internal sealed class ExpectedDesignTimeBuild
    {
        public TaskCompletionSource<string> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<LanguageServerProjectLoader.RemoteProjectLoadResult?> Result { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void CompleteSuccessfully(
            ProjectSystemProjectFactory projectFactory, string projectPath, string? targetFramework = null,
            string[]? documents = null, string[]? references = null)
            => Result.SetResult(new()
            {
                ProjectFileInfos = [ProjectFileInfo.CreateEmpty(LanguageNames.CSharp, projectPath) with
                {
                    CommandLineArgs = ["/target:library"],
                    TargetFramework = targetFramework,
                    Documents = documents?.Select(path => new MSBuildWorkspacesContracts::Microsoft.CodeAnalysis.MSBuild.DocumentFileInfo(
                        path, Path.GetFileName(path), isLinked: false, isGenerated: false, folders: [])).ToArray() ?? [],
                    ProjectReferences = references?.Select(path => new MSBuildWorkspacesContracts::Microsoft.CodeAnalysis.MSBuild.ProjectFileReference(
                        path, aliases: [], referenceOutputAssembly: true)).ToArray() ?? [],
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

    private sealed class FaultingLoadLogger(Exception exception) : ILoggerFactory, ILogger
    {
        private int _loadCount;
        public Action? OnSecondLoad { get; set; }

        public ILogger CreateLogger(string categoryName) => this;
        public void AddProvider(ILoggerProvider provider) => throw new NotSupportedException();
        public void Dispose() { }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? error, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Information && ++_loadCount == 2)
            {
                OnSecondLoad!();
                throw exception;
            }
        }
    }

    private sealed class TestProgressReporter : IProgress<LSP.WorkDoneProgress>
    {
        public ConcurrentQueue<LSP.WorkDoneProgress> Reports { get; } = new();

        public void Report(LSP.WorkDoneProgress value)
            => Reports.Enqueue(value);
    }
}
