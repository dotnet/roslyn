// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.FileWatching;
using Microsoft.CodeAnalysis.ProjectSystem;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.UnitTests;

public sealed class AbstractConsolidatingFileChangeWatcherTests : IDisposable
{
    private readonly TempRoot _tempRoot = new();

    public void Dispose() => _tempRoot.Dispose();

    #region Watch Consolidation Tests

    [Fact]
    public void CreateContext_WithEmptyDirectories_DoesNotAddWatchers()
    {
        var watcher = new TestFileChangeWatcher();

        using var context = watcher.CreateContext([]);

        // Empty directory lists should not register any shared watchers
        Assert.Empty(TestFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));
    }

    [Fact]
    public void CreateContext_WithExistingDirectory_AddsDirectoryWatcher()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var watcher = new TestFileChangeWatcher();

        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);

        // Watching one real directory should create one shared watcher
        var watchedDirectory = Assert.Single(TestFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));
        Assert.Equal(tempDirectory.Path, watchedDirectory.path);
        Assert.Empty(watchedDirectory.filters);
        Assert.True(watchedDirectory.includeSubdirectories);
    }

    [Fact]
    public void CreateContext_WithNonExistentDirectory_WatchesAHigherLevel()
    {
        var nonExistentPath = Path.Combine(TempRoot.Root, "NonExistent", "Directory", Guid.NewGuid().ToString());
        var watcher = new TestFileChangeWatcher();

        using var context = watcher.CreateContext([new WatchedDirectory(nonExistentPath, extensionFilters: [])]);

        // A single watch should be created, but in this case it'd be the TempRoot.Root
        var watchedDirectory = Assert.Single(TestFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));
        Assert.Equal(TempRoot.Root, watchedDirectory.path);
        Assert.Empty(watchedDirectory.filters);
        Assert.True(watchedDirectory.includeSubdirectories);
    }

    [Fact]
    public void CreateContext_WithContainedDirectory_ConsolidatesImmediately_ParentFirst()
    {
        var root = _tempRoot.CreateDirectory();
        var child = root.CreateDirectory("child");
        var watcher = new TestFileChangeWatcher();

        using var context = watcher.CreateContext([
            new WatchedDirectory(root.Path, extensionFilters: []),
            new WatchedDirectory(child.Path, extensionFilters: [])
        ]);

        // The child directory is covered by the parent directory watcher
        var watchedDirectory = Assert.Single(TestFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));
        Assert.Equal(root.Path, watchedDirectory.path);
        Assert.Empty(watchedDirectory.filters);
        Assert.True(watchedDirectory.includeSubdirectories);
    }

    [Fact]
    public void CreateContext_WithContainedDirectory_ConsolidatesImmediately_ChildFirst()
    {
        var root = _tempRoot.CreateDirectory();
        var child = root.CreateDirectory("child");
        var watcher = new TestFileChangeWatcher();

        using var context = watcher.CreateContext([
            new WatchedDirectory(child.Path, extensionFilters: []),
            new WatchedDirectory(root.Path, extensionFilters: [])
        ]);

        // The child directory is covered by the parent directory watcher
        var watchedDirectory = Assert.Single(TestFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));
        Assert.Equal(root.Path, watchedDirectory.path);
        Assert.Empty(watchedDirectory.filters);
        Assert.True(watchedDirectory.includeSubdirectories);
    }

    [Fact]
    public void CreateContext_WithPartiallyCoveredChildDirectory_UsesSharedAncestorWatcher()
    {
        var root = _tempRoot.CreateDirectory();
        var child = root.CreateDirectory("child");
        var watcher = new TestFileChangeWatcher();

        using var context = watcher.CreateContext([
            new WatchedDirectory(root.Path, extensionFilters: [".cs"]),
            new WatchedDirectory(child.Path, extensionFilters: [".vb"])
        ]);

        // Different filters still share the same underlying watcher for the directory tree
        var watchedDirectory = Assert.Single(TestFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));
        Assert.Equal(root.Path, watchedDirectory.path);
        AssertEx.SetEqual(watchedDirectory.filters, ["*.cs", "*.vb"]);
        Assert.True(watchedDirectory.includeSubdirectories);
    }

    [Fact]
    public void EnqueueWatchingFile_InWatchedDirectory_ReturnsNoOpWatcher()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "test.cs");
        var watcher = new TestFileChangeWatcher();

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
        var watcher = new TestFileChangeWatcher();

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
        var watcher = new TestFileChangeWatcher();

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
        var watcher = new TestFileChangeWatcher();

        using var context = watcher.CreateContext([]);
        using var watchedFile = context.EnqueueWatchingFile(filePath);

        // The parent directory becomes watched when we enqueue a file directly
        Assert.NotSame(NoOpWatchedFile.Instance, watchedFile);
        var watchedDirectory = Assert.Single(TestFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));
        Assert.Equal(tempDirectory.Path, watchedDirectory.path);
        Assert.Equal("*.cs", Assert.Single(watchedDirectory.filters));
        Assert.False(watchedDirectory.includeSubdirectories);
    }

    [Fact]
    public void EnqueueWatchingFile_MultipleFilesSameDirectory_UsesSingleWatcher()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var watcher = new TestFileChangeWatcher();
        using var context = watcher.CreateContext([]);

        using var watchedFile1 = context.EnqueueWatchingFile(Path.Combine(tempDirectory.Path, "a.cs"));
        using var watchedFile2 = context.EnqueueWatchingFile(Path.Combine(tempDirectory.Path, "b.cs"));

        // Multiple files in the same directory should share a single underlying watcher
        Assert.NotSame(NoOpWatchedFile.Instance, watchedFile1);
        Assert.NotSame(NoOpWatchedFile.Instance, watchedFile2);
        var watchedDirectory = Assert.Single(TestFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));

        Assert.Equal(tempDirectory.Path, watchedDirectory.path);
        Assert.Equal("*.cs", Assert.Single(watchedDirectory.filters));
        Assert.False(watchedDirectory.includeSubdirectories);
    }

    [Fact]
    public void EnqueueWatchingFile_FileInExistingSubdirectory_UsesSeparateNonRecursiveWatchers()
    {
        var root = _tempRoot.CreateDirectory();
        var child = root.CreateDirectory("child");
        var watcher = new TestFileChangeWatcher();
        using var context = watcher.CreateContext([]);

        using var rootFileWatch = context.EnqueueWatchingFile(Path.Combine(root.Path, "root.cs"));
        using var childFileWatch = context.EnqueueWatchingFile(Path.Combine(child.Path, "child.cs"));

        var watchedDirectories = TestFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher).ToArray();
        Assert.Equal(2, watchedDirectories.Length);

        Assert.All(watchedDirectories, w => Assert.Equal("*.cs", Assert.Single(w.filters)));
        Assert.All(watchedDirectories, w => Assert.False(w.includeSubdirectories));
    }

    [Fact]
    public void EnqueueWatchingFile_MultipleTimesForSameFile_AllReturnDisposableWatchers()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var filePath = Path.Combine(tempDirectory.Path, "multi.txt");
        var watcher = new TestFileChangeWatcher();

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
        var watcher = new TestFileChangeWatcher();

        using var context = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);
        using var watchedFile = context.EnqueueWatchingFile(filePath);

        // File in subdirectory should be covered by parent directory watch
        Assert.Same(NoOpWatchedFile.Instance, watchedFile);
    }

    [Fact]
    public void EnqueueWatchingFile_WithNonExistentDirectory_HandlesGracefully()
    {
        var watcher = new TestFileChangeWatcher();
        var nonExistentPath = Path.Combine(TempRoot.Root, "NonExistent", "file.cs");

        using var context = watcher.CreateContext([]);
        using var watchedFile = context.EnqueueWatchingFile(nonExistentPath);
        Assert.NotNull(watchedFile);

        var watchedDirectory = Assert.Single(TestFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));
        Assert.Equal(TempRoot.Root, watchedDirectory.path);
        Assert.True(watchedDirectory.includeSubdirectories);
    }

    [Fact]
    public void EnqueueWatchingFile_WithNonExistentDirectory_MergesWithParentWatch_SameExtension()
    {
        var watcher = new TestFileChangeWatcher();
        var nonExistentPath = Path.Combine(TempRoot.Root, "NonExistent", "file.cs");

        using var context = watcher.CreateContext([]);
        using var fileInExistingDirectory = context.EnqueueWatchingFile(Path.Combine(TempRoot.Root, "file.cs"));
        var watchedDirectory = Assert.Single(TestFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));
        Assert.Equal(TempRoot.Root, watchedDirectory.path);
        Assert.False(watchedDirectory.includeSubdirectories);

        using var fileInNonExistingDirectory = context.EnqueueWatchingFile(nonExistentPath);

        // We should still have a single watch, but was converted to watch directories
        watchedDirectory = Assert.Single(TestFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));
        Assert.Equal(TempRoot.Root, watchedDirectory.path);
        Assert.True(watchedDirectory.includeSubdirectories);
        Assert.Equal("*.cs", Assert.Single(watchedDirectory.filters));
    }

    [Fact]
    public void EnqueueWatchingFile_WithNonExistentDirectory_MergesWithParentWatch_DifferentExtension()
    {
        var watcher = new TestFileChangeWatcher();
        var nonExistentPath = Path.Combine(TempRoot.Root, "NonExistent", "file.cs");

        using var context = watcher.CreateContext([]);
        using var fileInExistingDirectory = context.EnqueueWatchingFile(Path.Combine(TempRoot.Root, "file.vb"));
        var watchedDirectory = Assert.Single(TestFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));
        Assert.Equal(TempRoot.Root, watchedDirectory.path);
        Assert.False(watchedDirectory.includeSubdirectories);

        using var fileInNonExistingDirectory = context.EnqueueWatchingFile(nonExistentPath);

        // We should still have a single watch, but was converted to watch directories
        watchedDirectory = Assert.Single(TestFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));
        Assert.Equal(TempRoot.Root, watchedDirectory.path);
        Assert.True(watchedDirectory.includeSubdirectories);
        AssertEx.SetEqual(["*.cs", "*.vb"], watchedDirectory.filters);
    }

    [Fact]
    public void EnqueueWatchingFile_WithMultipleExtensionFilters_MatchesAny()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var csFilePath = Path.Combine(tempDirectory.Path, "test.cs");
        var vbFilePath = Path.Combine(tempDirectory.Path, "test.vb");
        var txtFilePath = Path.Combine(tempDirectory.Path, "test.txt");
        var watcher = new TestFileChangeWatcher();

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
        var watcher = new TestFileChangeWatcher();

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
        var watcher = new TestFileChangeWatcher();

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
        var watcher = new TestFileChangeWatcher();

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
        var watcher = new TestFileChangeWatcher();

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

        var watcher = new TestFileChangeWatcher(maxWatcherCount: 10);
        using var context = watcher.CreateContext([]);

        for (var i = 0; i < 10 - 2; i++)
        {
            var seed = root.CreateDirectory($"seed{i}");
            context.EnqueueWatchingFile(Path.Combine(seed.Path, "seed.cs"));
        }

        context.EnqueueWatchingFile(Path.Combine(pairA.Path, "one.cs"));
        context.EnqueueWatchingFile(Path.Combine(pairB.Path, "two.cs"));
        context.EnqueueWatchingFile(Path.Combine(other.Path, "three.cs"));

        var watchedPaths = TestFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher).ToArray();

        // Consolidation should keep the active watcher count bounded
        Assert.InRange(watchedPaths.Length, 1, 10);
        Assert.All(watchedPaths, w => Assert.Contains("*.cs", w.filters));
        Assert.Contains(watchedPaths, w => w.includeSubdirectories);
    }

    [Fact]
    public void Consolidate_Works_FileWatchedFirst()
    {
        var root = _tempRoot.CreateDirectory();

        var watcher = new TestFileChangeWatcher(maxWatcherCount: 10);
        using var context = watcher.CreateContext([]);
        using var firstFile = context.EnqueueWatchingFile(Path.Combine(root.Path, "root.cs"));

        for (var i = 0; i < 20; i++)
        {
            var subdirectory = root.CreateDirectory($"subdir{i}");
            context.EnqueueWatchingFile(Path.Combine(subdirectory.Path, "file.cs"));
        }

        var watchedPaths = TestFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher).ToArray();

        // We would have had to consolidate to a single watcher at the root
        var watchedPath = Assert.Single(watchedPaths);
        Assert.Equal(root.Path, watchedPath.path);
        Assert.True(watchedPath.includeSubdirectories);
    }

    [Fact]
    public void Consolidate_WithExistingNonRecursiveWatcher_MergesFilter()
    {
        var root = _tempRoot.CreateDirectory();
        var watcher = new TestFileChangeWatcher(maxWatcherCount: 10);
        using var context = watcher.CreateContext([]);

        using var firstWatch = context.EnqueueWatchingFile(Path.Combine(root.Path, "one.cs"));
        using var secondWatch = context.EnqueueWatchingFile(Path.Combine(root.Path, "two.vb"));

        var watchedDirectory = Assert.Single(TestFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));
        Assert.Equal(root.Path, watchedDirectory.path);
        AssertEx.SetEqual(watchedDirectory.filters, ["*.cs", "*.vb"]);
        Assert.False(watchedDirectory.includeSubdirectories);
    }

    [Fact]
    public void DisposingAllContexts_WatcherCountReturnsToZero()
    {
        var root = _tempRoot.CreateDirectory();
        var watcher = new TestFileChangeWatcher();

        var context1 = watcher.CreateContext([new WatchedDirectory(root.Path, extensionFilters: [".cs"])]);
        var context2 = watcher.CreateContext([new WatchedDirectory(root.Path, extensionFilters: [".vb"])]);

        // Also add some individual file watches
        var file1 = context1.EnqueueWatchingFile(Path.Combine(root.Path, "extra.txt"));
        var file2 = context2.EnqueueWatchingFile(Path.Combine(root.Path, "extra2.log"));

        Assert.NotEmpty(TestFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));

        file1.Dispose();
        file2.Dispose();
        context1.Dispose();
        context2.Dispose();

        Assert.Empty(TestFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));
    }

    [Fact]
    public void DisposingAllContexts_WatcherCountReturnsToZero_EvenIfFileWatchUndisposed()
    {
        var root = _tempRoot.CreateDirectory();
        var watcher = new TestFileChangeWatcher();

        var context = watcher.CreateContext([new WatchedDirectory(root.Path, extensionFilters: [".cs"])]);

        // Also add an individual file watch that we will not dispose
        _ = context.EnqueueWatchingFile(Path.Combine(root.Path, "extra.txt"));

        Assert.NotEmpty(TestFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));

        context.Dispose();

        Assert.Empty(TestFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));
    }

    #endregion

    #region Shared Watcher Tests

    [Fact]
    public void SharedWatcher_MultipleContexts_ShareSameDirectoryWatcher()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var watcher = new TestFileChangeWatcher();

        using var context1 = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);
        using var context2 = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [".cs"])]);

        // Both contexts should share the same underlying watcher entry and in this case the filters should be empty since that's needed to
        // cover the directory that has empty filters
        var watchedPath = Assert.Single(TestFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));
        Assert.Equal(tempDirectory.Path, watchedPath.path);
        Assert.Empty(watchedPath.filters);
        Assert.True(watchedPath.includeSubdirectories);
    }

    [Fact]
    public void SharedWatcher_MultipleContexts_FileAndDirectoryShareSameWatcherAndFilter()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var watcher = new TestFileChangeWatcher();

        using var context1 = watcher.CreateContext([]);
        using var file = context1.EnqueueWatchingFile(Path.Combine(tempDirectory.Path, "file.cs"));

        using var context2 = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [".cs"])]);

        var watchedPath = Assert.Single(TestFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));
        Assert.Equal(tempDirectory.Path, watchedPath.path);
        AssertEx.SetEqual(["*.cs"], watchedPath.filters);
        Assert.True(watchedPath.includeSubdirectories);
    }

    [Fact]
    public void SharedWatcher_MultipleContexts_FileAndDirectoryShareSameWatcherButClearsFilter()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var watcher = new TestFileChangeWatcher();

        using var context1 = watcher.CreateContext([]);
        using var file = context1.EnqueueWatchingFile(Path.Combine(tempDirectory.Path, "file.cs"));

        using var context2 = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, [])]);

        var watchedPath = Assert.Single(TestFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));
        Assert.Equal(tempDirectory.Path, watchedPath.path);
        Assert.Empty(watchedPath.filters);
        Assert.True(watchedPath.includeSubdirectories);
    }

    [Fact]
    public void SharedWatcher_MultipleContexts_ShareSameDirectoryWatcherEvenIfExtraSlashes()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var watcher = new TestFileChangeWatcher();

        using var context1 = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);
        string pathWithExtraSeparators = tempDirectory.Path.Replace(Path.DirectorySeparatorChar.ToString(), Path.DirectorySeparatorChar.ToString() + Path.DirectorySeparatorChar);
        using var context2 = watcher.CreateContext([new WatchedDirectory(pathWithExtraSeparators, extensionFilters: [])]);

        // Both contexts should share the same underlying watcher entry
        var watchedPath = Assert.Single(TestFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));
        Assert.Equal(tempDirectory.Path, watchedPath.path);
        Assert.Empty(watchedPath.filters);
        Assert.True(watchedPath.includeSubdirectories);
    }

    [Fact]
    public void SharedWatcher_DisposingOneContext_KeepsWatcherForOther()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var watcher = new TestFileChangeWatcher();

        var context1 = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);
        var context2 = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [".cs"])]);

        // Shared watcher should exist
        var watchedPath = Assert.Single(TestFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));
        Assert.Equal(tempDirectory.Path, watchedPath.path);
        Assert.True(watchedPath.includeSubdirectories);

        // Dispose context1
        context1.Dispose();

        // Shared watcher should still exist for context2
        watchedPath = Assert.Single(TestFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));
        Assert.Equal(tempDirectory.Path, watchedPath.path);
        Assert.True(watchedPath.includeSubdirectories);

        // Dispose context2
        context2.Dispose();

        // Now shared watcher should be disposed
        Assert.Empty(TestFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));
    }

    [Fact]
    public void SharedWatcher_NewContextAfterDispose_CreatesNewWatcher()
    {
        var tempDirectory = _tempRoot.CreateDirectory();
        var watcher = new TestFileChangeWatcher();

        // Create and dispose first context
        var context1 = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);
        context1.Dispose();

        Assert.Empty(TestFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));

        // Create new context - should create a new watcher
        using var context2 = watcher.CreateContext([new WatchedDirectory(tempDirectory.Path, extensionFilters: [])]);

        var watchedPath = Assert.Single(TestFileChangeWatcher.TestAccessor.GetWatchedDirectories(watcher));
        Assert.Equal(tempDirectory.Path, watchedPath.path);
        Assert.True(watchedPath.includeSubdirectories);
    }

    #endregion

    private sealed class TestFileChangeWatcher(int maxWatcherCount = 1000) : AbstractConsolidatingFileChangeWatcher(maxWatcherCount)
    {
        protected override bool CanWatchDirectory(string path) => Directory.Exists(path);

        protected override IDirectoryWatcher CreateDirectoryWatcher(string path, ImmutableArray<string> filters, bool includeSubdirectories)
            => new TestDirectoryWatcher(filters, includeSubdirectories);

        private sealed class TestDirectoryWatcher(ImmutableArray<string> filters, bool includeSubdirectories) : IDirectoryWatcher
        {
            public IReadOnlyList<string> Filters { get; private set; } = filters;
            public bool IncludeSubdirectories { get; private set; } = includeSubdirectories;

            public event EventHandler<FileChangedEventArgs>? FileChanged
            {
                add { }
                remove { }
            }

            public void Update(ImmutableArray<string> filters, bool includeSubdirectories)
            {
                Filters = filters;
                IncludeSubdirectories = includeSubdirectories;
            }

            public void Dispose()
            {
            }
        }
    }
}
