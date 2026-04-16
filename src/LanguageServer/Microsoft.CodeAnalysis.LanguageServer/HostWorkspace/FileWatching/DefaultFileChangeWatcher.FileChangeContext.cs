// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.FileWatching;

internal sealed partial class DefaultFileChangeWatcher
{
    internal sealed class FileChangeContext : IFileChangeContext
    {
        private readonly DefaultFileChangeWatcher _owner;
        private readonly List<WatchedDirectoryEntry> _watchedDirectoryEntries = [];
        private readonly object _gate = new();
        private bool _disposed = false;

        public FileChangeContext(DefaultFileChangeWatcher owner, ImmutableArray<WatchedDirectory> watchedDirectories)
        {
            _owner = owner;

            foreach (var watchedDirectory in watchedDirectories)
            {
                if (!Directory.Exists(watchedDirectory.Path))
                    continue;

                AddOrConsolidateWatchedDirectory_NoLock(watchedDirectory.Path, watchedDirectory.ExtensionFilters);
            }
        }

        public event EventHandler<string>? FileChanged;

        public IWatchedFile EnqueueWatchingFile(string filePath)
        {
            lock (_gate)
            {
                var parentDirectory = Path.GetDirectoryName(filePath);
                if (parentDirectory is null || !Directory.Exists(parentDirectory))
                    return NoOpWatchedFile.Instance;

                AddOrConsolidateWatchedDirectory_NoLock(parentDirectory, extensionFilters: []);
                return NoOpWatchedFile.Instance;
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, true) == false)
            {
                lock (_gate)
                {
                    foreach (var entry in _watchedDirectoryEntries)
                        entry.Dispose();

                    _watchedDirectoryEntries.Clear();
                }
            }
        }

        private void AddOrConsolidateWatchedDirectory_NoLock(string directoryPath, ImmutableArray<string> extensionFilters)
        {
            var watchedDirectory = new WatchedDirectory(directoryPath, extensionFilters);

            if (_watchedDirectoryEntries.Any(entry => IsContainedBy(watchedDirectory.Path, entry.Path)))
                return;

            var containedEntries = _watchedDirectoryEntries.Where(entry => IsContainedBy(entry.Path, watchedDirectory.Path)).ToArray();
            foreach (var containedEntry in containedEntries)
            {
                _watchedDirectoryEntries.Remove(containedEntry);
                containedEntry.Dispose();
            }

            _watchedDirectoryEntries.Add(new WatchedDirectoryEntry(this, _owner, watchedDirectory));

            ConsolidateWatchedDirectories_NoLock();
        }

        private void ConsolidateWatchedDirectories_NoLock()
        {
            while (_watchedDirectoryEntries.Count > MaximumWatcherCount)
            {
                var pair = FindBestPairForConsolidation_NoLock();
                if (pair.left is null || pair.right is null || pair.commonPath is null)
                    break;

                var mergedExtensionFilters = pair.left.ExtensionFilters.IsEmpty || pair.right.ExtensionFilters.IsEmpty
                    ? ImmutableArray<string>.Empty
                    : [.. pair.left.ExtensionFilters.Intersect(pair.right.ExtensionFilters, s_pathStringComparer)];

                var mergedDirectory = new WatchedDirectory(pair.commonPath, mergedExtensionFilters);

                _watchedDirectoryEntries.Remove(pair.left);
                _watchedDirectoryEntries.Remove(pair.right);
                pair.left.Dispose();
                pair.right.Dispose();

                // If merged path is already covered by something else after removals, don't add it.
                if (_watchedDirectoryEntries.Any(entry => IsContainedBy(mergedDirectory.Path, entry.Path)))
                    continue;

                var containedByMerged = _watchedDirectoryEntries.Where(entry => IsContainedBy(entry.Path, mergedDirectory.Path)).ToArray();
                foreach (var contained in containedByMerged)
                {
                    _watchedDirectoryEntries.Remove(contained);
                    contained.Dispose();
                }

                _watchedDirectoryEntries.Add(new WatchedDirectoryEntry(this, _owner, mergedDirectory));
            }
        }

