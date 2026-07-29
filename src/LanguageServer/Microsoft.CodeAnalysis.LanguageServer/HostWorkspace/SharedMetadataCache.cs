// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Threading;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

/// <summary>
/// A weak cache of metadata shared by all workspaces created from the same host services.
/// </summary>
internal sealed class SharedMetadataCache(int cleanupThreshold = 500, bool collectStatistics = false)
{
    private readonly object _gate = new();
    private readonly int _cleanupThreshold = cleanupThreshold > 0 ? cleanupThreshold : throw new ArgumentOutOfRangeException(nameof(cleanupThreshold));
    private readonly bool _collectStatistics = collectStatistics;
    private readonly Dictionary<CacheKey, CacheEntry> _metadataCache = [];
    private int _addsSinceLastCleanup;
    private long _nextRequestId;
    private long _requestCount;
    private long _hitCount;
    private long _missCount;
    private long _metadataLoadCount;
    private long _failedLoadCount;
    private long _duplicateLoadCount;
    private long _nonCacheableLoadCount;
    private long _changedDuringLoadCount;
    private long _deadEntryRemovalCount;

    public Metadata GetMetadata(string fullPath, MetadataImageKind kind)
    {
        RecordStatistic(ref _requestCount);
        var timestamp = GetFileTimeStamp(fullPath);
        var key = new CacheKey(fullPath, kind);
        lock (_gate)
        {
            if (_metadataCache.TryGetValue(key, out var entry) &&
                entry.Timestamp == timestamp &&
                entry.Metadata.TryGetTarget(out var cachedMetadata))
            {
                RecordStatistic(ref _hitCount);
                return cachedMetadata;
            }
        }

        RecordStatistic(ref _missCount);
        var requestId = Interlocked.Increment(ref _nextRequestId);
        Metadata newMetadata;
        bool cacheable;
        try
        {
            (newMetadata, cacheable) = CreateMetadata(fullPath, kind);
        }
        catch
        {
            RecordStatistic(ref _failedLoadCount);
            throw;
        }

        RecordStatistic(ref _metadataLoadCount);
        if (!cacheable)
        {
            RecordStatistic(ref _nonCacheableLoadCount);
            return newMetadata;
        }

        try
        {
            // Do not cache metadata under a timestamp that changed while the file was being read.
            if (GetFileTimeStamp(fullPath) != timestamp)
            {
                RecordStatistic(ref _changedDuringLoadCount);
                return newMetadata;
            }
        }
        catch (IOException)
        {
            RecordStatistic(ref _changedDuringLoadCount);
            return newMetadata;
        }

        Metadata metadata;
        lock (_gate)
        {
            if (_metadataCache.TryGetValue(key, out var entry))
            {
                if (entry.Timestamp == timestamp && entry.Metadata.TryGetTarget(out var existingMetadata))
                {
                    RecordStatistic(ref _duplicateLoadCount);
                    metadata = existingMetadata;
                }
                else
                {
                    if (entry.RequestId < requestId)
                    {
                        entry.Metadata.SetTarget(newMetadata);
                        entry.Timestamp = timestamp;
                        entry.RequestId = requestId;
                        CleanUpIfNeeded_NoLock();
                    }

                    metadata = newMetadata;
                }
            }
            else
            {
                _metadataCache.Add(key, new CacheEntry(newMetadata, timestamp, requestId));
                metadata = newMetadata;
                CleanUpIfNeeded_NoLock();
            }
        }

        if (!ReferenceEquals(newMetadata, metadata))
            newMetadata.Dispose();

        return metadata;
    }

