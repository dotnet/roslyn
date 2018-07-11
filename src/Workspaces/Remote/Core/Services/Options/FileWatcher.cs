// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.IO;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.CodingConventions;

namespace Microsoft.CodeAnalysis.Editor.Options
{
    /// <summary>
    /// Provides an implementation of <see cref="IFileWatcher"/> necessary for using
    /// <see cref="ICodingConventionsManager"/> outside of Visual Studio.
    /// </summary>
    [Export(typeof(IFileWatcher)), Shared]
    internal sealed class FileWatcher : IFileWatcher
    {
        private readonly object _gate = new object();

        /// <summary>
        /// Access to this field is guarded by <see cref="_gate"/>. It is initialized at time of construction and set to
        /// <see langword="null"/> in <see cref="Dispose"/>.
        /// </summary>
        private Dictionary<(string fileName, string directoryPath), FileSystemWatcher> _watchers = new Dictionary<(string fileName, string directoryPath), FileSystemWatcher>();

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FileWatcher()
        {
        }

        public event ConventionsFileChangedAsyncEventHandler ConventionFileChanged;
        public event ContextFileMovedAsyncEventHandler ContextFileMoved;

        public void Dispose()
        {
            var watchers = new List<FileSystemWatcher>();
            lock (_gate)
            {
                if (_watchers is null)
                {
                    // Already disposed
                    return;
                }

                watchers.AddRange(_watchers.Values);
                _watchers = null;
            }

            foreach (var watcher in watchers)
            {
                watcher.Dispose();
            }

            ConventionFileChanged = null;
            ContextFileMoved = null;
        }

        public void StartWatching(string fileName, string directoryPath)
        {
            lock (_gate)
            {
                if (_watchers is null)
                {
                    throw new ObjectDisposedException(nameof(FileWatcher));
                }

                if (_watchers.ContainsKey((fileName, directoryPath)))
                {
                    return;
                }

                var watcher = new FileSystemWatcher(directoryPath, fileName);
                _watchers.Add((fileName, directoryPath), watcher);

                FileSystemEventHandler handler = (sender, e) => HandleFileSystemEvent(fileName, directoryPath, e);
                watcher.Changed += handler;
                watcher.Created += handler;
                watcher.Deleted += handler;
                watcher.Renamed += HandleRenamedEvent;
                watcher.EnableRaisingEvents = true;
            }
        }

        private void HandleFileSystemEvent(string fileName, string directoryPath, FileSystemEventArgs e)
        {
            if (e.ChangeType.HasFlag(WatcherChangeTypes.Created))
            {
                ConventionFileChanged?.Invoke(this, new ConventionsFileChangeEventArgs(fileName, directoryPath, ChangeType.FileCreated));
            }

            if (e.ChangeType.HasFlag(WatcherChangeTypes.Changed))
            {
                ConventionFileChanged?.Invoke(this, new ConventionsFileChangeEventArgs(fileName, directoryPath, ChangeType.FileModified));
            }

            if (e.ChangeType.HasFlag(WatcherChangeTypes.Deleted))
            {
                ConventionFileChanged?.Invoke(this, new ConventionsFileChangeEventArgs(fileName, directoryPath, ChangeType.FileDeleted));
            }
        }

        private void HandleRenamedEvent(object sender, RenamedEventArgs e)
        {
            ContextFileMoved?.Invoke(this, new ContextFileMovedEventArgs(e.OldFullPath, e.FullPath));
        }

        public void StopWatching(string fileName, string directoryPath)
        {
            lock (_gate)
            {
                if (_watchers is null)
                {
                    // Treat calls after Dispose as a NOP
                    return;
                }

                if (_watchers.TryGetValue((fileName, directoryPath), out var watcher))
                {
                    _watchers.Remove((fileName, directoryPath));
                    watcher.Dispose();
                }
            }
        }
    }
}
