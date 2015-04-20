// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    internal sealed class CompilerServerFileWatcher
    {
        // This value should be resolved whenever the watcher detects that an analyzer file has changed on
        // disk.  The server is listening for this event and will initiate a shutdown when it occurs.
        private static TaskCompletionSource<bool> s_fileChangedCompletionSource;

        // Maps from a directory path to the associated FileSystemWatcher.
        private static readonly Dictionary<string, FileSystemWatcher> s_fileSystemWatchers = new Dictionary<string, FileSystemWatcher>(StringComparer.OrdinalIgnoreCase);

        // Maps from a directory to a set of files within that directory
        // that are being watched.
        private static readonly Dictionary<string, HashSet<string>> s_watchedFiles = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        // Controls access to the file system watcher data structures.
        private static readonly object s_fileSystemWatcherLock = new object();

        public static void AddPath(string path)
        {
            lock (s_fileSystemWatcherLock)
            {
                var directoryPath = Path.GetDirectoryName(path);
                var fileName = Path.GetFileName(path);

                FileSystemWatcher watcher;
                if (!s_fileSystemWatchers.TryGetValue(directoryPath, out watcher))
                {
                    watcher = new FileSystemWatcher(directoryPath);
                    watcher.Changed += Watcher_Changed;
                    watcher.EnableRaisingEvents = true;

                    s_fileSystemWatchers.Add(directoryPath, watcher);
                }

                HashSet<string> watchedFilesInDirectory;
                if (!s_watchedFiles.TryGetValue(directoryPath, out watchedFilesInDirectory))
                {
                    watchedFilesInDirectory = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    s_watchedFiles.Add(directoryPath, watchedFilesInDirectory);
                }

                watchedFilesInDirectory.Add(fileName);
            }
        }

        private static void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            lock (s_fileSystemWatcherLock)
            {
                var directoryPath = Path.GetDirectoryName(e.FullPath);
                var fileName = Path.GetFileName(e.FullPath);

                HashSet<string> watchedFilesInDirectory;
                if (s_watchedFiles.TryGetValue(directoryPath, out watchedFilesInDirectory) &&
                    watchedFilesInDirectory.Contains(fileName))
                {
                    s_fileChangedCompletionSource.TrySetResult(true);
                }
            }
        }

        internal static Task CreateWatchFilesTask()
        {
            s_fileChangedCompletionSource = new TaskCompletionSource<bool>();
            return s_fileChangedCompletionSource.Task;
        }
    }
}