    internal Statistics GetStatistics()
    {
        lock (_gate)
        {
            return new Statistics(
                RequestCount: Volatile.Read(ref _requestCount),
                HitCount: Volatile.Read(ref _hitCount),
                MissCount: Volatile.Read(ref _missCount),
                MetadataLoadCount: Volatile.Read(ref _metadataLoadCount),
                FailedLoadCount: Volatile.Read(ref _failedLoadCount),
                DuplicateLoadCount: Volatile.Read(ref _duplicateLoadCount),
                NonCacheableLoadCount: Volatile.Read(ref _nonCacheableLoadCount),
                ChangedDuringLoadCount: Volatile.Read(ref _changedDuringLoadCount),
                DeadEntryRemovalCount: Volatile.Read(ref _deadEntryRemovalCount),
                EntryCount: _metadataCache.Count);
        }
    }

    private void RecordStatistic(ref long statistic)
    {
        if (_collectStatistics)
            Interlocked.Increment(ref statistic);
    }

    private void CleanUpIfNeeded_NoLock()
    {
        if (++_addsSinceLastCleanup < _cleanupThreshold)
            return;

        List<CacheKey>? deadKeys = null;
        foreach (var (key, entry) in _metadataCache)
        {
            if (!entry.Metadata.TryGetTarget(out _))
            {
                deadKeys ??= [];
                deadKeys.Add(key);
            }
        }

        if (deadKeys is not null)
        {
            foreach (var key in deadKeys)
                _metadataCache.Remove(key);

            if (_collectStatistics)
                Interlocked.Add(ref _deadEntryRemovalCount, deadKeys.Count);
        }

        _addsSinceLastCleanup = 0;
    }

    private static (Metadata metadata, bool cacheable) CreateMetadata(string fullPath, MetadataImageKind kind)
    {
        var module = ModuleMetadata.CreateFromStream(OpenRead(fullPath), PEStreamOptions.PrefetchEntireImage);

        if (kind == MetadataImageKind.Module)
            return (module, cacheable: true);

        try
        {
            // A manifest-only key cannot detect changes to secondary modules, so avoid sharing
            // multi-module assemblies until all constituent modules participate in the key.
            if (module.GetModuleNames().IsEmpty)
                return (AssemblyMetadata.Create(module), cacheable: true);

            module.Dispose();
            return (MetadataReference.CreateFromFile(fullPath).GetMetadata(), cacheable: false);
        }
        catch
        {
            module.Dispose();
            throw;
        }
    }

    private static Stream OpenRead(string fullPath)
    {
        try
        {
            return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (DirectoryNotFoundException e)
        {
            throw new FileNotFoundException(e.Message, fullPath, e);
        }
        catch (IOException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw new IOException(e.Message, e);
        }
    }

    private static DateTime GetFileTimeStamp(string fullPath)
    {
        try
        {
            return File.GetLastWriteTimeUtc(fullPath);
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (DirectoryNotFoundException e)
        {
            throw new FileNotFoundException(e.Message, fullPath, e);
        }
        catch (IOException)
        {
            throw;
        }
        catch (Exception e)
        {
            throw new IOException(e.Message, e);
        }
    }

    private readonly struct CacheKey(string fullPath, MetadataImageKind kind) : IEquatable<CacheKey>
    {
        private readonly string _fullPath = fullPath;
        private readonly MetadataImageKind _kind = kind;

        public bool Equals(CacheKey other)
            => _kind == other._kind
                && string.Equals(_fullPath, other._fullPath, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object? obj)
            => obj is CacheKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = StringComparer.OrdinalIgnoreCase.GetHashCode(_fullPath);
                return (hash * 397) ^ (int)_kind;
            }
        }
    }

    private sealed class CacheEntry(Metadata metadata, DateTime timestamp, long requestId)
    {
        public WeakReference<Metadata> Metadata { get; } = new(metadata);
        public DateTime Timestamp { get; set; } = timestamp;
        public long RequestId { get; set; } = requestId;
    }

    internal readonly record struct Statistics(
        long RequestCount,
        long HitCount,
        long MissCount,
        long MetadataLoadCount,
        long FailedLoadCount,
        long DuplicateLoadCount,
        long NonCacheableLoadCount,
        long ChangedDuringLoadCount,
        long DeadEntryRemovalCount,
        int EntryCount);
}
