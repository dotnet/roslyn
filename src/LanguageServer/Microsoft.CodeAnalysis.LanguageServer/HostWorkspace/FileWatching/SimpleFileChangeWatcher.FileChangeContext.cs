// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.ProjectSystem;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.FileWatching;

internal sealed partial class SimpleFileChangeWatcher
{
    /// <summary>
    /// A file change context that tracks watched directories and files.
    /// </summary>
    /// <remarks>
    /// Each context tracks which root watchers it has acquired, subscribing to their events
    /// and unsubscribing when disposed. It also tracks individual files being watched outside
    /// of directory watches.
    /// </remarks>
    internal sealed class FileChangeContext : IFileChangeContext
    {
        private readonly SimpleFileChangeWatcher _owner;
        private readonly ImmutableArray<WatchedDirectory> _watchedDirectories;
        private readonly ConcurrentDictionary<string, SharedRootWatcher> _sharedWatchers = new(s_stringComparer);

        /// <summary>
        /// The set of individual file paths being watched (outside of directory watches).
        /// Maps file path to number of watchers for that file.
        /// </summary>
        private readonly ConcurrentDictionary<string, int> _individualWatchedFiles = new(s_stringComparer);

        public FileChangeContext(SimpleFileChangeWatcher owner, ImmutableArray<WatchedDirectory> watchedDirectories)
        {
            _owner = owner;

            var watchedDirectoryBuilder = ImmutableArray.CreateBuilder<WatchedDirectory>(watchedDirectories.Length);
            foreach (var watchedDirectory in watchedDirectories)
            {
                if (!Directory.Exists(watchedDirectory.Path))
                    continue;

                watchedDirectoryBuilder.Add(watchedDirectory);
                AcquireSharedWatcher(watchedDirectory.Path);
            }

            _watchedDirectories = watchedDirectoryBuilder.ToImmutable();
        }

        public event EventHandler<string>? FileChanged;

        private bool AcquireSharedWatcher(string? filePath)
        {
            var rootPath = Path.GetPathRoot(filePath);
            if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
                return false;

            var sharedWatcher = _owner.GetOrCreateSharedWatcher(rootPath);
            sharedWatcher.FileChanged += RaiseEvent;
            _sharedWatchers.TryAdd(rootPath, sharedWatcher);
            return true;
        }

        private void RaiseEvent(object? sender, FileSystemEventArgs e)
        {
            var filePath = e.FullPath;
            if (!_watchedDirectories.IsEmpty &&
                WatchedDirectory.FilePathCoveredByWatchedDirectories(_watchedDirectories, filePath, s_stringComparison))
            {
                FileChanged?.Invoke(this, filePath);
            }
            else if (_individualWatchedFiles.ContainsKey(filePath))
            {
                FileChanged?.Invoke(this, filePath);
            }
        }

        public IWatchedFile EnqueueWatchingFile(string filePath)
        {
            // If this path is already covered by one of our directory watchers, nothing further to do
            if (WatchedDirectory.FilePathCoveredByWatchedDirectories(_watchedDirectories, filePath, s_stringComparison))
                return NoOpWatchedFile.Instance;

            // Individual files are ref counted so we know when to stop watching them
            _individualWatchedFiles.AddOrUpdate(filePath, 1, (_, count) => count + 1);

            // Try to acquire a root watcher that covers this file
            AcquireSharedWatcher(filePath);

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
            _individualWatchedFiles.Clear();
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
