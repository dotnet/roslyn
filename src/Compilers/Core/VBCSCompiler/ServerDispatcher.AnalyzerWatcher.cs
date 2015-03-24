// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal partial class ServerDispatcher
    {
        private sealed class AnalyzerWatcher
        {
            // This value should be resolved whenever the watcher detects that an analyzer file has changed on
            // disk.  The server is listening for this event and will initiate a shutdown when it occurs.
            private readonly TaskCompletionSource<bool> _fileChangedCompletionSource;

            // Maps from a directory path to the associated FileSystemWatcher.
            private readonly Dictionary<string, FileSystemWatcher> _fileSystemWatchers = new Dictionary<string, FileSystemWatcher>(StringComparer.OrdinalIgnoreCase);

            // Maps from a directory to a set of files within that directory
            // that are being watched.
            private readonly Dictionary<string, HashSet<string>> _watchedFiles = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            // Controls access to the file system watcher data structures.
            private readonly object _fileSystemWatcherLock = new object();

            public AnalyzerWatcher(TaskCompletionSource<bool> fileChangedCompletionSource)
            {
                _fileChangedCompletionSource = fileChangedCompletionSource;

                InMemoryAssemblyProvider.AssemblyLoad += AnalyzerFileReference_AssemblyLoad;
            }

            private void AnalyzerFileReference_AssemblyLoad(object sender, InMemoryAssemblyProvider.AssemblyLoadEventArgs e)
            {
                lock (_fileSystemWatcherLock)
                {
                    var directoryPath = Path.GetDirectoryName(e.Path);
                    var fileName = Path.GetFileName(e.Path);

                    FileSystemWatcher watcher;
                    if (!_fileSystemWatchers.TryGetValue(directoryPath, out watcher))
                    {
                        watcher = new FileSystemWatcher(directoryPath);
                        watcher.Changed += Watcher_Changed;
                        watcher.EnableRaisingEvents = true;

                        _fileSystemWatchers.Add(directoryPath, watcher);
                    }

                    HashSet<string> watchedFilesInDirectory;
                    if (!_watchedFiles.TryGetValue(directoryPath, out watchedFilesInDirectory))
                    {
                        watchedFilesInDirectory = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                        _watchedFiles.Add(directoryPath, watchedFilesInDirectory);
                    }

                    watchedFilesInDirectory.Add(fileName);
                }
            }

            private void Watcher_Changed(object sender, FileSystemEventArgs e)
            {
                lock (_fileSystemWatcherLock)
                {
                    var directoryPath = Path.GetDirectoryName(e.FullPath);
                    var fileName = Path.GetFileName(e.FullPath);

                    HashSet<string> watchedFilesInDirectory;
                    if (_watchedFiles.TryGetValue(directoryPath, out watchedFilesInDirectory) &&
                        watchedFilesInDirectory.Contains(fileName))
                    {
                        _fileChangedCompletionSource.TrySetResult(true);
                    }
                }
            }

            internal static Task CreateWatchFilesTask()
            {
                var source = new TaskCompletionSource<bool>();
                var _ = new AnalyzerWatcher(source);
                return source.Task;
            }
        }
    }
}
