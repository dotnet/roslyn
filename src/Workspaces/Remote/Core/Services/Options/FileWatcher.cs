// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.CodingConventions;

namespace Microsoft.CodeAnalysis.Editor.Options
{
    [Export(typeof(IFileWatcher)), Shared]
    internal sealed class FileWatcher : IFileWatcher
    {
        private ImmutableDictionary<(string fileName, string directoryPath), FileSystemWatcher> _watchers = ImmutableDictionary<(string fileName, string directoryPath), FileSystemWatcher>.Empty;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public FileWatcher()
        {
        }

        public event ConventionsFileChangedAsyncEventHandler ConventionFileChanged;
        public event ContextFileMovedAsyncEventHandler ContextFileMoved;

        public void Dispose()
        {
            var watchers = Interlocked.Exchange(ref _watchers, null);
            if (watchers == null)
            {
                return;
            }

            ConventionFileChanged = null;
            ContextFileMoved = null;
        }

        public void StartWatching(string fileName, string directoryPath)
        {
            FileSystemWatcher watcher = null;
            var updated = ImmutableInterlocked.Update(
                ref _watchers,
                watchers =>
                {
                    if (!watchers.TryGetValue((fileName, directoryPath), out var existingWatcher))
                    {
                        if (watcher == null)
                        {
                            watcher = new FileSystemWatcher(directoryPath, fileName);
                        }

                        return watchers.Add((fileName, directoryPath), watcher);
                    }

                    return watchers;
                });

            if (updated)
            {
                FileSystemEventHandler handler = (sender, e) => HandleFileSystemEvent(fileName, directoryPath, e);
                watcher.Changed += handler;
                watcher.Created += handler;
                watcher.Deleted += handler;
                watcher.Renamed += HandleRenamedEvent;
            }
            else
            {
                watcher?.Dispose();
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
                ConventionFileChanged?.Invoke(this, new ConventionsFileChangeEventArgs(fileName, directoryPath, ChangeType.FileCreated));
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
            FileSystemWatcher watcher = null;
            var updated = ImmutableInterlocked.Update(
                ref _watchers,
                watchers =>
                {
                    if (watchers.TryGetValue((fileName, directoryPath), out watcher))
                    {
                        return watchers.Remove((fileName, directoryPath));
                    }

                    return watchers;
                });

            watcher?.Dispose();
        }
    }
}
