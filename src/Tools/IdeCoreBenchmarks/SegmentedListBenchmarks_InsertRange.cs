// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.Collections;

namespace IdeCoreBenchmarks
{
    [DisassemblyDiagnoser]
    public class SegmentedListBenchmarks_InsertRange
    {
        private List<int> _values = null!;
        private int[] _insertValues = null!;

        private List<object?> _valuesObject = null!;
        private object?[] _insertValuesObject = null!;

        private Microsoft.CodeAnalysis.Collections.SegmentedList<int> _segmentedValues = null!;
        private SegmentedArray<int> _segmentedInsertValues;

        private Microsoft.CodeAnalysis.Collections.SegmentedList<object?> _segmentedValuesObject = null!;
        private SegmentedArray<object?> _segmentedInsertValuesObject;

        [Params(100000)]
        public int Count { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            _values = new List<int>(Count);
            _valuesObject = new List<object?>(Count);
            _segmentedValues = new Microsoft.CodeAnalysis.Collections.SegmentedList<int>(Count);
            _segmentedValuesObject = new Microsoft.CodeAnalysis.Collections.SegmentedList<object?>(Count);

            _insertValues = new int[100];
            _insertValuesObject = new object?[100];
            _segmentedInsertValues = new SegmentedArray<int>(100);
            _segmentedInsertValuesObject = new SegmentedArray<object?>(100);
        }

        [Benchmark(Description = "List<int>", Baseline = true)]
        public void InsertRangeList()
        {
            var iterations = Count / 100;
            for (var i = 0; i < iterations; i++)
            {
                _values.InsertRange(0, _insertValues);
            }

            _values.Clear();
        }

        [Benchmark(Description = "List<object>")]
        public void InsertRangeListObject()
        {
            var iterations = Count / 100;
            for (var i = 0; i < iterations; i++)
            {
                _valuesObject.InsertRange(0, _insertValuesObject);
            }

            _valuesObject.Clear();
        }

        [Benchmark(Description = "SegmentedList<int>")]
        public void InsertRangeSegmented()
        {
            var iterations = Count / 100;
            for (var i = 0; i < iterations; i++)
            {
                _segmentedValues.InsertRange(0, _segmentedInsertValues);
            }

            _segmentedValues.Clear();
        }

        [Benchmark(Description = "SegmentedList<object>")]
        public void InsertRangeSegmentedObject()
        {
            var iterations = Count / 100;
            for (var i = 0; i < iterations; i++)
            {
                _segmentedValuesObject.InsertRange(0, _segmentedInsertValuesObject);
            }

            _segmentedValuesObject.Clear();
        }
    }
}
