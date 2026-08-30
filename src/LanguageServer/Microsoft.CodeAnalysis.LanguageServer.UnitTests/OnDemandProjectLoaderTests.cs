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
    public async Task RepeatedTriggersShareOnlyInFlightProjectLoading()
    {
        var workspace = _tempRoot.CreateDirectory();
        var project = workspace.CreateFile("App.csproj");
        var document = workspace.CreateFile("Program.cs");
        var loadStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var loadCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var loadCount = 0;
        using var loader = CreateLoader(
            directory => [project.Path],
            async (projectPath, cancellationToken) =>
            {
                Assert.Equal(project.Path, projectPath);
                Interlocked.Increment(ref loadCount);
                loadStarted.TrySetResult();
                await loadCompletion.Task.WaitAsync(cancellationToken);
            });

        var uri = ProtocolConversions.CreateAbsoluteDocumentUri(document.Path);
        var firstOperation = loader.StartLoading(uri, [workspace.Path]);
        var secondOperation = loader.StartLoading(ProtocolConversions.CreateAbsoluteDocumentUri(document.Path.ToUpperInvariant()), [workspace.Path]);
        await loadStarted.Task.WaitAsync(TestHelpers.HangMitigatingTimeout);

        using var requestCancellationSource = new CancellationTokenSource();
        var canceledWait = firstOperation.WaitAsync(requestCancellationSource.Token);
        requestCancellationSource.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceledWait);

        loadCompletion.SetResult();
        await secondOperation.WaitAsync(CancellationToken.None).WaitAsync(TestHelpers.HangMitigatingTimeout);
        Assert.Equal(1, Volatile.Read(ref loadCount));

        await loader.StartLoading(uri, [workspace.Path]).WaitAsync(CancellationToken.None).WaitAsync(TestHelpers.HangMitigatingTimeout);
        Assert.Equal(2, Volatile.Read(ref loadCount));
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
            directory =>
            {
                Interlocked.Increment(ref enumerationCount);
                return [];
            },
            (projectPath, cancellationToken) => throw new InvalidOperationException(),
            isEnabled,
            isUsingDevKit);

        var operation = loader.StartLoading(ProtocolConversions.CreateAbsoluteDocumentUri(document.Path), [workspace.Path]);
        await operation.WaitAsync(CancellationToken.None);

        Assert.Equal(0, Volatile.Read(ref enumerationCount));
    }

    [Fact]
    public async Task DocumentInHostWorkspaceDoesNotDiscover()
    {
        var workspace = _tempRoot.CreateDirectory();
        var document = workspace.CreateFile("Program.cs");
        var enumerationCount = 0;
        using var loader = CreateLoader(
            directory =>
            {
                Interlocked.Increment(ref enumerationCount);
                return [];
            },
            (projectPath, cancellationToken) => throw new InvalidOperationException(),
            isDocumentInHostWorkspace: filePath => StringComparer.OrdinalIgnoreCase.Equals(filePath, document.Path));

        var operation = loader.StartLoading(ProtocolConversions.CreateAbsoluteDocumentUri(document.Path), [workspace.Path]);
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
            directory => [secondProject.Path, firstProject.Path],
            (projectPath, cancellationToken) =>
            {
                loadedProjects.Add(projectPath);
                return Task.CompletedTask;
            });

        var operation = loader.StartLoading(ProtocolConversions.CreateAbsoluteDocumentUri(document.Path), [workspace.Path]);
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
            directory =>
            {
                Interlocked.Increment(ref enumerationCount);
                return [];
            },
            (projectPath, cancellationToken) => throw new InvalidOperationException());
        var uri = ProtocolConversions.CreateAbsoluteDocumentUri(document.Path);

        await loader.StartLoading(uri, [workspace.Path]).WaitAsync(CancellationToken.None);
        await loader.StartLoading(uri, [workspace.Path]).WaitAsync(CancellationToken.None);

        Assert.Equal(2, Volatile.Read(ref enumerationCount));
    }

    [Fact]
    public async Task DependencyClosureIsLoadedAndShared()
    {
        var workspace = _tempRoot.CreateDirectory();
        var project = workspace.CreateFile("App.csproj");
        var dependency = workspace.CreateFile("Dependency.csproj");
        var document = workspace.CreateFile("Program.cs");
        var dependencyLoadCount = 0;
        using var loader = CreateLoader(
            directory => [project.Path],
            projectPath =>
            {
                if (StringComparer.OrdinalIgnoreCase.Equals(projectPath, dependency.Path))
                    Interlocked.Increment(ref dependencyLoadCount);

                return true;
            },
            projectPath => StringComparer.OrdinalIgnoreCase.Equals(projectPath, project.Path) ? [dependency.Path] : []);

        var operation = loader.StartLoading(ProtocolConversions.CreateAbsoluteDocumentUri(document.Path), [workspace.Path]);
        await Task.WhenAll(
            operation.WaitAsync(CancellationToken.None),
            operation.WaitAsync(CancellationToken.None));
        Assert.Equal(1, Volatile.Read(ref dependencyLoadCount));
    }

    [Fact]
    public async Task FailedRootIsNotRetriedWithinOperation()
    {
        var workspace = _tempRoot.CreateDirectory();
        var project = workspace.CreateFile("App.csproj");
        var document = workspace.CreateFile("Program.cs");
        var loadCount = 0;
        using var loader = CreateLoader(
            directory => [project.Path],
            projectPath =>
            {
                Assert.Equal(project.Path, projectPath);
                Interlocked.Increment(ref loadCount);
                return false;
            },
            getProjectReferences: _ => []);

        var operation = loader.StartLoading(ProtocolConversions.CreateAbsoluteDocumentUri(document.Path), [workspace.Path]);
        await operation.WaitAsync(CancellationToken.None);
        await operation.WaitAsync(CancellationToken.None);

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
        var references = ImmutableDictionary.Create<string, ImmutableArray<string>>(PathUtilities.Comparer)
            .Add(firstProject.Path, [sharedDependency.Path])
            .Add(secondProject.Path, [sharedDependency.Path, failedDependency.Path])
            .Add(sharedDependency.Path, [firstProject.Path]);
        var loadCounts = new ConcurrentDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        using var loader = CreateLoader(
            directory => [firstProject.Path, secondProject.Path],
            projectPath =>
            {
                loadCounts.AddOrUpdate(projectPath, 1, static (_, count) => count + 1);
                return !StringComparer.OrdinalIgnoreCase.Equals(projectPath, failedDependency.Path);
            },
            projectPath => references.GetValueOrDefault(projectPath, []));

        var operation = loader.StartLoading(ProtocolConversions.CreateAbsoluteDocumentUri(document.Path), [workspace.Path]);
        await operation.WaitAsync(CancellationToken.None);

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
        var firstCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var loader = CreateLoader(
            directory => [],
            projectPath => throw new InvalidOperationException(),
            getProjectReferences: _ => [],
            waitForActiveProjectLoadsAsync: cancellationToken => Task.WhenAll([
                firstCompletion.Task.WaitAsync(cancellationToken),
                secondCompletion.Task.WaitAsync(cancellationToken)]));

        var completion = loader.GetWorkspaceLoadOperation().WaitAsync(CancellationToken.None);
        firstCompletion.SetResult();
        Assert.False(completion.IsCompleted);

        secondCompletion.SetResult();
        await completion.WaitAsync(TestHelpers.HangMitigatingTimeout);
    }

    private static OnDemandProjectLoader CreateLoader(
        Func<string, ImmutableArray<string>> enumerateFiles,
        Func<string, CancellationToken, Task> loadProjectAsync,
        bool isEnabled = true,
        bool isUsingDevKit = false,
        Func<string, bool>? isDocumentInHostWorkspace = null)
    {
        var discoveryService = new WorkspaceProjectDiscoveryService(
            NullLoggerFactory.Instance,
            supportedProjectFileExtensions: ["csproj"],
            enumerateFiles: enumerateFiles);
        return new OnDemandProjectLoader(
            discoveryService,
            projectPath => BeginLoadAsync(projectPath, loadProjectAsync),
            static async (project, cancellationToken) =>
            {
                await project.WaitForLoadAsync(cancellationToken);
                return true;
            },
            static _ => Task.FromResult(ImmutableArray<string>.Empty),
            static _ => Task.CompletedTask,
            isDocumentInHostWorkspace ?? (static _ => false),
            () => isEnabled,
            () => isUsingDevKit,
            AsynchronousOperationListenerProvider.NullListener,
            NullLoggerFactory.Instance);
    }

    private static OnDemandProjectLoader CreateLoader(
        Func<string, ImmutableArray<string>> enumerateFiles,
        Func<string, bool> loadProject,
        Func<string, ImmutableArray<string>> getProjectReferences,
        Func<CancellationToken, Task>? waitForActiveProjectLoadsAsync = null)
    {
        var loadedProjects = new ConcurrentDictionary<string, Task<LoadedProject>>(PathUtilities.Comparer);
        var loadResults = new ConcurrentDictionary<string, bool>(PathUtilities.Comparer);
        var discoveryService = new WorkspaceProjectDiscoveryService(
            NullLoggerFactory.Instance,
            supportedProjectFileExtensions: ["csproj"],
            enumerateFiles: enumerateFiles);
        return new OnDemandProjectLoader(
            discoveryService,
            projectPath => loadedProjects.GetOrAdd(projectPath, path =>
            {
                loadResults[path] = loadProject(path);
                return BeginLoadAsync(path, static (_, _) => Task.CompletedTask);
            }),
            async (project, cancellationToken) =>
            {
                await project.WaitForLoadAsync(cancellationToken);
                return loadResults[project.ProjectFilePath];
            },
            project => Task.FromResult(getProjectReferences(project.ProjectFilePath)),
            waitForActiveProjectLoadsAsync ?? (static _ => Task.CompletedTask),
            static _ => false,
            () => true,
            () => false,
            AsynchronousOperationListenerProvider.NullListener,
            NullLoggerFactory.Instance);
    }

    private static async Task<LoadedProject> BeginLoadAsync(
        string projectPath,
        Func<string, CancellationToken, Task> loadProjectAsync)
    {
        var loadedProject = new LoadedProject(projectPath, NoOpFileChangeWatcher.Instance);
        Assert.True(await loadedProject.TryBeginLoadAsync());
        _ = CompleteAsync();
        return loadedProject;

        async Task CompleteAsync()
        {
            await loadProjectAsync(projectPath, CancellationToken.None);
            loadedProject.CompleteInitialLoad();
        }
    }

    private sealed class NoOpFileChangeWatcher : IFileChangeWatcher
    {
        public static readonly NoOpFileChangeWatcher Instance = new();

        public IFileChangeContext CreateContext(ImmutableArray<WatchedDirectory> watchedDirectories)
            => new NoOpFileChangeContext();
    }

    private sealed class NoOpFileChangeContext : IFileChangeContext
    {
        public event EventHandler<FileChangedEventArgs> FileChanged
        {
            add { }
            remove { }
        }

        public IWatchedFile EnqueueWatchingFile(string filePath)
            => NoOpWatchedFile.Instance;

        public void Dispose()
        {
        }
    }

}
