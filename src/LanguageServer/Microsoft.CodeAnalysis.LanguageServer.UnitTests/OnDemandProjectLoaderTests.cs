// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServer.Handler;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.Extensions.Logging.Abstractions;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class OnDemandProjectLoaderTests : IDisposable
{
    private readonly TempRoot _tempRoot = new();

    public void Dispose()
        => _tempRoot.Dispose();

    [Fact]
    public async Task RepeatedTriggersShareDiscoveryAndLoading()
    {
        var workspace = _tempRoot.CreateDirectory();
        var project = workspace.CreateFile("App.csproj");
        var document = workspace.CreateFile("Program.cs");
        var loadStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var loadCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var loadCount = 0;
        using var loader = CreateLoader(
            workspace.Path,
            directory => [project.Path],
            async (projectPath, cancellationToken) =>
            {
                Assert.Equal(project.Path, projectPath);
                Interlocked.Increment(ref loadCount);
                loadStarted.SetResult();
                await loadCompletion.Task.WaitAsync(cancellationToken);
            });

        var uri = ProtocolConversions.CreateAbsoluteDocumentUri(document.Path);
        var firstOperation = loader.StartLoading(uri);
        var secondOperation = loader.StartLoading(ProtocolConversions.CreateAbsoluteDocumentUri(document.Path.ToUpperInvariant()));
        await loadStarted.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);

        using var requestCancellationSource = new CancellationTokenSource();
        var canceledWait = firstOperation.WaitAsync(requestCancellationSource.Token);
        requestCancellationSource.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceledWait);

        loadCompletion.SetResult();
        await secondOperation.WaitAsync(CancellationToken.None).WaitAsync(TestHelpers.HangMitigatingTimeout);
        Assert.Equal(1, Volatile.Read(ref loadCount));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task DisabledOrDevKitDoesNotDiscover(bool isEnabled, bool isUsingDevKit)
    {
        var workspace = _tempRoot.CreateDirectory();
        var document = workspace.CreateFile("Program.cs");
        var enumerationCount = 0;
        using var loader = CreateLoader(
            workspace.Path,
            directory =>
            {
                Interlocked.Increment(ref enumerationCount);
                return [];
            },
            (projectPath, cancellationToken) => throw new InvalidOperationException(),
            isEnabled,
            isUsingDevKit);

        var operation = loader.StartLoading(ProtocolConversions.CreateAbsoluteDocumentUri(document.Path));
        await operation.WaitAsync(CancellationToken.None);

        Assert.Equal(0, Volatile.Read(ref enumerationCount));
    }

    [Fact]
    public async Task LoadsEveryNearestCandidate()
    {
        var workspace = _tempRoot.CreateDirectory();
        var firstProject = workspace.CreateFile("First.csproj");
        var secondProject = workspace.CreateFile("Second.csproj");
        var document = workspace.CreateFile("Program.cs");
        var loadedProjects = new ConcurrentSet<string>(StringComparer.OrdinalIgnoreCase);
        using var loader = CreateLoader(
            workspace.Path,
            directory => [secondProject.Path, firstProject.Path],
            (projectPath, cancellationToken) =>
            {
                loadedProjects.Add(projectPath);
                return Task.CompletedTask;
            });

        var operation = loader.StartLoading(ProtocolConversions.CreateAbsoluteDocumentUri(document.Path));
        await operation.WaitAsync(CancellationToken.None).WaitAsync(TestHelpers.HangMitigatingTimeout);

        AssertEx.SetEqual([firstProject.Path, secondProject.Path], loadedProjects);
    }

    [Fact]
    public async Task EmptyDiscoveryIsRetriedOnLaterDemand()
    {
        var workspace = _tempRoot.CreateDirectory();
        var document = workspace.CreateFile("Program.cs");
        var enumerationCount = 0;
        using var loader = CreateLoader(
            workspace.Path,
            directory =>
            {
                Interlocked.Increment(ref enumerationCount);
                return [];
            },
            (projectPath, cancellationToken) => throw new InvalidOperationException());
        var uri = ProtocolConversions.CreateAbsoluteDocumentUri(document.Path);

        await loader.StartLoading(uri).WaitAsync(CancellationToken.None);
        await loader.StartLoading(uri).WaitAsync(CancellationToken.None);

        Assert.Equal(2, Volatile.Read(ref enumerationCount));
    }

    [Fact]
    public async Task DependencyClosureStartsOnlyForDependencyPreferenceAndIsShared()
    {
        var workspace = _tempRoot.CreateDirectory();
        var project = workspace.CreateFile("App.csproj");
        var dependency = workspace.CreateFile("Dependency.csproj");
        var document = workspace.CreateFile("Program.cs");
        var projectId = ProjectId.CreateNewId();
        var dependencyLoadCount = 0;
        using var loader = CreateLoader(
            workspace.Path,
            directory => [project.Path],
            projectPath =>
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(projectPath, dependency.Path))
                    Interlocked.Increment(ref dependencyLoadCount);

                return new LanguageServerProjectLoadResult(
                    LanguageServerProjectLoadStatus.Loaded,
                    StringComparer.OrdinalIgnoreCase.Equals(projectPath, project.Path) ? [projectId] : []);
            },
            projectIds => projectIds.Contains(projectId) ? [dependency.Path] : []);

        var operation = loader.StartLoading(ProtocolConversions.CreateAbsoluteDocumentUri(document.Path));
        await operation.WaitAsync(LspSolutionContextPreference.Project, CancellationToken.None);
        Assert.Equal(0, Volatile.Read(ref dependencyLoadCount));

        await Task.WhenAll(
            operation.WaitAsync(LspSolutionContextPreference.ProjectAndDependencies, CancellationToken.None),
            operation.WaitAsync(LspSolutionContextPreference.ProjectAndDependencies, CancellationToken.None));
        Assert.Equal(1, Volatile.Read(ref dependencyLoadCount));
    }

    [Fact]
    public async Task DependencyPreferenceDoesNotRetryFailedRootWithinOperation()
    {
        var workspace = _tempRoot.CreateDirectory();
        var project = workspace.CreateFile("App.csproj");
        var document = workspace.CreateFile("Program.cs");
        var loadCount = 0;
        using var loader = CreateLoader(
            workspace.Path,
            directory => [project.Path],
            projectPath =>
            {
                Assert.Equal(project.Path, projectPath);
                Interlocked.Increment(ref loadCount);
                return new LanguageServerProjectLoadResult(LanguageServerProjectLoadStatus.Failed, []);
            },
            getProjectReferences: _ => []);

        var operation = loader.StartLoading(ProtocolConversions.CreateAbsoluteDocumentUri(document.Path));
        await operation.WaitAsync(LspSolutionContextPreference.Project, CancellationToken.None);
        await operation.WaitAsync(LspSolutionContextPreference.ProjectAndDependencies, CancellationToken.None);

        Assert.Equal(1, Volatile.Read(ref loadCount));
    }

    [Fact]
    public async Task DependencyClosureHandlesTransitiveCyclesOverlapAndPartialFailure()
    {
        var workspace = _tempRoot.CreateDirectory();
        var firstProject = workspace.CreateFile("First.csproj");
        var secondProject = workspace.CreateFile("Second.csproj");
        var sharedDependency = workspace.CreateFile("Shared.csproj");
        var failedDependency = workspace.CreateFile("Failed.csproj");
        var document = workspace.CreateDirectory("src").CreateDirectory("nested").CreateFile("Program.cs");
        var projectIds = ImmutableDictionary.CreateRange(StringComparer.OrdinalIgnoreCase, new[]
        {
            KeyValuePair.Create(firstProject.Path, ProjectId.CreateNewId()),
            KeyValuePair.Create(secondProject.Path, ProjectId.CreateNewId()),
            KeyValuePair.Create(sharedDependency.Path, ProjectId.CreateNewId()),
            KeyValuePair.Create(failedDependency.Path, ProjectId.CreateNewId()),
        });
        var references = ImmutableDictionary<ProjectId, ImmutableArray<string>>.Empty
            .Add(projectIds[firstProject.Path], [sharedDependency.Path])
            .Add(projectIds[secondProject.Path], [sharedDependency.Path, failedDependency.Path])
            .Add(projectIds[sharedDependency.Path], [firstProject.Path]);
        var loadCounts = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        using var loader = CreateLoader(
            workspace.Path,
            directory => [firstProject.Path, secondProject.Path],
            projectPath =>
            {
                loadCounts.AddOrUpdate(projectPath, 1, static (_, count) => count + 1);
                return new LanguageServerProjectLoadResult(
                    StringComparer.OrdinalIgnoreCase.Equals(projectPath, failedDependency.Path)
                        ? LanguageServerProjectLoadStatus.Failed
                        : LanguageServerProjectLoadStatus.Loaded,
                    [projectIds[projectPath]]);
            },
            loadedProjectIds => loadedProjectIds.SelectManyAsArray(projectId => references.GetValueOrDefault(projectId, [])));

        var operation = loader.StartLoading(ProtocolConversions.CreateAbsoluteDocumentUri(document.Path));
        var result = await operation.WaitAsync(LspSolutionContextPreference.ProjectAndDependencies, CancellationToken.None);

        Assert.True(result.IsProjectLoaded(firstProject.Path));
        Assert.True(result.IsProjectLoaded(secondProject.Path));
        Assert.True(result.HasCompleteDependencies(firstProject.Path));
        Assert.False(result.HasCompleteDependencies(secondProject.Path));
        Assert.Equal(4, loadCounts.Count);
        Assert.Equal(1, loadCounts[firstProject.Path]);
        Assert.Equal(1, loadCounts[secondProject.Path]);
        Assert.Equal(1, loadCounts[sharedDependency.Path]);
        Assert.Equal(1, loadCounts[failedDependency.Path]);
    }

    [Fact]
    public async Task WorkspaceLoadWaitsForAllExplicitProjectLoads()
    {
        var workspace = _tempRoot.CreateDirectory();
        var firstHandle = new LanguageServerProjectLoadHandle();
        var secondHandle = new LanguageServerProjectLoadHandle();
        using var loader = CreateLoader(
            workspace.Path,
            directory => [],
            projectPath => throw new InvalidOperationException(),
            getProjectReferences: _ => [],
            getPendingProjectLoadHandles: () => [firstHandle, secondHandle]);

        var completion = loader.GetWorkspaceLoadOperation().WaitAsync(CancellationToken.None);
        firstHandle.Complete(new LanguageServerProjectLoadResult(LanguageServerProjectLoadStatus.Loaded, []));
        Assert.False(completion.IsCompleted);

        secondHandle.Complete(new LanguageServerProjectLoadResult(LanguageServerProjectLoadStatus.Loaded, []));
        await completion.WaitAsync(TestHelpers.HangMitigatingTimeout);
    }

    private static OnDemandProjectLoader CreateLoader(
        string workspaceFolder,
        Func<string, ImmutableArray<string>> enumerateFiles,
        Func<string, CancellationToken, Task> loadProjectAsync,
        bool isEnabled = true,
        bool isUsingDevKit = false)
    {
        var discoveryService = new WorkspaceProjectDiscoveryService(
            NullLoggerFactory.Instance,
            new TestFileChangeWatcher(),
            supportedProjectFileExtensions: ["csproj"],
            enumerateFiles);
        discoveryService.GetTestAccessor().Initialize([workspaceFolder]);
        return new OnDemandProjectLoader(
            discoveryService,
            projectPath => BeginLoadAsync(projectPath, loadProjectAsync),
            _ => [],
            static () => [],
            () => isEnabled,
            () => isUsingDevKit,
            AsynchronousOperationListenerProvider.NullListener,
            NullLoggerFactory.Instance);
    }

    private static OnDemandProjectLoader CreateLoader(
        string workspaceFolder,
        Func<string, ImmutableArray<string>> enumerateFiles,
        Func<string, LanguageServerProjectLoadResult> loadProject,
        Func<ImmutableArray<ProjectId>, ImmutableArray<string>> getProjectReferences,
        Func<ImmutableArray<LanguageServerProjectLoadHandle>>? getPendingProjectLoadHandles = null)
    {
        var discoveryService = new WorkspaceProjectDiscoveryService(
            NullLoggerFactory.Instance,
            new TestFileChangeWatcher(),
            supportedProjectFileExtensions: ["csproj"],
            enumerateFiles);
        discoveryService.GetTestAccessor().Initialize([workspaceFolder]);
        return new OnDemandProjectLoader(
            discoveryService,
            projectPath => Task.FromResult(CreateCompletedHandle(loadProject(projectPath))),
            getProjectReferences,
            getPendingProjectLoadHandles ?? (static () => []),
            () => true,
            () => false,
            AsynchronousOperationListenerProvider.NullListener,
            NullLoggerFactory.Instance);
    }

    private static Task<LanguageServerProjectLoadHandle> BeginLoadAsync(
        string projectPath, Func<string, CancellationToken, Task> loadProjectAsync)
    {
        var handle = new LanguageServerProjectLoadHandle();
        _ = CompleteAsync();
        return Task.FromResult(handle);

        async Task CompleteAsync()
        {
            await loadProjectAsync(projectPath, CancellationToken.None);
            handle.Complete(new LanguageServerProjectLoadResult(LanguageServerProjectLoadStatus.Loaded, []));
        }
    }

    private static LanguageServerProjectLoadHandle CreateCompletedHandle(LanguageServerProjectLoadResult result)
    {
        var handle = new LanguageServerProjectLoadHandle();
        handle.Complete(result);
        return handle;
    }

    private sealed class TestFileChangeWatcher : IFileChangeWatcher
    {
        public IFileChangeContext CreateContext(ImmutableArray<WatchedDirectory> watchedDirectories)
            => new TestFileChangeContext();
    }

    private sealed class TestFileChangeContext : IFileChangeContext
    {
        public event EventHandler<string>? FileChanged
        {
            add { }
            remove { }
        }

        public void Dispose()
        {
        }

        public IWatchedFile EnqueueWatchingFile(string filePath)
            => NoOpWatchedFile.Instance;
    }
}
