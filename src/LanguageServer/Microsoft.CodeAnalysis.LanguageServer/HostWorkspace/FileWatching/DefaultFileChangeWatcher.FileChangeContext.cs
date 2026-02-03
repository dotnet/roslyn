// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
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
    internal sealed class FileChangeContext : IFileChangeContext
    {
        private readonly DefaultFileChangeWatcher _owner;
        private readonly ImmutableArray<WatchedDirectory> _watchedDirectories;
        private readonly ImmutableArray<IReferenceCountedDisposable<ICacheEntry<string, FileSystemWatcher>>> _rootFileSystemWatchers;
        private bool _disposed = false;

        public FileChangeContext(DefaultFileChangeWatcher owner, ImmutableArray<WatchedDirectory> watchedDirectories)
        {
            _owner = owner;

            var acquiredRoot = new HashSet<string>(s_pathStringComparer);
            var rootFileSystemWatchersBuilder = ImmutableArray.CreateBuilder<IReferenceCountedDisposable<ICacheEntry<string, FileSystemWatcher>>>();
            var watchedDirectoryBuilder = ImmutableArray.CreateBuilder<WatchedDirectory>(watchedDirectories.Length);
            foreach (var watchedDirectory in watchedDirectories)
            {
                if (!Directory.Exists(watchedDirectory.Path))
                    continue;

                watchedDirectoryBuilder.Add(watchedDirectory);

                var rootPath = Path.GetPathRoot(watchedDirectory.Path);
                if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
                    continue;

                if (!acquiredRoot.Add(rootPath))
                    continue;

                var rootWatcher = _owner.GetOrCreateSharedWatcher(rootPath);
                rootWatcher.Target.Value.Changed += RaiseEvent;
                rootWatcher.Target.Value.Created += RaiseEvent;
                rootWatcher.Target.Value.Deleted += RaiseEvent;
                rootWatcher.Target.Value.Renamed += RaiseEvent;
                rootFileSystemWatchersBuilder.Add(rootWatcher);
            }

            _watchedDirectories = watchedDirectoryBuilder.ToImmutable();
            _rootFileSystemWatchers = rootFileSystemWatchersBuilder.ToImmutable();
        }

        public event EventHandler<string>? FileChanged;

        private void RaiseEvent(object? sender, FileSystemEventArgs e)
        {
            if (_watchedDirectories.IsEmpty)
                return;

            if (WatchedDirectory.FilePathCoveredByWatchedDirectories(_watchedDirectories, e.FullPath, s_pathStringComparison))
            {
                FileChanged?.Invoke(this, e.FullPath);

                // On Windows we only get a renamed event instead of separate delete/create events, so also raise
                // a change event for the old file path.
                if (e is RenamedEventArgs re && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    FileChanged?.Invoke(this, re.OldFullPath);
            }
        }

        public IWatchedFile EnqueueWatchingFile(string filePath)
        {
            // If this path is already covered by one of our directory watchers, nothing further to do
            if (WatchedDirectory.FilePathCoveredByWatchedDirectories(_watchedDirectories, filePath, s_pathStringComparison))
                return NoOpWatchedFile.Instance;

            // If this path doesn't have a valid root, we can't watch it
            var rootPath = Path.GetPathRoot(filePath);
            if (string.IsNullOrEmpty(rootPath) || !Directory.Exists(rootPath))
                return NoOpWatchedFile.Instance;

            var rootWatcher = _owner.GetOrCreateSharedWatcher(rootPath);
            return new IndividualWatchedFile(this, filePath, rootWatcher);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, true) == false)
            {
                foreach (var rootWatcher in _rootFileSystemWatchers)
                {
                    rootWatcher.Target.Value.Changed -= RaiseEvent;
                    rootWatcher.Target.Value.Created -= RaiseEvent;
                    rootWatcher.Target.Value.Deleted -= RaiseEvent;
                    rootWatcher.Target.Value.Renamed -= RaiseEvent;
                    rootWatcher.Dispose();
                }
            }
        }

        private sealed class IndividualWatchedFile : IWatchedFile
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

                _watcher.Target.Value.Changed += RaiseEvent;
                _watcher.Target.Value.Created += RaiseEvent;
                _watcher.Target.Value.Deleted += RaiseEvent;
                _watcher.Target.Value.Renamed += RaiseEvent;
            }

            private void RaiseEvent(object? sender, FileSystemEventArgs e)
            {
                if (e.FullPath.Equals(_filePath, s_pathStringComparison))
                {
                    _context.FileChanged?.Invoke(this, e.FullPath);
                }
                // On Windows we only get a renamed event instead of separate delete/create events, so check
                // whether the old file path matches.
                else if (e is RenamedEventArgs re && RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
                    re.OldFullPath.Equals(_filePath, s_pathStringComparison))
                {
                    _context.FileChanged?.Invoke(this, re.OldFullPath);
                }
            }

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, true) == false)
                {
                    _watcher.Target.Value.Changed -= RaiseEvent;
                    _watcher.Target.Value.Created -= RaiseEvent;
                    _watcher.Target.Value.Deleted -= RaiseEvent;
                    _watcher.Target.Value.Renamed -= RaiseEvent;
                    _watcher.Dispose();
                }
            }
        }

        internal static class TestAccessor
        {
            public static ImmutableArray<IReferenceCountedDisposable<ICacheEntry<string, FileSystemWatcher>>> GetRootFileWatchers(FileChangeContext context)
                => context._rootFileSystemWatchers;
        }
    }
}
