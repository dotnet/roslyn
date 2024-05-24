// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;

namespace IdeCoreBenchmarks
{
    [MemoryDiagnoser]
    public class ChecksumBenchmarks
    {
        private const int ArraySizeSmall = 10;
        private const int ArraySizeMedium = 100;
        private const int ArraySizeLarge = 1000;

        private const int IterationCountSmall = 1000000;
        private const int IterationCountMedium = 100000;
        private const int IterationCountLarge = 10000;

        private ImmutableArray<Checksum> _smallImmutableArray;
        private ImmutableArray<Checksum> _mediumImmutableArray;
        private ImmutableArray<Checksum> _largeImmutableArray;

        private ImmutableArray<byte> _smallByteImmutableArray;
        private ImmutableArray<byte> _mediumByteImmutableArray;
        private ImmutableArray<byte> _largeByteImmutableArray;

        private ArrayBuilder<Checksum> _smallArrayBuilder;
        private ArrayBuilder<Checksum> _mediumArrayBuilder;
        private ArrayBuilder<Checksum> _largeArrayBuilder;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _smallImmutableArray = CreateChecksumImmutableArray(ArraySizeSmall);
            _mediumImmutableArray = CreateChecksumImmutableArray(ArraySizeMedium);
            _largeImmutableArray = CreateChecksumImmutableArray(ArraySizeLarge);

            _smallByteImmutableArray = CreateByteImmutableArray(ArraySizeSmall);
            _mediumByteImmutableArray = CreateByteImmutableArray(ArraySizeMedium);
            _largeByteImmutableArray = CreateByteImmutableArray(ArraySizeLarge);

            _smallArrayBuilder = CreateChecksumArrayBuilder(ArraySizeSmall);
            _mediumArrayBuilder = CreateChecksumArrayBuilder(ArraySizeMedium);
            _largeArrayBuilder = CreateChecksumArrayBuilder(ArraySizeLarge);
        }

        private static ImmutableArray<Checksum> CreateChecksumImmutableArray(int size)
        {
            var builder = new FixedSizeArrayBuilder<Checksum>(size);

            for (var i = 0; i < size; i++)
            {
                var checksum = Checksum.Create("ChecksumString " + i);
                builder.Add(checksum);
            }

            return builder.MoveToImmutable();
        }

        private static ImmutableArray<byte> CreateByteImmutableArray(int size)
        {
            var builder = new FixedSizeArrayBuilder<byte>(size);

            for (var i = 0; i < size; i++)
            {
                builder.Add((byte)(i % 256));
            }

            return builder.MoveToImmutable();
        }

        private static ArrayBuilder<Checksum> CreateChecksumArrayBuilder(int size)
        {
            var builder = ArrayBuilder<Checksum>.GetInstance(size);

            for (var i = 0; i < size; i++)
            {
                var checksum = Checksum.Create("ChecksumString " + i);
                builder.Add(checksum);
            }

            return builder;
        }

        #region ImmutableArray<byte>

        [Benchmark]
        public void Old_ImmutableArray_Byte_Small()
        {
            OldCreateChecksumByteArray(_smallImmutableArray, IterationCountSmall);
        }

        [Benchmark]
        public void New_ImmutableArray_Byte_Small()
        {
            NewCreateChecksumByteArray(_smallImmutableArray, IterationCountSmall);
        }

        [Benchmark]
        public void Old_ImmutableArray_Byte_Medium()
        {
            OldCreateChecksumByteArray(_mediumImmutableArray, IterationCountMedium);
        }

        [Benchmark]
        public void New_ImmutableArray_Byte_Medium()
        {
            NewCreateChecksumByteArray(_mediumImmutableArray, IterationCountMedium);
        }

        [Benchmark]
        public void Old_ImmutableArray_Byte_Large()
        {
            OldCreateChecksumByteArray(_largeImmutableArray, IterationCountLarge);
        }

