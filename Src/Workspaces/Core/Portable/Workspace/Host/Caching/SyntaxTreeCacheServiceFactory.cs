// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Composition;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Host
{
    [ExportWorkspaceServiceFactory(typeof(ISyntaxTreeCacheService), ServiceLayer.Default), Shared]
    internal partial class SyntaxTreeCacheServiceFactory : AbstractCacheServiceFactory
    {
        // 4M chars * 2bytes/char = 8MB of raw text, with some syntax nodes deduplicated.
        public const long CacheSize = 4 * 1024 * 1024;
        public const int TreeCount = 8;

        protected override IWorkspaceService CreateCache(IOptionService service)
        {
            int count;
            long size;
            GetInitialCacheValues(service, CacheOptions.SyntaxTreeCacheCount, CacheOptions.SyntaxTreeCacheSize, out count, out size);

            return new SyntaxTreeCacheService(service, count, size, tv => tv.FullSpan.Length);
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
                return TreeCount;
            }
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

            public SyntaxTreeCacheService(IOptionService service, int minCount, long minCost, Func<SyntaxNode, long> itemCost)
                : base(service, minCount, minCost, minCost * UpperBoundRatio,
                       CoolingRate, Increment, TimeSpan.FromMilliseconds(500),
                       itemCost, uniqueIdGetter)
            {
            }

            protected override void OnOptionChanged(OptionChangedEventArgs e)
            {
                if (e.Option == CacheOptions.SyntaxTreeCacheCount)
                {
                    this.minCount = (int)e.Value;
                }
                else if (e.Option == CacheOptions.SyntaxTreeCacheSize)
                {
                    var cost = (long)e.Value;

                    this.minCost = cost;
                    this.maxCost = cost * UpperBoundRatio;
                }
            }
        }
    }
}