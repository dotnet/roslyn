// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.Implementation.Workspaces;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    public class ProjectCacheHostServiceFactoryTests
    {
        private void Test(Action<IProjectCacheHostService, ProjectId, ICachedObjectOwner, ObjectReference> action)
        {
            // Putting cacheService.CreateStrongReference in a using statement
            // creates a temporary local that isn't collected in Debug builds
            // Wrapping it in a lambda allows it to get collected.
            var cacheService = new ProjectCacheHostServiceFactory.ProjectCacheService("Test", int.MaxValue);
            var projectId = ProjectId.CreateNewId();
            var owner = new Owner();
            var instance = new ObjectReference();

            action(cacheService, projectId, owner, instance);
        }

        [Fact]
        public void TestCacheKeepsObjectAlive1()
        {
            Test((cacheService, projectId, owner, instance) =>
            {
                ((Action)(() =>
                {
                    using (cacheService.EnableCaching(projectId))
                    {
                        cacheService.CacheObjectIfCachingEnabledForKey(projectId, (object)owner, instance.Strong);
                        instance.Strong = null;
                        CollectGarbage();
                        Assert.True(instance.Weak.IsAlive);
                    }
                })).Invoke();

                CollectGarbage();
                Assert.False(instance.Weak.IsAlive);
                GC.KeepAlive(owner);
            });
        }

        [Fact]
        public void TestCacheKeepsObjectAlive2()
        {
            Test((cacheService, projectId, owner, instance) =>
            {
                ((Action)(() =>
                {
                    using (cacheService.EnableCaching(projectId))
                    {
                        cacheService.CacheObjectIfCachingEnabledForKey(projectId, owner, instance.Strong);
                        instance.Strong = null;
                        CollectGarbage();
                        Assert.True(instance.Weak.IsAlive);
                    }
                })).Invoke();

                CollectGarbage();
                Assert.False(instance.Weak.IsAlive);
                GC.KeepAlive(owner);
            });
        }

        [Fact]
        public void TestCacheDoesNotKeepObjectsAliveAfterOwnerIsCollected1()
        {
            Test((cacheService, projectId, owner, instance) =>
            {
                using (cacheService.EnableCaching(projectId))
                {
                    cacheService.CacheObjectIfCachingEnabledForKey(projectId, (object)owner, instance);
                    owner = null;
                    instance.Strong = null;
                    CollectGarbage();
                    Assert.False(instance.Weak.IsAlive);
                }
            });
        }

        [Fact]
        public void TestCacheDoesNotKeepObjectsAliveAfterOwnerIsCollected2()
        {
            Test((cacheService, projectId, owner, instance) =>
            {
                using (cacheService.EnableCaching(projectId))
                {
                    cacheService.CacheObjectIfCachingEnabledForKey(projectId, owner, instance);
                    owner = null;
                    instance.Strong = null;
                    CollectGarbage();
                    Assert.False(instance.Weak.IsAlive);
                }
            });
        }

        [Fact]
        public void TestImplicitCacheKeepsObjectAlive1()
        {
            var cacheService = new ProjectCacheHostServiceFactory.ProjectCacheService("Test", int.MaxValue);
            var instance = new object();
            var weak = new WeakReference(instance);
            cacheService.CacheObjectIfCachingEnabledForKey(ProjectId.CreateNewId(), (object)null, instance);
            instance = null;
            CollectGarbage();
            Assert.True(weak.IsAlive);
            GC.KeepAlive(cacheService);
        }

        [Fact]
        public void TestEjectFromImplicitCache()
        {
            List<Compilation> compilations = new List<Compilation>();
            for (int i = 0; i < ProjectCacheHostServiceFactory.ProjectCacheService.ImplicitCacheSize + 1; i++)
            {
                compilations.Add(CSharpCompilation.Create(i.ToString()));
            }

            var weakFirst = new WeakReference(compilations[0]);
            var weakLast = new WeakReference(compilations[compilations.Count - 1]);

            var cache = new ProjectCacheHostServiceFactory.ProjectCacheService("Test", int.MaxValue);
            for (int i = 0; i < ProjectCacheHostServiceFactory.ProjectCacheService.ImplicitCacheSize + 1; i++)
            {
                cache.CacheObjectIfCachingEnabledForKey(ProjectId.CreateNewId(), (object)null, compilations[i]);
            }

            compilations = null;
            CollectGarbage();

            Assert.False(weakFirst.IsAlive);
            Assert.True(weakLast.IsAlive);
            GC.KeepAlive(cache);
        }

        [Fact]
        public void TestCacheCompilationTwice()
        {
            var comp1 = CSharpCompilation.Create("1");
            var comp2 = CSharpCompilation.Create("2");
            var comp3 = CSharpCompilation.Create("3");

            var weak3 = new WeakReference(comp3);
            var weak1 = new WeakReference(comp1);

            var cache = new ProjectCacheHostServiceFactory.ProjectCacheService("Test", int.MaxValue);
            var key = ProjectId.CreateNewId();
            var owner = new object();
            cache.CacheObjectIfCachingEnabledForKey(key, owner, comp1);
            cache.CacheObjectIfCachingEnabledForKey(key, owner, comp2);
            cache.CacheObjectIfCachingEnabledForKey(key, owner, comp3);

            // When we cache 3 again, 1 should stay in the cache
            cache.CacheObjectIfCachingEnabledForKey(key, owner, comp3);
            comp1 = null;
            comp2 = null;
            comp3 = null;

            CollectGarbage();

            Assert.True(weak1.IsAlive);
            Assert.True(weak3.IsAlive);
            GC.KeepAlive(cache);
        }

        private class Owner : ICachedObjectOwner
        {
            object ICachedObjectOwner.CachedObject { get; set; }
        }

        private static void CollectGarbage()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }
}
