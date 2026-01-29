// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.FileWatching;

/// <summary>
/// A trivial implementation of <see cref="IFileChangeWatcher" /> that is built atop the framework <see cref="FileSystemWatcher" />. This is used if we can't
/// use the LSP one.
/// </summary>
/// <remarks>
/// This implementation creates one <see cref="FileSystemWatcher"/> per root drive and uses glob pattern matching
/// to filter and route file change events to the appropriate watchers.
/// </remarks>
internal sealed class SimpleFileChangeWatcher : IFileChangeWatcher
{
    public IFileChangeContext CreateContext(ImmutableArray<WatchedDirectory> watchedDirectories)
        => new FileChangeContext(watchedDirectories);

    internal sealed class FileChangeContext : IFileChangeContext
    {
        private static readonly StringComparison s_stringComparison = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        private static readonly StringComparer s_stringComparer = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

        private readonly ImmutableArray<WatchedDirectory> _watchedDirectories;

        /// <summary>
        /// Maps root paths (drive root on Windows, "/" on Unix) to their FileSystemWatcher.
        /// </summary>
        private readonly ConcurrentDictionary<string, FileSystemWatcher> _rootWatchers = new(s_stringComparer);

        /// <summary>
        /// A lock to guard updates to <see cref="_individualWatchedFiles"/>.
        /// </summary>
        private readonly ReaderWriterLockSlim _watchedFilesLock = new();

        /// <summary>
        /// The set of individual file paths being watched (outside of directory watches).
        /// Maps file path to number of watchers for that file.
        /// </summary>
        private readonly ConcurrentDictionary<string, int> _individualWatchedFiles = new(s_stringComparer);

        public FileChangeContext(ImmutableArray<WatchedDirectory> watchedDirectories)
        {
            var watchedDirectoryBuilder = ImmutableArray.CreateBuilder<WatchedDirectory>(watchedDirectories.Length);

            foreach (var watchedDirectory in watchedDirectories)
            {
                if (!Directory.Exists(watchedDirectory.Path))
                    continue;

                watchedDirectoryBuilder.Add(watchedDirectory);
                TryAddRootWatcher(watchedDirectory.Path);
            }

            _watchedDirectories = watchedDirectoryBuilder.ToImmutable();
        }

        public event EventHandler<string>? FileChanged;

        private void TryAddRootWatcher(string filePath)
        {
            var rootPath = Path.GetPathRoot(filePath);
            if (rootPath != null && !_rootWatchers.ContainsKey(rootPath) && Directory.Exists(rootPath))
            {
                FileSystemWatcher watcher = new(rootPath)
                {
                    IncludeSubdirectories = true
                };

                watcher.Changed += OnFileSystemEvent;
                watcher.Created += OnFileSystemEvent;
                watcher.Deleted += OnFileSystemEvent;
                watcher.Renamed += OnFileSystemEvent;

                watcher.EnableRaisingEvents = true;
                _rootWatchers.Add(rootPath, watcher);
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

            // Try to add a root watcher that covers this file
            TryAddRootWatcher(filePath);

            return new IndividualWatchedFile(filePath, this);
        }

        private void OnFileSystemEvent(object sender, FileSystemEventArgs e)
        {
            var filePath = e.FullPath;

            if (!_watchedDirectories.IsEmpty && WatchedDirectory.FilePathCoveredByWatchedDirectories(_watchedDirectories, filePath, s_stringComparison))
            {
                FileChanged?.Invoke(this, filePath);
            }
            else if (_individualWatchedFiles.ContainsKey(filePath))
            {
                FileChanged?.Invoke(this, filePath);
            }
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
            foreach (var (_, watcher) in _rootWatchers)
            {
                watcher.Changed -= OnFileSystemEvent;
                watcher.Created -= OnFileSystemEvent;
                watcher.Deleted -= OnFileSystemEvent;
                watcher.Renamed -= OnFileSystemEvent;
                watcher.Dispose();
            }

            _rootWatchers.Clear();
            _watchedFilesLock.Dispose();
            _individualWatchedFiles.Clear();
        }

        private sealed class IndividualWatchedFile(string filePath, SimpleFileChangeWatcher.FileChangeContext context) : IWatchedFile
        {
            private readonly string _filePath = filePath;
            private readonly FileChangeContext _context = context;

            public void Dispose()
            {
                _context.StopWatchingFile(_filePath);
            }
        }

        internal static class TestAccessor
        {
            public static int GetRootWatcherCount(FileChangeContext context)
                => context._rootWatchers.Count;
        }
    }
}
