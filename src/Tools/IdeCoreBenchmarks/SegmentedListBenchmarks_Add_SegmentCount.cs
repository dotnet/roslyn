// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Collections.Internal;

[MemoryDiagnoser]
public class SegmentedListBenchmarks_Add_SegmentCounts
{
    [Params(16, 256, 4096, 65536)]
    public int RoughSegmentCount { get; set; }

    [ParamsAllValues]
    public bool AddExtraItem { get; set; }

    [Params(2, 3)]
    public int SegmentGrowthShiftValue { get; set; }

    private int _actualSegmentCount;

    [IterationSetup]
    public void IterationSetup()
    {
        if (SegmentGrowthShiftValue == 2)
        {
            _actualSegmentCount = RoughSegmentCount switch
            {
                16 => 18,
                256 => 293,
                4096 => 4241,
                65536 => 61697
            };
        }
        else if (SegmentGrowthShiftValue == 3)
        {
            _actualSegmentCount = RoughSegmentCount switch
            {
                16 => 18,
                256 => 289,
                4096 => 4282,
                65536 => 64247
            };
        }
    }

    [Benchmark]
    public void AddObjectToList()
        => AddToList(new object());

    [Benchmark]
    public void AddLargeStructToList()
        => AddToList(new LargeStruct());

    [Benchmark]
    public void AddEnormousStructToList()
        => AddToList(new EnormousStruct());

    private void AddToList<T>(T item)
    {
        SegmentedList<T>.SegmentGrowthShiftValue = SegmentGrowthShiftValue;

        var count = _actualSegmentCount * SegmentedArrayHelper.GetSegmentSize<T>();
        if (AddExtraItem)
            count++;

        var array = new SegmentedList<T>();
        for (var i = 0; i < count; i++)
            array.Add(item);
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
