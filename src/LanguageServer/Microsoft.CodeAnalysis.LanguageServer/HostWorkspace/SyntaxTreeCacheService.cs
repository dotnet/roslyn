// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

/// <summary>
/// Shares parsed syntax across workspaces by reusing immutable green syntax nodes.
/// </summary>
/// <remarks>
/// The cache cannot store green nodes directly because their types are internal to the compiler assemblies. Instead,
/// it stores weak references to public red <see cref="SyntaxNode"/> roots, which provide access to the green nodes they
/// wrap. A red root itself cannot be shared because it belongs to a specific <see cref="SyntaxTree"/> containing the
/// caller's source text, file path, encoding, and checksum information. Cache hits therefore create a new syntax tree
/// and red root around the cached green node.
/// <para>
/// Each cache key tracks all such red roots rather than only the most recently created root. Otherwise, unloading the
/// workspace that owns the newest root would make the cache miss even if an older root wrapping the same green node
/// were still alive. Weak references allow every workspace to determine the shared green node's lifetime without the
/// cache retaining any syntax tree.
/// </para>
/// </remarks>
[ExportWorkspaceService(typeof(ISyntaxTreeCacheService), ServiceLayer.Host), Shared]
internal sealed class SyntaxTreeCacheService : ISyntaxTreeCacheService
{
    private const int DefaultCleanupInterval = 10_000;

    private readonly bool _isDaemon;
    private readonly ConcurrentDictionary<CacheKey, CacheEntry> _entries = [];

    private int _addedRoots;
    private int _cleanupInProgress;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public SyntaxTreeCacheService(ServerConfiguration serverConfiguration)
    {
        _isDaemon = serverConfiguration.IsDaemon;
    }

    public SyntaxTree GetOrCreateSyntaxTree<TArg>(
        SourceText text,
        ParseOptions options,
        Func<TArg, CancellationToken, SyntaxTree> parseSyntaxTree,
        Func<SyntaxNode, TArg, SyntaxTree> createSyntaxTreeFromRoot,
        TArg arg,
        CancellationToken cancellationToken)
    {
        if (!_isDaemon)
            return parseSyntaxTree(arg, cancellationToken);

        var key = new CacheKey(Checksum.From(text.GetContentHash()), options);
        while (true)
        {
            var entry = _entries.GetOrAdd(key, static _ => new());
            var tree = entry.TryGetOrCreateSyntaxTree(
                parseSyntaxTree, createSyntaxTreeFromRoot, arg, cancellationToken, out var added);

            if (tree is null)
            {
                RemoveEntry(key, entry);
                continue;
            }

            if (added)
                CleanupIfNeeded();

            return tree;
        }
    }

    private void CleanupIfNeeded()
    {
        if (Interlocked.Increment(ref _addedRoots) % DefaultCleanupInterval == 0)
            RemoveDeadEntries();
    }

    private void RemoveDeadEntries()
    {
        if (Interlocked.CompareExchange(ref _cleanupInProgress, 1, 0) != 0)
            return;

        try
        {
            foreach (var (key, entry) in _entries)
            {
                if (entry.TryMarkRemovedIfEmpty())
                    RemoveEntry(key, entry);
            }
        }
        finally
        {
            Volatile.Write(ref _cleanupInProgress, 0);
        }
    }

    private void RemoveEntry(CacheKey key, CacheEntry entry)
        => ((ICollection<KeyValuePair<CacheKey, CacheEntry>>)_entries).Remove(new(key, entry));

    internal TestAccessor GetTestAccessor() => new(this);

    internal readonly struct TestAccessor(SyntaxTreeCacheService service)
    {
        public void TriggerCleanupOnNextAddedRoot()
            => service._addedRoots = DefaultCleanupInterval - 1;
    }

    private readonly record struct CacheKey(Checksum TextChecksum, ParseOptions Options);

    private sealed class CacheEntry
    {
        private readonly object _gate = new();
        private readonly List<WeakReference<SyntaxNode>> _roots = [];
        private bool _removed;

        public SyntaxTree? TryGetOrCreateSyntaxTree<TArg>(
            Func<TArg, CancellationToken, SyntaxTree> parseSyntaxTree,
            Func<SyntaxNode, TArg, SyntaxTree> createSyntaxTreeFromRoot,
            TArg arg,
            CancellationToken cancellationToken,
            out bool added)
        {
            lock (_gate)
            {
                added = false;
                if (_removed)
                    return null;

                if (TryGetRootAndPruneDeadReferences(out var cachedRoot))
                {
                    var cachedTree = createSyntaxTreeFromRoot(cachedRoot, arg);
                    _roots.Add(new(cachedTree.GetRoot(cancellationToken)));
                    return cachedTree;
                }

                var parsedTree = parseSyntaxTree(arg, cancellationToken);
                _roots.Add(new(parsedTree.GetRoot(cancellationToken)));
                added = true;
                return parsedTree;
            }
        }

        public bool TryMarkRemovedIfEmpty()
        {
            lock (_gate)
            {
                if (_removed || TryGetRootAndPruneDeadReferences(out _))
                    return false;

                _removed = true;
                return true;
            }
        }

        /// <summary>
        /// Removes dead references and returns any remaining live root.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> when at least one live root remains; otherwise, <see langword="false"/>.
        /// </returns>
        private bool TryGetRootAndPruneDeadReferences([NotNullWhen(true)] out SyntaxNode? root)
        {
            root = null;

            // Iterate backwards so removing an entry does not shift any indexes that remain to be inspected.
            for (var i = _roots.Count - 1; i >= 0; i--)
            {
                if (_roots[i].TryGetTarget(out var candidate))
                {
                    root ??= candidate;
                }
                else
                {
                    _roots.RemoveAt(i);
                }
            }

            return root is not null;
        }
    }
}
