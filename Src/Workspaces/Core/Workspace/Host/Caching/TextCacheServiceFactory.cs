// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host
{
    [ExportWorkspaceServiceFactory(typeof(ITextCacheService), ServiceLayer.Default)]
    internal partial class TextCacheServiceFactory : IWorkspaceServiceFactory
    {
        // 4M chars * 2bytes/char = 8 MB
        private const long DefaultSize = 1 << 20;
        private const int DefaultTextCount = 8;

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new TextCacheService(DefaultTextCount, DefaultSize, itemCost: tv => tv.Text.Length);
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

            public TextCacheService(int minCount, long minCost, Func<TextAndVersion, long> itemCost)
                : base(minCount, minCost, minCost * UpperBoundRatio, CoolingRate, Increment, TimeSpan.FromMilliseconds(200), itemCost, uniqueIdGetter)
            {
            }
        }
    }
}