        [Benchmark]
        public void New_ImmutableArray_Byte_Large()
        {
            NewCreateChecksumByteArray(_largeImmutableArray, IterationCountLarge);
        }

        private static void OldCreateChecksumByteArray(ImmutableArray<Checksum> array, int iterationCount)
        {
            for (var i = 0; i < iterationCount; i++)
            {
                _ = Checksum.Create(array);
            }
        }

        private static void NewCreateChecksumByteArray(ImmutableArray<Checksum> array, int iterationCount)
        {
            for (var i = 0; i < iterationCount; i++)
            {
                _ = Checksum.CreateNew(array);
            }
        }

        #endregion

        #region ImmutableArray<Checksum>

        [Benchmark]
        public void Old_ImmutableArray_Checksum_Small()
        {
            OldCreateChecksumImmutableArray(_smallImmutableArray, IterationCountSmall);
        }

        [Benchmark]
        public void New_ImmutableArray_Checksum_Small()
        {
            NewCreateChecksumImmutableArray(_smallImmutableArray, IterationCountSmall);
        }

        [Benchmark]
        public void Old_ImmutableArray_Checksum_Medium()
        {
            OldCreateChecksumImmutableArray(_mediumImmutableArray, IterationCountMedium);
        }

        [Benchmark]
        public void New_ImmutableArray_Checksum_Medium()
        {
            NewCreateChecksumImmutableArray(_mediumImmutableArray, IterationCountMedium);
        }

        [Benchmark]
        public void Old_ImmutableArray_Checksum_Large()
        {
            OldCreateChecksumImmutableArray(_largeImmutableArray, IterationCountLarge);
        }

        [Benchmark]
        public void New_ImmutableArray_Checksum_Large()
        {
            NewCreateChecksumImmutableArray(_largeImmutableArray, IterationCountLarge);
        }

        private static void OldCreateChecksumImmutableArray(ImmutableArray<Checksum> array, int iterationCount)
        {
            for (var i = 0; i < iterationCount; i++)
            {
                _ = Checksum.Create(array);
            }
        }

        private static void NewCreateChecksumImmutableArray(ImmutableArray<Checksum> array, int iterationCount)
        {
            for (var i = 0; i < iterationCount; i++)
            {
                _ = Checksum.CreateNew(array);
            }
        }

        #endregion

        #region ArrayBuilder<Checksum>

        [Benchmark]
        public void Old_ArrayBuilder_Small()
        {
            OldCreateChecksumArrayBuilder(_smallArrayBuilder, IterationCountSmall);
        }

        [Benchmark]
        public void New_ArrayBuilder_Small()
        {
            NewCreateChecksumArrayBuilder(_smallArrayBuilder, IterationCountSmall);
        }

        [Benchmark]
        public void Old_ArrayBuilder_Medium()
        {
            OldCreateChecksumArrayBuilder(_mediumArrayBuilder, IterationCountMedium);
        }

        [Benchmark]
        public void New_ArrayBuilder_Medium()
        {
            NewCreateChecksumArrayBuilder(_mediumArrayBuilder, IterationCountMedium);
        }

        [Benchmark]
        public void Old_ArrayBuilder_Large()
        {
            OldCreateChecksumArrayBuilder(_largeArrayBuilder, IterationCountLarge);
        }

        [Benchmark]
        public void New_ArrayBuilder_Large()
        {
            NewCreateChecksumArrayBuilder(_largeArrayBuilder, IterationCountLarge);
        }

        private static void OldCreateChecksumArrayBuilder(ArrayBuilder<Checksum> array, int iterationCount)
        {
            for (var i = 0; i < iterationCount; i++)
            {
                _ = Checksum.Create(array);
            }
        }

        private static void NewCreateChecksumArrayBuilder(ArrayBuilder<Checksum> array, int iterationCount)
        {
            for (var i = 0; i < iterationCount; i++)
            {
                _ = Checksum.CreateNew(array);
            }
        }

        #endregion
    }
}
