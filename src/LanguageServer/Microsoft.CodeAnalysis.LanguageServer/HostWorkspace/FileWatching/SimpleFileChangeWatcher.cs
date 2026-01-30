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
/// This implementation is not remotely efficient, but is available as a fallback implementation. If this needs to regularly be used, then this should get some improvements.
/// </remarks>
internal sealed partial class SimpleFileChangeWatcher : IFileChangeWatcher
{
    private static readonly StringComparison s_stringComparison = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    private static readonly StringComparer s_stringComparer = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;

    /// <summary>
    /// A dictionary mapping directory paths to shared watchers. This allows multiple <see cref="FileChangeContext"/>
    /// instances watching the same directory to share a single <see cref="FileSystemWatcher"/>.
    /// </summary>
    private readonly ConcurrentDictionary<string, SharedDirectoryWatcher> _sharedDirectoryWatchers = new();

    public IFileChangeContext CreateContext(ImmutableArray<WatchedDirectory> watchedDirectories)
        => new FileChangeContext(watchedDirectories, this);

    /// <summary>
    /// Gets or creates a shared watcher for the given directory path.
    /// </summary>
    private SharedDirectoryWatcher GetOrCreateSharedWatcher(string directoryPath)
    {
        return _sharedDirectoryWatchers.AddOrUpdate(
            directoryPath,
            new SharedDirectoryWatcher(directoryPath),
            (key, existingWatcher) =>
            {
                existingWatcher.AddRef();
                return existingWatcher;
            });
    }

    /// <summary>
    /// Releases a reference to a shared watcher. When the reference count reaches zero, the watcher is disposed
    /// and removed from the shared dictionary.
    /// </summary>
    private void ReleaseSharedWatcher(SharedDirectoryWatcher watcher)
    {
        if (watcher.Release())
        {
            _sharedDirectoryWatchers.TryRemove(watcher.DirectoryPath, out _);
        }
    }

    /// <summary>
    /// A shared <see cref="FileSystemWatcher"/> that can be used by multiple <see cref="FileChangeContext"/> instances.
    /// Uses reference counting to track how many contexts are using it. Does not use extension filters so it can be
    /// shared across contexts with different filters.
    /// </summary>
    private sealed class SharedDirectoryWatcher
    {
        private readonly FileSystemWatcher _watcher;
        private int _refCount = 1;

        public string DirectoryPath { get; }

        public event EventHandler<FileSystemEventArgs>? FileChanged;

        public SharedDirectoryWatcher(string directoryPath)
        {
            DirectoryPath = directoryPath;

            _watcher = new(directoryPath)
            {
                IncludeSubdirectories = true
            };

            _watcher.Changed += RaiseEvent;
            _watcher.Created += RaiseEvent;
            _watcher.Deleted += RaiseEvent;
            _watcher.Renamed += RaiseEvent;

            _watcher.EnableRaisingEvents = true;
        }

        public void AddRef() => Interlocked.Increment(ref _refCount);

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

        private void RaiseEvent(object sender, FileSystemEventArgs e)
        {
            FileChanged?.Invoke(this, e);
        }
    }

    internal static class TestAccessor
    {
        public static int GetSharedDirectoryWatcherCount(SimpleFileChangeWatcher watcher)
            => watcher._sharedDirectoryWatchers.Count;
    }
}
