// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.FileWatching;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class SimpleFileChangeWatcherTests : IDisposable
{
    private readonly TempRoot _tempRoot = new();

    public void Dispose() => _tempRoot.Dispose();

    [Fact]
    public void CreateContext_WithExistingDirectory_CreatesSharedWatcher()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var watcher = new SimpleFileChangeWatcher();

        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);

        Assert.Equal(1, SimpleFileChangeWatcher.TestAccessor.GetSharedDirectoryWatcherCount(watcher));
    }

    [Fact]
    public void CreateContext_WithNonExistingDirectory_DoesNotCreateSharedWatcher()
    {
        var nonExistentPath = Path.Combine(_tempRoot.CreateDirectory().Path, "non-existent");
        var watcher = new SimpleFileChangeWatcher();

        using var context = watcher.CreateContext([new WatchedDirectory(nonExistentPath, extensionFilters: [])]);

        Assert.Equal(0, SimpleFileChangeWatcher.TestAccessor.GetSharedDirectoryWatcherCount(watcher));
    }

    [Fact]
    public void CreateMultipleContexts_SameDirectory_SharesWatcher()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var watcher = new SimpleFileChangeWatcher();

        using var context1 = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);
        using var context2 = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [".cs"])]);

        // Both contexts should share the same directory watcher
        Assert.Equal(1, SimpleFileChangeWatcher.TestAccessor.GetSharedDirectoryWatcherCount(watcher));
    }

    [Fact]
    public void CreateMultipleContexts_DifferentDirectories_CreatesSeparateWatchers()
    {
        var tempDirectory1 = _tempRoot.CreateDirectory();
        var tempDirectory2 = _tempRoot.CreateDirectory();
        var watcher = new SimpleFileChangeWatcher();

        using var context1 = watcher.CreateContext([new WatchedDirectory(tempDirectory1.Path, extensionFilters: [])]);
        using var context2 = watcher.CreateContext([new WatchedDirectory(tempDirectory2.Path, extensionFilters: [])]);

        Assert.Equal(2, SimpleFileChangeWatcher.TestAccessor.GetSharedDirectoryWatcherCount(watcher));
    }

    [Fact]
    public void DisposeContext_ReleasesSharedWatcher()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var watcher = new SimpleFileChangeWatcher();

        var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);
        Assert.Equal(1, SimpleFileChangeWatcher.TestAccessor.GetSharedDirectoryWatcherCount(watcher));

        context.Dispose();
        Assert.Equal(0, SimpleFileChangeWatcher.TestAccessor.GetSharedDirectoryWatcherCount(watcher));
    }

    [Fact]
    public void DisposeOneContext_WithTwoContextsSharingWatcher_KeepsWatcher()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var watcher = new SimpleFileChangeWatcher();

        var context1 = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);
        var context2 = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);
        Assert.Equal(1, SimpleFileChangeWatcher.TestAccessor.GetSharedDirectoryWatcherCount(watcher));

        context1.Dispose();
        Assert.Equal(1, SimpleFileChangeWatcher.TestAccessor.GetSharedDirectoryWatcherCount(watcher));

        context2.Dispose();
        Assert.Equal(0, SimpleFileChangeWatcher.TestAccessor.GetSharedDirectoryWatcherCount(watcher));
    }

    [Fact]
    public void EnqueueWatchingFile_InWatchedDirectory_ReturnsNoOpWatchedFile()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var tempFile = tempDirectory.CreateFile("test.cs");
        var watcher = new SimpleFileChangeWatcher();

        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [".cs"])]);
        var watchedFile = context.EnqueueWatchingFile(tempFile.Path);

        Assert.IsType<NoOpWatchedFile>(watchedFile);
    }

    [Fact]
    public void EnqueueWatchingFile_OutsideWatchedDirectory_CreatesSharedWatcherForFileDirectory()
    {
        var watchedDirectory = _tempRoot.CreateDirectory();
        var otherDirectory = _tempRoot.CreateDirectory();
        var tempFile = otherDirectory.CreateFile("test.cs");
        var watcher = new SimpleFileChangeWatcher();

        using var context = watcher.CreateContext([new WatchedDirectory(watchedDirectory.Path, extensionFilters: [])]);
        Assert.Equal(1, SimpleFileChangeWatcher.TestAccessor.GetSharedDirectoryWatcherCount(watcher));

        using var watchedFile = context.EnqueueWatchingFile(tempFile.Path);

        // Should create a shared watcher for the file's parent directory
        Assert.Equal(2, SimpleFileChangeWatcher.TestAccessor.GetSharedDirectoryWatcherCount(watcher));
    }

    [Fact]
    public void EnqueueWatchingFile_MultipleFilesInSameDirectory_SharesWatcher()
    {
        var watchedDirectory = _tempRoot.CreateDirectory();
        var otherDirectory = _tempRoot.CreateDirectory();
        var tempFile1 = otherDirectory.CreateFile("test1.cs");
        var tempFile2 = otherDirectory.CreateFile("test2.cs");
        var watcher = new SimpleFileChangeWatcher();

        using var context = watcher.CreateContext([new WatchedDirectory(watchedDirectory.Path, extensionFilters: [])]);
        Assert.Equal(1, SimpleFileChangeWatcher.TestAccessor.GetSharedDirectoryWatcherCount(watcher));

        using var watchedFile1 = context.EnqueueWatchingFile(tempFile1.Path);
        using var watchedFile2 = context.EnqueueWatchingFile(tempFile2.Path);

        // Both individual files should share the same directory watcher
        Assert.Equal(2, SimpleFileChangeWatcher.TestAccessor.GetSharedDirectoryWatcherCount(watcher));
    }

    [Fact]
    public void FileChangeContext_GetSharedWatcherCount_ReturnsCorrectCount()
    {
        var tempDirectory1 = _tempRoot.CreateDirectory();
        var tempDirectory2 = _tempRoot.CreateDirectory();
        var watcher = new SimpleFileChangeWatcher();

        using var context = watcher.CreateContext([
            new WatchedDirectory(tempDirectory1.Path, extensionFilters: []),
            new WatchedDirectory(tempDirectory2.Path, extensionFilters: [])
        ]);

        Assert.Equal(2, SimpleFileChangeWatcher.FileChangeContext.TestAccessor.GetSharedWatcherCount((SimpleFileChangeWatcher.FileChangeContext)context));
    }
}
