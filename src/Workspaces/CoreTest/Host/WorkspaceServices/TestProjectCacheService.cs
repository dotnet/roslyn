// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.UnitTests.Persistence
{
    [ExportWorkspaceService(typeof(IProjectCacheHostService), ServiceLayer.Test), Shared, PartNotDiscoverable]
    public class TestProjectCacheService : IProjectCacheHostService
    {
        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TestProjectCacheService()
        {
        }

        T IProjectCacheHostService.CacheObjectIfCachingEnabledForKey<T>(ProjectId key, ICachedObjectOwner owner, T instance)
            => instance;

        T IProjectCacheHostService.CacheObjectIfCachingEnabledForKey<T>(ProjectId key, object owner, T instance)
            => instance;

        IDisposable IProjectCacheService.EnableCaching(ProjectId key)
            => null;
    }
}
