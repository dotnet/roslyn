// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace.FileWatching;

/// <summary>
/// An implementation of <see cref="IFileChangeWatcher" /> that is built atop the framework <see cref="FileSystemWatcher" />. This is used if we can't
/// use the LSP one.
/// </summary>
/// <remarks>
/// This implementation creates FileSystemWatchers for each directory being watched, reusing watchers where possible, and trying to consolidate watchers to stay under a
/// maximum.
/// </remarks>
internal sealed partial class DefaultFileChangeWatcher : IFileChangeWatcher
{
    private static readonly StringComparer s_pathStringComparer = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? StringComparer.OrdinalIgnoreCase
        : StringComparer.Ordinal;
    private static readonly StringComparison s_pathStringComparison = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? StringComparison.OrdinalIgnoreCase
        : StringComparison.Ordinal;

    /// <summary>
    /// A monitor to be held for all manipulations to the entire tree structure. For now, no effort is made to to reduce locking with something more fine-grained, since the
    /// primary goal of this implementation is to ensure we're respecting operating system limits. The only unsynchronized action that does not acquire this lock is the actual
    /// reporting of file changes; since <see cref="DirectoryNode.ActiveContexts"/> is an ImmutableArray we can enumerate it safely.
    /// </summary>
    private readonly object _gate = new object();
    private readonly Dictionary<string, DirectoryNode> _roots = new(s_pathStringComparer);
    private readonly int _maxWatcherCount;
    private int _currentWatcherCount = 0;

    /// <param name="maxWatcherCount">The maxiumum number of watchers to create. Note this technically a soft limit -- it might be the case that we simply have to create more if we have to create
    /// a number of watchers for different drives or roots. For example, if the limit is 10 and we have to watch files on 11 different drives, we'll have no choice but to exceed our limit.</param>
    public DefaultFileChangeWatcher(int maxWatcherCount = 1000)
    {
        _maxWatcherCount = maxWatcherCount;
    }

    public IFileChangeContext CreateContext(ImmutableArray<WatchedDirectory> watchedDirectories)
        => new FileChangeContext(this, watchedDirectories);

    private IDisposable AcquireDirectoryWatch(WatchedDirectory watchedDirectory, FileChangeContext fileChangeContext)
    {
        var watchedDirectoryPath = watchedDirectory.Path.AsSpan();

        lock (_gate)
        {
            // First create the node that represents the root of the path we're trying to watch.
            var root = Path.GetPathRoot(watchedDirectoryPath);

            if (!_roots.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(root, out var node))
            {
                var rootString = root.ToString();
                node = new DirectoryNode { Path = rootString, Parent = null };
                _roots.Add(rootString, node);
            }

            // We're now going to navigate our tree to create a full set of nodes for this path.
            bool parentAlreadyHadFileWatch = false;
            var pathRelativeToRoot = watchedDirectoryPath[root.Length..];
            foreach (var pathComponentRange in pathRelativeToRoot.SplitAny([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar]))
            {
                var pathComponent = pathRelativeToRoot[pathComponentRange];

                // Empty component, just ignore it
                if (pathComponent.Length == 0)
                    continue;

                if (node.Watcher is not null)
                {
                    if (!parentAlreadyHadFileWatch)
                    {
                        // We already have a watcher on this node, so we can just reuse it, but need to ensure filters are good enough to include us
                        ExpandFiltersToCover(node.Watcher.Filters, watchedDirectory.ExtensionFilters.SelectAsArray(static extension => '*' + extension));
                        node.ActiveContexts = node.ActiveContexts.Add(fileChangeContext);
                    }

                    parentAlreadyHadFileWatch = true;
                }

                if (!node.Children.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(pathComponent, out var childNode))
                {
                    childNode = new DirectoryNode { Path = watchedDirectoryPath[0..(root.Length + pathComponentRange.End.Value)].ToString(), Parent = node };
                    node.Children.Add(pathComponent.ToString(), childNode);
                }

                // Before we move to the child, we want to record that we'll have an active context below this one.
                node.ActiveContextsRecursiveCount++;
                node = childNode;
            }

            if (!parentAlreadyHadFileWatch)
            {
                if (node.Watcher is not null)
                {
                    // We already have a watcher on this node, so we can just reuse it, but need to ensure filters are good enough to include us
                    ExpandFiltersToCover(node.Watcher.Filters, watchedDirectory.ExtensionFilters.SelectAsArray(static extension => '*' + extension));
                    node.ActiveContexts = node.ActiveContexts.Add(fileChangeContext);
                }
                else
                {
                    // We will have to create a file watch for this directory and we have nothing to cover it; walk back up until we find a directory to watch.
                    // Ideally it'll just the final one we created, but since we can't watch an non-existent directory we might have to walk back a bit.
                    var parentNodeForWatch = node;
                    while (parentNodeForWatch is not null)
                    {
                        if (Directory.Exists(parentNodeForWatch.Path))
                        {
                            parentNodeForWatch.CreateNewFileWatcher(watchedDirectory.ExtensionFilters.SelectAsArray(static extension => '*' + extension));
                            _currentWatcherCount++;

                            // It's possible this is a watch higher up than a directory we've previously watched, so consolidate any further children
                            var allActiveContextsBuilder = parentNodeForWatch.ActiveContexts.ToBuilder();
                            allActiveContextsBuilder.Capacity = parentNodeForWatch.ActiveContextsRecursiveCount - parentNodeForWatch.ActiveContexts.Length + 1;
                            allActiveContextsBuilder.Add(fileChangeContext);

                            IList<string>? filters = parentNodeForWatch.Watcher.Filters;

                            RemoveWatchersFromChildrenForConsolidation_NoLock(parentNodeForWatch, ref filters, allActiveContextsBuilder);

                            Contract.ThrowIfFalse(filters == parentNodeForWatch.Watcher.Filters, "We should have been updating the existing list of filters.");

                            parentNodeForWatch.ActiveContexts = allActiveContextsBuilder.ToImmutable();
                            break;
                        }
                        else
                        {
                            parentNodeForWatch = parentNodeForWatch.Parent;
                        }
                    }

                    // We've now added a new watcher; if we're above our limit then we need to consolidate
                    FindBestNodeToConsolidateAndConsolidateIt_NoLock();
                }
            }

            node.ActiveContextsRecursiveCount++;

            return new DirectoryWatch(this, node, fileChangeContext);
        }
    }

