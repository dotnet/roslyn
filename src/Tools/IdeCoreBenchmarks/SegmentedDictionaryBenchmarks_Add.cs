// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.Collections;

namespace IdeCoreBenchmarks
{
    [MemoryDiagnoser]
    public class SegmentedDictionaryBenchmarks_Add
    {
        [Params(1_000, 10_000, 100_000, 1_000_000)]
        public int Count { get; set; }

        private int[]? _intItems;
        private object[]? _objectItems;
        private LargeStruct[]? _largeItems;
        private EnormousStruct[]? _enormousItems;

        [IterationSetup]
        public void IterationSetup()
        {
            _intItems = new int[Count];
            _objectItems = new object[Count];
            _largeItems = new LargeStruct[Count];
            _enormousItems = new EnormousStruct[Count];

            for (var i = 0; i < Count; i++)
            {
                _intItems[i] = i;
                _objectItems[i] = new object();
                _largeItems[i] = new LargeStruct() { s1 = new MediumStruct() { i1 = i } };
                _enormousItems[i] = new EnormousStruct() { s1 = _largeItems[i] };
            }
        }

        [Benchmark]
        public void AddIntToList()
            => AddToList(_intItems!);

        [Benchmark]
        public void AddObjectToList()
            => AddToList(_objectItems!);

        [Benchmark]
        public void AddLargeStructToList()
            => AddToList(_largeItems!);

        [Benchmark]
        public void AddEnormousStructToList()
            => AddToList(_enormousItems!);

        private void AddToList<T>(T[] items) where T : notnull
        {
            var dict = new SegmentedDictionary<T, T>();
            var iterations = Count;

            for (var i = 0; i < iterations; i++)
                dict.Add(items[i], items[i]);
        }

        private struct MediumStruct
        {
            public int i1 { get; set; }
            public int i2 { get; set; }
            public int i3 { get; set; }
            public int i4 { get; set; }
            public int i5 { get; set; }
        }

        private struct LargeStruct
        {
            public MediumStruct s1 { get; set; }
            public MediumStruct s2 { get; set; }
            public MediumStruct s3 { get; set; }
            public MediumStruct s4 { get; set; }
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
