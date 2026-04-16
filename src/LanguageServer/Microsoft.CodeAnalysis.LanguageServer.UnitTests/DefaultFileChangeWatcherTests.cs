// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.FileWatching;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class SimpleFileChangeWatcherTests : IDisposable
{
    private readonly TimeSpan _fileChangeTimeout = TimeSpan.FromSeconds(1);
    private readonly TempRoot _tempRoot = new();

    public void Dispose() => _tempRoot.Dispose();

    [Fact]
    public void CreateContext_WithEmptyDirectories_DoesNotAddWatchers()
    {
        var watcher = new DefaultFileChangeWatcher();

        using var context = watcher.CreateContext([]);

        Assert.Empty(DefaultFileChangeWatcher.TestAccessor.GetWatchedDirectoryPaths(watcher));
        Assert.Equal(0, DefaultFileChangeWatcher.FileChangeContext.TestAccessor.GetWatchedDirectoryCount((DefaultFileChangeWatcher.FileChangeContext)context));
    }

    [Fact]
    public void CreateContext_WithExistingDirectory_AddsDirectoryWatcher()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var watcher = new DefaultFileChangeWatcher();

        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);

        Assert.Single(DefaultFileChangeWatcher.TestAccessor.GetWatchedDirectoryPaths(watcher));
        Assert.Equal(1, DefaultFileChangeWatcher.FileChangeContext.TestAccessor.GetWatchedDirectoryCount((DefaultFileChangeWatcher.FileChangeContext)context));
    }

    [Fact]
    public void CreateContext_WithContainedDirectory_ConsolidatesImmediately()
    {
        var root = _tempRoot.CreateDirectory();
        var child = root.CreateDirectory("child");
        var watcher = new DefaultFileChangeWatcher();

        using var context = watcher.CreateContext([
            new WatchedDirectory(child.Path, extensionFilters: []),
            new WatchedDirectory(root.Path, extensionFilters: [])
        ]);

        var fileChangeContext = (DefaultFileChangeWatcher.FileChangeContext)context;
        Assert.Equal(1, DefaultFileChangeWatcher.FileChangeContext.TestAccessor.GetWatchedDirectoryCount(fileChangeContext));
        Assert.Single(DefaultFileChangeWatcher.FileChangeContext.TestAccessor.GetWatchedDirectories(fileChangeContext));
    }

    [Fact]
    public void EnqueueWatchingFile_WatchesParentDirectory()
    {
        var directory = _tempRoot.CreateDirectory();
        var filePath = Path.Combine(directory.Path, "test.cs");
        var watcher = new DefaultFileChangeWatcher();

        using var context = watcher.CreateContext([]);
        using var watchedFile = context.EnqueueWatchingFile(filePath);

        Assert.Same(NoOpWatchedFile.Instance, watchedFile);
        Assert.Single(DefaultFileChangeWatcher.TestAccessor.GetWatchedDirectoryPaths(watcher));
    }

    [Fact]
    public void EnqueueWatchingFile_MultipleFilesSameDirectory_UsesSingleWatcher()
    {
        var directory = _tempRoot.CreateDirectory();
        var watcher = new DefaultFileChangeWatcher();
        using var context = watcher.CreateContext([]);

        using var watchedFile1 = context.EnqueueWatchingFile(Path.Combine(directory.Path, "a.cs"));
        using var watchedFile2 = context.EnqueueWatchingFile(Path.Combine(directory.Path, "b.cs"));

        Assert.Same(NoOpWatchedFile.Instance, watchedFile1);
        Assert.Same(NoOpWatchedFile.Instance, watchedFile2);
        Assert.Single(DefaultFileChangeWatcher.TestAccessor.GetWatchedDirectoryPaths(watcher));
    }

    [Fact]
    public void Consolidate_MergesPairWithMostCommonPath()
    {
        var root = _tempRoot.CreateDirectory();
        var pairBase = root.CreateDirectory("pair");
        var pairA = pairBase.CreateDirectory("a");
        var pairB = pairBase.CreateDirectory("b");
        var other = root.CreateDirectory("other").CreateDirectory("x");

        var watcher = new DefaultFileChangeWatcher();
        using var context = watcher.CreateContext([]);
        var fileChangeContext = (DefaultFileChangeWatcher.FileChangeContext)context;

        for (var i = 0; i < DefaultFileChangeWatcher.MaximumWatcherCount - 2; i++)
        {
            var seed = root.CreateDirectory($"seed{i}");
            context.EnqueueWatchingFile(Path.Combine(seed.Path, "seed.cs"));
        }

        context.EnqueueWatchingFile(Path.Combine(pairA.Path, "one.cs"));
        context.EnqueueWatchingFile(Path.Combine(pairB.Path, "two.cs"));
        context.EnqueueWatchingFile(Path.Combine(other.Path, "three.cs"));

        Assert.True(DefaultFileChangeWatcher.FileChangeContext.TestAccessor.GetWatchedDirectoryCount(fileChangeContext) <= DefaultFileChangeWatcher.MaximumWatcherCount);
        Assert.Contains(DefaultFileChangeWatcher.FileChangeContext.TestAccessor.GetWatchedDirectories(fileChangeContext), p => p.StartsWith(pairBase.Path, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task FileCreated_InWatchedParentDirectory_RaisesFileChangedEvent()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "created.cs");
        var watcher = new DefaultFileChangeWatcher();

        using var context = watcher.CreateContext([]);
        var fileChangeContext = (DefaultFileChangeWatcher.FileChangeContext)context;
        var fileChangeTask = ListenForFileChangeAsync(fileChangeContext, filePath);

        context.EnqueueWatchingFile(filePath);
        File.WriteAllText(filePath, "initial content");

        var eventFired = await WaitForFileChangeAsync(fileChangeTask, _fileChangeTimeout);
        Assert.True(eventFired, "FileChanged event should fire when a file is created");
    }

    [Fact]
    public async Task FileRenamed_InWatchedParentDirectory_FiresForBothPaths()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var originalPath = Path.Combine(tempDirectory.Path, "original.cs");
        var renamedPath = Path.Combine(tempDirectory.Path, "renamed.cs");
        var watcher = new DefaultFileChangeWatcher();

        File.WriteAllText(originalPath, "content");

        using var context = watcher.CreateContext([]);
        var fileChangeContext = (DefaultFileChangeWatcher.FileChangeContext)context;
        context.EnqueueWatchingFile(originalPath);

        var originalPathTask = ListenForFileChangeAsync(fileChangeContext, originalPath);
        var renamedPathTask = ListenForFileChangeAsync(fileChangeContext, renamedPath);

        File.Move(originalPath, renamedPath);

        Assert.True(await WaitForFileChangeAsync(originalPathTask, _fileChangeTimeout));
        Assert.True(await WaitForFileChangeAsync(renamedPathTask, _fileChangeTimeout));
    }

    private static async Task<bool> WaitForFileChangeAsync(Task fileChangeTask, TimeSpan timeout)
    {
        var completed = await Task.WhenAny(fileChangeTask, Task.Delay(timeout));
        return completed == fileChangeTask;
    }

    private static Task ListenForFileChangeAsync(DefaultFileChangeWatcher.FileChangeContext context, string filePath)
    {
        var eventSource = new TaskCompletionSource();

        context.FileChanged += (sender, path) =>
        {
            if (path == filePath)
                eventSource.TrySetResult();
        };

        return eventSource.Task;
    }
}
