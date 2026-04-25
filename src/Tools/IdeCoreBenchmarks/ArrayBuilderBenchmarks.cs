// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.PooledObjects;

namespace IdeCoreBenchmarks
{
    [MemoryDiagnoser]
    public class ArrayBuilderBenchmarks
    {
        private ArrayBuilder<int> _intBuilder = null!;
        private ArrayBuilder<object> _objectBuilder = null!;

        [Params(5, 8, 10, 25, 100, 1_000, 10_000)]
        public int Count { get; set; }

        [GlobalSetup]
        public void GlobalSetup()
        {
            _intBuilder = new ArrayBuilder<int>(Count);
            for (var i = 0; i < Count; i++)
                _intBuilder.Add(i);

            _objectBuilder = new ArrayBuilder<object>(Count);
            for (var i = 0; i < Count; i++)
                _objectBuilder.Add(i.ToString());
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _intBuilder = null!;
            _objectBuilder = null!;
        }

        [Benchmark]
        public ImmutableArray<int> SelectAsArray()
        {
            return _intBuilder.SelectAsArray(static x => x * 2);
        }
    }
}
