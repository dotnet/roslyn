// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

/// <summary>
/// A weak cache of metadata shared by all workspaces created from the same host services.
/// </summary>
internal sealed class SharedMetadataCache(int cleanupThreshold = 500)
{
    private readonly object _gate = new();
    private readonly int _cleanupThreshold = cleanupThreshold > 0 ? cleanupThreshold : throw new ArgumentOutOfRangeException(nameof(cleanupThreshold));
    private readonly Dictionary<CacheKey, CacheEntry> _metadataCache = [];
    private int _addsSinceLastCleanup;
    private long _nextRequestId;

    public MetadataProviderResult GetMetadata(
        string fullPath,
        MetadataImageKind kind,
        Func<string, MetadataImageKind, MetadataProviderResult> getMetadata)
    {
        var timestamp = GetFileTimeStamp(fullPath);
        var key = new CacheKey(fullPath, kind);
        lock (_gate)
        {
            if (_metadataCache.TryGetValue(key, out var entry) &&
                entry.Timestamp == timestamp &&
                entry.Metadata.TryGetTarget(out var cachedMetadata))
            {
                return new(cachedMetadata, IsCacheable: true);
            }
        }

        var requestId = Interlocked.Increment(ref _nextRequestId);
        var result = getMetadata(fullPath, kind);
        var newMetadata = result.Metadata;

        if (!result.IsCacheable)
            return result;

        try
        {
            // Do not cache metadata under a timestamp that changed while the file was being read.
            if (GetFileTimeStamp(fullPath) != timestamp)
                return result;
        }
        catch (IOException)
        {
            return result;
        }

        Metadata metadata;
        lock (_gate)
        {
            if (_metadataCache.TryGetValue(key, out var entry))
            {
                if (entry.Timestamp == timestamp && entry.Metadata.TryGetTarget(out var existingMetadata))
                {
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

        return new(metadata, IsCacheable: true);
    }

    internal TestAccessor GetTestAccessor()
        => new(this);

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
        }

        _addsSinceLastCleanup = 0;
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

    internal readonly struct TestAccessor(SharedMetadataCache cache)
    {
        internal int EntryCount
        {
            get
            {
                lock (cache._gate)
                    return cache._metadataCache.Count;
            }
        }
    }
}
