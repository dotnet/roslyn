// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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

    /// <summary>
    /// Lock to protect access to <see cref="_sharedRootWatchers"/>.
    /// </summary>
    private readonly object _sharedWatchersLock = new();

    /// <summary>
    /// Shared FileSystemWatchers for root paths, with reference counts.
    /// </summary>
    private readonly Dictionary<string, SharedRootWatcher> _sharedRootWatchers = new(s_stringComparer);

    public IFileChangeContext CreateContext(ImmutableArray<WatchedDirectory> watchedDirectories)
        => new FileChangeContext(this, watchedDirectories);

    /// <summary>
    /// Acquires a shared watcher for the given root path, creating one if necessary.
    /// </summary>
    private SharedRootWatcher AcquireRootWatcher(string rootPath)
    {
        lock (_sharedWatchersLock)
        {
            if (_sharedRootWatchers.TryGetValue(rootPath, out var existingWatcher))
            {
                existingWatcher.IncrementRefCount();
                return existingWatcher;
            }

            var newWatcher = new SharedRootWatcher(rootPath);
            _sharedRootWatchers[rootPath] = newWatcher;
            return newWatcher;
        }
    }

    /// <summary>
    /// Tries to get an existing shared watcher for the given root path.
    /// </summary>
    private bool TryGetRootWatcher(string rootPath, [NotNullWhen(returnValue: true)] out SharedRootWatcher? watcher)
    {
        lock (_sharedWatchersLock)
        {
            _sharedRootWatchers.TryGetValue(rootPath, out var existingWatcher);
            watcher = existingWatcher;
            return existingWatcher != null;
        }
    }

    /// <summary>
    /// Releases a reference to a shared watcher, disposing it if this was the last reference.
    /// </summary>
    private void ReleaseRootWatcher(string rootPath)
    {
        lock (_sharedWatchersLock)
        {
            if (_sharedRootWatchers.TryGetValue(rootPath, out var watcher))
            {
                if (watcher.DecrementRefCount() == 0)
                {
                    watcher.Dispose();
                    _sharedRootWatchers.Remove(rootPath);
                }
            }
        }
    }

    /// <summary>
    /// A shared FileSystemWatcher with reference counting.
    /// </summary>
    private sealed class SharedRootWatcher : IDisposable
    {
        private readonly FileSystemWatcher _watcher;
        private int _refCount = 1;

        /// <summary>
        /// Event raised when any file in the watched root changes.
        /// </summary>
        public event EventHandler<string>? FileChanged;

        public SharedRootWatcher(string rootPath)
        {
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

        public void IncrementRefCount()
        {
            Interlocked.Increment(ref _refCount);
        }

        public int DecrementRefCount()
        {
            return Interlocked.Decrement(ref _refCount);
        }

        private void OnFileSystemEvent(object sender, FileSystemEventArgs e)
        {
            FileChanged?.Invoke(this, e.FullPath);
        }

        public void Dispose()
        {
            _watcher.Changed -= OnFileSystemEvent;
            _watcher.Created -= OnFileSystemEvent;
            _watcher.Deleted -= OnFileSystemEvent;
            _watcher.Renamed -= OnFileSystemEvent;
            _watcher.Dispose();
        }
    }

    internal static class TestAccessor
    {
        public static int GetSharedRootWatcherCount(SimpleFileChangeWatcher watcher)
        {
            lock (watcher._sharedWatchersLock)
            {
                return watcher._sharedRootWatchers.Count;
            }
        }
    }
}
