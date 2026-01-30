// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.ProjectSystem;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.FileWatching;

/// <summary>
/// A trivial implementation of <see cref="IFileChangeWatcher" /> that is built atop the framework <see cref="FileSystemWatcher" />. This is used if we can't
/// use the LSP one.
/// </summary>
/// <remarks>
/// This implementation is not remotely efficient, but is available as a fallback implementation. If this needs to regularly be used, then this should get some improvements.
/// </remarks>
internal sealed partial class SimpleFileChangeWatcher
{
    internal sealed class FileChangeContext : IFileChangeContext
    {
        private readonly SimpleFileChangeWatcher _owner;
        private readonly ImmutableArray<WatchedDirectory> _watchedDirectories;
        private readonly ConcurrentDictionary<string, SharedDirectoryWatcher> _sharedWatchers = new(s_stringComparer);

        /// <summary>
        /// The set of individual file paths being watched (outside of directory watches).
        /// Maps file path to number of watchers for that file.
        /// </summary>
        private readonly ConcurrentDictionary<string, int> _individualWatchedFiles = new(s_stringComparer);

        public FileChangeContext(ImmutableArray<WatchedDirectory> watchedDirectories, SimpleFileChangeWatcher owner)
        {
            _owner = owner;

            var watchedDirectoriesBuilder = ImmutableArray.CreateBuilder<WatchedDirectory>(watchedDirectories.Length);

            foreach (var watchedDirectory in watchedDirectories)
            {
                if (AcquireSharedWatcher(watchedDirectory.Path))
                    watchedDirectoriesBuilder.Add(watchedDirectory);
            }

            _watchedDirectories = watchedDirectoriesBuilder.ToImmutable();
        }

        public event EventHandler<string>? FileChanged;

        private bool AcquireSharedWatcher(string? directoryPath)
        {
            if (directoryPath == null || !Directory.Exists(directoryPath))
                return false;

            if (_sharedWatchers.ContainsKey(directoryPath))
                return true;

            var sharedWatcher = _owner.GetOrCreateSharedWatcher(directoryPath);
            sharedWatcher.FileChanged += RaiseEvent;
            _sharedWatchers.TryAdd(directoryPath, sharedWatcher);
            return true;
        }

        private void RaiseEvent(object? sender, FileSystemEventArgs e)
        {
            if (!_watchedDirectories.IsEmpty &&
                WatchedDirectory.FilePathCoveredByWatchedDirectories(_watchedDirectories, e.FullPath, StringComparison.Ordinal))
            {
                FileChanged?.Invoke(this, e.FullPath);
            }
            else if (_individualWatchedFiles.ContainsKey(e.FullPath))
            {
                FileChanged?.Invoke(this, e.FullPath);
            }
        }

        public IWatchedFile EnqueueWatchingFile(string filePath)
        {
            // If this path is already covered by one of our directory watchers, nothing further to do
            if (WatchedDirectory.FilePathCoveredByWatchedDirectories(_watchedDirectories, filePath, s_stringComparison))
                return NoOpWatchedFile.Instance;

            // Individual files are ref counted so we know when to stop watching them
            _individualWatchedFiles.AddOrUpdate(filePath, 1, (_, count) => count + 1);

            AcquireSharedWatcher(Path.GetDirectoryName(filePath));

            return new IndividualWatchedFile(filePath, this);
        }

        private void StopWatchingFile(string filePath)
        {
            if (_individualWatchedFiles.AddOrUpdate(filePath, 0, (_, count) => count - 1) == 0)
                _individualWatchedFiles.TryRemove(filePath, out _);
        }

        public void Dispose()
        {
            foreach (var (_, sharedWatcher) in _sharedWatchers)
            {
                sharedWatcher.FileChanged -= RaiseEvent;
                _owner.ReleaseSharedWatcher(sharedWatcher);
            }

            _sharedWatchers.Clear();
        }

        private sealed class IndividualWatchedFile(string filePath, FileChangeContext context) : IWatchedFile
        {
            public void Dispose() => context.StopWatchingFile(filePath);
        }

        internal static class TestAccessor
        {
            public static int GetSharedWatcherCount(FileChangeContext context)
                => context._sharedWatchers.Count;
        }
    }
}
