// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.Collections;

namespace IdeCoreBenchmarks
{
    [DisassemblyDiagnoser]
    public class SegmentedArrayBenchmarks_Indexer
    {
        private int[] _values = null!;
        private object?[] _valuesObject = null!;

        private SegmentedArray<int> _segmentedValues;
        private SegmentedArray<object?> _segmentedValuesObject;

        [Params(100000)]
        public int Count { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            _values = new int[Count];
            _valuesObject = new object?[Count];
            _segmentedValues = new SegmentedArray<int>(Count);
            _segmentedValuesObject = new SegmentedArray<object?>(Count);
        }

        [Benchmark(Description = "int[]", Baseline = true)]
        public void ShiftAllArray()
        {
            for (var i = 0; i < _values.Length - 1; i++)
            {
                _values[i] = _values[i + 1];
            }

            _values[^1] = 0;
        }

        [Benchmark(Description = "object[]")]
        public void ShiftAllArrayObject()
        {
            for (var i = 0; i < _valuesObject.Length - 1; i++)
            {
                _valuesObject[i] = _valuesObject[i + 1];
            }

            _valuesObject[^1] = null;
        }

        [Benchmark(Description = "SegmentedArray<int>")]
        public void ShiftAllSegmented()
        {
            for (var i = 0; i < _segmentedValues.Length - 1; i++)
            {
                _segmentedValues[i] = _segmentedValues[i + 1];
            }

            _segmentedValues[^1] = 0;
        }

        [Benchmark(Description = "SegmentedArray<object>")]
        public void ShiftAllSegmentedObject()
        {
            for (var i = 0; i < _segmentedValuesObject.Length - 1; i++)
            {
                _segmentedValuesObject[i] = _segmentedValuesObject[i + 1];
            }

            _segmentedValuesObject[^1] = null;
        }
    }
}