    private void ReleaseWatch(DirectoryNode node, FileChangeContext fileChangeContext)
    {
        lock (_gate)
        {
            var current = node;
            while (current is not null)
            {
                Contract.ThrowIfFalse(current.ActiveContextsRecursiveCount > 0);
                current.ActiveContextsRecursiveCount--;

                // If this node's ActiveContexts contains the context, remove it; this is necessary to check at all levels since we might have moved it up
                // if we consolidated.
                current.ActiveContexts = current.ActiveContexts.Remove(fileChangeContext);

                // If this node is no longer needed, then we can clean it up
                if (current.ActiveContextsRecursiveCount == 0)
                {
                    Contract.ThrowIfFalse(current.ActiveContexts.IsDefaultOrEmpty);
                    Contract.ThrowIfFalse(current.Children.Count == 0, "If we have no active contexts, we should have no children as well.");

                    if (current.Watcher is not null)
                    {
                        current.DisposeWatcher();
                        _currentWatcherCount--;
                    }

                    // Prune this node from its parent if it's now completely empty (no watcher, no contexts, no children).
                    if (current.Parent is not null)
                    {
                        // Find and remove from parent's Children dictionary.
                        foreach (var (key, value) in current.Parent.Children)
                        {
                            if (ReferenceEquals(value, current))
                            {
                                current.Parent.Children.Remove(key);
                                break;
                            }
                        }
                    }
                    else
                    {
                        // This is a root node; remove it from _roots.
                        Contract.ThrowIfFalse(_roots.Remove(current.Path));
                    }
                }

                current = current.Parent;
            }
        }
    }

