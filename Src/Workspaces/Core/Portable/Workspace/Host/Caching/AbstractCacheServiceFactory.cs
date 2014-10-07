// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Host
{
    internal abstract class AbstractCacheServiceFactory : IWorkspaceServiceFactory
    {
        private IWorkspaceService cache = null;

        protected abstract int InitialMinimumCount { get; }
        protected abstract long InitialCacheSize { get; }
        protected abstract IWorkspaceService CreateCache(IOptionService service);

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            if (cache == null)
            {
                var service = workspaceServices.GetService<IOptionService>();
                var newCache = CreateCache(service);

                Interlocked.CompareExchange(ref cache, newCache, null);
            }

            return cache;
        }

        protected void GetInitialCacheValues(IOptionService service, Option<int> minimumCountKey, Option<long> sizeKey, out int minimumCount, out long size)
        {
            if (service == null)
            {
                minimumCount = InitialMinimumCount;
                size = InitialCacheSize;
            }

            minimumCount = service.GetOption(minimumCountKey);
            size = service.GetOption(sizeKey);
        }
    }
}