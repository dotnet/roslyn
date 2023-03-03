// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[Export(typeof(IFileChangeWatcher)), Shared]
internal sealed class FileChangeWatcher : IFileChangeWatcher
{
    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public FileChangeWatcher()
    {
    }

    public IFileChangeContext CreateContext(params WatchedDirectory[] watchedDirectories)
    {
        return new FileChangeContext(watchedDirectories.ToImmutableArray());
    }

    private class FileChangeContext : IFileChangeContext
    {
        private readonly ImmutableArray<WatchedDirectory> _watchedDirectories;

        /// <summary>
        /// The directory watchers for the <see cref="_watchedDirectories"/>.
        /// </summary>
        private readonly ImmutableArray<FileSystemWatcher> _directoryFileSystemWatchers;
        private readonly ConcurrentSet<IndividualWatchedFile> _individualWatchedFiles = new ConcurrentSet<IndividualWatchedFile>();

        public FileChangeContext(ImmutableArray<WatchedDirectory> watchedDirectories)
        {
            _watchedDirectories = watchedDirectories;
            var builder = ImmutableArray.CreateBuilder<FileSystemWatcher>(watchedDirectories.Length);

            foreach (var watchedDirectory in watchedDirectories)
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

                builder.Add(watcher);
            }

            _directoryFileSystemWatchers = builder.ToImmutable();
        }

        public event EventHandler<string>? FileChanged;

        public IWatchedFile EnqueueWatchingFile(string filePath)
        {
            // If this path is already covered by one of our directory watchers, nothing further to do
            foreach (var watchedDirectory in _watchedDirectories)
            {
                if (filePath.StartsWith(watchedDirectory.Path, StringComparison.Ordinal))
                {
                    // If ExtensionFilter is null, then we're watching for all files in the directory so the prior check
                    // of the directory containment was sufficient. If it isn't null, then we have to check the extension
                    // matches.
                    if (watchedDirectory.ExtensionFilter == null || filePath.EndsWith(watchedDirectory.ExtensionFilter, StringComparison.Ordinal))
                    {
                        return NoOpWatchedFile.Instance;
                    }
                }
            }

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

        /// <summary>
        /// When a FileChangeWatcher already has a watch on a directory, a request to watch a specific file is a no-op. In that case, we return this token,
        /// which when disposed also does nothing.
        /// </summary>
        internal sealed class NoOpWatchedFile : IWatchedFile
        {
            public static readonly IWatchedFile Instance = new NoOpWatchedFile();

            private NoOpWatchedFile()
            {
            }

            public void Dispose()
            {
            }
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
                if (_context._individualWatchedFiles.Remove(this))
                {
                    Contract.ThrowIfNull(_watcher);
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
