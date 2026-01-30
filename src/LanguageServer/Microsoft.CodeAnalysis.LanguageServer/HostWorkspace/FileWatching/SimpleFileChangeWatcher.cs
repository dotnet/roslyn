// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.ProjectSystem;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.FileWatching;

/// <summary>
/// An implementation of <see cref="IFileChangeWatcher" /> that is built atop the framework <see cref="FileSystemWatcher" />. This is used if we can't
/// use the LSP one.
/// </summary>
/// <remarks>
/// This implementation creates one <see cref="FileSystemWatcher"/> per root drive and uses filtering to route file
/// change events to the appropriate watchers. The watchers are shared between all <see cref="FileChangeContext"/>
/// instances and are disposed when all contexts using them have been disposed.
/// </remarks>
internal sealed partial class SimpleFileChangeWatcher : IFileChangeWatcher
{
    private static readonly StringComparer s_stringComparer = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;
    private static readonly StringComparison s_stringComparison = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    /// <summary>
    /// Shared FileSystemWatchers for root paths, with reference counts.
    /// </summary>
    private readonly ConcurrentDictionary<string, SharedRootWatcher> _sharedRootWatchers = new(s_stringComparer);

    public IFileChangeContext CreateContext(ImmutableArray<WatchedDirectory> watchedDirectories)
        => new FileChangeContext(this, watchedDirectories);

    /// <summary>
    /// Acquires a shared watcher for the given root path, creating one if necessary.
    /// </summary>
    private SharedRootWatcher GetOrCreateSharedWatcher(string rootPath)
    {
        return _sharedRootWatchers.AddOrUpdate(
            rootPath,
            new SharedRootWatcher(rootPath),
            (key, existingWatcher) =>
            {
                existingWatcher.AddReference();
                return existingWatcher;
            });
    }

    /// <summary>
    /// Releases a reference to a shared watcher, disposing it if this was the last reference.
    /// </summary>
    private void ReleaseSharedWatcher(SharedRootWatcher watcher)
    {
        if (watcher.Release())
            _sharedRootWatchers.TryRemove(watcher.RootPath, out _);
    }

    /// <summary>
    /// A shared <see cref="FileSystemWatcher"/> that can be used by multiple <see cref="FileChangeContext"/> instances.
    /// Uses reference counting to track how many contexts are using it. Does not use extension filters so it can be
    /// shared across contexts with different filters.
    /// </summary>
    private sealed class SharedRootWatcher
    {
        private readonly FileSystemWatcher _watcher;
        private int _refCount = 1;

        public string RootPath { get; }

        /// <summary>
        /// Event raised when any file in the watched root changes.
        /// </summary>
        public event EventHandler<FileSystemEventArgs>? FileChanged;

        public SharedRootWatcher(string rootPath)
        {
            RootPath = rootPath;

            _watcher = new FileSystemWatcher(rootPath)
            {
                IncludeSubdirectories = true
            };

            _watcher.Changed += OnFileSystemEvent;
            _watcher.Created += OnFileSystemEvent;
            _watcher.Deleted += OnFileSystemEvent;
            _watcher.Renamed += OnFileSystemEvent;

            _watcher.EnableRaisingEvents = true;
        }

        public void AddReference() => Interlocked.Increment(ref _refCount);

        /// <summary>
        /// Releases a reference.
        /// </summary>
        /// <returns>Returns true if the watcher should be removed from the shared dictionary (refcount reached zero).</returns>
        public bool Release()
        {
            if (Interlocked.Decrement(ref _refCount) == 0)
            {
                _watcher.Dispose();
                return true;
            }

            return false;
        }

        private void OnFileSystemEvent(object sender, FileSystemEventArgs e)
            => FileChanged?.Invoke(this, e);
    }

    internal static class TestAccessor
    {
        public static int GetSharedRootWatcherCount(SimpleFileChangeWatcher watcher)
            => watcher._sharedRootWatchers.Count;
    }
}
