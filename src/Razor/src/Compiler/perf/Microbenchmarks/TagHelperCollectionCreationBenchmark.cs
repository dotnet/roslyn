// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;
using Microsoft.AspNetCore.Razor.Language;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks;

public class TagHelperCollectionCreationBenchmark
{
    private ImmutableArray<TagHelperDescriptor> _tagHelpers;
    private ImmutableArray<TagHelperDescriptor> _duplicateTagHelpers;

    [Params(10, 100, 1000)]
    public int Count { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _tagHelpers = TagHelperCollectionHelpers.CreateTagHelpers(Count);
        _duplicateTagHelpers = TagHelperCollectionHelpers.CreateTagHelpersWithDuplicates(Count);
    }

    [Benchmark(Description = "Create from ImmutableArray")]
    public TagHelperCollection CreateFromImmutableArray()
    {
        return TagHelperCollection.Create(_tagHelpers);
    }

    [Benchmark(Description = "Create from ReadOnlySpan")]
    public TagHelperCollection CreateFromReadOnlySpan()
    {
        var span = _tagHelpers.AsSpan();
        return TagHelperCollection.Create(span);
    }

    [Benchmark(Description = "Create from IEnumerable")]
    public TagHelperCollection CreateFromIEnumerable()
    {
        var enumerable = (IEnumerable<TagHelperDescriptor>)_tagHelpers;
        return TagHelperCollection.Create(enumerable);
    }

    [Benchmark(Description = "Create with Duplicates")]
    public TagHelperCollection CreateWithDuplicates()
    {
        return TagHelperCollection.Create(_duplicateTagHelpers);
    }
}
