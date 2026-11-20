// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

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
internal sealed class SyntaxTreeCacheService : ISyntaxTreeCacheService
{
    /// <summary>
    /// An arbitrary initial threshold that can be tuned based on observed cache behavior.
    /// </summary>
    private const int DefaultCleanupInterval = 10_000;

    private readonly ConcurrentDictionary<CacheKey, CacheEntry> _entries = [];

    private int _addedRoots;

    public SyntaxTree GetOrCreateSyntaxTree<TArg>(
        SourceText text,
        ParseOptions options,
        Func<TArg, CancellationToken, SyntaxTree> parseSyntaxTree,
        Func<SyntaxNode, TArg, SyntaxTree> createSyntaxTreeFromRoot,
        TArg arg,
        CancellationToken cancellationToken)
    {
        var key = new CacheKey(Checksum.From(text.GetContentHash()), options);
        while (true)
        {
            var entry = _entries.GetOrAdd(key, static _ => new());
            var tree = entry.TryGetOrCreateSyntaxTree(
                parseSyntaxTree, createSyntaxTreeFromRoot, arg, cancellationToken, out var added);

            if (tree is null)
            {
                _entries.TryRemove(new(key, entry));
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
        foreach (var (key, entry) in _entries)
        {
            if (entry.TryMarkRemovedIfEmpty())
                _entries.TryRemove(new(key, entry));
        }
    }

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

                if (GetRootAndPruneDeadReferences() is { } cachedRoot)
                {
                    var cachedTree = createSyntaxTreeFromRoot(cachedRoot, arg);
                    _roots.Add(new(cachedTree.GetRoot(cancellationToken)));
                    // Do not set added: cleanup is based on cache growth, not additional users of an existing entry.
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
                if (_removed || GetRootAndPruneDeadReferences() is not null)
                    return false;

                _removed = true;
                return true;
            }
        }

        /// <summary>
        /// Removes dead references and returns any remaining live root.
        /// </summary>
        /// <returns>
        /// A remaining live root, or <see langword="null"/> when all references are dead.
        /// </returns>
        private SyntaxNode? GetRootAndPruneDeadReferences()
        {
            Contract.ThrowIfFalse(Monitor.IsEntered(_gate));

            SyntaxNode? root = null;

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

            return root;
        }
    }
}

[ExportWorkspaceServiceFactory(typeof(ISyntaxTreeCacheService), ServiceLayer.Host), Shared]
internal sealed class SyntaxTreeCacheServiceFactory : IWorkspaceServiceFactory
{
    private readonly SyntaxTreeCacheService? _service;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public SyntaxTreeCacheServiceFactory(ServerConfiguration serverConfiguration)
    {
        if (serverConfiguration.IsDaemon)
            _service = new();
    }

    // Although the return type is non-nullable, IWorkspaceServiceFactory explicitly permits null when the service
    // is not applicable to the workspace.
    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        => _service!;
}
