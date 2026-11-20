// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.ProjectSystem;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.FileWatching;

internal abstract partial class AbstractConsolidatingFileChangeWatcher
{
    internal sealed class FileChangeContext : IFileChangeContext
    {
        private readonly AbstractConsolidatingFileChangeWatcher _owner;
        private readonly RoslynTelemetry _telemetry;

        /// <summary>
        /// A monitor lock held for code touching <see cref="_explicitlyWatchedFiles"/>, and <see cref="_watchedDirectoriesWatches"/> during disposal. It is not expected to be held
        /// when calling into <see cref="_owner"/>, but that shouldn't really be necessary given the simplicitly of this type.
        /// </summary>
        private readonly object _gate = new();

        private readonly ImmutableArray<WatchedDirectory> _watchedDirectories;

        /// <summary>
        /// The actual watches created for each watch in <see cref="_watchedDirectories"/>.
        /// </summary>
        private readonly List<IDisposable> _watchedDirectoriesWatches;

        /// <summary>
        /// A map from a file path to the number of times <see cref="EnqueueWatchingFile(string)"/> was called for that file path, and the IDisposable for the
        /// return from <see cref="AbstractConsolidatingFileChangeWatcher.AcquireDirectoryWatch"/> when it was called for the first time.
        /// </summary>
        private readonly Dictionary<string, (int count, IDisposable directoryWatch)> _explicitlyWatchedFiles = new(s_pathStringComparer);

        private bool _disposed;

        public FileChangeContext(AbstractConsolidatingFileChangeWatcher owner, ImmutableArray<WatchedDirectory> watchedDirectories)
        {
            _owner = owner;
            _telemetry = RoslynTelemetry.Current;
            _watchedDirectories = watchedDirectories;

            // Acquire the directory watches for each directory; it's important this happens last in the constructor since events
            // could get notified immediatly after the directory is watched.
            _watchedDirectoriesWatches = new List<IDisposable>(_watchedDirectories.Length);
            foreach (var watchedDirectory in _watchedDirectories)
                _watchedDirectoriesWatches.Add(_owner.AcquireDirectoryWatch(watchedDirectory, this, includeSubdirectories: true));
        }

        public event EventHandler<FileChangedEventArgs>? FileChanged;

        public IWatchedFile EnqueueWatchingFile(string filePath)
        {
            if (WatchedDirectory.FilePathCoveredByWatchedDirectories(_watchedDirectories, filePath, s_pathStringComparison))
                return NoOpWatchedFile.Instance;

            var parentDirectory = Path.GetDirectoryName(filePath);
            if (parentDirectory is null)
                return NoOpWatchedFile.Instance;

            lock (_gate)
            {

                if (_explicitlyWatchedFiles.TryGetValue(filePath, out var countAndWatcher))
                {
                    _explicitlyWatchedFiles[filePath] = countAndWatcher with { count = countAndWatcher.count + 1 };
                }
                else
                {
                    var extension = Path.GetExtension(filePath);
                    var directoryWatchToken = _owner.AcquireDirectoryWatch(
                        new WatchedDirectory(parentDirectory, extensionFilters: string.IsNullOrEmpty(extension) ? [] : [extension]),
                        this,
                        includeSubdirectories: false);
                    countAndWatcher = (count: 1, directoryWatchToken);
                    _explicitlyWatchedFiles.Add(filePath, countAndWatcher);
                }
            }

            return new ExplicitlyWatchedFile(this, filePath);

        }

        /// <summary>
        /// Routes a filesystem event to this context when the changed path matches one of the context's watched
        /// directories or explicitly watched files.
        /// </summary>
        internal void OnFileChanged(FileChangedEventArgs e)
        {
            // The underlying watcher is shared between contexts (and possibly between servers), so
            // attribute to the instance that owns this context rather than to the ambient one.
            using var _ = RoslynTelemetry.SetCurrent(_telemetry);

            lock (_gate)
            {
                if (_disposed || !ShouldRaiseForPath_NoLock(e.FilePath))
                    return;
            }

            FileChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Returns <see langword="true"/> when this context is interested in notifications for <paramref name="filePath"/>.
        /// </summary>
        private bool ShouldRaiseForPath_NoLock(string filePath)
            => _explicitlyWatchedFiles.ContainsKey(filePath) ||
               WatchedDirectory.FilePathCoveredByWatchedDirectories(_watchedDirectories, filePath, s_pathStringComparison);

        /// <summary>
        /// Removes one explicit file watch registration when a returned <see cref="IWatchedFile"/> is disposed.
        /// </summary>
        private void RemoveExplicitlyWatchedFile(string filePath)
        {
            lock (_gate)
            {
                // If it's already not in that dictionary, then the entire context might have already been disposed
                if (!_explicitlyWatchedFiles.TryGetValue(filePath, out var countAndWatcher))
                    return;

                if (countAndWatcher.count == 1)
                {
                    countAndWatcher.directoryWatch.Dispose();
                    _explicitlyWatchedFiles.Remove(filePath);
                }
                else
                {
                    _explicitlyWatchedFiles[filePath] = countAndWatcher with { count = countAndWatcher.count - 1 };
                }
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, true))
                return;

            List<IDisposable> watches;

            lock (_gate)
            {
                watches = [.. _watchedDirectoriesWatches, .. _explicitlyWatchedFiles.Values.Select(v => v.directoryWatch)];
                _watchedDirectoriesWatches.Clear();
                _explicitlyWatchedFiles.Clear();
            }

            foreach (var watch in watches)
                watch.Dispose();
        }

        private sealed class ExplicitlyWatchedFile(FileChangeContext context, string filePath) : IWatchedFile
        {
            private bool _disposed;

            public void Dispose()
            {
                if (Interlocked.Exchange(ref _disposed, true))
                    return;

                context.RemoveExplicitlyWatchedFile(filePath);
            }
        }
    }
}
