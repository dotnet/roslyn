// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.ProjectSystem;
using Roslyn.Utilities;

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
        private static readonly StringComparison s_stringComparison = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        private readonly SimpleFileChangeWatcher _owner;
        private readonly ImmutableArray<WatchedDirectory> _watchedDirectories;

        /// <summary>
        /// Lock to protect access to <see cref="_acquiredRootPaths"/>.
        /// </summary>
        private readonly object _acquiredRootPathsLock = new();

        /// <summary>
        /// The set of root paths this context has acquired watchers for.
        /// </summary>
        private readonly HashSet<string> _acquiredRootPaths = new(s_stringComparer);

        /// <summary>
        /// A lock to guard updates to <see cref="_individualWatchedFiles"/>.
        /// </summary>
        private readonly ReaderWriterLockSlim _watchedFilesLock = new();

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
                TryAcquireRootWatcher(watchedDirectory.Path);
            }

            _watchedDirectories = watchedDirectoryBuilder.ToImmutable();
        }

        public event EventHandler<string>? FileChanged;

        private void TryAcquireRootWatcher(string filePath)
        {
            var rootPath = Path.GetPathRoot(filePath);
            if (rootPath == null || !Directory.Exists(rootPath))
                return;

            lock (_acquiredRootPathsLock)
            {
                if (_acquiredRootPaths.Contains(rootPath))
                    return;

                var sharedWatcher = _owner.AcquireRootWatcher(rootPath);
                sharedWatcher.FileChanged += OnSharedWatcherFileChanged;
                _acquiredRootPaths.Add(rootPath);
            }
        }

        private void OnSharedWatcherFileChanged(object? sender, string filePath)
        {
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
            using (_watchedFilesLock.DisposableWrite())
            {
                _individualWatchedFiles.TryGetValue(filePath, out var existingCount);
                _individualWatchedFiles[filePath] = existingCount + 1;
            }

            // Try to acquire a root watcher that covers this file
            TryAcquireRootWatcher(filePath);

            return new IndividualWatchedFile(filePath, this);
        }

        private void StopWatchingFile(string filePath)
        {
            using (_watchedFilesLock.DisposableWrite())
            {
                if (_individualWatchedFiles.TryGetValue(filePath, out var count))
                {
                    if (count == 1)
                        _individualWatchedFiles.TryRemove(filePath, out _);
                    else
                        _individualWatchedFiles[filePath] = count - 1;
                }
            }
        }

        public void Dispose()
        {
            // Release all acquired root watchers
            lock (_acquiredRootPathsLock)
            {
                foreach (var rootPath in _acquiredRootPaths)
                {
                    // Unsubscribe from the shared watcher's events before releasing
                    if (_owner.TryGetRootWatcher(rootPath, out var sharedWatcher))
                        sharedWatcher.FileChanged -= OnSharedWatcherFileChanged;

                    _owner.ReleaseRootWatcher(rootPath);
                }

                _acquiredRootPaths.Clear();
            }

            _watchedFilesLock.Dispose();
            _individualWatchedFiles.Clear();
        }

        private sealed class IndividualWatchedFile(string filePath, FileChangeContext context) : IWatchedFile
        {
            public void Dispose() => context.StopWatchingFile(filePath);
        }

        internal static class TestAccessor
        {
            public static int GetAcquiredRootPathCount(FileChangeContext context)
                => context._acquiredRootPaths.Count;
        }
    }
}
