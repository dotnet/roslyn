// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host
{
    [ExportWorkspaceServiceFactory(typeof(ICompilationCacheService), ServiceLayer.Default)]
    internal partial class CompilationCacheServiceFactory : IWorkspaceServiceFactory
    {
        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new CompilationCacheService();
        }

        private class CompilationCacheService : ICompilationCacheService
        {
            public CompilationCacheService()
            {
                // this will basically slowly push up current cache threshold but
                // fastly bring it down to the normal threshold.
                this.Primary = new CompilationCache();

                // this one will push up and down threshold fast but at the end
                // will not leave anything in the cache.
                this.Secondary = new CompilationCache(
                    increment: 100,
                    minimumCompilationsCount: 0,
                    defaultTotalCostForAllCompilations: 0,
                    eagarlyEvict: true);
            }

            public ICompilationCache Primary { get; private set; }
            public ICompilationCache Secondary { get; private set; }

            public void Clear()
            {
                this.Primary.Clear();
                this.Secondary.Clear();
            }

            private class CompilationCache : CostBasedCache<Compilation>, ICompilationCache
            {
                // this will make cache size to be half in about 3 seconds with 500ms buffer time.
                private const double CoolingRate = 0.00006;

                private const int Increment = 1;
                private const int MinimumCompilationsCount = 2;
                private const long DefaultTotalCostForAllCompilations = 1000;
                private const long MaxTotalCostForAllCompilations = 10000;

                internal CompilationCache(
                    int increment = Increment,
                    int minimumCompilationsCount = MinimumCompilationsCount,
                    long defaultTotalCostForAllCompilations = DefaultTotalCostForAllCompilations,
                    bool eagarlyEvict = false)
                    : base(
                        minimumCompilationsCount,
                        defaultTotalCostForAllCompilations,
                        MaxTotalCostForAllCompilations,
                        CoolingRate,
                        Increment,
                        TimeSpan.FromMilliseconds(500), // Keep all added compilations at least 500ms
                        ComputeCompilationCost,
                        eagarlyEvict,
                        GetUniqueId)
                {
                }

                private static string GetUniqueId(Compilation compilation)
                {
                    return compilation.AssemblyName;
                }

                private static long ComputeCompilationCost(Compilation compilation)
                {
                    // having same value as cost and Increment will make cache to be too eagar on increasing its high water mark.
                    // now, 1:100 will make cache to increase its cache slot by 1 for every 100 cache accesses. it will basically linearly 
                    // increase its high water mark up to 100 cache slots.
                    return 100;
                }
            }
        }
    }
}