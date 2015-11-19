// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Host;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Workspaces
{
    internal partial class ProjectCacheService : IProjectCacheHostService
    {
        internal const int ImplicitCacheSize = 3;

        private readonly object _gate = new object();

        private readonly Workspace _workspace;
        private readonly Dictionary<ProjectId, Cache> _activeCaches = new Dictionary<ProjectId, Cache>();

        private readonly SimpleMRUCache _implicitCache = new SimpleMRUCache();
        private readonly ImplicitCacheMonitor _implicitCacheMonitor;

        public ProjectCacheService(Workspace workspace, int implicitCacheTimeout, bool forceCleanup = false)
        {
            _workspace = workspace;

            // forceCleanup is for testing
            if (workspace?.Kind == WorkspaceKind.Host || forceCleanup)
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

        public void ClearExpiredImplicitCache(DateTime expirationTime)
        {
            lock (_gate)
            {
                _implicitCache.ClearExpiredItems(expirationTime);
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
                else if (!PartOfP2PReferences(key))
                {
                    _implicitCache.Touch(instance);
                    _implicitCacheMonitor?.Touch();
                }

                return instance;
            }
        }

        private bool PartOfP2PReferences(ProjectId key)
        {
            if (_activeCaches.Count == 0 || _workspace == null)
            {
                return false;
            }

            var solution = _workspace.CurrentSolution;
            var graph = solution.GetProjectDependencyGraph();

            foreach (var projectId in _activeCaches.Keys)
            {
                // this should be cheap. graph is cached every time project reference is updated.
                var p2pReferences = (ImmutableHashSet<ProjectId>)graph.GetProjectsThatThisProjectTransitivelyDependsOn(projectId);
                if (p2pReferences.Contains(key))
                {
                    return true;
                }
            }

            return false;
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

        private void DisableCaching(ProjectId key, Cache cache)
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

        private class Cache : IDisposable
        {
            internal int Count;
            private readonly ProjectCacheService _cacheService;
            private readonly ProjectId _key;
            private readonly ConditionalWeakTable<object, object> _cache = new ConditionalWeakTable<object, object>();
            private readonly List<WeakReference<ICachedObjectOwner>> _ownerObjects = new List<WeakReference<ICachedObjectOwner>>();

            public Cache(ProjectCacheService cacheService, ProjectId key)
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
