// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.FileWatching;

internal sealed partial class DefaultFileChangeWatcher
{
    /// <summary>
    /// A file change context that tracks watched directories and files.
    /// </summary>
    /// <remarks>
    /// Each context tracks which root watchers it has acquired, subscribing to their events
    /// and unsubscribing when disposed. It also tracks individual files being watched outside
    /// of directory watches.
    /// </remarks>
    internal sealed class FileChangeContext : IFileChangeContext, IEventRaiser
    {
        private readonly DefaultFileChangeWatcher _owner;
        private readonly HashSet<WatchedDirectory> _watchedDirectories;
        private readonly ImmutableArray<IReferenceCountedDisposable<ICacheEntry<string, FileSystemWatcher>>> _fileSystemWatchersForWatchedDirectories;
        private readonly HashSet<string> _watchedRootPaths;
        private readonly List<IReferenceCountedDisposable<ICacheEntry<string, FileSystemWatcher>>> _additionalFileSystemWatchersForWatchedDirectories = [];
        private readonly Dictionary<string, int> _watchedFiles;
        private readonly Dictionary<string, int> _watchedDirectoriesByPath;
        private readonly object _gate = new();
        private bool _disposed = false;

        public FileChangeContext(DefaultFileChangeWatcher owner, ImmutableArray<WatchedDirectory> watchedDirectories)
        {
            _owner = owner;

            var watchedRootPaths = new HashSet<string>(s_pathStringComparer);
            var fileSystemWatchersForWatchedDirectoriesBuilder = ImmutableArray.CreateBuilder<IReferenceCountedDisposable<ICacheEntry<string, FileSystemWatcher>>>();
            var watchedDirectoriesByPath = new Dictionary<string, int>(s_pathStringComparer);
            var watchedDirectorySet = new HashSet<WatchedDirectory>();
            foreach (var watchedDirectory in watchedDirectories)
            {
                if (!Directory.Exists(watchedDirectory.Path))
                    continue;

                watchedDirectorySet.Add(watchedDirectory);
                watchedDirectoriesByPath.TryGetValue(watchedDirectory.Path, out var existingCount);
                watchedDirectoriesByPath[watchedDirectory.Path] = existingCount + 1;

                var rootPath = Path.GetPathRoot(watchedDirectory.Path)!;
                if (!watchedRootPaths.Add(rootPath))
                    continue;

                var rootWatcher = _owner.GetOrCreateSharedWatcher(rootPath);
                fileSystemWatchersForWatchedDirectoriesBuilder.Add(rootWatcher);
            }

            _watchedDirectories = watchedDirectorySet;
            _watchedRootPaths = watchedRootPaths;
            _watchedFiles = new Dictionary<string, int>(s_pathStringComparer);
            _watchedDirectoriesByPath = watchedDirectoriesByPath;
            _fileSystemWatchersForWatchedDirectories = fileSystemWatchersForWatchedDirectoriesBuilder.ToImmutable();

            // Attach watchers after fields are assigned to avoid race conditions where events
            // fire before _watchedDirectories is initialized.
            foreach (var rootWatcher in _fileSystemWatchersForWatchedDirectories)
                AttachWatcher(this, rootWatcher);
        }

        public event EventHandler<string>? FileChanged;

        void IEventRaiser.RaiseEvent(object? sender, FileSystemEventArgs e)
        {
            bool shouldRaise;
            string? oldPathToRaise = null;

            lock (_gate)
            {
                shouldRaise = ShouldRaiseForPath_NoLock(e.FullPath);

                if (shouldRaise && e is RenamedEventArgs re)
                    oldPathToRaise = re.OldFullPath;
            }

            if (!shouldRaise)
                return;

            FileChanged?.Invoke(this, e.FullPath);

            if (oldPathToRaise is not null)
                FileChanged?.Invoke(this, oldPathToRaise);
        }

