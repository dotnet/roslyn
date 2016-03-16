// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editor.Implementation.Workspaces;
using Microsoft.CodeAnalysis.Host;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
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
            var cacheService = new ProjectCacheService(null, int.MaxValue);
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
            var cacheService = new ProjectCacheService(null, int.MaxValue);
            var instance = new object();
            var weak = new WeakReference(instance);
            cacheService.CacheObjectIfCachingEnabledForKey(ProjectId.CreateNewId(), (object)null, instance);
            instance = null;
            CollectGarbage();
            Assert.True(weak.IsAlive);
            GC.KeepAlive(cacheService);
        }

        [Fact]
        public void TestImplicitCacheMonitoring()
        {
            var workspace = new AdhocWorkspace(MockHostServices.Instance, workspaceKind: WorkspaceKind.Host);
            var cacheService = new ProjectCacheService(workspace, 10);
            var weak = PutObjectInImplicitCache(cacheService);

            var timeout = TimeSpan.FromSeconds(10);
            var current = DateTime.UtcNow;
            do
            {
                Thread.Sleep(100);
                CollectGarbage();

                if (DateTime.UtcNow - current > timeout)
                {
                    break;
                }
            }
            while (weak.IsAlive);

            // FailFast (and thereby capture a dump) to investigate what's
            // rooting the object.
            if (weak.IsAlive)
            {
                CodeAnalysis.Test.Utilities.ExceptionUtilities.FailFast(new Exception("Please investigate why the object wasn't collected!"));
            }

            GC.KeepAlive(cacheService);
        }

        private static WeakReference PutObjectInImplicitCache(ProjectCacheService cacheService)
        {
            var instance = new object();
            var weak = new WeakReference(instance);

            cacheService.CacheObjectIfCachingEnabledForKey(ProjectId.CreateNewId(), (object)null, instance);
            instance = null;

            return weak;
        }

        [Fact]
        public void TestP2PReference()
        {
            var workspace = new AdhocWorkspace();

            var project1 = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Default, "proj1", "proj1", LanguageNames.CSharp);
            var project2 = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Default, "proj2", "proj2", LanguageNames.CSharp, projectReferences: SpecializedCollections.SingletonEnumerable(new ProjectReference(project1.Id)));
            var solutionInfo = SolutionInfo.Create(SolutionId.CreateNewId(), VersionStamp.Default, projects: new ProjectInfo[] { project1, project2 });

            var solution = workspace.AddSolution(solutionInfo);

            var instance = new object();
            var weak = new WeakReference(instance);

            var cacheService = new ProjectCacheService(workspace, int.MaxValue);
            using (var cache = cacheService.EnableCaching(project2.Id))
            {
                cacheService.CacheObjectIfCachingEnabledForKey(project1.Id, (object)null, instance);
                instance = null;
                solution = null;

                workspace.OnProjectRemoved(project1.Id);
                workspace.OnProjectRemoved(project2.Id);
            }

            // make sure p2p reference doesn't go to implicit cache
            CollectGarbage();
            Assert.False(weak.IsAlive);
        }

        [Fact]
        public void TestEjectFromImplicitCache()
        {
            List<Compilation> compilations = new List<Compilation>();
            for (int i = 0; i < ProjectCacheService.ImplicitCacheSize + 1; i++)
            {
                compilations.Add(CSharpCompilation.Create(i.ToString()));
            }

            var weakFirst = new WeakReference(compilations[0]);
            var weakLast = new WeakReference(compilations[compilations.Count - 1]);

            var cache = new ProjectCacheService(null, int.MaxValue);
            for (int i = 0; i < ProjectCacheService.ImplicitCacheSize + 1; i++)
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

            var cache = new ProjectCacheService(null, int.MaxValue);
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
            for (var i = 0; i < 10; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }

        private class MockHostServices : HostServices
        {
            public static readonly MockHostServices Instance = new MockHostServices();

            private MockHostServices() { }

            protected internal override HostWorkspaceServices CreateWorkspaceServices(Workspace workspace)
            {
                return new MockHostWorkspaceServices(this, workspace);
            }
        }

        private class MockHostWorkspaceServices : HostWorkspaceServices
        {
            private readonly HostServices _hostServices;
            private readonly Workspace _workspace;
            private static readonly IWorkspaceTaskSchedulerFactory s_taskSchedulerFactory = new WorkspaceTaskSchedulerFactory();

            public MockHostWorkspaceServices(HostServices hostServices, Workspace workspace)
            {
                _hostServices = hostServices;
                _workspace = workspace;
            }

            public override HostServices HostServices => _hostServices;

            public override Workspace Workspace => _workspace;
            
            public override IEnumerable<TLanguageService> FindLanguageServices<TLanguageService>(MetadataFilter filter)
            {
                return ImmutableArray<TLanguageService>.Empty;
            }

            public override TWorkspaceService GetService<TWorkspaceService>()
            {
                if (s_taskSchedulerFactory is TWorkspaceService)
                {
                    return (TWorkspaceService)s_taskSchedulerFactory;
                }

                return default(TWorkspaceService);
            }
        }
    }
}
