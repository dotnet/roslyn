// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

internal partial class VisualStudioMetadataReferenceManager
{
    private sealed partial class VisualStudioPortableExecutableReference
    {
        /// <summary>
        /// Provides a performant mechanism to get the timestamp of a file. Caches the timestamp of requested
        /// files and watches the directories containing those files for changes.
        /// </summary>
        private sealed class FileTimeStampProvider
        {
            private static FileTimeStampProvider? s_instance;

            private readonly Dictionary<string, DateTime> _cachedTimestamps = [];
            private readonly Dictionary<string, IFileChangeContext> _fileChangeContexts = [];

            private FileTimeStampProvider(Workspace workspace)
            {
                _ = workspace.RegisterWorkspaceChangedHandler(OnWorkspaceChanged);
            }

            private void OnWorkspaceChanged(WorkspaceChangeEventArgs args)
            {
                switch (args.Kind)
                {
                    case WorkspaceChangeKind.SolutionAdded:
                    case WorkspaceChangeKind.SolutionCleared:
                    case WorkspaceChangeKind.SolutionReloaded:
                    case WorkspaceChangeKind.SolutionRemoved:
                        ResetCaches();
                        break;
                }

                return;

                void ResetCaches()
                {
                    lock (_cachedTimestamps)
                    {
                        foreach (var (_, context) in _fileChangeContexts)
                            context.FileChanged -= OnContextFileChanged;

                        _fileChangeContexts.Clear();
                        _cachedTimestamps.Clear();
                    }
                }
            }

            public static DateTime GetTimeStamp(string fullPath, IFileChangeWatcher watcher, Workspace workspace)
               => GetInstance(workspace).GetTimeStamp(fullPath, watcher);

            private static FileTimeStampProvider GetInstance(Workspace workspace)
            {
                return s_instance ??= new FileTimeStampProvider(workspace);
            }

            private DateTime GetTimeStamp(string fullPath, IFileChangeWatcher watcher)
            {
                DateTime timestamp;

                lock (_cachedTimestamps)
                {
                    // Attempt to use the cached timestamp for this file. 
                    if (_cachedTimestamps.TryGetValue(fullPath, out timestamp))
                        return timestamp;
                }

                // If we don't have a cached timestamp, we'll need to recalculate. Do this outside
                // the lock as it can take some time.
                var directory = Path.GetDirectoryName(fullPath);
                timestamp = FileUtilities.GetFileTimeStamp(fullPath);

                lock (_cachedTimestamps)
                {
                    // Ensure we are listening for changes to this directory.
                    _ = _fileChangeContexts.GetOrAdd(directory, static (directory, arg) =>
                    {
                        var (watcher, self) = arg;
                        var context = watcher.CreateContext([new WatchedDirectory(directory, extensionFilters: [])]);
                        context.FileChanged += self.OnContextFileChanged;

                        return context;
                    }, (watcher, this));

                    // No need to enqueue this file path onto the context, as the containing directory is already being watched
                    _cachedTimestamps[fullPath] = timestamp;

                    return timestamp;
                }
            }

            private void OnContextFileChanged(object? sender, string filePath)
            {
                lock (_cachedTimestamps)
                {
                    // This file has changed, next GetTimeStamp request will need to recalculate the timestamp
                    _cachedTimestamps.Remove(filePath);
                }
            }
        }
    }
}
