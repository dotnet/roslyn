// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.FileWatching;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Test.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class SimpleFileChangeWatcherTests : IDisposable
{
    private readonly TimeSpan _fileChangeTimeout = TimeSpan.FromSeconds(10);
    private readonly TempRoot _tempRoot = new();

    public void Dispose() => _tempRoot.Dispose();

    [Fact]
    public void CreateContext_WithEmptyDirectories_DoesNotAddRootWatcher()
    {
        var watcher = new DefaultFileChangeWatcher();

        using var context = watcher.CreateContext([]);

        Assert.Empty(DefaultFileChangeWatcher.FileChangeContext.TestAccessor.GetRootFileWatchers((DefaultFileChangeWatcher.FileChangeContext)context));
    }

    [Fact]
    public void CreateContext_WithExistingDirectory_AddsRootWatcher()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var watcher = new DefaultFileChangeWatcher();

        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);

        Assert.Single(DefaultFileChangeWatcher.FileChangeContext.TestAccessor.GetRootFileWatchers((DefaultFileChangeWatcher.FileChangeContext)context));
    }

    [Fact]
    public void CreateContext_WithNonExistentDirectory_DoesNotAddRootWatcher()
    {
        var nonExistentPath = Path.Combine(TempRoot.Root, "NonExistent", "Directory", Guid.NewGuid().ToString());
        var watcher = new DefaultFileChangeWatcher();

        using var context = watcher.CreateContext([new WatchedDirectory(nonExistentPath, extensionFilters: [])]);

        Assert.Empty(DefaultFileChangeWatcher.FileChangeContext.TestAccessor.GetRootFileWatchers((DefaultFileChangeWatcher.FileChangeContext)context));
    }

    [Fact]
    public void CreateContext_WithMultipleDirectories_OnSameRoot_CreatesOneRootWatcher()
    {
        var rootDir = _tempRoot.CreateDirectory();
        var subDir1 = rootDir.CreateDirectory("sub1");
        var subDir2 = rootDir.CreateDirectory("sub2");
        var watcher = new DefaultFileChangeWatcher();

        // Both directories are under the same root
        using var context = watcher.CreateContext([
            new WatchedDirectory(subDir1.Path, extensionFilters: []),
            new WatchedDirectory(subDir2.Path, extensionFilters: [])
        ]);

        Assert.Single(DefaultFileChangeWatcher.FileChangeContext.TestAccessor.GetRootFileWatchers((DefaultFileChangeWatcher.FileChangeContext)context));
    }

    [Fact]
    public void EnqueueWatchingFile_InWatchedDirectory_ReturnsNoOpWatcher()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "test.cs");
        var watcher = new DefaultFileChangeWatcher();

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
        var watcher = new DefaultFileChangeWatcher();

        using var context = watcher.CreateContext([new WatchedDirectory(watchedDir.Path, extensionFilters: [])]);
        using var watchedFile = context.EnqueueWatchingFile(filePath);

        // When a file is not covered, it returns an actual watcher
        Assert.NotSame(NoOpWatchedFile.Instance, watchedFile);
    }

    [Fact]
    public void EnqueueWatchingFile_WithExtensionFilter_MatchingExtension_ReturnsNoOpWatcher()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "test.cs");
        var watcher = new DefaultFileChangeWatcher();

        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [".cs"])]);
        using var watchedFile = context.EnqueueWatchingFile(filePath);

        Assert.Same(NoOpWatchedFile.Instance, watchedFile);
    }

    [Fact]
    public void EnqueueWatchingFile_WithExtensionFilter_NonMatchingExtension_ReturnsIndividualWatcher()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "test.txt");
        var watcher = new DefaultFileChangeWatcher();

        // Only watching for .cs files
        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [".cs"])]);
        using var watchedFile = context.EnqueueWatchingFile(filePath);

        // .txt file is not covered by .cs filter, so it gets an individual watcher
        Assert.NotSame(NoOpWatchedFile.Instance, watchedFile);
    }

    [Fact]
    public void EnqueueWatchingFile_MultipleTimesForSameFile_AllReturnDisposableWatchers()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "multi.txt");
        var watcher = new DefaultFileChangeWatcher();

        using var context = watcher.CreateContext([]);

        using var watcher1 = context.EnqueueWatchingFile(filePath);
        using var watcher2 = context.EnqueueWatchingFile(filePath);

        // Both should be valid individual watchers
        Assert.NotSame(NoOpWatchedFile.Instance, watcher1);
        Assert.NotSame(NoOpWatchedFile.Instance, watcher2);
        Assert.NotSame(watcher1, watcher2);
    }

    [Fact]
    public void EnqueueWatchingFile_InNestedDirectory_ReturnsNoOpWatcher()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var subDirectory = tempDirectory.CreateDirectory("subdir");
        var filePath = Path.Combine(subDirectory.Path, "nested.cs");
        var watcher = new DefaultFileChangeWatcher();

        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);
        using var watchedFile = context.EnqueueWatchingFile(filePath);

        // File in subdirectory should be covered by parent directory watch
        Assert.Same(NoOpWatchedFile.Instance, watchedFile);
    }

    [Fact]
    public void EnqueueWatchingFile_WithNonExistentDirectory_HandlesGracefully()
    {
        var watcher = new DefaultFileChangeWatcher();
        var nonExistentPath = Path.Combine(TempRoot.Root, "NonExistent", "file.cs");

        using var context = watcher.CreateContext([]);

        // Should not throw, though the file won't actually be watched until the directory exists
        using var watchedFile = context.EnqueueWatchingFile(nonExistentPath);
        Assert.NotNull(watchedFile);
    }

    [Fact]
    public void EnqueueWatchingFile_WithMultipleExtensionFilters_MatchesAny()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var csFilePath = Path.Combine(tempDirectory.Path, "test.cs");
        var vbFilePath = Path.Combine(tempDirectory.Path, "test.vb");
        var txtFilePath = Path.Combine(tempDirectory.Path, "test.txt");
        var watcher = new DefaultFileChangeWatcher();

        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [".cs", ".vb"])]);

        using var csWatcher = context.EnqueueWatchingFile(csFilePath);
        using var vbWatcher = context.EnqueueWatchingFile(vbFilePath);
        using var txtWatcher = context.EnqueueWatchingFile(txtFilePath);

        // .cs and .vb should be covered by directory watch
        Assert.Same(NoOpWatchedFile.Instance, csWatcher);
        Assert.Same(NoOpWatchedFile.Instance, vbWatcher);

        // .txt should need individual watch
        Assert.NotSame(NoOpWatchedFile.Instance, txtWatcher);
    }

    [Fact]
    public void EnqueueWatchingFile_DeeplyNestedFile_ReturnsNoOpWatcher()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var level1 = tempDirectory.CreateDirectory("level1");
        var level2 = level1.CreateDirectory("level2");
        var level3 = level2.CreateDirectory("level3");
        var filePath = Path.Combine(level3.Path, "deep.cs");
        var watcher = new DefaultFileChangeWatcher();

        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [".cs"])]);
        using var watchedFile = context.EnqueueWatchingFile(filePath);

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
        var watcher = new DefaultFileChangeWatcher();

        using var context = watcher.CreateContext([new WatchedDirectory(watchedDir.Path, extensionFilters: [])]);
        using var watchedFile = context.EnqueueWatchingFile(filePath);

        // File in sibling directory should not be covered
        Assert.NotSame(NoOpWatchedFile.Instance, watchedFile);
    }

    [Fact]
    public void EnqueueWatchingFile_ParentDirectory_NotCovered()
    {
        var rootDir = _tempRoot.CreateDirectory();
        var watchedDir = rootDir.CreateDirectory("subdir");
        var filePath = Path.Combine(rootDir.Path, "test.cs");
        var watcher = new DefaultFileChangeWatcher();

        using var context = watcher.CreateContext([new WatchedDirectory(watchedDir.Path, extensionFilters: [])]);
        using var watchedFile = context.EnqueueWatchingFile(filePath);

        // File in parent directory should not be covered
        Assert.NotSame(NoOpWatchedFile.Instance, watchedFile);
    }

    [Fact]
    public void EnqueueWatchingFile_DisposeThenEnqueueAgain_Works()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "test.txt");
        var watcher = new DefaultFileChangeWatcher();

        using var context = watcher.CreateContext([]);

        using var watcher1 = context.EnqueueWatchingFile(filePath);
        watcher1.Dispose();

        using var watcher2 = context.EnqueueWatchingFile(filePath);
        Assert.NotSame(NoOpWatchedFile.Instance, watcher2);
    }

    #region File System Event Tests

    /// <summary>
    /// Helper method to wait for a file change event with timeout.
    /// </summary>
    private static async Task<bool> WaitForFileChangeAsync(Task fileChangeTask, TimeSpan timeout)
    {
        var completed = await Task.WhenAny(fileChangeTask, Task.Delay(timeout));
        return completed == fileChangeTask;
    }

    private static async Task<bool> WaitForAllFileChangesAsync(Task[] fileChangeTasks, TimeSpan timeout)
    {
        var delay = Task.Delay(timeout);
        var completed = await Task.WhenAny(Task.WhenAll(fileChangeTasks), delay);
        return completed != delay;
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

    [Fact]
    public async Task FileCreated_InWatchedDirectory_RaisesFileChangedEvent()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "created.cs");
        var watcher = new DefaultFileChangeWatcher();

        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);
        var fileChangeTask = ListenForFileChangeAsync((DefaultFileChangeWatcher.FileChangeContext)context, filePath);

        // Create the file
        File.WriteAllText(filePath, "initial content");

        // Wait for the event
        var eventFired = await WaitForFileChangeAsync(fileChangeTask, _fileChangeTimeout);

        Assert.True(eventFired, "FileChanged event should fire when a file is created");
    }

    [Fact]
    public async Task FileModified_InWatchedDirectory_RaisesFileChangedEvent()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "modified.cs");
        var watcher = new DefaultFileChangeWatcher();

        // Create file first before setting up the watcher
        File.WriteAllText(filePath, "initial content");

        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);
        var fileChangeTask = ListenForFileChangeAsync((DefaultFileChangeWatcher.FileChangeContext)context, filePath);

        // Modify the file
        File.WriteAllText(filePath, "modified content");

        // Wait for the event
        var eventFired = await WaitForFileChangeAsync(fileChangeTask, _fileChangeTimeout);

        Assert.True(eventFired, "FileChanged event should fire when a file is modified");
    }

    [Fact]
    public async Task FileDeleted_InWatchedDirectory_RaisesFileChangedEvent()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "deleted.cs");
        var watcher = new DefaultFileChangeWatcher();

        // Create file first before setting up the watcher
        File.WriteAllText(filePath, "content to delete");

        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);
        var fileChangeTask = ListenForFileChangeAsync((DefaultFileChangeWatcher.FileChangeContext)context, filePath);

        // Delete the file
        File.Delete(filePath);

        // Wait for the event
        var eventFired = await WaitForFileChangeAsync(fileChangeTask, _fileChangeTimeout);

        Assert.True(eventFired, "FileChanged event should fire when a file is deleted");
    }

    [Fact]
    public async Task FileCreated_WithMatchingExtensionFilter_RaisesFileChangedEvent()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "filtered.cs");
        var watcher = new DefaultFileChangeWatcher();

        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [".cs"])]);
        var fileChangeTask = ListenForFileChangeAsync((DefaultFileChangeWatcher.FileChangeContext)context, filePath);

        // Create a .cs file (should match filter)
        File.WriteAllText(filePath, "content");

        // Wait for the event
        var eventFired = await WaitForFileChangeAsync(fileChangeTask, _fileChangeTimeout);

        Assert.True(eventFired, "FileChanged event should fire for files matching extension filter");
    }

    [Fact]
    public async Task FileCreated_WithNonMatchingExtensionFilter_DoesNotRaiseFileChangedEvent()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var txtFilePath = Path.Combine(tempDirectory.Path, "filtered.txt");
        var watcher = new DefaultFileChangeWatcher();

        // Only watching for .cs files
        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [".cs"])]);
        var fileChangeTask = ListenForFileChangeAsync((DefaultFileChangeWatcher.FileChangeContext)context, txtFilePath);

        // Create a .txt file (should not match filter)
        File.WriteAllText(txtFilePath, "content");

        // Wait a bit to ensure no event fires
        await Task.Delay(TimeSpan.FromSeconds(1));

        Assert.False(fileChangeTask.IsCompleted, "FileChanged event should NOT fire for files not matching extension filter");
    }

    [Fact]
    public async Task FileCreated_InSubdirectory_RaisesFileChangedEvent()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var subDirectory = tempDirectory.CreateDirectory("subdir");
        var filePath = Path.Combine(subDirectory.Path, "nested.cs");
        var watcher = new DefaultFileChangeWatcher();

        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);
        var fileChangeTask = ListenForFileChangeAsync((DefaultFileChangeWatcher.FileChangeContext)context, filePath);

        // Create file in subdirectory
        File.WriteAllText(filePath, "nested content");

        // Wait for the event
        var eventFired = await WaitForFileChangeAsync(fileChangeTask, _fileChangeTimeout);

        Assert.True(eventFired, "FileChanged event should fire for files created in subdirectories");
    }

    [Fact]
    public async Task IndividualFileWatch_FileCreated_RaisesFileChangedEvent()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "individual.txt");
        var watcher = new DefaultFileChangeWatcher();

        // Create context without directory watches
        using var context = watcher.CreateContext([]);
        var fileChangeTask = ListenForFileChangeAsync((DefaultFileChangeWatcher.FileChangeContext)context, filePath);

        // Watch the specific file
        using var watchedFile = context.EnqueueWatchingFile(filePath);

        // Create the file
        File.WriteAllText(filePath, "individual file content");

        // Wait for the event
        var eventFired = await WaitForFileChangeAsync(fileChangeTask, _fileChangeTimeout);

        Assert.True(eventFired, "FileChanged event should fire for individually watched files");
    }

    [Fact]
    public async Task IndividualFileWatch_FileModified_RaisesFileChangedEvent()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "individual_modify.txt");
        var watcher = new DefaultFileChangeWatcher();

        // Create the file first
        File.WriteAllText(filePath, "initial content");

        // Create context without directory watches
        using var context = watcher.CreateContext([]);

        // Watch the specific file
        using var watchedFile = context.EnqueueWatchingFile(filePath);
        var fileChangeTask = ListenForFileChangeAsync((DefaultFileChangeWatcher.FileChangeContext)context, filePath);

        // Modify the file
        File.WriteAllText(filePath, "modified content");

        // Wait for the event
        var eventFired = await WaitForFileChangeAsync(fileChangeTask, _fileChangeTimeout);

        Assert.True(eventFired, "FileChanged event should fire when individually watched file is modified");
    }

    [Fact]
    public async Task IndividualFileWatch_AfterDispose_DoesNotRaiseEvent()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "disposed.txt");
        var watcher = new DefaultFileChangeWatcher();

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
        await Task.Delay(_fileChangeTimeout);

        Assert.False(eventFired, "FileChanged event should NOT fire after individual file watch is disposed");
    }

    [Fact]
    public async Task MultipleFileChanges_AllRaiseEvents()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var watcher = new DefaultFileChangeWatcher();

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
        var completed = await Task.WhenAny(allEventsReceived.Task, Task.Delay(_fileChangeTimeout));

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
        var watcher = new DefaultFileChangeWatcher();

        // Create original file
        File.WriteAllText(originalPath, "content");

        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);
        var fileChangeTask = ListenForFileChangeAsync((DefaultFileChangeWatcher.FileChangeContext)context, originalPath);

        // Rename the file
        File.Move(originalPath, renamedPath);

        // Wait for the event
        var eventFired = await WaitForFileChangeAsync(fileChangeTask, _fileChangeTimeout);

        Assert.True(eventFired, "FileChanged event should fire when a file is renamed");
    }

    [Fact]
    public async Task FileRenamed_InWatchedDirectory_FireEventForRenamedPath()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var originalPath = Path.Combine(tempDirectory.Path, "original.cs");
        var renamedPath = Path.Combine(tempDirectory.Path, "renamed.cs");
        var watcher = new DefaultFileChangeWatcher();

        // Create original file
        File.WriteAllText(originalPath, "content");

        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);
        var fileChangeTask = ListenForFileChangeAsync((DefaultFileChangeWatcher.FileChangeContext)context, renamedPath);

        // Rename the file
        File.Move(originalPath, renamedPath);

        // Wait for the event
        var eventFired = await WaitForFileChangeAsync(fileChangeTask, _fileChangeTimeout);

        Assert.True(eventFired, "FileChanged event should fire when a file is renamed");
    }

    #endregion

    #region Shared Watcher Tests

    [Fact]
    public void SharedWatcher_MultipleContexts_ShareSameRootWatcher()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var watcher = new DefaultFileChangeWatcher();

        using var context1 = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);
        using var context2 = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [".cs"])]);

        // Both contexts should have acquired 1 root path
        Assert.Single(DefaultFileChangeWatcher.FileChangeContext.TestAccessor.GetRootFileWatchers((DefaultFileChangeWatcher.FileChangeContext)context1));
        Assert.Single(DefaultFileChangeWatcher.FileChangeContext.TestAccessor.GetRootFileWatchers((DefaultFileChangeWatcher.FileChangeContext)context2));

        // The watcher should only have 1 shared root watcher
        Assert.Single(DefaultFileChangeWatcher.TestAccessor.GetWatchedRootPaths(watcher));
    }

    [Fact]
    public void SharedWatcher_DisposingOneContext_KeepsWatcherForOther()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var watcher = new DefaultFileChangeWatcher();

        var context1 = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);
        var context2 = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [".cs"])]);

        // Shared watcher should exist
        Assert.Single(DefaultFileChangeWatcher.TestAccessor.GetWatchedRootPaths(watcher));

        // Dispose context1
        context1.Dispose();

        // Shared watcher should still exist for context2
        Assert.Single(DefaultFileChangeWatcher.TestAccessor.GetWatchedRootPaths(watcher));

        // Dispose context2
        context2.Dispose();

        // Now shared watcher should be disposed
        Assert.Empty(DefaultFileChangeWatcher.TestAccessor.GetWatchedRootPaths(watcher));
    }

    [Fact]
    public async Task SharedWatcher_MultipleContexts_BothReceiveEvents()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "test.cs");
        var watcher = new DefaultFileChangeWatcher();

        using var context1 = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);
        using var context2 = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [".cs"])]);

        var fileChangeTasks = new[]
        {
            ListenForFileChangeAsync((DefaultFileChangeWatcher.FileChangeContext)context1, filePath),
            ListenForFileChangeAsync((DefaultFileChangeWatcher.FileChangeContext)context2, filePath)
        };

        // Create file
        File.WriteAllText(filePath, "content");

        // Both contexts should receive the event
        var bothReceived = await WaitForAllFileChangesAsync(fileChangeTasks, _fileChangeTimeout);

        Assert.True(bothReceived, "Both contexts should have received FileChanged events");
    }

    [Fact]
    public async Task SharedWatcher_DisposedContext_DoesNotReceiveEvents()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "test.cs");
        var watcher = new DefaultFileChangeWatcher();

        var context1 = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);
        using var context2 = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [".cs"])]);

        var context1Events = new List<string>();
        var context2Received = ListenForFileChangeAsync((DefaultFileChangeWatcher.FileChangeContext)context2, filePath);

        context1.FileChanged += (sender, path) => context1Events.Add(path);

        // Dispose context1 before creating file
        context1.Dispose();

        // Create file
        File.WriteAllText(filePath, "content");

        // Only context2 should receive the event
        var context2ReceivedEvent = await WaitForFileChangeAsync(context2Received, _fileChangeTimeout);

        Assert.True(context2ReceivedEvent, "Context 2 should receive FileChanged event");
        Assert.DoesNotContain(filePath, context1Events);
    }

    [Fact]
    public void SharedWatcher_NewContextAfterDispose_CreatesNewWatcher()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var watcher = new DefaultFileChangeWatcher();

        // Create and dispose first context
        var context1 = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);
        context1.Dispose();

        Assert.Empty(DefaultFileChangeWatcher.TestAccessor.GetWatchedRootPaths(watcher));

        // Create new context - should create a new watcher
        using var context2 = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);

        Assert.Single(DefaultFileChangeWatcher.TestAccessor.GetWatchedRootPaths(watcher));
    }

    #endregion
}
