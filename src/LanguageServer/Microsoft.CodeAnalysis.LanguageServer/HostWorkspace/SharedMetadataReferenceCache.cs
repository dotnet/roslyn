// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.LanguageServer.HostWorkspace;

/// <summary>
/// A weak cache of metadata references shared by all workspaces created from the same host services.
/// </summary>
internal sealed class SharedMetadataReferenceCache(int cleanupThreshold = 500)
{
    private readonly ConcurrentDictionary<CacheKey, ReferenceSet> _referenceSets = [];
    private int _addsSinceLastCleanup;

    public PortableExecutableReference GetReference(
        string fullPath,
        MetadataReferenceProperties properties,
        Func<string, MetadataReferenceProperties, PortableExecutableReference> createReference)
    {
        var key = new CacheKey(fullPath, properties.Kind);
        while (true)
        {
            var referenceSet = _referenceSets.GetOrAdd(key, static _ => new ReferenceSet());
            if (!referenceSet.TryGetReference(
                    fullPath, properties, createReference, out var result, out var added))
            {
                _referenceSets.TryRemove(new KeyValuePair<CacheKey, ReferenceSet>(key, referenceSet));
                continue;
            }

            if (added)
                CleanUpIfNeeded();

            return result;
        }
    }

    internal TestAccessor GetTestAccessor()
        => new(this);

    private void CleanUpIfNeeded()
    {
        if (Interlocked.Increment(ref _addsSinceLastCleanup) < cleanupThreshold ||
            Interlocked.Exchange(ref _addsSinceLastCleanup, 0) < cleanupThreshold)
        {
            return;
        }

        foreach (var pair in _referenceSets)
        {
            if (pair.Value.TryMarkRemovedIfNoLiveReferences())
                _referenceSets.TryRemove(pair);
        }
    }

    private sealed class ReferenceSet
    {
        private readonly object _gate = new();
        private readonly Dictionary<MetadataReferenceProperties, WeakReference<PortableExecutableReference>> _references = [];
        private DateTime? _timestamp;
        private bool _isRemoved;

        public bool TryGetReference(
            string fullPath,
            MetadataReferenceProperties properties,
            Func<string, MetadataReferenceProperties, PortableExecutableReference> createReference,
            out PortableExecutableReference result,
            out bool added)
        {
            lock (_gate)
            {
                if (_isRemoved)
                {
                    result = null!;
                    added = false;
                    return false;
                }

                DateTime timestamp;
                try
                {
                    timestamp = FileUtilities.GetFileTimeStamp(fullPath);
                }
                catch (Exception e) when (IOUtilities.IsNormalIOException(e))
                {
                    result = createReference(fullPath, properties);
                    added = false;
                    return true;
                }

                if (_timestamp == timestamp)
                {
                    if (_references.TryGetValue(properties, out var weakReference) &&
                        weakReference.TryGetTarget(out var cachedReference))
                    {
                        result = cachedReference;
                        added = false;
                        return true;
                    }

                    PortableExecutableReference? referenceWithDifferentProperties = null;
                    using var deadProperties = TemporaryArray<MetadataReferenceProperties>.Empty;
                    foreach (var (existingProperties, reference) in _references)
                    {
                        if (reference.TryGetTarget(out var existingReference))
                        {
                            referenceWithDifferentProperties ??= existingReference;
                        }
                        else
                        {
                            deadProperties.Add(existingProperties);
                        }
                    }

                    foreach (var deadProperty in deadProperties)
                        _references.Remove(deadProperty);

                    if (referenceWithDifferentProperties is not null)
                    {
                        var variant = referenceWithDifferentProperties.WithProperties(properties);
                        _references[properties] = new(variant);
                        result = variant;
                        added = true;
                        return true;
                    }
                }
                else
                {
                    _references.Clear();
                }

                result = createReference(fullPath, properties);
                _references[properties] = new(result);
                _timestamp = timestamp;
                added = true;
                return true;
            }
        }

        public bool TryMarkRemovedIfNoLiveReferences()
        {
            lock (_gate)
            {
                if (_isRemoved)
                    return true;

                foreach (var reference in _references.Values)
                {
                    if (reference.TryGetTarget(out _))
                        return false;
                }

                _isRemoved = true;
                return true;
            }
        }
    }

    private readonly struct CacheKey(string fullPath, MetadataImageKind kind) : IEquatable<CacheKey>
    {
        private readonly string _fullPath = fullPath;
        private readonly MetadataImageKind _kind = kind;

        public bool Equals(CacheKey other)
            => _kind == other._kind
                && PathUtilities.Comparer.Equals(_fullPath, other._fullPath);

        public override bool Equals(object? obj)
            => obj is CacheKey other && Equals(other);

        public override int GetHashCode()
            => Hash.Combine((int)_kind, PathUtilities.Comparer.GetHashCode(_fullPath));
    }

    internal readonly struct TestAccessor(SharedMetadataReferenceCache cache)
    {
        internal int EntryCount => cache._referenceSets.Count;
    }
}
