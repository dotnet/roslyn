// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using Microsoft.CodeAnalysis.Host.Mef;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host;

/// <summary>
/// A cache for metadata references.
/// </summary>
internal sealed class MetadataReferenceCache : IMetadataReferenceCacheService
{
    private ImmutableDictionary<string, ReferenceSet> _referenceSets
        = ImmutableDictionary<string, ReferenceSet>.Empty;

    public PortableExecutableReference GetReference(
        string path,
        MetadataReferenceProperties properties,
        Func<string, MetadataReferenceProperties, PortableExecutableReference> createReference)
    {
        if (!_referenceSets.TryGetValue(path, out var referenceSet))
        {
            referenceSet = ImmutableInterlocked.GetOrAdd(ref _referenceSets, path, new ReferenceSet());
        }

        return referenceSet.GetAddOrUpdate(path, properties, createReference);
    }

    /// <summary>
    /// A collection of references to the same underlying metadata, each with different properties.
    /// </summary>
    private sealed class ReferenceSet
    {
        private readonly SemaphoreSlim _gate = new(initialCount: 1);

        // metadata references are held weakly, so even though this is a cache that enables reuse, it does not control lifetime.
        private readonly Dictionary<MetadataReferenceProperties, WeakReference<PortableExecutableReference>> _references = [];

        public PortableExecutableReference GetAddOrUpdate(
            string path,
            MetadataReferenceProperties properties,
            Func<string, MetadataReferenceProperties, PortableExecutableReference> createReference)
        {
            using (_gate.DisposableWait())
            {
                PortableExecutableReference mref = null;
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

                    mref ??= createReference(path, properties);

                    _references[properties] = new WeakReference<PortableExecutableReference>(mref);
                }

                return mref;
            }
        }
    }
}

[ExportWorkspaceServiceFactory(typeof(IMetadataReferenceCacheService), ServiceLayer.Default), Shared]
[method: ImportingConstructor]
[method: Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
internal sealed class MetadataReferenceCacheFactory() : IWorkspaceServiceFactory
{
    public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        => new MetadataReferenceCache();
}
