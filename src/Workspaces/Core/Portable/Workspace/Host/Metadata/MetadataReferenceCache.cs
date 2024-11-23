// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host;

/// <summary>
/// A cache for metadata references.
/// </summary>
internal sealed class MetadataReferenceCache(Func<string, MetadataReferenceProperties, MetadataReference> createReference)
{
    private ImmutableDictionary<string, ReferenceSet> _referenceSets
        = ImmutableDictionary<string, ReferenceSet>.Empty;

    private readonly Func<string, MetadataReferenceProperties, MetadataReference> _createReference = createReference ?? throw new ArgumentNullException(nameof(createReference));

    public MetadataReference GetReference(string path, MetadataReferenceProperties properties)
    {
        if (!_referenceSets.TryGetValue(path, out var referenceSet))
        {
            referenceSet = ImmutableInterlocked.GetOrAdd(ref _referenceSets, path, new ReferenceSet(this));
        }

        return referenceSet.GetAddOrUpdate(path, properties);
    }

    /// <summary>
    /// A collection of references to the same underlying metadata, each with different properties.
    /// </summary>
    private sealed class ReferenceSet(MetadataReferenceCache cache)
    {
        private readonly SemaphoreSlim _gate = new(initialCount: 1);

        // metadata references are held weakly, so even though this is a cache that enables reuse, it does not control lifetime.
        private readonly Dictionary<MetadataReferenceProperties, WeakReference<MetadataReference>> _references = [];

        public MetadataReference GetAddOrUpdate(string path, MetadataReferenceProperties properties)
        {
            using (_gate.DisposableWait())
            {
                MetadataReference mref = null;
                if (!(_references.TryGetValue(properties, out var weakref) && weakref.TryGetTarget(out mref)))
                {
                    // try to base this metadata reference off of an existing one, so we don't load the metadata bytes twice.
                    foreach (var wr in _references.Values)
                    {
                        if (wr.TryGetTarget(out mref))
                        {
                            mref = mref.WithProperties(properties);
                            break;
                        }
                    }

                    mref ??= cache._createReference(path, properties);

                    _references[properties] = new WeakReference<MetadataReference>(mref);
                }

                return mref;
            }
        }
    }
}
