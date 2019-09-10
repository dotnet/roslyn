// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.UnitTests.Persistence
{
    [ExportWorkspaceService(typeof(IProjectCacheHostService), "NotKeptAlive"), Shared]
    public class TestProjectCacheService : IProjectCacheHostService
    {
        [ImportingConstructor]
        public TestProjectCacheService()
        {
        }

        T IProjectCacheHostService.CacheObjectIfCachingEnabledForKey<T>(ProjectId key, ICachedObjectOwner owner, T instance)
        {
            return instance;
        }

        T IProjectCacheHostService.CacheObjectIfCachingEnabledForKey<T>(ProjectId key, object owner, T instance)
        {
            return instance;
        }

        IDisposable IProjectCacheService.EnableCaching(ProjectId key)
        {
            return null;
        }
    }
}
