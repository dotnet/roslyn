// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;

namespace IdeCoreBenchmarks
{
    [MemoryDiagnoser]
    public class SegmentedListBenchmarks_Add
    {
        [Params(1_000, 10_000, 100_000, 1_000_000)]
        public int Count { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
        }

        [Benchmark(Description = "AddToSegmentedList<object>", Baseline = true)]
        public void AddList()
        {
            var array = new Microsoft.CodeAnalysis.Collections.SegmentedList<object?>();
            var iterations = Count;
            for (var i = 0; i < iterations; i++)
            {
                array.Add(null);
            }
        }
    }
}
