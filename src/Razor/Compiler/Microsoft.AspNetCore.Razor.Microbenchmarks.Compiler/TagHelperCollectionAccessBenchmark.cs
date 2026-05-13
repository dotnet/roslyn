// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks;

public class TagHelperCollectionAccessBenchmark
{
    private ImmutableArray<TagHelperDescriptor> _tagHelpers;
    private TagHelperCollection? _collection1;
    private TagHelperCollection? _collection2;

    [Params(10, 100, 1000)]
    public int Count { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _tagHelpers = TagHelperCollectionHelpers.CreateTagHelpers(Count);
        _collection1 = TagHelperCollection.Create(_tagHelpers);
        _collection2 = TagHelperCollection.Create(_tagHelpers);
    }

    [Benchmark(Description = "Collection Indexer Access")]
    public TagHelperDescriptor IndexerAccess()
    {
        var collection = _collection1.AssumeNotNull();

        TagHelperDescriptor result = null!;
        var count = collection.Count;

        for (var i = 0; i < count; i++)
        {
            result = collection[i];
        }

        return result;
    }

    [Benchmark(Description = "Collection Contains")]
    public bool ContainsCheck()
    {
        var collection = _collection1.AssumeNotNull();
        var result = false;

        foreach (var helper in _tagHelpers)
        {
            result = collection.Contains(helper);
        }

        return result;
    }

    [Benchmark(Description = "Collection IndexOf")]
    public int IndexOfCheck()
    {
        var collection = _collection1.AssumeNotNull();
        var result = -1;

        foreach (var helper in _tagHelpers)
        {
            result = collection.IndexOf(helper);
        }

        return result;
    }

    [Benchmark(Description = "Collection Enumeration")]
    public int EnumerateCollection()
    {
        var collection = _collection1.AssumeNotNull();
        var count = 0;

        foreach (var item in collection)
        {
            count++;
        }

        return count;
    }

    [Benchmark(Description = "Collection CopyTo")]
    public TagHelperDescriptor[] CopyToArray()
    {
        var collection = _collection1.AssumeNotNull();
        var destination = new TagHelperDescriptor[collection.Count];
        collection.CopyTo(destination);

        return destination;
    }

    [Benchmark(Description = "Collection Equality")]
    public bool EqualityCheck()
    {
        var collection1 = _collection1.AssumeNotNull();
        var collection2 = _collection2.AssumeNotNull();

        return collection1.Equals(collection2);
    }

    [Benchmark(Description = "Collection GetHashCode")]
    public int GetHashCodeCheck()
    {
        var collection = _collection1.AssumeNotNull();
        return collection.GetHashCode();
    }

    [Benchmark(Description = "Collection Where")]
    public TagHelperCollection WhereFilter()
    {
        var collection = _collection1.AssumeNotNull();
        return collection.Where(th => th.Name.Contains("TagHelper"));
    }
}
