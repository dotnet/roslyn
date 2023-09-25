// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace IdeCoreBenchmarks
{
    [DisassemblyDiagnoser]
    [MemoryDiagnoser]
    public class SegmentedList_AddAndSort
    {
        [Params(10, 100, 1000, 10000)]
        public int Count { get; set; }

        private readonly List<string> _10Entries = new(10);
        private readonly List<string> _100Entries = new(100);
        private readonly List<string> _1000Entries = new(1000);
        private readonly List<string> _10000Entries = new(10000);

        [GlobalSetup]
        public void GlobalSetup()
        {
            PopulateLists(_10Entries);
            PopulateLists(_100Entries);
            PopulateLists(_1000Entries);
            PopulateLists(_10000Entries);
        }

        private void PopulateLists(List<string> strings)
        {
            for (var i = strings.Capacity; i > 0; i--)
            {
                strings.Add(i.ToString());
            }
        }

        private List<string> GetList()
        {
            if (Count == 10)
                return _10Entries;
            else if (Count == 100)
                return _100Entries;
            else if (Count == 1000)
                return _1000Entries;
            else
                return _10000Entries;
        }

        [Benchmark]
        public void SegmentedListAddAndSort()
        {
            var strings = GetList();

            var list = new Microsoft.CodeAnalysis.Collections.SegmentedList<string>(strings.Count);

            foreach (var item in strings)
            {
                list.Add(item);
            }

            list.Sort(StringComparer.OrdinalIgnoreCase);
        }

        [Benchmark]
        public void ArrayBuilderAddAndSort()
        {
            var strings = GetList();

            using var _ = Microsoft.CodeAnalysis.PooledObjects.ArrayBuilder<string>.GetInstance(strings.Count, out var builder);

            foreach (var item in strings)
            {
                builder.Add(item);
            }

            builder.Sort(StringComparer.OrdinalIgnoreCase);
        }

        private static readonly Microsoft.CodeAnalysis.PooledObjects.ObjectPool<List<string>> s_sortListPool = new(factory: () => new List<string>(), size: 5);

        [Benchmark]
        public void ListAddAndSortOwnPool()
        {
            var strings = GetList();
            var list = s_sortListPool.Allocate();

            try
            {
                foreach (var item in strings)
                {
                    list.Add(item);
                }

                list.Sort(StringComparer.OrdinalIgnoreCase);
            }
            finally
            {
                list.Clear();
                s_sortListPool.Free(list);
            }
        }
    }
}
