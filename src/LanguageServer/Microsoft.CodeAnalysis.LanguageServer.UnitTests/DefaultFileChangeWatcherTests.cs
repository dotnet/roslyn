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

    #region Watch Consolidation Tests

    [Fact]
    public void CreateContext_WithEmptyDirectories_DoesNotAddWatchers()
    {
        var watcher = new DefaultFileChangeWatcher();

        using var context = watcher.CreateContext([]);

        // Empty directory lists should not register any shared watchers
        Assert.Empty(DefaultFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));
    }

    [Fact]
    public void CreateContext_WithExistingDirectory_AddsDirectoryWatcher()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var watcher = new DefaultFileChangeWatcher();

        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);

        // Watching one real directory should create one shared watcher
        var watchedDirectory = Assert.Single(DefaultFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));
        Assert.Equal(tempDirectory.Path, watchedDirectory.path);
        Assert.Empty(watchedDirectory.filters);
    }

    [Fact]
    public void CreateContext_WithNonExistentDirectory_WatchesAHigherLevel()
    {
        var nonExistentPath = Path.Combine(TempRoot.Root, "NonExistent", "Directory", Guid.NewGuid().ToString());
        var watcher = new DefaultFileChangeWatcher();

        using var context = watcher.CreateContext([new WatchedDirectory(nonExistentPath, extensionFilters: [])]);

        // A single watch should be created, but in this case it'd be the TempRoot.Root
        var watchedDirectory = Assert.Single(DefaultFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));
        Assert.Equal(TempRoot.Root, watchedDirectory.path);
        Assert.Empty(watchedDirectory.filters);
    }

    [Fact]
    public void CreateContext_WithContainedDirectory_ConsolidatesImmediately_ParentFirst()
    {
        var root = _tempRoot.CreateDirectory();
        var child = root.CreateDirectory("child");
        var watcher = new DefaultFileChangeWatcher();

        using var context = watcher.CreateContext([
            new WatchedDirectory(root.Path, extensionFilters: []),
            new WatchedDirectory(child.Path, extensionFilters: [])
        ]);

        // The child directory is covered by the parent directory watcher
        var watchedDirectory = Assert.Single(DefaultFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));
        Assert.Equal(root.Path, watchedDirectory.path);
        Assert.Empty(watchedDirectory.filters);
    }

    [Fact]
    public void CreateContext_WithContainedDirectory_ConsolidatesImmediately_ChildFirst()
    {
        var root = _tempRoot.CreateDirectory();
        var child = root.CreateDirectory("child");
        var watcher = new DefaultFileChangeWatcher();

        using var context = watcher.CreateContext([
            new WatchedDirectory(child.Path, extensionFilters: []),
            new WatchedDirectory(root.Path, extensionFilters: [])
        ]);

        // The child directory is covered by the parent directory watcher
        var watchedDirectory = Assert.Single(DefaultFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));
        Assert.Equal(root.Path, watchedDirectory.path);
        Assert.Empty(watchedDirectory.filters);
    }

    [Fact]
    public void CreateContext_WithPartiallyCoveredChildDirectory_UsesSharedAncestorWatcher()
    {
        var root = _tempRoot.CreateDirectory();
        var child = root.CreateDirectory("child");
        var watcher = new DefaultFileChangeWatcher();

        using var context = watcher.CreateContext([
            new WatchedDirectory(root.Path, extensionFilters: [".cs"]),
            new WatchedDirectory(child.Path, extensionFilters: [".vb"])
        ]);

        // Different filters still share the same underlying watcher for the directory tree
        var watchedDirectory = Assert.Single(DefaultFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));
        Assert.Equal(root.Path, watchedDirectory.path);
        AssertEx.SetEqual(watchedDirectory.filters, ["*.cs", "*.vb"]);
    }

    [Fact]
    public void EnqueueWatchingFile_InWatchedDirectory_ReturnsNoOpWatcher()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "test.cs");
        var watcher = new DefaultFileChangeWatcher();

        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [".cs"])]);
        using var watchedFile = context.EnqueueWatchingFile(filePath);

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
    public void EnqueueWatchingFile_WatchesParentDirectory()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "test.cs");
        var watcher = new DefaultFileChangeWatcher();

        using var context = watcher.CreateContext([]);
        using var watchedFile = context.EnqueueWatchingFile(filePath);

        // The parent directory becomes watched when we enqueue a file directly
        Assert.NotSame(NoOpWatchedFile.Instance, watchedFile);
        var watchedDirectory = Assert.Single(DefaultFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));
        Assert.Equal(tempDirectory.Path, watchedDirectory.path);
        Assert.Equal("*.cs", Assert.Single(watchedDirectory.filters));
    }

    [Fact]
    public void EnqueueWatchingFile_MultipleFilesSameDirectory_UsesSingleWatcher()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var watcher = new DefaultFileChangeWatcher();
        using var context = watcher.CreateContext([]);

        using var watchedFile1 = context.EnqueueWatchingFile(Path.Combine(tempDirectory.Path, "a.cs"));
        using var watchedFile2 = context.EnqueueWatchingFile(Path.Combine(tempDirectory.Path, "b.cs"));

        // Multiple files in the same directory should share a single underlying watcher
        Assert.NotSame(NoOpWatchedFile.Instance, watchedFile1);
        Assert.NotSame(NoOpWatchedFile.Instance, watchedFile2);
        var watchedDirectory = Assert.Single(DefaultFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));

        Assert.Equal(tempDirectory.Path, watchedDirectory.path);
        Assert.Equal("*.cs", Assert.Single(watchedDirectory.filters));
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

    [Fact]
    public void Consolidate_Works()
    {
        var root = _tempRoot.CreateDirectory();
        var pairBase = root.CreateDirectory("pair");
        var pairA = pairBase.CreateDirectory("a");
        var pairB = pairBase.CreateDirectory("b");
        var other = root.CreateDirectory("other").CreateDirectory("x");

        var watcher = new DefaultFileChangeWatcher(maxWatcherCount: 10);
        using var context = watcher.CreateContext([]);

        for (var i = 0; i < 10 - 2; i++)
        {
            var seed = root.CreateDirectory($"seed{i}");
            context.EnqueueWatchingFile(Path.Combine(seed.Path, "seed.cs"));
        }

        context.EnqueueWatchingFile(Path.Combine(pairA.Path, "one.cs"));
        context.EnqueueWatchingFile(Path.Combine(pairB.Path, "two.cs"));
        context.EnqueueWatchingFile(Path.Combine(other.Path, "three.cs"));

        var watchedPaths = DefaultFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher).ToArray();

        // Consolidation should keep the active watcher count bounded
        Assert.InRange(watchedPaths.Length, 1, 10);
        Assert.All(watchedPaths, w => Assert.Contains("*.cs", w.filters));
    }

    [Fact]
    public void DisposingAllContexts_WatcherCountReturnsToZero()
    {
        var root = _tempRoot.CreateDirectory();
        var watcher = new DefaultFileChangeWatcher();

        var context1 = watcher.CreateContext([new WatchedDirectory(root.Path, extensionFilters: [".cs"])]);
        var context2 = watcher.CreateContext([new WatchedDirectory(root.Path, extensionFilters: [".vb"])]);

        // Also add some individual file watches
        var file1 = context1.EnqueueWatchingFile(Path.Combine(root.Path, "extra.txt"));
        var file2 = context2.EnqueueWatchingFile(Path.Combine(root.Path, "extra2.log"));

        Assert.NotEmpty(DefaultFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));

        file1.Dispose();
        file2.Dispose();
        context1.Dispose();
        context2.Dispose();

        Assert.Empty(DefaultFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));
    }

    [Fact]
    public void DisposingAllContexts_WatcherCountReturnsToZero_EvenIfFileWatchUndisposed()
    {
        var root = _tempRoot.CreateDirectory();
        var watcher = new DefaultFileChangeWatcher();

        var context = watcher.CreateContext([new WatchedDirectory(root.Path, extensionFilters: [".cs"])]);

        // Also add an individual file watch that we will not dispose
        _ = context.EnqueueWatchingFile(Path.Combine(root.Path, "extra.txt"));

        Assert.NotEmpty(DefaultFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));

        context.Dispose();

        Assert.Empty(DefaultFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));
    }

    #endregion

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
        var eventSource = new TaskCompletionSource();

        context.FileChanged += (sender, path) =>
        {
            if (path == filePath)
                eventSource.TrySetResult();
        };

        return new FileChangeTask(eventSource.Task, filePath);
    }

    /// <summary>
    /// Represents a wait for a single file change; includes the file path so failures are easy to understand which one didn't trigger.
    /// </summary>
    private sealed record FileChangeTask(Task Task, string FilePath);

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

    [Fact]
    public void SharedWatcher_MultipleContexts_ShareSameDirectoryWatcher()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var watcher = new DefaultFileChangeWatcher();

        using var context1 = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);
        using var context2 = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [".cs"])]);

        // Both contexts should share the same underlying watcher entry and in this case the filters should be empty since that's needed to
        // cover the directory that has empty filters
        var watchedPath = Assert.Single(DefaultFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));
        Assert.Equal(tempDirectory.Path, watchedPath.path);
        Assert.Empty(watchedPath.filters);
    }

    [Fact]
    public void SharedWatcher_MultipleContexts_ShareSameDirectoryWatcherEvenIfExtraSlashes()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var watcher = new DefaultFileChangeWatcher();

        using var context1 = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);
        string pathWithExtraSeparators = tempDirectory.Path.Replace(Path.DirectorySeparatorChar.ToString(), Path.DirectorySeparatorChar.ToString() + Path.DirectorySeparatorChar);
        using var context2 = watcher.CreateContext([new WatchedDirectory(pathWithExtraSeparators, extensionFilters: [])]);

        // Both contexts should share the same underlying watcher entry
        var watchedPath = Assert.Single(DefaultFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));
        Assert.Equal(tempDirectory.Path, watchedPath.path);
        Assert.Empty(watchedPath.filters);
    }

    [Fact]
    public void SharedWatcher_DisposingOneContext_KeepsWatcherForOther()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var watcher = new DefaultFileChangeWatcher();

        var context1 = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);
        var context2 = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [".cs"])]);

        // Shared watcher should exist
        var watchedPath = Assert.Single(DefaultFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));
        Assert.Equal(tempDirectory.Path, watchedPath.path);

        // Dispose context1
        context1.Dispose();

        // Shared watcher should still exist for context2
        watchedPath = Assert.Single(DefaultFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));
        Assert.Equal(tempDirectory.Path, watchedPath.path);

        // Dispose context2
        context2.Dispose();

        // Now shared watcher should be disposed
        Assert.Empty(DefaultFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));
    }

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

        context1.FileChanged += (sender, path) => context1Events.Add(path);

        // Dispose context1 before creating file
        context1.Dispose();

        // Create file
        File.WriteAllText(filePath, "content");

        // Only context2 should receive the event
        await AssertAllChangesFire([context2Received], s_fileChangeTimeout);
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

        Assert.Empty(DefaultFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));

        // Create new context - should create a new watcher
        using var context2 = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);

        var watchedPath = Assert.Single(DefaultFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));
        Assert.Equal(tempDirectory.Path, watchedPath.path);
    }

    #endregion
}
