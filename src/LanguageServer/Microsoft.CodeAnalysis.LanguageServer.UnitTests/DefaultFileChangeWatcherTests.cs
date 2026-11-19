// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.FileWatching;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class DefaultFileChangeWatcherTests : IDisposable
{
    private static readonly TimeSpan s_fileChangeTimeout = TimeSpan.FromSeconds(1);
    private readonly TempRoot _tempRoot = new();

    public void Dispose() => _tempRoot.Dispose();

    #region File System Event Tests

    /// <summary>
    /// Helper method to wait for a file change event with timeout.
    /// </summary>
    private static async Task AssertAllChangesFire(FileChangeTask[] fileChangeTasks, TimeSpan timeout)
    {
        var delay = Task.Delay(timeout);
        var completed = await Task.WhenAny(Task.WhenAll(fileChangeTasks.Select(t => t.Task)), delay);
        if (completed == delay)
        {
            // At least one didn't fire, so assert which one it is
            Assert.Empty(fileChangeTasks.Where(f => !f.Task.IsCompleted).Select(f => f.FilePath));
        }
    }

    private static FileChangeTask ListenForFileChangeAsync(IFileChangeContext context, string filePath)
    {
        var eventSource = new TaskCompletionSource<FileChangeKind>();

        context.FileChanged += (sender, e) =>
        {
            if (e.FilePath == filePath)
                eventSource.TrySetResult(e.ChangeKind);
        };

        return new FileChangeTask(eventSource.Task, filePath);
    }

    /// <summary>
    /// Represents a wait for a single file change; includes the file path so failures are easy to understand which one didn't trigger.
    /// </summary>
    private sealed record FileChangeTask(Task<FileChangeKind> Task, string FilePath);

    [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/83180")]
    public async Task FileCreated_InWatchedParentDirectory_RaisesFileChangedEvent()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "created.cs");
        var watcher = new DefaultFileChangeWatcher();

        using var context = watcher.CreateContext([]);
        var fileChangeTask = ListenForFileChangeAsync(context, filePath);

        // Watch the specific file
        context.EnqueueWatchingFile(filePath);

        // Create the file
        File.WriteAllText(filePath, "initial content");

        // Wait for the event
        await AssertAllChangesFire([fileChangeTask], s_fileChangeTimeout);
        Assert.Equal(FileChangeKind.Created, await fileChangeTask.Task);
    }

    [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/83180")]
    public async Task FileModified_InWatchedDirectory_RaisesFileChangedEvent()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "modified.cs");
        var watcher = new DefaultFileChangeWatcher();

        // Create file first before setting up the watcher
        File.WriteAllText(filePath, "initial content");

        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);
        var fileChangeTask = ListenForFileChangeAsync(context, filePath);

        // Modify the file
        File.WriteAllText(filePath, "modified content");

        // Wait for the event
        await AssertAllChangesFire([fileChangeTask], s_fileChangeTimeout);
        Assert.Equal(FileChangeKind.Changed, await fileChangeTask.Task);
    }

    [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/83180")]
    public async Task FileDeleted_InWatchedDirectory_RaisesFileChangedEvent()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "deleted.cs");
        var watcher = new DefaultFileChangeWatcher();

        // Create file first before setting up the watcher
        File.WriteAllText(filePath, "content to delete");

        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);
        var fileChangeTask = ListenForFileChangeAsync(context, filePath);

        // Delete the file
        File.Delete(filePath);

        // Wait for the event
        await AssertAllChangesFire([fileChangeTask], s_fileChangeTimeout);
        Assert.Equal(FileChangeKind.Deleted, await fileChangeTask.Task);
    }

    [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/83180")]
    public async Task FileCreated_WithMatchingExtensionFilter_RaisesFileChangedEvent()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "filtered.cs");
        var watcher = new DefaultFileChangeWatcher();

        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [".cs"])]);
        var fileChangeTask = ListenForFileChangeAsync(context, filePath);

        // Create a .cs file (should match filter)
        File.WriteAllText(filePath, "content");

        // Wait for the event
        await AssertAllChangesFire([fileChangeTask], s_fileChangeTimeout);
    }

    [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/83180")]
    public async Task FileCreated_WithNonMatchingExtensionFilter_DoesNotRaiseFileChangedEvent()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var txtFilePath = Path.Combine(tempDirectory.Path, "filtered.txt");
        var watcher = new DefaultFileChangeWatcher();

        // Only watching for .cs files
        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [".cs"])]);
        var fileChangeTask = ListenForFileChangeAsync(context, txtFilePath);

        // Create a .txt file (should not match filter)
        File.WriteAllText(txtFilePath, "content");

        // Wait a bit to ensure no event fires
        await Task.Delay(s_fileChangeTimeout);

        Assert.False(fileChangeTask.Task.IsCompleted, "FileChanged event should NOT fire for files not matching extension filter");
    }

    [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/83180")]
    public async Task FileCreated_InSubdirectory_RaisesFileChangedEvent()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var subDirectory = tempDirectory.CreateDirectory("subdir");
        var filePath = Path.Combine(subDirectory.Path, "nested.cs");
        var watcher = new DefaultFileChangeWatcher();

        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);
        var fileChangeTask = ListenForFileChangeAsync(context, filePath);

        // Create file in subdirectory
        File.WriteAllText(filePath, "nested content");

        // Wait for the event
        await AssertAllChangesFire([fileChangeTask], s_fileChangeTimeout);
    }

    [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/83180")]
    public async Task IndividualFileWatch_FileCreated_RaisesFileChangedEvent()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "individual.txt");
        var watcher = new DefaultFileChangeWatcher();

        // Create context without directory watches
        using var context = watcher.CreateContext([]);
        var fileChangeTask = ListenForFileChangeAsync(context, filePath);

        // Watch the specific file
        using var watchedFile = context.EnqueueWatchingFile(filePath);

        // Create the file
        File.WriteAllText(filePath, "individual file content");

        // Wait for the event
        await AssertAllChangesFire([fileChangeTask], s_fileChangeTimeout);
    }

    [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/83180")]
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
        var fileChangeTask = ListenForFileChangeAsync(context, filePath);

        // Modify the file
        File.WriteAllText(filePath, "modified content");

        // Wait for the event
        await AssertAllChangesFire([fileChangeTask], s_fileChangeTimeout);
    }

    [Fact]
    public async Task FileCreated_WithDifferentExtensionFiltersOnSameDirectory_RaisesForBothExtensions()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var csharpFilePath = Path.Combine(tempDirectory.Path, "created.cs");
        var visualBasicFilePath = Path.Combine(tempDirectory.Path, "created.vb");
        var watcher = new DefaultFileChangeWatcher();

        using var context = watcher.CreateContext([
            new WatchedDirectory(tempDirectory.Path, extensionFilters: [".cs"]),
            new WatchedDirectory(tempDirectory.Path, extensionFilters: [".vb"])
        ]);

        var fileChangeTasks = new[]
        {
            ListenForFileChangeAsync(context, csharpFilePath),
            ListenForFileChangeAsync(context, visualBasicFilePath),
        };

        // Create files matching both filters
        File.WriteAllText(csharpFilePath, "csharp content");
        File.WriteAllText(visualBasicFilePath, "visual basic content");

        await AssertAllChangesFire(fileChangeTasks, s_fileChangeTimeout);
    }

    [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/83180")]
    public async Task IndividualFileWatch_AfterDispose_DoesNotRaiseEvent()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "disposed.txt");
        var watcher = new DefaultFileChangeWatcher();

        using var context = watcher.CreateContext([]);
        var fileChangeContext = (DefaultFileChangeWatcher.FileChangeContext)context;
        var fileChangeTask = ListenForFileChangeAsync(fileChangeContext, filePath);

        // Watch and then immediately dispose
        var watchedFile = context.EnqueueWatchingFile(filePath);
        watchedFile.Dispose();

        // Small delay to ensure dispose completes
        await Task.Delay(TimeSpan.FromMilliseconds(200));

        // Create the file after disposing the watch
        File.WriteAllText(filePath, "content after dispose");

        // Wait to see if any events fire
        await Task.Delay(s_fileChangeTimeout);

        Assert.False(fileChangeTask.Task.IsCompleted, "FileChanged event should NOT fire after individual file watch is disposed");
    }

    [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/83180")]
    public async Task MultipleFileChanges_AllRaiseEvents()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var watcher = new DefaultFileChangeWatcher();

        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);

        // Create file paths first
        var file1 = Path.Combine(tempDirectory.Path, "file1.cs");
        var file2 = Path.Combine(tempDirectory.Path, "file2.cs");
        var file3 = Path.Combine(tempDirectory.Path, "file3.cs");

        var fileChangeTasks = new[]
        {
            ListenForFileChangeAsync(context, file1),
            ListenForFileChangeAsync(context, file2),
            ListenForFileChangeAsync(context, file3)
        };

        // Create several files in sequence
        File.WriteAllText(file1, "content1");
        await Task.Delay(100); // Small delay between operations
        File.WriteAllText(file2, "content2");
        await Task.Delay(100);
        File.WriteAllText(file3, "content3");

        // Wait for all events
        await AssertAllChangesFire(fileChangeTasks, s_fileChangeTimeout);
    }

    [Fact]
    public async Task FileCreated_InNonExistentDirectory_RaisesEventAfterDirectoryCreated()
    {
        var root = _tempRoot.CreateDirectory();
        var nonExistentDir = Path.Combine(root.Path, "not_yet_created");
        var filePath = Path.Combine(nonExistentDir, "new_file.cs");
        var watcher = new DefaultFileChangeWatcher();

        using var context = watcher.CreateContext([]);
        var fileChangeTask = ListenForFileChangeAsync(context, filePath);

        // Watch the file whose directory doesn't exist yet; the watcher should be placed on an ancestor
        using var watchedFile = context.EnqueueWatchingFile(filePath);

        // Now create the directory and the file
        Directory.CreateDirectory(nonExistentDir);

        // On Linux, a directory watch is not recursive. This is implemented for us by the .NET Runtime -- when it sees a new directory
        // created, it adds that directory to the existing watch list. This means however that in the case of a new directory created
        // and then immediately creating a new file, there's not a guarantee the file change could be seen if the directory watch
        // hasn't been processed yet.
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            await Task.Delay(100);

        File.WriteAllText(filePath, "content");

        // The ancestor watcher should still pick up the event
        await AssertAllChangesFire([fileChangeTask], s_fileChangeTimeout);
    }

    [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/83180")]
    public async Task FileRenamed_InWatchedDirectory_FiresEventForOriginalPath()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var originalPath = Path.Combine(tempDirectory.Path, "original.cs");
        var renamedPath = Path.Combine(tempDirectory.Path, "renamed.cs");
        var watcher = new DefaultFileChangeWatcher();

        // Create original file
        File.WriteAllText(originalPath, "content");

        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);
        var fileChangeTask = ListenForFileChangeAsync(context, originalPath);

        // Rename the file
        File.Move(originalPath, renamedPath);

        await AssertAllChangesFire([fileChangeTask], s_fileChangeTimeout);
    }

    [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/83180")]
    public async Task FileRenamed_InWatchedDirectory_FiresEventForRenamedPath()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var originalPath = Path.Combine(tempDirectory.Path, "original.cs");
        var renamedPath = Path.Combine(tempDirectory.Path, "renamed.cs");
        var watcher = new DefaultFileChangeWatcher();

        // Create original file
        File.WriteAllText(originalPath, "content");

        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);
        var fileChangeTask = ListenForFileChangeAsync(context, renamedPath);

        // Rename the file
        File.Move(originalPath, renamedPath);

        await AssertAllChangesFire([fileChangeTask], s_fileChangeTimeout);
    }

    #endregion

    #region Shared Watcher Tests

    [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/83180")]
    public async Task SharedWatcher_MultipleContexts_BothReceiveEvents()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "test.cs");
        var watcher = new DefaultFileChangeWatcher();

        using var context1 = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);
        using var context2 = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [".cs"])]);

        var fileChangeTasks = new[]
        {
            ListenForFileChangeAsync(context1, filePath),
            ListenForFileChangeAsync(context2, filePath)
        };

        // Create file
        File.WriteAllText(filePath, "content");

        // Both contexts should receive the event
        await AssertAllChangesFire(fileChangeTasks, s_fileChangeTimeout);
    }

    [ConditionalFact(typeof(WindowsOnly), Reason = "https://github.com/dotnet/roslyn/issues/83180")]
    public async Task SharedWatcher_DisposedContext_DoesNotReceiveEvents()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "test.cs");
        var watcher = new DefaultFileChangeWatcher();

        var context1 = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);
        using var context2 = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [".cs"])]);

        var context1Events = new List<string>();
        var context2Received = ListenForFileChangeAsync(context2, filePath);

        context1.FileChanged += (sender, e) => context1Events.Add(e.FilePath);

        // Dispose context1 before creating file
        context1.Dispose();

        // Create file
        File.WriteAllText(filePath, "content");

        // Only context2 should receive the event
        await AssertAllChangesFire([context2Received], s_fileChangeTimeout);
        Assert.DoesNotContain(filePath, context1Events);
    }

    #endregion
}
