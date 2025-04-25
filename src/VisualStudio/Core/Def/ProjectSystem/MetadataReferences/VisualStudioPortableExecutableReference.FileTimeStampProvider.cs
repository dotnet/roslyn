// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;

internal partial class VisualStudioMetadataReferenceManager
{
    private sealed partial class VisualStudioPortableExecutableReference
    {
        private class FileTimeStampProvider
        {
            private readonly Dictionary<string, DateTime> _cachedTimestamps = [];
            private readonly Dictionary<string, IFileChangeContext> _fileChangeContexts = [];

            public DateTime GetTimeStamp(string fullPath, IFileChangeWatcher watcher)
            {
                lock (_cachedTimestamps)
                {
                    // Attempt to use the cached timestamp for this file. If we don't have a cached timestamp,
                    // we'll need to recalculate
                    if (!_cachedTimestamps.TryGetValue(fullPath, out var timestamp))
                        return timestamp;

                    var directory = Path.GetDirectoryName(fullPath);
                    var context = _fileChangeContexts.GetOrAdd(directory, static (directory, arg) =>
                    {
                        var (watcher, self) = arg;
                        var context = watcher.CreateContext([new WatchedDirectory(directory, extensionFilters: [])]);
                        context.FileChanged += self.OnContextFileChanged;

                        return context;
                    }, (watcher, this));

                    // No need to enqueue this file path onto the context, as the containing directory is already being watched
                    timestamp = FileUtilities.GetFileTimeStamp(fullPath);
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
