// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation;

[Export(typeof(AnalyzerFileWatcherService))]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class AnalyzerFileWatcherService(SVsServiceProvider serviceProvider)
{
    private readonly IVsFileChangeEx _fileChangeService = (IVsFileChangeEx)serviceProvider.GetService(typeof(SVsFileChangeEx));

    private readonly Dictionary<string, FileChangeTracker> _fileChangeTrackers = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Holds a list of assembly modified times that we can use to detect a file change prior to the <see cref="FileChangeTracker"/> being in place.
    /// Once it's in place and subscribed, we'll remove the entry because any further changes will be detected that way.
    /// </summary>
    private readonly Dictionary<string, DateTime> _assemblyUpdatedTimesUtc = new(StringComparer.OrdinalIgnoreCase);

    private readonly object _guard = new();

    private static DateTime? GetLastUpdateTimeUtc(string fullPath)
    {
        try
        {
            var creationTimeUtc = File.GetCreationTimeUtc(fullPath);
            var writeTimeUtc = File.GetLastWriteTimeUtc(fullPath);

            return writeTimeUtc > creationTimeUtc ? writeTimeUtc : creationTimeUtc;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    internal void TrackFilePathAndReportErrorIfChanged(string filePath)
    {
        lock (_guard)
        {
            if (!_fileChangeTrackers.TryGetValue(filePath, out var tracker))
            {
                tracker = new FileChangeTracker(_fileChangeService, filePath);
                tracker.UpdatedOnDisk += Tracker_UpdatedOnDisk;
                _ = tracker.StartFileChangeListeningAsync();

                _fileChangeTrackers.Add(filePath, tracker);
            }

            if (_assemblyUpdatedTimesUtc.TryGetValue(filePath, out var assemblyUpdatedTime))
            {
                var currentFileUpdateTime = GetLastUpdateTimeUtc(filePath);

                if (currentFileUpdateTime != null)
                {
                    // If the the tracker is in place, at this point we can stop checking any further for this assembly
                    if (tracker.PreviousCallToStartFileChangeHasAsynchronouslyCompleted)
                    {
                        _assemblyUpdatedTimesUtc.Remove(filePath);
                    }
                }
            }
            else
            {
                // We don't have an assembly updated time. This means we either haven't ever checked it, or we have a file watcher in place.
                // If the file watcher is in place, then nothing further to do. Otherwise we'll add the update time to the map for future checking
                if (!tracker.PreviousCallToStartFileChangeHasAsynchronouslyCompleted)
                {
                    var currentFileUpdateTime = GetLastUpdateTimeUtc(filePath);

                    if (currentFileUpdateTime != null)
                    {
                        _assemblyUpdatedTimesUtc[filePath] = currentFileUpdateTime.Value;
                    }
                }
            }
        }
    }

    private void Tracker_UpdatedOnDisk(object sender, EventArgs e)
    {
        var tracker = (FileChangeTracker)sender;
        var filePath = tracker.FilePath;

        lock (_guard)
        {
            // Once we've created a diagnostic for a given analyzer file, there's
            // no need to keep watching it.
            _fileChangeTrackers.Remove(filePath);
        }

        tracker.Dispose();
        tracker.UpdatedOnDisk -= Tracker_UpdatedOnDisk;
    }
}
