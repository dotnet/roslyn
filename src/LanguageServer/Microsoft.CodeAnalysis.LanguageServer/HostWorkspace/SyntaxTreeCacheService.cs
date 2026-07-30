// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

[ExportWorkspaceService(typeof(ISyntaxTreeCacheService), ServiceLayer.Host), Shared]
internal sealed class SyntaxTreeCacheService : ISyntaxTreeCacheService
{
    // Accommodates several large solutions while placing a fixed upper bound on retained cache keys.
    private const int DefaultMaximumEntryCount = 100_000;
    private const int MinimumCleanupInterval = 256;

    private readonly ConcurrentDictionary<SyntaxTreeCacheKey, WeakReference<SyntaxNode>> _roots = [];
    private readonly int _maximumEntryCount;
    private readonly int _cleanupInterval;

    private int _entryCount;
    private int _publicationCount;
    private int _admissionAttemptCount;
    private int _cleanupInProgress;
    private int _lookupCount;
    private int _hitCount;
    private int _deadRootCount;
    private int _publicationRaceCount;
    private int _admissionBypassCount;
    private int _cleanupCount;
    private int _refreshCount;

    [ImportingConstructor]
    [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
    public SyntaxTreeCacheService()
        : this(DefaultMaximumEntryCount)
    {
    }

    internal SyntaxTreeCacheService(int maximumEntryCount)
    {
        Contract.ThrowIfFalse(maximumEntryCount > 0);
        _maximumEntryCount = maximumEntryCount;
        _cleanupInterval = Math.Max(MinimumCleanupInterval, maximumEntryCount / 10);
    }

    public SyntaxTreeCacheKey CreateKey(string language, SourceText text, ParseOptions options)
        => new(language, Checksum.From(text.GetContentHash()), options);

    public bool TryGetRoot(SyntaxTreeCacheKey key, out SyntaxNode? root)
    {
        Interlocked.Increment(ref _lookupCount);

        if (_roots.TryGetValue(key, out var weakRoot) &&
            weakRoot.TryGetTarget(out root))
        {
            Interlocked.Increment(ref _hitCount);
            return true;
        }

        if (weakRoot is not null)
        {
            Interlocked.Increment(ref _deadRootCount);
            RemoveDeadEntry(key, weakRoot);
        }

        root = null;
        return false;
    }

    public SyntaxNode GetOrAddRoot(SyntaxTreeCacheKey key, SyntaxNode root)
    {
        var newWeakRoot = new WeakReference<SyntaxNode>(root);

        while (true)
        {
            if (_roots.TryGetValue(key, out var existingWeakRoot))
            {
                if (existingWeakRoot.TryGetTarget(out var existingRoot))
                {
                    Interlocked.Increment(ref _publicationRaceCount);
                    return existingRoot;
                }

                if (_roots.TryUpdate(key, newWeakRoot, existingWeakRoot))
                    return root;

                continue;
            }

            if (!TryReserveEntry())
            {
                Interlocked.Increment(ref _admissionBypassCount);
                return root;
            }

            if (_roots.TryAdd(key, newWeakRoot))
            {
                CleanupIfNeeded();
                return root;
            }

            Interlocked.Decrement(ref _entryCount);
        }
    }

    public void RefreshRoot(SyntaxTreeCacheKey key, SyntaxNode root)
    {
        var newWeakRoot = new WeakReference<SyntaxNode>(root);

        while (_roots.TryGetValue(key, out var existingWeakRoot))
        {
            if (_roots.TryUpdate(key, newWeakRoot, existingWeakRoot))
            {
                Interlocked.Increment(ref _refreshCount);
                return;
            }
        }

        _ = GetOrAddRoot(key, root);
    }

    private bool TryReserveEntry()
    {
        while (true)
        {
            var entryCount = Volatile.Read(ref _entryCount);
            if (entryCount >= _maximumEntryCount)
            {
                CleanupForAdmission();
                entryCount = Volatile.Read(ref _entryCount);
                if (entryCount >= _maximumEntryCount)
                    return false;
            }

            if (Interlocked.CompareExchange(ref _entryCount, entryCount + 1, entryCount) == entryCount)
                return true;
        }
    }

    private void CleanupIfNeeded()
    {
        if (Interlocked.Increment(ref _publicationCount) % _cleanupInterval == 0)
            RemoveDeadEntries();
    }

    private void CleanupForAdmission()
    {
        var admissionAttemptCount = Interlocked.Increment(ref _admissionAttemptCount);
        if (admissionAttemptCount == 1 || admissionAttemptCount % _cleanupInterval == 0)
            RemoveDeadEntries();
    }

    private void RemoveDeadEntries()
    {
        if (Interlocked.CompareExchange(ref _cleanupInProgress, 1, 0) != 0)
            return;

        try
        {
            Interlocked.Increment(ref _cleanupCount);

            foreach (var (key, weakRoot) in _roots)
            {
                if (!weakRoot.TryGetTarget(out _))
                    RemoveDeadEntry(key, weakRoot);
            }
        }
        finally
        {
            Volatile.Write(ref _cleanupInProgress, 0);
        }
    }

    private void RemoveDeadEntry(SyntaxTreeCacheKey key, WeakReference<SyntaxNode> weakRoot)
    {
        if (((ICollection<KeyValuePair<SyntaxTreeCacheKey, WeakReference<SyntaxNode>>>)_roots).Remove(new(key, weakRoot)))
            Interlocked.Decrement(ref _entryCount);
    }

    internal int EntryCount
        => Volatile.Read(ref _entryCount);

    internal TestAccessor GetTestAccessor()
        => new(this);

    internal readonly struct TestAccessor(SyntaxTreeCacheService service)
    {
        public int EntryCount => Volatile.Read(ref service._entryCount);
        public int LookupCount => Volatile.Read(ref service._lookupCount);
        public int HitCount => Volatile.Read(ref service._hitCount);
        public int DeadRootCount => Volatile.Read(ref service._deadRootCount);
        public int PublicationRaceCount => Volatile.Read(ref service._publicationRaceCount);
        public int AdmissionBypassCount => Volatile.Read(ref service._admissionBypassCount);
        public int CleanupCount => Volatile.Read(ref service._cleanupCount);
        public int RefreshCount => Volatile.Read(ref service._refreshCount);
    }
}
