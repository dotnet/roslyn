// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Host
{
    [ExportWorkspaceServiceFactory(typeof(ITextCacheService), ServiceLayer.Default)]
    internal partial class TextCacheServiceFactory : AbstractCacheServiceFactory
    {
        // 1M chars * 2bytes/char = 2 MB
        public const long CacheSize = 1 << 20;
        public const int TextCount = 8;

        protected override IWorkspaceService CreateCache(IOptionService service)
        {
            int count;
            long size;
            GetInitialCacheValues(service, CacheOptions.TextCacheCount, CacheOptions.TextCacheSize, out count, out size);

            return new TextCacheService(service, count, size, itemCost: tv => tv.Text.Length);
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
                return TextCount;
            }
        }

        private class TextCacheService : CostBasedCache<TextAndVersion>, ITextCacheService
        {
            // this will make cache size to be half in about 3 seconds with 200ms buffer time.
            private const double CoolingRate = 0.000025;

            // 1K increase per a hit.
            private const int Increment = 1 << 10;

            // upper bound ratio
            // this is chosen by manual experiments. 
            // 3 was the lowest number that gives same result as a purely time based cache for a perf
            // test (more specifically vb typing responsiveness test)
            private const int UpperBoundRatio = 2;

            private static readonly Func<TextAndVersion, string> uniqueIdGetter = tv => tv.FilePath;

            public TextCacheService(IOptionService service, int minCount, long minCost, Func<TextAndVersion, long> itemCost)
                : base(service, minCount, minCost, minCost * UpperBoundRatio, CoolingRate, Increment, TimeSpan.FromMilliseconds(200), itemCost, uniqueIdGetter)
            {
            }

            protected override void OnOptionChanged(OptionChangedEventArgs e)
            {
                if (e.Option == CacheOptions.TextCacheCount)
                {
                    this.minCount = (int)e.Value;
                }
                else if (e.Option == CacheOptions.TextCacheSize)
                {
                    var cost = (long)e.Value;

                    this.minCost = cost;
                    this.maxCost = cost * UpperBoundRatio;
                }
            }
        }
    }
}
