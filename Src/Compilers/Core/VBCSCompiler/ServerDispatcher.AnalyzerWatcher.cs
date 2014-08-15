// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    partial class ServerDispatcher
    {
        private sealed class AnalyzerWatcher
        {
            private readonly ServerDispatcher dispatcher;

            // Maps from a directory path to the associated FileSystemWatcher.
            private readonly Dictionary<string, FileSystemWatcher> fileSystemWatchers = new Dictionary<string, FileSystemWatcher>(StringComparer.OrdinalIgnoreCase);

            // Maps from a directory to a set of files within that directory
            // that are being watched.
            private readonly Dictionary<string, HashSet<string>> watchedFiles = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            // Controls access to the file system watcher data structures.
            private readonly object fileSystemWatcherLock = new object();

            public AnalyzerWatcher(ServerDispatcher dispatcher)
            {
                this.dispatcher = dispatcher;

                AnalyzerFileReference.AssemblyLoad += AnalyzerFileReference_AssemblyLoad;
            }

            private void AnalyzerFileReference_AssemblyLoad(object sender, AnalyzerAssemblyLoadEventArgs e)
            {
                lock (this.fileSystemWatcherLock)
                {
                    var directoryPath = Path.GetDirectoryName(e.Path);
                    var fileName = Path.GetFileName(e.Path);

                    FileSystemWatcher watcher;
                    if (!this.fileSystemWatchers.TryGetValue(directoryPath, out watcher))
                    {
                        watcher = new FileSystemWatcher(directoryPath);
                        watcher.Changed += Watcher_Changed;
                        watcher.EnableRaisingEvents = true;

                        this.fileSystemWatchers.Add(directoryPath, watcher);
                    }

                    HashSet<string> watchedFilesInDirectory;
                    if (!this.watchedFiles.TryGetValue(directoryPath, out watchedFilesInDirectory))
                    {
                        watchedFilesInDirectory = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                        this.watchedFiles.Add(directoryPath, watchedFilesInDirectory);
                    }

                    watchedFilesInDirectory.Add(fileName);
                }
            }

            private void Watcher_Changed(object sender, FileSystemEventArgs e)
            {
                lock (this.fileSystemWatcherLock)
                {
                    var directoryPath = Path.GetDirectoryName(e.FullPath);
                    var fileName = Path.GetFileName(e.FullPath);

                    HashSet<string> watchedFilesInDirectory;
                    if (this.watchedFiles.TryGetValue(directoryPath, out watchedFilesInDirectory) &&
                        watchedFilesInDirectory.Contains(fileName))
                    {
                        this.dispatcher.AnalyzerFileChanged();
                    }
                }
            }
        }
    }
}