        public IWatchedFile EnqueueWatchingFile(string filePath)
        {
            lock (_gate)
            {
                // If this path is already covered by one of our directory watchers, nothing further to do
                if (ShouldRaiseForPath_NoLock(filePath))
                    return NoOpWatchedFile.Instance;

                // If this path doesn't have a valid root, we can't watch it
                var rootPath = Path.GetPathRoot(filePath);
                if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
                    return NoOpWatchedFile.Instance;

                // Once this context reaches the cap, consolidate by watching the parent directory and
                // stop creating additional individual file watchers.
                if (_watchedFiles.Count >= MaximumWatcherCount)
                {
                    var parentDirectory = Path.GetDirectoryName(filePath);
                    if (parentDirectory is null || !Directory.Exists(parentDirectory))
                        return NoOpWatchedFile.Instance;

                    var watchedDirectory = new WatchedDirectory(parentDirectory, extensionFilters: []);
                    if (_watchedDirectories.Add(watchedDirectory))
                    {
                        if (_watchedRootPaths.Add(rootPath))
                        {
                            var rootWatcher = _owner.GetOrCreateSharedWatcher(rootPath);
                            _additionalFileSystemWatchersForWatchedDirectories.Add(rootWatcher);
                            AttachWatcher(this, rootWatcher);
                        }
                    }

                    _watchedDirectoriesByPath.TryGetValue(watchedDirectory.Path, out var existingCount);
                    _watchedDirectoriesByPath[watchedDirectory.Path] = existingCount + 1;
                    return NoOpWatchedFile.Instance;
                }

                _watchedFiles.TryGetValue(filePath, out var existingWatchCount);
                _watchedFiles[filePath] = existingWatchCount + 1;

                var individualRootWatcher = _owner.GetOrCreateSharedWatcher(rootPath);
                return new IndividualWatchedFile(this, filePath, individualRootWatcher);
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, true) == false)
            {
                lock (_gate)
                {
                    foreach (var rootWatcher in _fileSystemWatchersForWatchedDirectories)
                        DetachAndDisposeWatcher(this, rootWatcher);

                    foreach (var rootWatcher in _additionalFileSystemWatchersForWatchedDirectories)
                        DetachAndDisposeWatcher(this, rootWatcher);
                }
            }
        }

        private void RemoveWatchedFile(string filePath)
        {
            lock (_gate)
            {
                if (!_watchedFiles.TryGetValue(filePath, out var existingWatchCount))
                    return;

                if (existingWatchCount == 1)
                    _watchedFiles.Remove(filePath);
                else
                    _watchedFiles[filePath] = existingWatchCount - 1;
            }
        }

        private bool ShouldRaiseForPath_NoLock(string filePath)
        {
            if (_watchedDirectories.Count == 0)
                return false;

            foreach (var watchedDirectory in _watchedDirectories)
            {
                if (filePath.StartsWith(watchedDirectory.Path, s_pathStringComparison))
                {
                    if (watchedDirectory.ExtensionFilters.Length == 0 ||
                        watchedDirectory.ExtensionFilters.Any(filter => filePath.EndsWith(filter, s_pathStringComparison)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private sealed class IndividualWatchedFile : IWatchedFile, IEventRaiser
        {
            private readonly FileChangeContext _context;
            private readonly string _filePath;
            private readonly IReferenceCountedDisposable<ICacheEntry<string, FileSystemWatcher>> _watcher;
            private bool _disposed = false;

            public IndividualWatchedFile(FileChangeContext context, string filePath, IReferenceCountedDisposable<ICacheEntry<string, FileSystemWatcher>> watcher)
            {
                _context = context;
                _filePath = filePath;
                _watcher = watcher;

                AttachWatcher(this, _watcher);
            }

            void IEventRaiser.RaiseEvent(object? sender, FileSystemEventArgs e)
            {
                if (e.FullPath.Equals(_filePath, s_pathStringComparison))
                {
                    _context.FileChanged?.Invoke(this, e.FullPath);
                }
                else if (e is RenamedEventArgs re && RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                    re.OldFullPath.Equals(_filePath, s_pathStringComparison))
                {
                    // On Windows we only get a renamed event instead of separate delete/create events, so check
                    // whether the old file path matches.
                    _context.FileChanged?.Invoke(this, re.OldFullPath);
                }
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, true) == false)
                {
                    _context.RemoveWatchedFile(_filePath);
                    DetachAndDisposeWatcher(this, _watcher);
                }
            }
        }

        internal static class TestAccessor
        {
            public static ImmutableArray<IReferenceCountedDisposable<ICacheEntry<string, FileSystemWatcher>>> GetRootFileWatchers(FileChangeContext context)
                => context._fileSystemWatchersForWatchedDirectories;

            public static int GetWatchedDirectoryCount(FileChangeContext context)
                => context._watchedDirectoriesByPath.Count;

            public static int GetWatchedFileCount(FileChangeContext context)
                => context._watchedFiles.Count;
        }
    }
}
