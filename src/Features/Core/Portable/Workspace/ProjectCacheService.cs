// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// This service will implicitly cache previous Compilations used by each supported Workspace implementation.
    /// The number of Compilations cached is determined by <see cref="ImplicitCacheSize"/>.  For now, we'll only
    /// support implicit caching for VS Workspaces (<see cref="WorkspaceKind.Host"/>), as caching is known to
    /// reduce latency in designer scenarios using the VS workspace.  For other Workspace kinds, the cost of the
    /// cache is likely to outweigh the benefit (for example, in Misc File Workspace cases, we can end up holding
    /// onto a lot of memory even after a file is closed).  We can opt in other kinds of Workspaces as needed.
    /// </summary>
    internal partial class ProjectCacheService : IProjectCacheHostService
    {
        internal const int ImplicitCacheSize = 3;

        private readonly object _gate = new();

        private readonly Workspace? _workspace;
        private readonly ISyntaxTreeConfigurationService? _configurationService;
        private readonly Dictionary<ProjectId, Cache> _activeCaches = new();

        private readonly SimpleMRUCache? _implicitCache;
        private readonly ImplicitCacheMonitor? _implicitCacheMonitor;

        public ProjectCacheService(Workspace? workspace)
        {
            _workspace = workspace;
            _configurationService = workspace?.Services.GetService<ISyntaxTreeConfigurationService>();
        }

        public ProjectCacheService(Workspace? workspace, TimeSpan implicitCacheTimeout)
            : this(workspace)
        {
            _implicitCache = new SimpleMRUCache();
            _implicitCacheMonitor = new ImplicitCacheMonitor(this, implicitCacheTimeout);
        }

        /// <summary>
        /// Recoverable trees only save significant memory for larger trees.
        /// </summary>
        public int MinimumLengthForRecoverableTree
            => (_configurationService?.DisableRecoverableTrees != true) ? 4 * 1024 : int.MaxValue;

        public bool IsImplicitCacheEmpty
        {
            get
            {
                lock (_gate)
                {
                    return _implicitCache?.IsEmpty ?? false;
                }
            }
        }

        public void ClearImplicitCache()
        {
            lock (_gate)
            {
                _implicitCache?.Clear();
            }
        }

        public void ClearExpiredImplicitCache(DateTime expirationTime)
        {
            lock (_gate)
            {
                _implicitCache?.ClearExpiredItems(expirationTime);
            }
        }

        public IDisposable EnableCaching(ProjectId key)
        {
            lock (_gate)
            {
                if (!_activeCaches.TryGetValue(key, out var cache))
                {
                    cache = new Cache(this, key);
                    _activeCaches.Add(key, cache);
                }

                cache.Count++;
                return cache;
            }
        }

        [return: NotNullIfNotNull("instance")]
        public T? CacheObjectIfCachingEnabledForKey<T>(ProjectId key, object owner, T? instance) where T : class
        {
            if (IsEnabled)
            {
                lock (_gate)
                {
                    if (_activeCaches.TryGetValue(key, out var cache))
                    {
                        cache.CreateStrongReference(owner, instance);
                    }
                    else if (_implicitCache != null && !PartOfP2PReferences(key))
                    {
                        RoslynDebug.Assert(_implicitCacheMonitor != null);
                        _implicitCache.Touch(instance);
                        _implicitCacheMonitor.Touch();
                    }
                }
            }

            return instance;
        }

        private bool IsEnabled
            => _configurationService?.DisableProjectCacheService != true;

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

        [return: NotNullIfNotNull("instance")]
        public T? CacheObjectIfCachingEnabledForKey<T>(ProjectId key, ICachedObjectOwner owner, T? instance) where T : class
        {
            if (IsEnabled)
            {
                lock (_gate)
                {
                    if (owner.CachedObject == null && _activeCaches.TryGetValue(key, out var cache))
                    {
                        owner.CachedObject = instance;
                        cache.CreateOwnerEntry(owner);
                    }
                }
            }

            return instance;
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

        private sealed class Cache : IDisposable
        {
            internal int Count;
            private readonly ProjectCacheService _cacheService;
            private readonly ProjectId _key;
            private ConditionalWeakTable<object, object?>? _cache = new();
            private readonly List<WeakReference<ICachedObjectOwner>> _ownerObjects = new();

            public Cache(ProjectCacheService cacheService, ProjectId key)
            {
                _cacheService = cacheService;
                _key = key;
            }

            public void Dispose()
                => _cacheService.DisableCaching(_key, this);

            internal void CreateStrongReference(object key, object? instance)
            {
                if (_cache == null)
                {
                    throw new ObjectDisposedException(nameof(Cache));
                }

                if (!_cache.TryGetValue(key, out _))
                {
                    _cache.Add(key, instance);
                }
            }

            internal void CreateOwnerEntry(ICachedObjectOwner owner)
                => _ownerObjects.Add(new WeakReference<ICachedObjectOwner>(owner));

            internal void FreeOwnerEntries()
            {
                foreach (var entry in _ownerObjects)
                {
                    if (entry.TryGetTarget(out var owner))
                    {
                        owner.CachedObject = null;
                    }
                }

                // Explicitly free our ConditionalWeakTable to make sure it's released. We have a number of places in the codebase
                // (in both tests and product code) that do using (service.EnableCaching), which implicitly returns a disposable instance
                // this type. The runtime in many cases disposes, but does not unroot, the underlying object after the the using block is exited.
                // This means the cache could still be rooting objects we don't expect it to be rooting by that point. By explicitly clearing
                // these out, we get the expected behavior.
                _cache = null;
                _ownerObjects.Clear();
            }
        }
    }
}
