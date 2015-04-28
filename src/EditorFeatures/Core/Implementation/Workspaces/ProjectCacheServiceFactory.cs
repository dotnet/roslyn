// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Editor.Implementation.Workspaces
{
    [ExportWorkspaceServiceFactory(typeof(IProjectCacheHostService), ServiceLayer.Editor)]
    [Shared]
    internal class ProjectCacheHostServiceFactory : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            var projectCacheService = new ProjectCacheService();
            var documentTrackingService = workspaceServices.GetService<IDocumentTrackingService>();

            // Subscribe to events so that we can cache items from the active document's project
            var manager = new ActiveProjectCacheManager(documentTrackingService, projectCacheService);

            // Subscribe to requests to clear the cache
            var workspaceCacheService = workspaceServices.GetService<IWorkspaceCacheService>();
            if (workspaceCacheService != null)
            {
                workspaceCacheService.CacheFlushRequested += (s, e) => manager.Clear();
            }

            // Also clear the cache when the solution is cleared or removed.
            workspaceServices.Workspace.WorkspaceChanged += (s, e) =>
            {
                if (e.Kind == WorkspaceChangeKind.SolutionCleared || e.Kind == WorkspaceChangeKind.SolutionRemoved)
                {
                    manager.Clear();
                }
            };

            return projectCacheService;
        }

        internal class ProjectCacheService : IProjectCacheHostService
        {
            private readonly object _gate = new object();
            private readonly Dictionary<object, Cache> _activeCaches = new Dictionary<object, Cache>();
            private readonly Queue<object> _implicitCache = new Queue<object>();
            internal const int ImplicitCacheSize = 3;

            public void ClearImplicitCache()
            {
                _implicitCache.Clear();
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
                        if (!_implicitCache.Contains(instance))
                        {
                            if (_implicitCache.Count == ImplicitCacheSize)
                            {
                                _implicitCache.Dequeue();
                            }

                            _implicitCache.Enqueue(instance);
                        }
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

        private class ActiveProjectCacheManager
        {
            private readonly IDocumentTrackingService _documentTrackingService;
            private readonly ProjectCacheService _projectCacheService;
            private readonly object _guard = new object();

            private ProjectId _mostRecentActiveProjectId;
            private IDisposable _mostRecentCache;

            public ActiveProjectCacheManager(IDocumentTrackingService documentTrackingService, ProjectCacheService projectCacheService)
            {
                _documentTrackingService = documentTrackingService;
                _projectCacheService = projectCacheService;

                if (documentTrackingService != null)
                {
                    documentTrackingService.ActiveDocumentChanged += UpdateCache;
                    UpdateCache(null, documentTrackingService.GetActiveDocument());
                }
            }

            private void UpdateCache(object sender, DocumentId activeDocument)
            {
                lock (_guard)
                {
                    if (activeDocument != null && activeDocument.ProjectId != _mostRecentActiveProjectId)
                    {
                        ClearMostRecentCache_NoLock();
                        _mostRecentCache = _projectCacheService.EnableCaching(activeDocument.ProjectId);
                        _mostRecentActiveProjectId = activeDocument.ProjectId;
                    }
                }
            }

            public void Clear()
            {
                lock (_guard)
                {
                    // clear most recent cache
                    ClearMostRecentCache_NoLock();

                    // clear implicit cache
                    _projectCacheService.ClearImplicitCache();
                }
            }

            private void ClearMostRecentCache_NoLock()
            {
                if (_mostRecentCache != null)
                {
                    _mostRecentCache.Dispose();
                    _mostRecentCache = null;
                }

                _mostRecentActiveProjectId = null;
            }
        }
    }
}
