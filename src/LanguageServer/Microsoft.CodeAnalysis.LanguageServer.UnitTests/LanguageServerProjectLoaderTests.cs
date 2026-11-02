// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

extern alias MSBuildWorkspacesContracts;

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics;
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
using Roslyn.Utilities;
using StreamJsonRpc;
using Xunit.Abstractions;
using DocumentFileInfo = MSBuildWorkspacesContracts::Microsoft.CodeAnalysis.MSBuild.DocumentFileInfo;
using FileGlobs = MSBuildWorkspacesContracts::Microsoft.CodeAnalysis.MSBuild.FileGlobs;
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeletedSourceFilesDoNotDelayCompilationAfterReload(bool deleteDirectory)
        => await TestDeletedSourceFilesAsync(deleteDirectory, reloadBeforeNotification: false);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DeletedSourceNotificationsAfterReloadDoNotChangeDocuments(bool deleteDirectory)
        => await TestDeletedSourceFilesAsync(deleteDirectory, reloadBeforeNotification: true);

    private async Task TestDeletedSourceFilesAsync(bool deleteDirectory, bool reloadBeforeNotification)
    {
        AsynchronousOperationListenerProvider.Enable(enable: true);
        await using var server = await CreateLanguageServerAsync(
            new LSP.ClientCapabilities
            {
                Workspace = new LSP.WorkspaceClientCapabilities
                {
                    DidChangeWatchedFiles = new LSP.DidChangeWatchedFilesClientCapabilities { DynamicRegistration = true },
                },
            },
            ServerConfigurationWithoutDevKit);
        server.AddClientLocalRpcTarget(new FileWatcherClient());

        var directory = TempRoot.CreateDirectory();
        var projectPath = directory.CreateFile("Project.csproj").WriteAllText("<Project />").Path;
        // OpenProjectHandler receives this URI-normalized path, including a lower-case drive letter on Windows.
        projectPath = ProtocolConversions.CreateAbsoluteDocumentUri(projectPath).GetRequiredParsedUri().FsPath;
        var sourceDirectory = directory.CreateDirectory("Sources");
        var deletedPaths = Enumerable.Range(0, 20)
            .Select(i => sourceDirectory.CreateFile($"C{i}.cs").WriteAllText($"class C{i} {{ }}").Path).ToArray();
        var keptPath = directory.CreateFile("Kept.cs").WriteAllText("class Kept { }").Path;
        var projectInfo = ProjectFileInfo.CreateEmpty(LanguageNames.CSharp, projectPath) with
        {
            CommandLineArgs = ["/target:library"],
            ProjectAssetsFilePath = directory.CreateFile("project.assets.json").WriteAllText("{}").Path,
            Documents = [.. deletedPaths.Append(keptPath).Select(path => new DocumentFileInfo(
                path, Path.GetFileName(path), isLinked: false, isGenerated: false, folders: []))],
            FileGlobs = [new FileGlobs(["**/*.cs"], [], [])],
        };

        var loader = server.GetRequiredLspService<TestProjectLoader>();
        var (loadedProject, initialBuild) = await loader.ExpectDesignTimeBuildAndBeginLoadingProjectAsync(projectPath);
        TestOutputHelper.WriteLine("Waiting for initial design-time build.");
        await initialBuild.Started.Task.WaitAsync(TimeSpan.FromSeconds(30));
        initialBuild.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath, projectInfo: projectInfo);
        Assert.True(await loadedProject.WaitForLoadAsync(CancellationToken.None).AsTask().WaitAsync(TimeSpan.FromSeconds(30)));
        var workspaceWaiter = server.ExportProvider.GetExportedValue<AsynchronousOperationListenerProvider>()
            .GetWaiter(FeatureAttribute.Workspace);
        await workspaceWaiter.ExpeditedWaitAsync().WaitAsync(TimeSpan.FromSeconds(30));

        var workspace = loader.WorkspaceFactory.HostWorkspace;
        var initialProject = Assert.Single(workspace.CurrentSolution.Projects);
        TestOutputHelper.WriteLine("Building initial compilation.");
        var initialCompilation = await initialProject.GetCompilationAsync();
        Assert.NotNull(initialCompilation!.GetTypeByMetadataName("C0"));
        var deletedIds = initialProject.Documents.Where(d => d.FilePath != keptPath).Select(d => d.Id).ToArray();
        var keptId = initialProject.Documents.Single(d => d.FilePath == keptPath).Id;
        var reload = loader.ExpectDesignTimeBuild(projectPath);

        if (deleteDirectory)
        {
            Directory.Delete(sourceDirectory.Path, recursive: true);
        }
        else
        {
            foreach (var path in deletedPaths)
                File.Delete(path);
        }

        if (reloadBeforeNotification)
            loadedProject.GetTestAccessor().RaiseNeedsReload();
        else
            await SendDeletionNotificationsAsync();
        TestOutputHelper.WriteLine("Waiting for reload design-time build.");
        await reload.Started.Task.WaitAsync(TimeSpan.FromSeconds(30));

        // Hold the design-time build until file-change processing has invalidated every deleted file's cached text.
        // This is the intermediate snapshot that a fast reload can otherwise hide.
        if (!reloadBeforeNotification)
        {
            await WaitForConditionAsync(() => deletedIds.All(id =>
                !workspace.CurrentSolution.GetDocument(id)!.TryGetText(out _))).WaitAsync(TimeSpan.FromSeconds(30));
        }
        TestOutputHelper.WriteLine("Completing reload.");
        reload.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath,
            projectInfo: projectInfo with { Documents = [projectInfo.Documents.Last()] });
        await workspaceWaiter.ExpeditedWaitAsync().WaitAsync(TimeSpan.FromSeconds(30));

        if (reloadBeforeNotification)
        {
            var solutionAfterRemoval = workspace.CurrentSolution;
            var notificationReload = loader.ExpectDesignTimeBuild(projectPath);
            await SendDeletionNotificationsAsync();
            await notificationReload.Started.Task.WaitAsync(TimeSpan.FromSeconds(30));
            notificationReload.CompleteSuccessfully(loader.WorkspaceFactory.HostProjectFactory, projectPath,
                projectInfo: projectInfo with { Documents = [projectInfo.Documents.Last()] });
            await workspaceWaiter.ExpeditedWaitAsync().WaitAsync(TimeSpan.FromSeconds(30));
            Assert.Same(solutionAfterRemoval.GetDocument(keptId)!.State, workspace.CurrentSolution.GetDocument(keptId)!.State);
        }

        var remainingDocument = Assert.Single(Assert.Single(workspace.CurrentSolution.Projects).Documents);
        Assert.Equal(keptId, remainingDocument.Id);
        var stopwatch = Stopwatch.StartNew();
        var semanticModel = await remainingDocument.GetSemanticModelAsync();
        stopwatch.Stop();
        TestOutputHelper.WriteLine($"Compilation after deleting {deletedPaths.Length} files: {stopwatch.Elapsed}.");
        Assert.NotNull(semanticModel);
        Assert.Null(semanticModel.Compilation.GetTypeByMetadataName("C0"));
        Assert.NotNull(semanticModel.Compilation.GetTypeByMetadataName("Kept"));
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(10), $"Compilation took {stopwatch.Elapsed}.");
        GC.KeepAlive(initialCompilation);

        async Task SendDeletionNotificationsAsync()
        {
            TestOutputHelper.WriteLine("Delivering deletion notifications.");
            await server.ExecuteRequestAsync<LSP.DidChangeWatchedFilesParams, object>(
                LSP.Methods.WorkspaceDidChangeWatchedFilesName,
                new LSP.DidChangeWatchedFilesParams
                {
                    Changes = deletedPaths.Select(path => new LSP.FileEvent
                    {
                        Uri = ProtocolConversions.CreateAbsoluteDocumentUri(path),
                        FileChangeType = LSP.FileChangeType.Deleted,
                    }).ToArray(),
                },
                CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(30));
        }
    }

    private sealed class FileWatcherClient
    {
        [JsonRpcMethod("client/registerCapability", UseSingleObjectParameterDeserialization = true)]
        public void RegisterCapability(LSP.RegistrationParams _)
        {
        }

        [JsonRpcMethod("client/unregisterCapability", UseSingleObjectParameterDeserialization = true)]
        public void UnregisterCapability(LSP.UnregistrationParams _)
        {
        }
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
        var allLoads = loader.WaitForAllProjectLoadsAsync(CancellationToken.None);
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

        public void CompleteSuccessfully(ProjectSystemProjectFactory projectFactory, string projectPath, string? targetFramework = null, ProjectFileInfo? projectInfo = null)
            => Result.SetResult(new()
            {
                ProjectFileInfos = [projectInfo ?? ProjectFileInfo.CreateEmpty(LanguageNames.CSharp, projectPath) with { CommandLineArgs = ["/target:library"], TargetFramework = targetFramework }],
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
