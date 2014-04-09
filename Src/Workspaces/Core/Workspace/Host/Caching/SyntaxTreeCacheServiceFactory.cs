// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Host.Mef;

namespace Microsoft.CodeAnalysis.Host
{
    [ExportWorkspaceServiceFactory(typeof(ISyntaxTreeCacheService), ServiceLayer.Default)]
    internal partial class SyntaxTreeCacheServiceFactory : IWorkspaceServiceFactory
    {
        // 4M chars * 2bytes/char = 8MB of raw text, with some syntax nodes deduplicated.
        private const long DefaultSize = 4 * 1024 * 1024;
        private const int DefaultTextCount = 8;

        public IWorkspaceService CreateService(HostWorkspaceServices workspaceServices)
        {
            return new SyntaxTreeCacheService(DefaultTextCount, DefaultSize, tv => tv.FullSpan.Length);
        }

        private class SyntaxTreeCacheService : CostBasedCache<SyntaxNode>, ISyntaxTreeCacheService
        {
            // this will make cache size to be half in about 3 seconds with 500ms buffer time.
            private const double CoolingRate = 0.00006;

            // 512 increase per a hit.
            private const int Increment = 1 << 9;

            // upper bound ratio
            // this is chosen by manual experiments. 
            // 6 was the lowest number that gives same result as a purely time based cache for a perf
            // test (more specifically vb typing responsiveness test)
            private const int UpperBoundRatio = 3;

            private static readonly Func<SyntaxNode, string> uniqueIdGetter =
                t => t.SyntaxTree == null ? string.Empty : t.SyntaxTree.FilePath;

            public SyntaxTreeCacheService(int minCount, long minCost, Func<SyntaxNode, long> itemCost)
                : base(minCount, minCost, minCost * UpperBoundRatio,
                       CoolingRate, Increment, TimeSpan.FromMilliseconds(500),
                       itemCost, uniqueIdGetter)
            {
            }
        }
    }
}