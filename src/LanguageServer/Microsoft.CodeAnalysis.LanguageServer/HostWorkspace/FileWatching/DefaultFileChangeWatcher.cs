// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.ProjectSystem;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.FileWatching;

/// <summary>
/// An implementation of <see cref="IFileChangeWatcher" /> that is built atop the framework <see cref="FileSystemWatcher" />. This is used if we can't
/// use the LSP one.
/// </summary>
internal sealed class DefaultFileChangeWatcher(int maxWatcherCount = 1000) : AbstractConsolidatingFileChangeWatcher(maxWatcherCount)
{
    protected override bool CanWatchDirectory(string path) => Directory.Exists(path);

    protected override IDirectoryWatcher CreateDirectoryWatcher(string path, ImmutableArray<string> filters, bool includeSubdirectories)
        => new DirectoryWatcher(path, filters, includeSubdirectories);

    private sealed class DirectoryWatcher : IDirectoryWatcher
    {
        private readonly FileSystemWatcher _watcher;

        public IReadOnlyList<string> Filters => _watcher.Filters;
        public bool IncludeSubdirectories => _watcher.IncludeSubdirectories;
        public event EventHandler<FileChangedEventArgs>? FileChanged;

        public DirectoryWatcher(string path, ImmutableArray<string> filters, bool includeSubdirectories)
        {
            _watcher = new FileSystemWatcher(path);
            Update(filters, includeSubdirectories);
            _watcher.Created += OnFileSystemEvent;
            _watcher.Changed += OnFileSystemEvent;
            _watcher.Deleted += OnFileSystemEvent;
            _watcher.Renamed += OnFileSystemEvent;
            _watcher.EnableRaisingEvents = true;
        }

        public void Update(ImmutableArray<string> filters, bool includeSubdirectories)
        {
            // Update filters incrementally to avoid unnecessary mutations, especially when only IncludeSubdirectories changes.
            foreach (var filter in filters)
            {
                if (!_watcher.Filters.Contains(filter))
                    _watcher.Filters.Add(filter);
            }

            for (var i = _watcher.Filters.Count - 1; i >= 0; i--)
            {
                if (!filters.Contains(_watcher.Filters[i]))
                    _watcher.Filters.RemoveAt(i);
            }

            _watcher.IncludeSubdirectories = includeSubdirectories;
        }

        private void OnFileSystemEvent(object sender, FileSystemEventArgs e)
        {
            var changeKind = e.ChangeType switch
            {
                WatcherChangeTypes.Created or WatcherChangeTypes.Renamed => FileChangeKind.Created,
                WatcherChangeTypes.Deleted => FileChangeKind.Deleted,
                _ => FileChangeKind.Changed,
            };

            FileChanged?.Invoke(this, new(e.FullPath, changeKind));

            if (e is RenamedEventArgs renamedEventArgs && RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                FileChanged?.Invoke(this, new(renamedEventArgs.OldFullPath, FileChangeKind.Deleted));
        }

        public void Dispose() => _watcher.Dispose();
    }
}
