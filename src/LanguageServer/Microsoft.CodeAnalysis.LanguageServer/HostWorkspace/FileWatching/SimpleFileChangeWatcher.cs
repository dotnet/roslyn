// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.FileWatching;

/// <summary>
/// A trivial implementation of <see cref="IFileChangeWatcher" /> that is built atop the framework <see cref="FileSystemWatcher" />. This is used if we can't
/// use the LSP one.
/// </summary>
/// <remarks>
/// This implementation is not remotely efficient, but is available as a fallback implementation. If this needs to regularly be used, then this should get some improvements.
/// </remarks>
internal sealed class SimpleFileChangeWatcher : IFileChangeWatcher
{
    public IFileChangeContext CreateContext(params WatchedDirectory[] watchedDirectories)
    {
        return new FileChangeContext([.. watchedDirectories]);
    }

    private class FileChangeContext : IFileChangeContext
    {
        private readonly ImmutableArray<WatchedDirectory> _watchedDirectories;

        /// <summary>
        /// The directory watchers for the <see cref="_watchedDirectories"/>.
        /// </summary>
        private readonly ImmutableArray<FileSystemWatcher> _directoryFileSystemWatchers;
        private readonly ConcurrentSet<IndividualWatchedFile> _individualWatchedFiles = [];

        public FileChangeContext(ImmutableArray<WatchedDirectory> watchedDirectories)
        {
            var watchedDirectoriesBuilder = ImmutableArray.CreateBuilder<WatchedDirectory>(watchedDirectories.Length);
            var watcherBuilder = ImmutableArray.CreateBuilder<FileSystemWatcher>(watchedDirectories.Length);

            foreach (var watchedDirectory in watchedDirectories)
            {
                // If the directory doesn't exist, we can't create a watcher for changes inside of it. In this case, we'll just skip this as a directory
                // to watch; any requests for a watch within that directory will still create a one-off watcher for that specific file. That's not likely
                // to be an issue in practice: directories that are missing would be things like global reference directories -- if it's not there, we
                // probably won't ever see a watch for a file under there later anyways.
                if (Directory.Exists(watchedDirectory.Path))
                {
                    var watcher = new FileSystemWatcher(watchedDirectory.Path);
                    watcher.IncludeSubdirectories = true;

                    if (watchedDirectory.ExtensionFilter != null)
                        watcher.Filter = '*' + watchedDirectory.ExtensionFilter;

                    watcher.Changed += RaiseEvent;
                    watcher.Created += RaiseEvent;
                    watcher.Deleted += RaiseEvent;
                    watcher.Renamed += RaiseEvent;

                    watcher.EnableRaisingEvents = true;

                    watchedDirectoriesBuilder.Add(watchedDirectory);
                    watcherBuilder.Add(watcher);
                }
            }

            _watchedDirectories = watchedDirectoriesBuilder.ToImmutable();
            _directoryFileSystemWatchers = watcherBuilder.ToImmutable();
        }

        public event EventHandler<string>? FileChanged;

        public IWatchedFile EnqueueWatchingFile(string filePath)
        {
            // If this path is already covered by one of our directory watchers, nothing further to do
            if (WatchedDirectory.FilePathCoveredByWatchedDirectories(_watchedDirectories, filePath, StringComparison.Ordinal))
                return NoOpWatchedFile.Instance;

            var individualWatchedFile = new IndividualWatchedFile(filePath, this);
            _individualWatchedFiles.Add(individualWatchedFile);
            return individualWatchedFile;
        }

        private void RaiseEvent(object sender, FileSystemEventArgs e)
        {
            FileChanged?.Invoke(this, e.FullPath);
        }

        public void Dispose()
        {
            foreach (var directoryWatcher in _directoryFileSystemWatchers)
                directoryWatcher.Dispose();
        }

        private class IndividualWatchedFile : IWatchedFile
        {
            private readonly FileChangeContext _context;
            private readonly FileSystemWatcher? _watcher;

            public IndividualWatchedFile(string filePath, FileChangeContext context)
            {
                _context = context;

                // We always must create a watch on an entire directory, so create that, filtered to the single file name
                var directoryPath = Path.GetDirectoryName(filePath)!;

                // TODO: support missing directories properly
                if (Directory.Exists(directoryPath))
                {
                    _watcher = new FileSystemWatcher(directoryPath, Path.GetFileName(filePath));
                    _watcher.IncludeSubdirectories = false;

                    _watcher.Changed += _context.RaiseEvent;
                    _watcher.Created += _context.RaiseEvent;
                    _watcher.Deleted += _context.RaiseEvent;
                    _watcher.Renamed += _context.RaiseEvent;

                    _watcher.EnableRaisingEvents = true;
                }
                else
                {
                    _watcher = null;
                }
            }

            public void Dispose()
            {
                if (_context._individualWatchedFiles.Remove(this) && _watcher != null)
                {
                    _watcher.Changed -= _context.RaiseEvent;
                    _watcher.Created -= _context.RaiseEvent;
                    _watcher.Deleted -= _context.RaiseEvent;
                    _watcher.Renamed -= _context.RaiseEvent;
                    _watcher.Dispose();
                }
            }
        }
    }
}
