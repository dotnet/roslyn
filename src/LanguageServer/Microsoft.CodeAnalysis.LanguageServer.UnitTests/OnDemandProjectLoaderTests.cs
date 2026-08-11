// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.Extensions.Logging.Abstractions;
using Roslyn.Test.Utilities;

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
            async (projects, cancellationToken) =>
            {
                AssertEx.Equal([project.Path], projects);
                Interlocked.Increment(ref loadCount);
                loadStarted.SetResult();
                await loadCompletion.Task.WaitAsync(cancellationToken);
            });

        var uri = ProtocolConversions.CreateAbsoluteDocumentUri(document.Path);
        var firstOperation = loader.StartLoading(uri);
        var secondOperation = loader.StartLoading(uri);
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
            (projects, cancellationToken) => throw new InvalidOperationException(),
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
        ImmutableArray<string> loadedProjects = default;
        using var loader = CreateLoader(
            workspace.Path,
            directory => [secondProject.Path, firstProject.Path],
            (projects, cancellationToken) =>
            {
                loadedProjects = projects;
                return Task.CompletedTask;
            });

        var operation = loader.StartLoading(ProtocolConversions.CreateAbsoluteDocumentUri(document.Path));
        await operation.WaitAsync(CancellationToken.None).WaitAsync(TestHelpers.HangMitigatingTimeout);

        AssertEx.Equal([firstProject.Path, secondProject.Path], loadedProjects);
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
            (projects, cancellationToken) => throw new InvalidOperationException());
        var uri = ProtocolConversions.CreateAbsoluteDocumentUri(document.Path);

        await loader.StartLoading(uri).WaitAsync(CancellationToken.None);
        await loader.StartLoading(uri).WaitAsync(CancellationToken.None);

        Assert.Equal(2, Volatile.Read(ref enumerationCount));
    }

    private static OnDemandProjectLoader CreateLoader(
        string workspaceFolder,
        Func<string, ImmutableArray<string>> enumerateFiles,
        Func<ImmutableArray<string>, CancellationToken, Task> loadProjectsAsync,
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
            loadProjectsAsync,
            () => isEnabled,
            () => isUsingDevKit,
            AsynchronousOperationListenerProvider.NullListener,
            NullLoggerFactory.Instance);
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