        private (WatchedDirectoryEntry? left, WatchedDirectoryEntry? right, string? commonPath) FindBestPairForConsolidation_NoLock()
        {
            WatchedDirectoryEntry? bestLeft = null;
            WatchedDirectoryEntry? bestRight = null;
            string? bestPath = null;
            var bestSegmentCount = -1;

            for (var i = 0; i < _watchedDirectoryEntries.Count; i++)
            {
                for (var j = i + 1; j < _watchedDirectoryEntries.Count; j++)
                {
                    var left = _watchedDirectoryEntries[i];
                    var right = _watchedDirectoryEntries[j];

                    var commonPath = GetCommonDirectoryPath(left.Path, right.Path);
                    if (commonPath is null)
                        continue;

                    var segmentCount = CountSegments(commonPath);
                    if (segmentCount > bestSegmentCount)
                    {
                        bestLeft = left;
                        bestRight = right;
                        bestPath = commonPath;
                        bestSegmentCount = segmentCount;
                    }
                }
            }

            return (bestLeft, bestRight, bestPath);
        }

        private static string? GetCommonDirectoryPath(string leftPath, string rightPath)
        {
            var leftRoot = Path.GetPathRoot(leftPath);
            var rightRoot = Path.GetPathRoot(rightPath);
            if (leftRoot is null || rightRoot is null || !leftRoot.Equals(rightRoot, s_pathStringComparison))
                return null;

            var leftTrimmed = leftPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var rightTrimmed = rightPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            var leftParts = leftTrimmed.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var rightParts = rightTrimmed.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            var max = Math.Min(leftParts.Length, rightParts.Length);
            var commonParts = new List<string>(max);
            for (var i = 0; i < max; i++)
            {
                if (!leftParts[i].Equals(rightParts[i], s_pathStringComparison))
                    break;

                commonParts.Add(leftParts[i]);
            }

            if (commonParts.Count == 0)
                return null;

            var combined = string.Join(Path.DirectorySeparatorChar, commonParts);
            return combined.EndsWith(Path.DirectorySeparatorChar.ToString(), s_pathStringComparison)
                ? combined
                : combined + Path.DirectorySeparatorChar;
        }

        private static int CountSegments(string path)
            => path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                   .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                   .Length;

        private static bool IsContainedBy(string candidatePath, string containerPath)
            => candidatePath.StartsWith(containerPath, s_pathStringComparison);

        private bool ShouldRaiseForPath_NoLock(string filePath)
        {
            foreach (var entry in _watchedDirectoryEntries)
            {
                if (filePath.StartsWith(entry.Path, s_pathStringComparison))
                {
                    if (entry.ExtensionFilters.Length == 0 ||
                        entry.ExtensionFilters.Any(filter => filePath.EndsWith(filter, s_pathStringComparison)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private sealed class WatchedDirectoryEntry : IEventRaiser, IDisposable
        {
            private readonly FileChangeContext _context;
            private readonly WatchedDirectory _watchedDirectory;
            private IReferenceCountedDisposable<ICacheEntry<string, FileSystemWatcher>> _watcher;
            private bool _disposed;

            public WatchedDirectoryEntry(FileChangeContext context, DefaultFileChangeWatcher owner, WatchedDirectory watchedDirectory)
            {
                _context = context;
                _watchedDirectory = watchedDirectory;
                _watcher = owner.GetOrCreateSharedWatcher(watchedDirectory.Path);
                AttachWatcher(this, _watcher);
            }

            public string Path => _watchedDirectory.Path;
            public ImmutableArray<string> ExtensionFilters => _watchedDirectory.ExtensionFilters;

            void IEventRaiser.RaiseEvent(object? sender, FileSystemEventArgs e)
            {
                bool shouldRaise;
                string? oldPathToRaise = null;

                lock (_context._gate)
                {
                    shouldRaise = _context.ShouldRaiseForPath_NoLock(e.FullPath);

                    if (shouldRaise && e is RenamedEventArgs renamedEventArgs)
                        oldPathToRaise = renamedEventArgs.OldFullPath;
                }

                if (!shouldRaise)
                    return;

                _context.FileChanged?.Invoke(_context, e.FullPath);

                if (oldPathToRaise is not null)
                    _context.FileChanged?.Invoke(_context, oldPathToRaise);
            }

            public void Dispose()
            {
                if (_disposed)
                    return;

                _disposed = true;
                DetachAndDisposeWatcher(this, _watcher);
            }
        }

        internal static class TestAccessor
        {
            public static int GetWatchedDirectoryCount(FileChangeContext context)
                => context._watchedDirectoryEntries.Count;

            public static ImmutableArray<string> GetWatchedDirectories(FileChangeContext context)
                => [.. context._watchedDirectoryEntries.Select(entry => entry.Path)];
        }
    }
}
