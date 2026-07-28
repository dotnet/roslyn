// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Threading;

namespace Microsoft.CodeAnalysis.Host;

/// <summary>
/// A bounded cache of metadata shared by all workspaces created from the same host services.
/// </summary>
internal sealed class SharedMetadataCache(int capacity = 500)
{
    private readonly object _gate = new();
    private readonly int _capacity = capacity > 0 ? capacity : throw new ArgumentOutOfRangeException(nameof(capacity));
    private readonly Dictionary<CacheKey, CacheEntry> _metadataCache = new(capacity);
    private readonly LinkedList<CacheKey> _lru = [];
    private long _nextRequestId;

    public Metadata GetMetadata(string fullPath, MetadataImageKind kind)
    {
        var timestamp = GetFileTimeStamp(fullPath);
        var key = new CacheKey(fullPath, kind);
        lock (_gate)
        {
            if (_metadataCache.TryGetValue(key, out var entry) && entry.Timestamp == timestamp)
            {
                MoveToFront(entry.Node);
                return entry.Metadata;
            }
        }

        var requestId = Interlocked.Increment(ref _nextRequestId);
        var (newMetadata, cacheable) = CreateMetadata(fullPath, kind);
        if (!cacheable)
            return newMetadata;

        try
        {
            // Do not cache metadata under a timestamp that changed while the file was being read.
            if (GetFileTimeStamp(fullPath) != timestamp)
                return newMetadata;
        }
        catch (IOException)
        {
            return newMetadata;
        }

        Metadata metadata;
        lock (_gate)
        {
            if (_metadataCache.TryGetValue(key, out var entry))
            {
                if (entry.Timestamp == timestamp)
                {
                    metadata = entry.Metadata;
                }
                else
                {
                    if (entry.RequestId < requestId)
                    {
                        entry.Metadata = newMetadata;
                        entry.Timestamp = timestamp;
                        entry.RequestId = requestId;
                    }

                    metadata = newMetadata;
                }

                MoveToFront(entry.Node);
            }
            else
            {
                if (_metadataCache.Count == _capacity)
                {
                    var lastNode = _lru.Last!;
                    _lru.RemoveLast();
                    _metadataCache.Remove(lastNode.Value);
                }

                var node = _lru.AddFirst(key);
                _metadataCache.Add(key, new CacheEntry(newMetadata, timestamp, requestId, node));
                metadata = newMetadata;
            }
        }

        if (!ReferenceEquals(newMetadata, metadata))
            newMetadata.Dispose();

        return metadata;
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

    private void MoveToFront(LinkedListNode<CacheKey> node)
    {
        if (!ReferenceEquals(_lru.First, node))
        {
            _lru.Remove(node);
            _lru.AddFirst(node);
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

    private sealed class CacheEntry(Metadata metadata, DateTime timestamp, long requestId, LinkedListNode<CacheKey> node)
    {
        public Metadata Metadata { get; set; } = metadata;
        public DateTime Timestamp { get; set; } = timestamp;
        public long RequestId { get; set; } = requestId;
        public LinkedListNode<CacheKey> Node { get; } = node;
    }
}