    private void FindBestNodeToConsolidateAndConsolidateIt_NoLock()
    {
        Contract.ThrowIfFalse(Monitor.IsEntered(_gate));

        if (_currentWatcherCount < _maxWatcherCount)
            return;

        // Find the root with the most watchers; that's where the biggest win would be
        var nodeToConsolidate = _roots.Values.MaxBy(static r => r.ActiveWatchersRecursiveCount);

        Contract.ThrowIfNull(nodeToConsolidate, "Why are we consolidating when we have no watchers?");

        if (nodeToConsolidate.ActiveWatchersRecursiveCount == 1)
        {
            // Well, this means each watcher we have is on a different drive, so we can't actually consolidate.
            return;
        }

        // Let's look at the children -- is there any child of this node that is worth consolidating to keep our watches precise?
        while (true)
        {
            var childToConsolidate = nodeToConsolidate.Children.Values.MaxBy(static r => r.ActiveWatchersRecursiveCount);

            if (childToConsolidate is not null && childToConsolidate.ActiveWatchersRecursiveCount > 1)
            {
                // Sure, let's do this instead, but we'll keep recursing further
                nodeToConsolidate = childToConsolidate;
            }
            else
            {
                // Nope nothing more precise, so we found it
                IList<string>? filters = null;
                var activeContexts = ImmutableArray.CreateBuilder<FileChangeContext>(initialCapacity: nodeToConsolidate.ActiveContextsRecursiveCount);

                RemoveWatchersFromChildrenForConsolidation_NoLock(nodeToConsolidate, ref filters, activeContexts);

                Contract.ThrowIfNull(filters, "We knew we had at least one watcher to consolidate, so we should have consolidated it's filters.");
                Contract.ThrowIfTrue(activeContexts.Count == 0, "We had at least one watcher to consolidate, and that should have had at least one context listening.");
                nodeToConsolidate.CreateNewFileWatcher(filters.ToImmutableArray());
                nodeToConsolidate.ActiveContexts = activeContexts.ToImmutable();
                _currentWatcherCount++;
                break;
            }
        }

        // And now assert we're back under our limit
        Contract.ThrowIfFalse(_currentWatcherCount <= _maxWatcherCount);
    }

    /// <summary>
    /// Recursively removes the watchers from the given node's children and all it's descendants, and updates the list of filters and activeContexts
    /// a consolidated node should have.
    /// </summary>
    /// <param name="filters">
    /// The list of filters. Our convention is an empty list of filters means the entire directory is being watched,
    /// but we need a different value to indicate that we haven't found a watcher at all yet and we should use it's filters when we find it.
    /// </param>
    private void RemoveWatchersFromChildrenForConsolidation_NoLock(DirectoryNode node, ref IList<string>? filters, ImmutableArray<FileChangeContext>.Builder activeContexts)
    {
        Contract.ThrowIfFalse(Monitor.IsEntered(_gate));

        foreach (var child in node.Children.Values)
        {
            if (child.Watcher is not null)
            {
                // We're going to get rid of this child's watcher; we'll transfer all filters up and also transfer up the active contexts
                // that are listening. That now means those other contexts will also get notified, but it's up for them to do final filtering
                // to ensure they only react to the files they care about. Doing this now means we can avoid traversing the tree on each
                // event to know which contexts to notify. This might be a bad tradeoff -- preserving the original structure would potentially
                // mean we can filter events better by parsing the file paths out and more quickly navigating our tree structure.
                if (filters is null)
                {
                    filters = new List<string>(child.Watcher.Filters);
                }
                else
                {
                    ExpandFiltersToCover(filters, [.. child.Watcher.Filters]);
                }

                activeContexts.AddRange(child.ActiveContexts);
                child.ActiveContexts = ImmutableArray<FileChangeContext>.Empty;
                child.DisposeWatcher();
                _currentWatcherCount--;
            }
            else if (child.ActiveWatchersRecursiveCount > 0)
            {
                RemoveWatchersFromChildrenForConsolidation_NoLock(child, ref filters, activeContexts);
            }
        }
    }

    /// <summary>
    /// Represents a single directory in the directory tree structure.
    /// </summary>
    private sealed class DirectoryNode
    {
        public required string Path { get; init; }

        /// <summary>
        /// The parent directory, or null if this is the root of a drive.
        /// </summary>
        public required DirectoryNode? Parent { get; init; }
        public Dictionary<string, DirectoryNode> Children { get; } = new Dictionary<string, DirectoryNode>(s_pathStringComparer);

        /// <summary>
        /// The watcher for this directory, if we have one.
        /// </summary>
        public FileSystemWatcher? Watcher { get; private set; }

        /// <summary>
        /// The set of contexts that are subscribed to this location. In the case of consolidation, these contexts are moved towards the directory with the actual watch
        /// so we can avoid having to recursively enumerate the tree each time we have to fire an event. This is immutable so firing events can be done without taking a lock;
        /// see <see cref="_gate"/>'s comments for the overall threading model here.
        /// </summary>
        public ImmutableArray<FileChangeContext> ActiveContexts { get; set; } = ImmutableArray<FileChangeContext>.Empty;

        /// <summary>
        /// The total number of <see cref="ActiveContexts"/> in this node and all child nodes.
        /// </summary>
        public int ActiveContextsRecursiveCount { get; set; }

        /// <summary>
        /// The total number of <see cref="Watcher"/> in this node and all child nodes.
        /// </summary>
        public int ActiveWatchersRecursiveCount { get; private set; }

