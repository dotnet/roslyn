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

        [Benchmark(Description = "AddIntToList")]
        public void AddIntToList()
        {
            var array = new Microsoft.CodeAnalysis.Collections.SegmentedList<int>();
            var iterations = Count;

            for (var i = 0; i < iterations; i++)
            {
                array.Add(i);
            }
        }

        [Benchmark(Description = "AddObjectToList")]
        public void AddObjectToList()
        {
            var array = new Microsoft.CodeAnalysis.Collections.SegmentedList<object?>();
            var iterations = Count;

            for (var i = 0; i < iterations; i++)
            {
                array.Add(null);
            }
        }

        [Benchmark(Description = "AddLargeStructToList")]
        public void AddLargeStructToList()
        {
            var array = new Microsoft.CodeAnalysis.Collections.SegmentedList<LargeStruct>();
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
            var array = new Microsoft.CodeAnalysis.Collections.SegmentedList<EnormousStruct>();
            var item = new EnormousStruct();
            var iterations = Count;

            for (var i = 0; i < iterations; i++)
            {
                array.Add(item);
            }
        }

        private struct LargeStruct
        {
            public int i1;
            public int i2;
            public int i3;
            public int i4;
            public int i5;
            public int i6;
            public int i7;
            public int i8;
            public int i9;
            public int i10;
            public int i11;
            public int i12;
            public int i13;
            public int i14;
            public int i15;
            public int i16;
            public int i17;
            public int i18;
            public int i19;
            public int i20;
        }

        private struct EnormousStruct
        {
            public LargeStruct s1;
            public LargeStruct s2;
            public LargeStruct s3;
            public LargeStruct s4;
            public LargeStruct s5;
            public LargeStruct s6;
            public LargeStruct s7;
            public LargeStruct s8;
            public LargeStruct s9;
            public LargeStruct s10;
        }
    }
}
