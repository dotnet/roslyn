// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Host
{
    [ExportWorkspaceServiceFactory(typeof(ICompilationCacheService), ServiceLayer.Default)]
    internal partial class CompilationCacheServiceFactory : AbstractCacheServiceFactory
    {
        public const long CacheSize = 1000;
        public const int CompilationCount = 2;

        protected override IWorkspaceService CreateCache(IOptionService service)
        {
            int count;
            long size;
            GetInitialCacheValues(service, CacheOptions.CompilationCacheCount, CacheOptions.CompilationCacheSize, out count, out size);

            return new CompilationCacheService(service, count, size);
        }

        protected override long InitialCacheSize
        {
            get
            {
                return CacheSize;
            }
        }

        protected override int InitialMinimumCount
        {
            get
            {
                return CompilationCount;
            }
        }

        private class CompilationCacheService : CostBasedCache<Compilation>, ICompilationCacheService
        {
            // this will make cache size to be half in about 3 seconds with 500ms buffer time.
            private const double CoolingRate = 0.00006;

            private const int Increment = 1;
            private const int UpperBoundRatio = 10;

            public CompilationCacheService(IOptionService service, int minCount, long minCost)
                : base(service, minCount, minCost, minCost * UpperBoundRatio,
                       CoolingRate, Increment, TimeSpan.FromMilliseconds(500), // Keep all added compilations at least 500ms
                       ComputeCompilationCost, false, GetUniqueId)
            {
            }

            protected override void OnOptionChanged(OptionChangedEventArgs e)
            {
                if (e.Option == CacheOptions.CompilationCacheCount)
                {
                    this.minCount = (int)e.Value;
                }
                else if (e.Option == CacheOptions.CompilationCacheSize)
                {
                    var cost = (long)e.Value;

                    this.minCost = cost;
                    this.maxCost = cost * UpperBoundRatio;
                }
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