        [MemberNotNull(nameof(Watcher))]
        public void CreateNewFileWatcher(ImmutableArray<string> filters)
        {
            Contract.ThrowIfTrue(Watcher is not null);
            Contract.ThrowIfFalse(ActiveContexts.IsEmpty);

            Watcher = new FileSystemWatcher(Path);
            Watcher.Filters.AddRange(filters);

            Watcher.Created += OnFileSystemWatcherEvent;
            Watcher.Changed += OnFileSystemWatcherEvent;
            Watcher.Renamed += OnFileSystemWatcherEvent;
            Watcher.Deleted += OnFileSystemWatcherEvent;

            Watcher.IncludeSubdirectories = true;
            Watcher.EnableRaisingEvents = true;

            UpdateActiveWatchersRecursiveCountIncludingAllParents(delta: 1);
        }

        private void UpdateActiveWatchersRecursiveCountIncludingAllParents(int delta)
        {
            var node = this;
            while (node is not null)
            {
                node.ActiveWatchersRecursiveCount += delta;
                Contract.ThrowIfTrue(node.ActiveWatchersRecursiveCount < 0);
                node = node.Parent;
            }
        }

        public void DisposeWatcher()
        {
            // Assert here, usually because the caller already had checked this since it had other logic
            Contract.ThrowIfNull(Watcher);
            Contract.ThrowIfTrue(ActiveContexts.Length > 0);

            Watcher.Dispose();
            Watcher = null;

            UpdateActiveWatchersRecursiveCountIncludingAllParents(delta: -1);
        }

        private void OnFileSystemWatcherEvent(object sender, FileSystemEventArgs e)
        {
            var activeContexts = ActiveContexts;
            foreach (var activeContext in activeContexts)
                activeContext.OnFileSystemEvent(e);
        }
    }

    private sealed class DirectoryWatch : IDisposable
    {
        /// <summary>
        /// The parent object; mutable and nullable so we can set it to null when we're disposed to ensure an accidental double-dispose doesn't break anything.
        /// </summary>
        private DefaultFileChangeWatcher? _owner;
        private readonly DirectoryNode _node;
        private readonly FileChangeContext _fileChangeContext;

        public DirectoryWatch(DefaultFileChangeWatcher owner, DirectoryNode node, FileChangeContext fileChangeContext)
        {
            _owner = owner;
            _node = node;
            _fileChangeContext = fileChangeContext;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _owner, value: null) is { } owner)
            {
                owner.ReleaseWatch(_node, _fileChangeContext);
            }
        }
    }

    /// <summary>
    /// Merges the list of filters in <paramref name="newFilters"/> into <paramref name="watcherFilters"/>. The convention is an empty list
    /// means everything, so if either list is empty, the result is an empty list. Otherwise, we merge the two lists and remove duplicates.
    /// </summary>
    private static void ExpandFiltersToCover(ICollection<string> watcherFilters, IEnumerable<string> newFilters)
    {
        // If we're already watching everything, can just be done
        if (watcherFilters.Count == 0)
            return;

        bool hadAFilter = false;

        foreach (var newFilter in newFilters)
        {
            hadAFilter = true;
            if (!watcherFilters.Contains(newFilter))
                watcherFilters.Add(newFilter);
        }

        if (!hadAFilter)
            watcherFilters.Clear();
    }

    internal static class TestAccessor
    {
        public static IEnumerable<(string path, ImmutableArray<string> filters)> GetWatchedDirectories(DefaultFileChangeWatcher watcher)
        {
            var paths = new List<(string path, ImmutableArray<string> filters)>(capacity: watcher._currentWatcherCount);

            int currentWatcherCountPerRoots = 0;

            foreach (var root in watcher._roots.Values)
            {
                currentWatcherCountPerRoots += root.ActiveWatchersRecursiveCount;
                AddWatchedDirectoryPaths(root);
            }

            Contract.ThrowIfTrue(paths.Count != watcher._currentWatcherCount, $"{nameof(watcher._currentWatcherCount)} is out of sync with the actual number of watchers.");
            Contract.ThrowIfTrue(currentWatcherCountPerRoots != watcher._currentWatcherCount, $"{nameof(watcher._currentWatcherCount)} is out of sync with the actual number of watchers in the roots.");

            return paths;

            void AddWatchedDirectoryPaths(DirectoryNode node)
            {
                if (node.Watcher is not null)
                    paths.Add((node.Path, node.Watcher.Filters.ToImmutableArray()));

                foreach (var child in node.Children.Values)
                    AddWatchedDirectoryPaths(child);
            }
        }
    }
}
