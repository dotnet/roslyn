// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks;

public class TagHelperCollectionMergeBenchmark
{
    private ImmutableArray<TagHelperDescriptor> _tagHelpers;
    private ImmutableArray<TagHelperCollection> _collections;
    private TagHelperCollection? _collection1;
    private TagHelperCollection? _collection2;

    [Params(10, 50, 100)]
    public int Count { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        var collectionCount = Count switch
        {
            10 => 2,
            50 => 10,
            100 => 20,
            _ => Assumed.Unreachable<int>()
        };

        _tagHelpers = TagHelperCollectionHelpers.CreateTagHelpers(Count);
        _collections = TagHelperCollectionHelpers.CreateTagHelperCollections(collectionCount, helpersPerCollection: Count);

        var span = _tagHelpers.AsSpan();
        _collection1 = TagHelperCollection.Create(span[..(Count / 2)]);
        _collection2 = TagHelperCollection.Create(span[..(Count / 4)]);

        // Warm up to ensure consistent measurements
        _ = TagHelperCollection.Merge(_collections);
    }

    [Benchmark(Description = "Merge Two Collections")]
    public TagHelperCollection MergeTwoCollections()
    {
        if (_collections.Length >= 2)
        {
            return TagHelperCollection.Merge(_collections[0], _collections[1]);
        }

        return TagHelperCollection.Empty;
    }

    [Benchmark(Description = "Merge Collections ImmutableArray")]
    public TagHelperCollection MergeCollectionsImmutableArray()
    {
        return TagHelperCollection.Merge(_collections);
    }

    [Benchmark(Description = "Merge Collections ReadOnlySpan")]
    public TagHelperCollection MergeCollectionsReadOnlySpan()
    {
        var span = _collections.AsSpan();
        return TagHelperCollection.Merge(span);
    }

    [Benchmark(Description = "Merge Collections IEnumerable")]
    public TagHelperCollection MergeCollectionsIEnumerable()
    {
        var enumerable = (IEnumerable<TagHelperCollection>)_collections;
        return TagHelperCollection.Merge(enumerable);
    }

    [Benchmark(Description = "Merge with Duplicates")]
    public TagHelperCollection MergeWithDuplicates()
    {
        var collection1 = _collection1.AssumeNotNull();
        var collection2 = _collection2.AssumeNotNull();

        return TagHelperCollection.Merge(collection1, collection2);
    }
}
