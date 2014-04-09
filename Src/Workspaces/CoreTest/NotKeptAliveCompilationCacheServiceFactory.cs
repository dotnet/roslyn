// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.UnitTests
{
    [ExportWorkspaceServiceFactory(typeof(ICompilationCacheService), "NotKeptAlive")]
    internal class NotKeptAliveCompilationCacheServiceFactory : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new CacheService();
        }

        internal class CacheService : ICompilationCacheService
        {
            private readonly Cache cache = new Cache();

            public ICompilationCache Primary
            {
                get { return this.cache; }
            }

            public ICompilationCache Secondary
            {
                get { return this.cache; }
            }

            public void Clear()
            {
            }

            private class Cache : ICompilationCache
            {
                public void AddOrAccess(Compilation instance, IWeakAction<Compilation> evictor)
                {
                    evictor.Invoke(instance);
                }

                public void Clear()
                {
                }
            }
        }
    }
}