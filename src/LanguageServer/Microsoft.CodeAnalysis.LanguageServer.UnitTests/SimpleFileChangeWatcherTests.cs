// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.FileWatching;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class SimpleFileChangeWatcherTests : IDisposable
{
    private const int FileChangeTimeout = 10;
    private readonly TempRoot _tempRoot = new();

    public void Dispose() => _tempRoot.Dispose();

    [Fact]
    public void CreateContext_WithEmptyDirectories_DoesNotAddRootWatcher()
    {
        var watcher = new SimpleFileChangeWatcher();

        using var context = watcher.CreateContext([]);

        Assert.NotNull(context);
        Assert.Equal(0, SimpleFileChangeWatcher.FileChangeContext.TestAccessor.GetRootWatcherCount((SimpleFileChangeWatcher.FileChangeContext)context));
    }

    [Fact]
    public void CreateContext_WithExistingDirectory_AddsRootWatcher()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var watcher = new SimpleFileChangeWatcher();

        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);

        Assert.NotNull(context);
        Assert.Equal(1, SimpleFileChangeWatcher.FileChangeContext.TestAccessor.GetRootWatcherCount((SimpleFileChangeWatcher.FileChangeContext)context));
    }

    [Fact]
    public void CreateContext_WithNonExistentDirectory_DoesNotAddRootWatcher()
    {
        var nonExistentPath = Path.Combine(TempRoot.Root, "NonExistent", "Directory");
        var watcher = new SimpleFileChangeWatcher();

        using var context = watcher.CreateContext([new WatchedDirectory(nonExistentPath, extensionFilters: [])]);

        Assert.NotNull(context);
        Assert.Equal(0, SimpleFileChangeWatcher.FileChangeContext.TestAccessor.GetRootWatcherCount((SimpleFileChangeWatcher.FileChangeContext)context));
    }

    [Fact]
    public void CreateContext_WithMultipleDirectories_OnSameRoot_CreatesOneRootWatcher()
    {
        var rootDir = _tempRoot.CreateDirectory();
        var subDir1 = rootDir.CreateDirectory("sub1");
        var subDir2 = rootDir.CreateDirectory("sub2");
        var watcher = new SimpleFileChangeWatcher();

        // Both directories are under the same root
        using var context = watcher.CreateContext([
            new WatchedDirectory(subDir1.Path, extensionFilters: []),
            new WatchedDirectory(subDir2.Path, extensionFilters: [])
        ]);

        Assert.NotNull(context);
        Assert.Equal(1, SimpleFileChangeWatcher.FileChangeContext.TestAccessor.GetRootWatcherCount((SimpleFileChangeWatcher.FileChangeContext)context));
    }

    [Fact]
    public void EnqueueWatchingFile_InWatchedDirectory_ReturnsNoOpWatcher()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "test.cs");
        var watcher = new SimpleFileChangeWatcher();

        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [".cs"])]);
        var watchedFile = context.EnqueueWatchingFile(filePath);

        // When a file is already covered by a directory watch, it returns NoOpWatchedFile
        Assert.Same(NoOpWatchedFile.Instance, watchedFile);
    }

    [Fact]
    public void EnqueueWatchingFile_OutsideWatchedDirectory_ReturnsIndividualWatcher()
    {
        var watchedDir = _tempRoot.CreateDirectory();
        var otherDir = _tempRoot.CreateDirectory();
        var filePath = Path.Combine(otherDir.Path, "test.cs");
        var watcher = new SimpleFileChangeWatcher();

        using var context = watcher.CreateContext([new WatchedDirectory(watchedDir.Path, extensionFilters: [])]);
        var watchedFile = context.EnqueueWatchingFile(filePath);

        // When a file is not covered, it returns an actual watcher
        Assert.NotSame(NoOpWatchedFile.Instance, watchedFile);
        watchedFile.Dispose();
    }

    [Fact]
    public void EnqueueWatchingFile_WithExtensionFilter_MatchingExtension_ReturnsNoOpWatcher()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "test.cs");
        var watcher = new SimpleFileChangeWatcher();

        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [".cs"])]);
        var watchedFile = context.EnqueueWatchingFile(filePath);

        Assert.Same(NoOpWatchedFile.Instance, watchedFile);
    }

    [Fact]
    public void EnqueueWatchingFile_WithExtensionFilter_NonMatchingExtension_ReturnsIndividualWatcher()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "test.txt");
        var watcher = new SimpleFileChangeWatcher();

        // Only watching for .cs files
        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [".cs"])]);
        var watchedFile = context.EnqueueWatchingFile(filePath);

        // .txt file is not covered by .cs filter, so it gets an individual watcher
        Assert.NotSame(NoOpWatchedFile.Instance, watchedFile);
        watchedFile.Dispose();
    }

    [Fact]
    public void EnqueueWatchingFile_MultipleTimesForSameFile_AllReturnDisposableWatchers()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "multi.txt");
        var watcher = new SimpleFileChangeWatcher();

        using var context = watcher.CreateContext([]);

        var watcher1 = context.EnqueueWatchingFile(filePath);
        var watcher2 = context.EnqueueWatchingFile(filePath);

        // Both should be valid individual watchers
        Assert.NotSame(NoOpWatchedFile.Instance, watcher1);
        Assert.NotSame(NoOpWatchedFile.Instance, watcher2);

        watcher1.Dispose();
        watcher2.Dispose();
    }

    [Fact]
    public void EnqueueWatchingFile_InNestedDirectory_ReturnsNoOpWatcher()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var subDirectory = tempDirectory.CreateDirectory("subdir");
        var filePath = Path.Combine(subDirectory.Path, "nested.cs");
        var watcher = new SimpleFileChangeWatcher();

        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);
        var watchedFile = context.EnqueueWatchingFile(filePath);

        // File in subdirectory should be covered by parent directory watch
        Assert.Same(NoOpWatchedFile.Instance, watchedFile);
    }

    [Fact]
    public void EnqueueWatchingFile_WithNonExistentDirectory_HandlesGracefully()
    {
        var watcher = new SimpleFileChangeWatcher();
        var nonExistentPath = Path.Combine(TempRoot.Root, "NonExistent", "file.cs");

        using var context = watcher.CreateContext([]);

        // Should not throw, though the file won't actually be watched until the directory exists
        var watchedFile = context.EnqueueWatchingFile(nonExistentPath);
        Assert.NotNull(watchedFile);
        watchedFile.Dispose();
    }

    [Fact]
    public void EnqueueWatchingFile_WithMultipleExtensionFilters_MatchesAny()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var csFilePath = Path.Combine(tempDirectory.Path, "test.cs");
        var vbFilePath = Path.Combine(tempDirectory.Path, "test.vb");
        var txtFilePath = Path.Combine(tempDirectory.Path, "test.txt");
        var watcher = new SimpleFileChangeWatcher();

        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [".cs", ".vb"])]);

        var csWatcher = context.EnqueueWatchingFile(csFilePath);
        var vbWatcher = context.EnqueueWatchingFile(vbFilePath);
        var txtWatcher = context.EnqueueWatchingFile(txtFilePath);

        // .cs and .vb should be covered by directory watch
        Assert.Same(NoOpWatchedFile.Instance, csWatcher);
        Assert.Same(NoOpWatchedFile.Instance, vbWatcher);

        // .txt should need individual watch
        Assert.NotSame(NoOpWatchedFile.Instance, txtWatcher);
        txtWatcher.Dispose();
    }

    [Fact]
    public void EnqueueWatchingFile_DeeplyNestedFile_ReturnsNoOpWatcher()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var level1 = tempDirectory.CreateDirectory("level1");
        var level2 = level1.CreateDirectory("level2");
        var level3 = level2.CreateDirectory("level3");
        var filePath = Path.Combine(level3.Path, "deep.cs");
        var watcher = new SimpleFileChangeWatcher();

        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [".cs"])]);
        var watchedFile = context.EnqueueWatchingFile(filePath);

        // Deeply nested file should still be covered by root directory watch
        Assert.Same(NoOpWatchedFile.Instance, watchedFile);
    }

    [Fact]
    public void EnqueueWatchingFile_SiblingDirectory_NotCovered()
    {
        var rootDir = _tempRoot.CreateDirectory();
        var watchedDir = rootDir.CreateDirectory("watched");
        var siblingDir = rootDir.CreateDirectory("sibling");
        var filePath = Path.Combine(siblingDir.Path, "test.cs");
        var watcher = new SimpleFileChangeWatcher();

        using var context = watcher.CreateContext([new WatchedDirectory(watchedDir.Path, extensionFilters: [])]);
        var watchedFile = context.EnqueueWatchingFile(filePath);

        // File in sibling directory should not be covered
        Assert.NotSame(NoOpWatchedFile.Instance, watchedFile);
        watchedFile.Dispose();
    }

    [Fact]
    public void EnqueueWatchingFile_ParentDirectory_NotCovered()
    {
        var rootDir = _tempRoot.CreateDirectory();
        var watchedDir = rootDir.CreateDirectory("subdir");
        var filePath = Path.Combine(rootDir.Path, "test.cs");
        var watcher = new SimpleFileChangeWatcher();

        using var context = watcher.CreateContext([new WatchedDirectory(watchedDir.Path, extensionFilters: [])]);
        var watchedFile = context.EnqueueWatchingFile(filePath);

        // File in parent directory should not be covered
        Assert.NotSame(NoOpWatchedFile.Instance, watchedFile);
        watchedFile.Dispose();
    }

    [Fact]
    public void EnqueueWatchingFile_DisposeThenEnqueueAgain_Works()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "test.txt");
        var watcher = new SimpleFileChangeWatcher();

        using var context = watcher.CreateContext([]);

        var watcher1 = context.EnqueueWatchingFile(filePath);
        watcher1.Dispose();

        var watcher2 = context.EnqueueWatchingFile(filePath);
        Assert.NotSame(NoOpWatchedFile.Instance, watcher2);
        watcher2.Dispose();
    }

    #region File System Event Tests

    /// <summary>
    /// Helper method to wait for a file change event with timeout.
    /// </summary>
    private static async Task<bool> WaitForFileChangeAsync(TaskCompletionSource eventSource, TimeSpan timeout)
    {
        var completed = await Task.WhenAny(eventSource.Task, Task.Delay(timeout));
        return completed == eventSource.Task;
    }

    [Fact]
    public async Task FileCreated_InWatchedDirectory_RaisesFileChangedEvent()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "created.cs");
        var watcher = new SimpleFileChangeWatcher();

        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);

        var eventSource = new TaskCompletionSource();
        context.FileChanged += (sender, path) =>
        {
            if (path == filePath)
                eventSource.SetResult();
        };

        // Create the file
        File.WriteAllText(filePath, "initial content");

        // Wait for the event
        var eventFired = await WaitForFileChangeAsync(eventSource, TimeSpan.FromSeconds(FileChangeTimeout));

        Assert.True(eventFired, "FileChanged event should fire when a file is created");
    }

    [Fact]
    public async Task FileModified_InWatchedDirectory_RaisesFileChangedEvent()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "modified.cs");
        var watcher = new SimpleFileChangeWatcher();

        // Create file first before setting up the watcher
        File.WriteAllText(filePath, "initial content");

        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);

        var eventSource = new TaskCompletionSource();
        context.FileChanged += (sender, path) =>
        {
            if (path == filePath)
                eventSource.SetResult();
        };

        // Modify the file
        File.WriteAllText(filePath, "modified content");

        // Wait for the event
        var eventFired = await WaitForFileChangeAsync(eventSource, TimeSpan.FromSeconds(FileChangeTimeout));

        Assert.True(eventFired, "FileChanged event should fire when a file is modified");
    }

    [Fact]
    public async Task FileDeleted_InWatchedDirectory_RaisesFileChangedEvent()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "deleted.cs");
        var watcher = new SimpleFileChangeWatcher();

        // Create file first before setting up the watcher
        File.WriteAllText(filePath, "content to delete");

        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);

        var eventSource = new TaskCompletionSource();
        context.FileChanged += (sender, path) =>
        {
            if (path == filePath)
                eventSource.SetResult();
        };

        // Delete the file
        File.Delete(filePath);

        // Wait for the event
        var eventFired = await WaitForFileChangeAsync(eventSource, TimeSpan.FromSeconds(FileChangeTimeout));

        Assert.True(eventFired, "FileChanged event should fire when a file is deleted");
    }

    [Fact]
    public async Task FileCreated_WithMatchingExtensionFilter_RaisesFileChangedEvent()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "filtered.cs");
        var watcher = new SimpleFileChangeWatcher();

        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [".cs"])]);

        var eventSource = new TaskCompletionSource();
        context.FileChanged += (sender, path) =>
        {
            if (path == filePath)
                eventSource.SetResult();
        };

        // Create a .cs file (should match filter)
        File.WriteAllText(filePath, "content");

        // Wait for the event
        var eventFired = await WaitForFileChangeAsync(eventSource, TimeSpan.FromSeconds(FileChangeTimeout));

        Assert.True(eventFired, "FileChanged event should fire for files matching extension filter");
    }

    [Fact]
    public async Task FileCreated_WithNonMatchingExtensionFilter_DoesNotRaiseFileChangedEvent()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var txtFilePath = Path.Combine(tempDirectory.Path, "filtered.txt");
        var watcher = new SimpleFileChangeWatcher();

        // Only watching for .cs files
        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [".cs"])]);

        var eventFired = false;
        context.FileChanged += (sender, path) =>
        {
            if (path == txtFilePath)
                eventFired |= true;
        };

        // Create a .txt file (should not match filter)
        File.WriteAllText(txtFilePath, "content");

        // Wait a bit to ensure no event fires
        await Task.Delay(TimeSpan.FromSeconds(1));

        Assert.False(eventFired, "FileChanged event should NOT fire for files not matching extension filter");
    }

    [Fact]
    public async Task FileCreated_InSubdirectory_RaisesFileChangedEvent()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var subDirectory = tempDirectory.CreateDirectory("subdir");
        var filePath = Path.Combine(subDirectory.Path, "nested.cs");
        var watcher = new SimpleFileChangeWatcher();

        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);

        var eventSource = new TaskCompletionSource();
        context.FileChanged += (sender, path) =>
        {
            if (path == filePath)
                eventSource.SetResult();
        };

        // Create file in subdirectory
        File.WriteAllText(filePath, "nested content");

        // Wait for the event
        var eventFired = await WaitForFileChangeAsync(eventSource, TimeSpan.FromSeconds(FileChangeTimeout));

        Assert.True(eventFired, "FileChanged event should fire for files created in subdirectories");
    }

    [Fact]
    public async Task IndividualFileWatch_FileCreated_RaisesFileChangedEvent()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "individual.txt");
        var watcher = new SimpleFileChangeWatcher();

        // Create context without directory watches
        using var context = watcher.CreateContext([]);

        var eventSource = new TaskCompletionSource();
        context.FileChanged += (sender, path) =>
        {
            if (path == filePath)
                eventSource.SetResult();
        };

        // Watch the specific file
        var watchedFile = context.EnqueueWatchingFile(filePath);

        // Create the file
        File.WriteAllText(filePath, "individual file content");

        // Wait for the event
        var eventFired = await WaitForFileChangeAsync(eventSource, TimeSpan.FromSeconds(FileChangeTimeout));

        Assert.True(eventFired, "FileChanged event should fire for individually watched files");

        watchedFile.Dispose();
    }

    [Fact]
    public async Task IndividualFileWatch_FileModified_RaisesFileChangedEvent()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "individual_modify.txt");
        var watcher = new SimpleFileChangeWatcher();

        // Create the file first
        File.WriteAllText(filePath, "initial content");

        // Create context without directory watches
        using var context = watcher.CreateContext([]);

        // Watch the specific file
        var watchedFile = context.EnqueueWatchingFile(filePath);

        var eventSource = new TaskCompletionSource();
        context.FileChanged += (sender, path) =>
        {
            if (path == filePath)
                eventSource.SetResult();
        };

        // Modify the file
        File.WriteAllText(filePath, "modified content");

        // Wait for the event
        var eventFired = await WaitForFileChangeAsync(eventSource, TimeSpan.FromSeconds(FileChangeTimeout));

        Assert.True(eventFired, "FileChanged event should fire when individually watched file is modified");

        watchedFile.Dispose();
    }

    [Fact]
    public async Task IndividualFileWatch_AfterDispose_DoesNotRaiseEvent()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "disposed.txt");
        var watcher = new SimpleFileChangeWatcher();

        using var context = watcher.CreateContext([]);

        var eventFired = false;
        context.FileChanged += (sender, path) =>
        {
            if (path == filePath)
                eventFired = true;
        };

        // Watch and then immediately dispose
        var watchedFile = context.EnqueueWatchingFile(filePath);
        watchedFile.Dispose();

        // Small delay to ensure dispose completes
        await Task.Delay(TimeSpan.FromMilliseconds(200));

        // Create the file after disposing the watch
        File.WriteAllText(filePath, "content after dispose");

        // Wait to see if any events fire
        await Task.Delay(TimeSpan.FromSeconds(1));

        Assert.False(eventFired, "FileChanged event should NOT fire after individual file watch is disposed");
    }

    [Fact]
    public async Task MultipleFileChanges_AllRaiseEvents()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var watcher = new SimpleFileChangeWatcher();

        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);

        var changedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var allEventsReceived = new TaskCompletionSource<bool>();

        // Create file paths first
        var file1 = Path.Combine(tempDirectory.Path, "file1.cs");
        var file2 = Path.Combine(tempDirectory.Path, "file2.cs");
        var file3 = Path.Combine(tempDirectory.Path, "file3.cs");

        context.FileChanged += (sender, path) =>
        {
            lock (changedFiles)
            {
                changedFiles.Add(path);
                // Check if we've seen all 3 files (regardless of how many events each generated)
                if (changedFiles.Contains(file1) && changedFiles.Contains(file2) && changedFiles.Contains(file3))
                    allEventsReceived.TrySetResult(true);
            }
        };

        File.WriteAllText(file1, "content1");
        await Task.Delay(100); // Small delay between operations
        File.WriteAllText(file2, "content2");
        await Task.Delay(100);
        File.WriteAllText(file3, "content3");

        // Wait for all events
        var completed = await Task.WhenAny(allEventsReceived.Task, Task.Delay(TimeSpan.FromSeconds(15)));

        Assert.True(completed == allEventsReceived.Task, "Should receive events for all file changes");
        Assert.Contains(file1, changedFiles);
        Assert.Contains(file2, changedFiles);
        Assert.Contains(file3, changedFiles);
    }

    [Fact]
    public async Task FileRenamed_InWatchedDirectory_FireEventForOriginalPath()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var originalPath = Path.Combine(tempDirectory.Path, "original.cs");
        var renamedPath = Path.Combine(tempDirectory.Path, "renamed.cs");
        var watcher = new SimpleFileChangeWatcher();

        // Create original file
        File.WriteAllText(originalPath, "content");

        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);

        var eventSource = new TaskCompletionSource();
        context.FileChanged += (sender, path) =>
        {
            if (path == originalPath)
                eventSource.SetResult();
        };

        // Rename the file
        File.Move(originalPath, renamedPath);

        // Wait for the event
        var eventFired = await WaitForFileChangeAsync(eventSource, TimeSpan.FromSeconds(FileChangeTimeout));

        Assert.True(eventFired, "FileChanged event should fire when a file is renamed");
    }

    [Fact]
    public async Task FileRenamed_InWatchedDirectory_FireEventForRenamedPath()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var originalPath = Path.Combine(tempDirectory.Path, "original.cs");
        var renamedPath = Path.Combine(tempDirectory.Path, "renamed.cs");
        var watcher = new SimpleFileChangeWatcher();

        // Create original file
        File.WriteAllText(originalPath, "content");

        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);

        var eventSource = new TaskCompletionSource();
        context.FileChanged += (sender, path) =>
        {
            if (path == renamedPath)
                eventSource.SetResult();
        };

        // Rename the file
        File.Move(originalPath, renamedPath);

        // Wait for the event
        var eventFired = await WaitForFileChangeAsync(eventSource, TimeSpan.FromSeconds(FileChangeTimeout));

        Assert.True(eventFired, "FileChanged event should fire when a file is renamed");
    }

    #endregion
}
