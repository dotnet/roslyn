using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Collections.Internal;

[MemoryDiagnoser]
public class SegmentedListBenchmarks_Add_SegmentCounts
{
    [Params(16, 256, 4096, 65536)]
    public int SegmentCount { get; set; }

    [ParamsAllValues]
    public bool AddExtraItem { get; set; }

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
        var count = SegmentCount * SegmentedArrayHelper.GetSegmentSize<T>();
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
