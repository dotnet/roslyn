// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.Extensions.Logging.Abstractions;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class WorkspaceProjectDiscoveryServiceTests : IDisposable
{
    private readonly TempRoot _tempRoot = new();

    public void Dispose()
        => _tempRoot.Dispose();

    [Fact]
    public void InitializationRecordsRootsWithoutEnumeration()
    {
        var workspace = _tempRoot.CreateDirectory();
        var enumerationCount = 0;
        var service = CreateDiscoveryService(
            enumerateFiles: _ =>
            {
                Interlocked.Increment(ref enumerationCount);
                return [];
            });

        service.GetTestAccessor().Initialize([workspace.Path]);

        Assert.Equal(0, enumerationCount);
        Assert.Equal(1, service.GetTestAccessor().WorkspaceFolderCount);
    }

    [Fact]
    public async Task NestedWorkspaceUsesDeepestBoundary()
    {
        var outerWorkspace = _tempRoot.CreateDirectory();
        var outerProject = outerWorkspace.CreateFile("Outer.csproj");
        var innerWorkspace = outerWorkspace.CreateDirectory("inner");
        var codeFile = innerWorkspace.CreateDirectory("src").CreateFile("Program.cs");
        var service = CreateDiscoveryService();
        service.GetTestAccessor().Initialize([outerWorkspace.Path, innerWorkspace.Path]);

        var candidates = await service.GetTestAccessor().GetCandidateProjectsAsync(codeFile.Path, CancellationToken.None);

        Assert.Empty(candidates);
        Assert.True(File.Exists(outerProject.Path));
    }

    [Fact]
    public async Task ReturnsAllSupportedProjectsFromNearestAncestorInOrdinalOrder()
    {
        var workspace = _tempRoot.CreateDirectory();
        workspace.CreateFile("Root.csproj");
        var sourceDirectory = workspace.CreateDirectory("src");
        var secondProject = sourceDirectory.CreateFile("B.csproj");
        sourceDirectory.CreateFile("Unsupported.vbproj");
        var firstProject = sourceDirectory.CreateFile("A.csproj");
        var codeFile = sourceDirectory.CreateDirectory("nested").CreateFile("Program.cs");
        var service = CreateDiscoveryService();
        service.GetTestAccessor().Initialize([workspace.Path]);

        var candidates = await service.GetTestAccessor().GetCandidateProjectsAsync(codeFile.Path, CancellationToken.None);

        AssertEx.Equal([firstProject.Path, secondProject.Path], candidates);
    }

    [Fact]
    public async Task FileOutsideWorkspaceReturnsNoCandidatesOrEnumeration()
    {
        var workspace = _tempRoot.CreateDirectory();
        var outsideDirectory = _tempRoot.CreateDirectory();
        var codeFile = outsideDirectory.CreateFile("Program.cs");
        var enumerationCount = 0;
        var service = CreateDiscoveryService(
            enumerateFiles: directory =>
            {
                Interlocked.Increment(ref enumerationCount);
                return EnumerateFiles(directory);
            });
        service.GetTestAccessor().Initialize([workspace.Path]);

        var candidates = await service.GetTestAccessor().GetCandidateProjectsAsync(codeFile.Path, CancellationToken.None);

        Assert.Empty(candidates);
        Assert.Equal(0, enumerationCount);
    }

    [Fact]
    public async Task EmptyDirectoryIsRecheckedOnLaterDemand()
    {
        var workspace = _tempRoot.CreateDirectory();
        var codeFile = workspace.CreateFile("Program.cs");
        var enumerationCount = 0;
        var service = CreateDiscoveryService(
            enumerateFiles: directory =>
            {
                Interlocked.Increment(ref enumerationCount);
                return EnumerateFiles(directory);
            });
        service.GetTestAccessor().Initialize([workspace.Path]);

        Assert.Empty(await service.GetTestAccessor().GetCandidateProjectsAsync(codeFile.Path, CancellationToken.None));
        Assert.Equal(0, service.GetTestAccessor().ProjectDirectoryCount);

        var project = workspace.CreateFile("Project.csproj");
        var candidates = await service.GetTestAccessor().GetCandidateProjectsAsync(codeFile.Path, CancellationToken.None);

        AssertEx.Equal([project.Path], candidates);
        Assert.Equal(2, enumerationCount);
    }

    [Fact]
    public async Task ConcurrentLookupsCoalesceDirectoryEnumeration()
    {
        var workspace = _tempRoot.CreateDirectory();
        var project = workspace.CreateFile("Project.csproj");
        var codeFile = workspace.CreateFile("Program.cs");
        using var enumerationStarted = new ManualResetEventSlim();
        using var releaseEnumeration = new ManualResetEventSlim();
        var enumerationCount = 0;
        var service = CreateDiscoveryService(
            enumerateFiles: directory =>
            {
                Interlocked.Increment(ref enumerationCount);
                enumerationStarted.Set();
                Assert.True(releaseEnumeration.Wait(TestHelpers.HangMitigatingTimeout));
                return EnumerateFiles(directory);
            });
        service.GetTestAccessor().Initialize([workspace.Path]);

        var firstLookup = Task.Run(async () => await service.GetTestAccessor().GetCandidateProjectsAsync(codeFile.Path, CancellationToken.None));
        Assert.True(enumerationStarted.Wait(TestHelpers.HangMitigatingTimeout));
        var secondLookup = Task.Run(async () => await service.GetTestAccessor().GetCandidateProjectsAsync(codeFile.Path, CancellationToken.None));
        releaseEnumeration.Set();

        var results = await Task.WhenAll(firstLookup, secondLookup).WaitAsync(TestHelpers.HangMitigatingTimeout);
        Assert.All(results, candidates => AssertEx.Equal([project.Path], candidates));
        Assert.Equal(1, enumerationCount);
    }

    [Fact]
    public async Task CancellationStopsWaitingWithoutCancelingSharedEnumeration()
    {
        var workspace = _tempRoot.CreateDirectory();
        var project = workspace.CreateFile("Project.csproj");
        var codeFile = workspace.CreateFile("Program.cs");
        using var enumerationStarted = new ManualResetEventSlim();
        using var releaseEnumeration = new ManualResetEventSlim();
        var service = CreateDiscoveryService(
            enumerateFiles: directory =>
            {
                enumerationStarted.Set();
                Assert.True(releaseEnumeration.Wait(TestHelpers.HangMitigatingTimeout));
                return EnumerateFiles(directory);
            });
        service.GetTestAccessor().Initialize([workspace.Path]);
        using var cancellationSource = new CancellationTokenSource();

        var canceledLookup = service.GetTestAccessor().GetCandidateProjectsAsync(codeFile.Path, cancellationSource.Token).AsTask();
        Assert.True(enumerationStarted.Wait(TestHelpers.HangMitigatingTimeout));
        var sharedLookup = service.GetTestAccessor().GetCandidateProjectsAsync(codeFile.Path, CancellationToken.None).AsTask();

        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await canceledLookup.WaitAsync(TestHelpers.HangMitigatingTimeout));
        Assert.False(sharedLookup.IsCompleted);

        releaseEnumeration.Set();
        AssertEx.Equal([project.Path], await sharedLookup.WaitAsync(TestHelpers.HangMitigatingTimeout));
    }

    [Fact]
    public async Task UnexpectedEnumerationFailureSettlesAllWaitersAndReleasesOwnership()
    {
        var workspace = _tempRoot.CreateDirectory();
        var project = workspace.CreateFile("Project.csproj");
        var codeFile = workspace.CreateFile("Program.cs");
        var watcher = new TestFileChangeWatcher();
        using var enumerationStarted = new ManualResetEventSlim();
        using var releaseEnumeration = new ManualResetEventSlim();
        var enumerationCount = 0;
        var service = CreateDiscoveryService(
            watcher,
            directory =>
            {
                if (Interlocked.Increment(ref enumerationCount) == 1)
                {
                    enumerationStarted.Set();
                    Assert.True(releaseEnumeration.Wait(TestHelpers.HangMitigatingTimeout));
                    throw new UnexpectedEnumerationException();
                }

                return EnumerateFiles(directory);
            });
        service.GetTestAccessor().Initialize([workspace.Path]);

        var firstLookup = service.GetTestAccessor().GetCandidateProjectsAsync(codeFile.Path, CancellationToken.None).AsTask();
        Assert.True(enumerationStarted.Wait(TestHelpers.HangMitigatingTimeout));
        var coalescedLookup = service.GetTestAccessor().GetCandidateProjectsAsync(codeFile.Path, CancellationToken.None).AsTask();
        releaseEnumeration.Set();

        await Assert.ThrowsAsync<UnexpectedEnumerationException>(async () => await firstLookup.WaitAsync(TestHelpers.HangMitigatingTimeout));
        await Assert.ThrowsAsync<UnexpectedEnumerationException>(async () => await coalescedLookup.WaitAsync(TestHelpers.HangMitigatingTimeout));
        Assert.Equal(1, watcher.Contexts.Single().DisposalCount);

        AssertEx.Equal([project.Path], await service.GetTestAccessor().GetCandidateProjectsAsync(codeFile.Path, CancellationToken.None));
        Assert.Equal(2, enumerationCount);
    }

    [Fact]
    public async Task SupersededEnumerationDoesNotRemoveReplacement()
    {
        var workspace = _tempRoot.CreateDirectory();
        var project = workspace.CreateFile("Project.csproj");
        var codeFile = workspace.CreateFile("Program.cs");
        using var firstEnumerationStarted = new ManualResetEventSlim();
        using var releaseFirstEnumeration = new ManualResetEventSlim();
        using var secondEnumerationStarted = new ManualResetEventSlim();
        using var releaseSecondEnumeration = new ManualResetEventSlim();
        var enumerationCount = 0;
        var service = CreateDiscoveryService(
            enumerateFiles: directory =>
            {
                var currentCount = Interlocked.Increment(ref enumerationCount);
                var enumerationStarted = currentCount == 1 ? firstEnumerationStarted : secondEnumerationStarted;
                var releaseEnumeration = currentCount == 1 ? releaseFirstEnumeration : releaseSecondEnumeration;
                enumerationStarted.Set();
                Assert.True(releaseEnumeration.Wait(TestHelpers.HangMitigatingTimeout));
                return EnumerateFiles(directory);
            });
        var accessor = service.GetTestAccessor();
        accessor.Initialize([workspace.Path]);

        var firstLookup = Task.Run(async () => await accessor.GetCandidateProjectsAsync(codeFile.Path, CancellationToken.None));
        Assert.True(firstEnumerationStarted.Wait(TestHelpers.HangMitigatingTimeout));

        accessor.RemoveWorkspaceFolder(workspace.Path);
        accessor.AddWorkspaceFolder(workspace.Path);

        var secondLookup = Task.Run(async () => await accessor.GetCandidateProjectsAsync(codeFile.Path, CancellationToken.None));
        Assert.True(secondEnumerationStarted.Wait(TestHelpers.HangMitigatingTimeout));

        releaseFirstEnumeration.Set();
        Assert.Empty(await firstLookup.WaitAsync(TestHelpers.HangMitigatingTimeout));

        releaseSecondEnumeration.Set();
        AssertEx.Equal([project.Path], await secondLookup.WaitAsync(TestHelpers.HangMitigatingTimeout));
        Assert.Equal(2, enumerationCount);
    }

    [Fact]
    public async Task EnumerationFailureContinuesToParent()
    {
        var workspace = _tempRoot.CreateDirectory();
        var project = workspace.CreateFile("Project.csproj");
        var childDirectory = workspace.CreateDirectory("src");
        var codeFile = childDirectory.CreateFile("Program.cs");
        var service = CreateDiscoveryService(
            enumerateFiles: directory =>
            {
                if (PathUtilities.Comparer.Equals(directory, childDirectory.Path))
                    throw new IOException("Expected test failure");

                return EnumerateFiles(directory);
            });
        service.GetTestAccessor().Initialize([workspace.Path]);

        var candidates = await service.GetTestAccessor().GetCandidateProjectsAsync(codeFile.Path, CancellationToken.None);

        AssertEx.Equal([project.Path], candidates);
    }

    [Fact]
    public async Task WorkspaceChangesUsePlatformPathSemanticsAndCleanCachedState()
    {
        var workspace = _tempRoot.CreateDirectory();
        var project = workspace.CreateFile("Project.csproj");
        var codeFile = workspace.CreateFile("Program.cs");
        var watcher = new TestFileChangeWatcher();
        var service = CreateDiscoveryService(watcher);
        var accessor = service.GetTestAccessor();
        accessor.Initialize([workspace.Path]);
        AssertEx.Equal([project.Path], await accessor.GetCandidateProjectsAsync(codeFile.Path, CancellationToken.None));

        var alternateCasePath = workspace.Path.ToUpperInvariant();
        accessor.AddWorkspaceFolder(alternateCasePath);
        Assert.Equal(PathUtilities.IsUnixLikePlatform ? 2 : 1, accessor.WorkspaceFolderCount);

        accessor.RemoveWorkspaceFolder(workspace.Path);

        Assert.Empty(await accessor.GetCandidateProjectsAsync(codeFile.Path, CancellationToken.None));
        Assert.Equal(0, accessor.ProjectDirectoryCount);
        Assert.True(watcher.Contexts.Single().IsDisposed);
    }

    [Fact]
    public async Task RemovingOuterWorkspacePreservesNestedWorkspaceCache()
    {
        var outerWorkspace = _tempRoot.CreateDirectory();
        var innerWorkspace = outerWorkspace.CreateDirectory("inner");
        var project = innerWorkspace.CreateFile("Project.csproj");
        var codeFile = innerWorkspace.CreateFile("Program.cs");
        var service = CreateDiscoveryService();
        var accessor = service.GetTestAccessor();
        accessor.Initialize([outerWorkspace.Path, innerWorkspace.Path]);
        AssertEx.Equal([project.Path], await accessor.GetCandidateProjectsAsync(codeFile.Path, CancellationToken.None));

        accessor.RemoveWorkspaceFolder(outerWorkspace.Path);

        AssertEx.Equal([project.Path], await accessor.GetCandidateProjectsAsync(codeFile.Path, CancellationToken.None));
        Assert.Equal(1, accessor.ProjectDirectoryCount);
    }

    [Fact]
    public async Task RemovingOuterWorkspacePreservesNestedWorkspaceEnumeration()
    {
        var outerWorkspace = _tempRoot.CreateDirectory();
        var innerWorkspace = outerWorkspace.CreateDirectory("inner");
        var project = innerWorkspace.CreateFile("Project.csproj");
        var codeFile = innerWorkspace.CreateFile("Program.cs");
        using var enumerationStarted = new ManualResetEventSlim();
        using var releaseEnumeration = new ManualResetEventSlim();
        var service = CreateDiscoveryService(
            enumerateFiles: directory =>
            {
                var files = EnumerateFiles(directory);
                enumerationStarted.Set();
                Assert.True(releaseEnumeration.Wait(TestHelpers.HangMitigatingTimeout));
                return files;
            });
        var accessor = service.GetTestAccessor();
        accessor.Initialize([outerWorkspace.Path, innerWorkspace.Path]);

        var lookup = Task.Run(async () => await accessor.GetCandidateProjectsAsync(codeFile.Path, CancellationToken.None));
        Assert.True(enumerationStarted.Wait(TestHelpers.HangMitigatingTimeout));
        accessor.RemoveWorkspaceFolder(outerWorkspace.Path);
        releaseEnumeration.Set();

        AssertEx.Equal([project.Path], await lookup.WaitAsync(TestHelpers.HangMitigatingTimeout));
    }

    [Fact]
    public async Task RemovingOwningWorkspaceSettlesCoalescedLookupBeforeEnumerationCompletes()
    {
        var workspace = _tempRoot.CreateDirectory();
        workspace.CreateFile("Project.csproj");
        var codeFile = workspace.CreateFile("Program.cs");
        var watcher = new TestFileChangeWatcher();
        using var enumerationStarted = new ManualResetEventSlim();
        using var releaseEnumeration = new ManualResetEventSlim();
        var service = CreateDiscoveryService(
            watcher,
            directory =>
            {
                enumerationStarted.Set();
                Assert.True(releaseEnumeration.Wait(TestHelpers.HangMitigatingTimeout));
                return EnumerateFiles(directory);
            });
        var accessor = service.GetTestAccessor();
        accessor.Initialize([workspace.Path]);

        var firstLookup = Task.Run(async () => await accessor.GetCandidateProjectsAsync(codeFile.Path, CancellationToken.None));
        Assert.True(enumerationStarted.Wait(TestHelpers.HangMitigatingTimeout));
        var coalescedLookup = accessor.GetCandidateProjectsAsync(codeFile.Path, CancellationToken.None).AsTask();
        Assert.False(coalescedLookup.IsCompleted);

        accessor.RemoveWorkspaceFolder(workspace.Path);

        Assert.Empty(await coalescedLookup.WaitAsync(TestHelpers.HangMitigatingTimeout));
        Assert.Equal(1, watcher.Contexts.Single().DisposalCount);

        releaseEnumeration.Set();
        Assert.Empty(await firstLookup.WaitAsync(TestHelpers.HangMitigatingTimeout));
        Assert.Equal(1, watcher.Contexts.Single().DisposalCount);
    }

    [Fact]
    public async Task RemovedWorkspaceDoesNotCreateDirectoryState()
    {
        var workspace = _tempRoot.CreateDirectory();
        workspace.CreateFile("Project.csproj");
        var watcher = new TestFileChangeWatcher();
        var service = CreateDiscoveryService(watcher);
        var accessor = service.GetTestAccessor();
        accessor.Initialize([workspace.Path]);
        accessor.RemoveWorkspaceFolder(workspace.Path);

        var projects = await accessor.GetProjectsInDirectoryAsync(workspace.Path, workspace.Path, CancellationToken.None);

        Assert.Empty(projects);
        Assert.Empty(watcher.Contexts);
    }

    [Fact]
    public async Task DisposedServiceDoesNotCreateDirectoryState()
    {
        var workspace = _tempRoot.CreateDirectory();
        workspace.CreateFile("Project.csproj");
        var watcher = new TestFileChangeWatcher();
        var service = CreateDiscoveryService(watcher);
        var accessor = service.GetTestAccessor();
        accessor.Initialize([workspace.Path]);
        service.Dispose();

        var projects = await accessor.GetProjectsInDirectoryAsync(workspace.Path, workspace.Path, CancellationToken.None);

        Assert.Empty(projects);
        Assert.Empty(watcher.Contexts);
    }

    [Fact]
    public async Task DisposeReleasesWatchersAndCancelsInFlightEnumeration()
    {
        var workspace = _tempRoot.CreateDirectory();
        var codeFile = workspace.CreateFile("Program.cs");
        var watcher = new TestFileChangeWatcher();
        using var enumerationStarted = new ManualResetEventSlim();
        using var releaseEnumeration = new ManualResetEventSlim();
        var service = CreateDiscoveryService(
            watcher,
            directory =>
            {
                enumerationStarted.Set();
                Assert.True(releaseEnumeration.Wait(TestHelpers.HangMitigatingTimeout));
                return EnumerateFiles(directory);
            });
        service.GetTestAccessor().Initialize([workspace.Path]);

        var lookup = Task.Run(async () => await service.GetTestAccessor().GetCandidateProjectsAsync(codeFile.Path, CancellationToken.None));
        Assert.True(enumerationStarted.Wait(TestHelpers.HangMitigatingTimeout));
        var coalescedLookup = service.GetTestAccessor().GetCandidateProjectsAsync(codeFile.Path, CancellationToken.None).AsTask();
        Assert.False(coalescedLookup.IsCompleted);
        service.Dispose();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await coalescedLookup.WaitAsync(TestHelpers.HangMitigatingTimeout));
        releaseEnumeration.Set();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await lookup.WaitAsync(TestHelpers.HangMitigatingTimeout));
        Assert.All(watcher.Contexts, context => Assert.Equal(1, context.DisposalCount));
    }

    [Fact]
    public async Task WatcherCreationAndDeletionUpdatePositiveCache()
    {
        var workspace = _tempRoot.CreateDirectory();
        var firstProject = workspace.CreateFile("First.csproj");
        var codeFile = workspace.CreateFile("Program.cs");
        var watcher = new TestFileChangeWatcher();
        var service = CreateDiscoveryService(watcher);
        var accessor = service.GetTestAccessor();
        accessor.Initialize([workspace.Path]);
        AssertEx.Equal([firstProject.Path], await accessor.GetCandidateProjectsAsync(codeFile.Path, CancellationToken.None));
        var watchedDirectory = Assert.Single(watcher.Contexts).WatchedDirectories.Single();
        Assert.Equal(workspace.Path + Path.DirectorySeparatorChar, watchedDirectory.Path);
        AssertEx.Equal([".csproj"], watchedDirectory.ExtensionFilters);

        var secondProject = workspace.CreateFile("Second.csproj");
        watcher.Notify(secondProject.Path);
        AssertEx.Equal([firstProject.Path, secondProject.Path], await accessor.GetCandidateProjectsAsync(codeFile.Path, CancellationToken.None));

        File.Delete(firstProject.Path);
        watcher.Notify(firstProject.Path);
        AssertEx.Equal([secondProject.Path], await accessor.GetCandidateProjectsAsync(codeFile.Path, CancellationToken.None));
    }

    [Fact]
    public async Task WatcherEventRacingEnumerationIsMergedAndValidated()
    {
        var workspace = _tempRoot.CreateDirectory();
        var firstProject = workspace.CreateFile("First.csproj");
        var codeFile = workspace.CreateFile("Program.cs");
        var watcher = new TestFileChangeWatcher();
        using var enumerationStarted = new ManualResetEventSlim();
        using var releaseEnumeration = new ManualResetEventSlim();
        var service = CreateDiscoveryService(
            watcher,
            directory =>
            {
                var files = EnumerateFiles(directory);
                enumerationStarted.Set();
                Assert.True(releaseEnumeration.Wait(TestHelpers.HangMitigatingTimeout));
                return files;
            });
        service.GetTestAccessor().Initialize([workspace.Path]);

        var lookup = Task.Run(async () => await service.GetTestAccessor().GetCandidateProjectsAsync(codeFile.Path, CancellationToken.None));
        Assert.True(enumerationStarted.Wait(TestHelpers.HangMitigatingTimeout));
        var secondProject = workspace.CreateFile("Second.csproj");
        watcher.Notify(secondProject.Path);
        File.Delete(firstProject.Path);
        watcher.Notify(firstProject.Path);
        releaseEnumeration.Set();

        var candidates = await lookup.WaitAsync(TestHelpers.HangMitigatingTimeout);
        AssertEx.Equal([secondProject.Path], candidates);
    }

    private static WorkspaceProjectDiscoveryService CreateDiscoveryService(
        TestFileChangeWatcher? watcher = null,
        Func<string, ImmutableArray<string>>? enumerateFiles = null)
        => new(
            NullLoggerFactory.Instance,
            watcher ?? new TestFileChangeWatcher(),
            supportedProjectFileExtensions: ["csproj"],
            enumerateFiles);

    private static ImmutableArray<string> EnumerateFiles(string directory)
        => [.. Directory.EnumerateFiles(directory, searchPattern: "*", SearchOption.TopDirectoryOnly)];

    private sealed class TestFileChangeWatcher : IFileChangeWatcher
    {
        public List<TestFileChangeContext> Contexts { get; } = [];

        public IFileChangeContext CreateContext(ImmutableArray<WatchedDirectory> watchedDirectories)
        {
            var context = new TestFileChangeContext(watchedDirectories);
            Contexts.Add(context);
            return context;
        }

        public void Notify(string filePath)
        {
            foreach (var context in Contexts.ToArray())
                context.Notify(filePath);
        }
    }

    private sealed class TestFileChangeContext(ImmutableArray<WatchedDirectory> watchedDirectories) : IFileChangeContext
    {
        private int _disposalCount;

        public event EventHandler<string>? FileChanged;
        public ImmutableArray<WatchedDirectory> WatchedDirectories { get; } = watchedDirectories;
        public bool IsDisposed => DisposalCount > 0;
        public int DisposalCount => Volatile.Read(ref _disposalCount);

        public IWatchedFile EnqueueWatchingFile(string filePath)
            => NoOpWatchedFile.Instance;

        public void Notify(string filePath)
        {
            if (!IsDisposed)
                FileChanged?.Invoke(this, filePath);
        }

        public void Dispose()
            => Interlocked.Increment(ref _disposalCount);
    }

    private sealed class UnexpectedEnumerationException : Exception;
}
