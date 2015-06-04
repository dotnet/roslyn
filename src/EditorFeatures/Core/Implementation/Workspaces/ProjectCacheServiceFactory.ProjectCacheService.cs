// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.TestHooks;
using Microsoft.CodeAnalysis.SolutionCrawler;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Workspaces
{
    internal partial class ProjectCacheHostServiceFactory : IWorkspaceServiceFactory
    {
        internal class ProjectCacheService : IProjectCacheHostService
        {
            internal const int ImplicitCacheSize = 3;

            private readonly object _gate = new object();
            private readonly Dictionary<object, Cache> _activeCaches = new Dictionary<object, Cache>();

            private readonly SimpleMRUCache _implicitCache = new SimpleMRUCache();
            private readonly ImplicitCacheMonitor _implicitCacheMonitor;

            public ProjectCacheService(string workspaceKind, int implicitCacheTimeout)
            {
                if (workspaceKind == WorkspaceKind.Host)
                {
                    // monitor implicit cache for host
                    _implicitCacheMonitor = new ImplicitCacheMonitor(this, implicitCacheTimeout);
                }
            }

            public bool IsImplicitCacheEmpty
            {
                get
                {
                    lock (_gate)
                    {
                        return _implicitCache.Empty;
                    }
                }
            }

            public void ClearImplicitCache()
            {
                lock (_gate)
                {
                    _implicitCache.Clear();
                }
            }

            public void ClearExpiredImplicitCache(int expirationTimeInMS)
            {
                lock (_gate)
                {
                    _implicitCache.ClearExpiredItems(expirationTimeInMS);
                }
            }

            public IDisposable EnableCaching(ProjectId key)
            {
                lock (_gate)
                {
                    Cache cache;
                    if (!_activeCaches.TryGetValue(key, out cache))
                    {
                        cache = new Cache(this, key);
                        _activeCaches.Add(key, cache);
                    }

                    cache.Count++;
                    return cache;
                }
            }

            public T CacheObjectIfCachingEnabledForKey<T>(ProjectId key, object owner, T instance) where T : class
            {
                lock (_gate)
                {
                    Cache cache;
                    if (_activeCaches.TryGetValue(key, out cache))
                    {
                        cache.CreateStrongReference(owner, instance);
                    }
                    else
                    {
                        _implicitCacheMonitor?.Touch();
                        _implicitCache.Touch(instance);
                    }

                    return instance;
                }
            }

            public T CacheObjectIfCachingEnabledForKey<T>(ProjectId key, ICachedObjectOwner owner, T instance) where T : class
            {
                lock (_gate)
                {
                    Cache cache;
                    if (owner.CachedObject == null && _activeCaches.TryGetValue(key, out cache))
                    {
                        owner.CachedObject = instance;
                        cache.CreateOwnerEntry(owner);
                    }

                    return instance;
                }
            }

            private void DisableCaching(object key, Cache cache)
            {
                lock (_gate)
                {
                    cache.Count--;
                    if (cache.Count == 0)
                    {
                        _activeCaches.Remove(key);
                        cache.FreeOwnerEntries();
                    }
                }
            }

            private class ImplicitCacheMonitor : IdleProcessor
            {
                private readonly ProjectCacheService _owner;
                private readonly SemaphoreSlim _gate;

                public ImplicitCacheMonitor(ProjectCacheService owner, int backOffTimeSpanInMS) :
                    base(AggregateAsynchronousOperationListener.CreateEmptyListener(),
                         backOffTimeSpanInMS,
                         CancellationToken.None)
                {
                    _owner = owner;
                    _gate = new SemaphoreSlim(0);

                    Start();
                }

                protected override Task ExecuteAsync()
                {
                    _owner.ClearExpiredImplicitCache(Environment.TickCount - BackOffTimeSpanInMS);

                    return SpecializedTasks.EmptyTask;
                }

                public void Touch()
                {
                    UpdateLastAccessTime();

                    if (_gate.CurrentCount == 0)
                    {
                        _gate.Release();
                    }
                }

                protected override Task WaitAsync(CancellationToken cancellationToken)
                {
                    if (_owner.IsImplicitCacheEmpty)
                    {
                        return _gate.WaitAsync(cancellationToken);
                    }

                    return SpecializedTasks.EmptyTask;
                }
            }

            private class SimpleMRUCache
            {
                private const int CacheSize = 3;

                private readonly Node[] nodes = new Node[CacheSize];

                public bool Empty
                {
                    get
                    {
                        for (var i = 0; i < nodes.Length; i++)
                        {
                            if (nodes[i].Data != null)
                            {
                                return false;
                            }
                        }

                        return true;
                    }
                }

                public void Touch(object instance)
                {
                    var oldIndex = -1;
                    var oldTime = Environment.TickCount;

                    for (var i = 0; i < nodes.Length; i++)
                    {
                        if (instance == nodes[i].Data)
                        {
                            nodes[i].LastTouchedInMS = Environment.TickCount;
                            return;
                        }

                        if (oldTime >= nodes[i].LastTouchedInMS)
                        {
                            oldTime = nodes[i].LastTouchedInMS;
                            oldIndex = i;
                        }
                    }

                    Contract.Requires(oldIndex >= 0);
                    nodes[oldIndex] = new Node(instance, Environment.TickCount);
                }

                public void ClearExpiredItems(int expirationTimeInMS)
                {
                    for (var i = 0; i < nodes.Length; i++)
                    {
                        if (nodes[i].Data != null && nodes[i].LastTouchedInMS < expirationTimeInMS)
                        {
                            nodes[i] = default(Node);
                        }
                    }
                }

                public void Clear()
                {
                    for (var i = 0; i < nodes.Length; i++)
                    {
                        nodes[i] = default(Node);
                    }
                }

                private struct Node
                {
                    public readonly object Data;
                    public int LastTouchedInMS;

                    public Node(object data, int lastTouchedInMS)
                    {
                        Data = data;
                        LastTouchedInMS = lastTouchedInMS;
                    }
                }
            }

            private class Cache : IDisposable
            {
                internal int Count;
                private readonly ProjectCacheService _cacheService;
                private readonly object _key;
                private readonly ConditionalWeakTable<object, object> _cache = new ConditionalWeakTable<object, object>();
                private readonly List<WeakReference<ICachedObjectOwner>> _ownerObjects = new List<WeakReference<ICachedObjectOwner>>();

                public Cache(ProjectCacheService cacheService, object key)
                {
                    _cacheService = cacheService;
                    _key = key;
                }

                public void Dispose()
                {
                    _cacheService.DisableCaching(_key, this);
                }

                internal void CreateStrongReference(object key, object instance)
                {
                    object o;
                    if (!_cache.TryGetValue(key, out o))
                    {
                        _cache.Add(key, instance);
                    }
                }

                internal void CreateOwnerEntry(ICachedObjectOwner owner)
                {
                    _ownerObjects.Add(new WeakReference<ICachedObjectOwner>(owner));
                }

                internal void FreeOwnerEntries()
                {
                    foreach (var entry in _ownerObjects)
                    {
                        ICachedObjectOwner owner;
                        if (entry.TryGetTarget(out owner))
                        {
                            owner.CachedObject = null;
                        }
                    }
                }
            }
        }
    }
}
