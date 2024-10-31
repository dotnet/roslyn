// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.Collections;

namespace IdeCoreBenchmarks
{
    [MemoryDiagnoser]
    public class SegmentedListBenchmarks_Add
    {
        [Params(1_000, 10_000, 100_000, 1_000_000)]
        public int Count { get; set; }

        [Benchmark(Description = "AddIntToList")]
        public void AddIntToList()
        {
            var array = new SegmentedList<int>();
            var iterations = Count;

            for (var i = 0; i < iterations; i++)
            {
                array.Add(i);
            }
        }

        [Benchmark(Description = "AddObjectToList")]
        public void AddObjectToList()
        {
            var array = new SegmentedList<object?>();
            var iterations = Count;

            for (var i = 0; i < iterations; i++)
            {
                array.Add(null);
            }
        }

        [Benchmark(Description = "AddLargeStructToList")]
        public void AddLargeStructToList()
        {
            var array = new SegmentedList<LargeStruct>();
            var item = new LargeStruct();
            var iterations = Count;

            for (var i = 0; i < iterations; i++)
            {
                array.Add(item);
            }
        }

        [Benchmark(Description = "AddEnormousStructToList")]
        public void AddEnormousStructToList()
        {
            var array = new SegmentedList<EnormousStruct>();
            var item = new EnormousStruct();
            var iterations = Count;

            for (var i = 0; i < iterations; i++)
            {
                array.Add(item);
            }
        }

        private struct LargeStruct
        {
            public int i1 { get; set; }
            public int i2 { get; set; }
            public int i3 { get; set; }
            public int i4 { get; set; }
            public int i5 { get; set; }
            public int i6 { get; set; }
            public int i7 { get; set; }
            public int i8 { get; set; }
            public int i9 { get; set; }
            public int i10 { get; set; }
            public int i11 { get; set; }
            public int i12 { get; set; }
            public int i13 { get; set; }
            public int i14 { get; set; }
            public int i15 { get; set; }
            public int i16 { get; set; }
            public int i17 { get; set; }
            public int i18 { get; set; }
            public int i19 { get; set; }
            public int i20 { get; set; }
        }

        private struct EnormousStruct
        {
            public LargeStruct s1 { get; set; }
            public LargeStruct s2 { get; set; }
            public LargeStruct s3 { get; set; }
            public LargeStruct s4 { get; set; }
            public LargeStruct s5 { get; set; }
            public LargeStruct s6 { get; set; }
            public LargeStruct s7 { get; set; }
            public LargeStruct s8 { get; set; }
            public LargeStruct s9 { get; set; }
            public LargeStruct s10 { get; set; }
        }
    }
}
