// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.AspNetCore.Razor.Language.Test;

public class TagHelperDescriptorListPoolTest
{
    [Fact]
    public void NamespaceLists_StartSmall()
    {
        var pool = CreateNamespacePool();

        Assert.Equal(4, pool.Get().Capacity);
    }

    [Theory]
    [InlineData(4096)]
    [InlineData(32768)]
    public void BuilderLists_RetainGrownCapacity(int capacity)
    {
        var pool = CreateBuilderPool();
        var list = new List<TagHelperDescriptor>(capacity) { CreateTagHelper() };

        pool.Return(list);
        var reused = pool.Get();

        Assert.Same(list, reused);
        Assert.Empty(reused);
        Assert.Equal(capacity, reused.Capacity);
    }

    [Fact]
    public void BuilderLists_OversizedBufferFallsBackToSmallPool()
    {
        var pool = CreateBuilderPool();
        var list = new List<TagHelperDescriptor>(65536) { CreateTagHelper() };

        pool.Return(list);

        Assert.Empty(list);
        Assert.Equal(2048, list.Capacity);
    }

    [Fact]
    public void BuilderLists_FullLargeTierFallsBackToSmallPool()
    {
        var pool = CreateBuilderPool();
        var retained = new List<TagHelperDescriptor>[8];
        for (var i = 0; i < retained.Length; i++)
        {
            retained[i] = new(32768);
            pool.Return(retained[i]);
        }

        var discarded = new List<TagHelperDescriptor>(32768) { CreateTagHelper() };
        pool.Return(discarded);

        Assert.Empty(discarded);
        Assert.Equal(2048, discarded.Capacity);
        for (var i = 0; i < retained.Length; i++)
        {
            Assert.Contains(pool.Get(), retained);
        }
    }

    [Fact]
    public void BuilderLists_SmallLeasesDoNotExhaustLargeTier()
    {
        var pool = CreateBuilderPool();
        var lists = new List<TagHelperDescriptor>[128];
        for (var i = 0; i < lists.Length; i++)
        {
            lists[i] = pool.Get();
            lists[i].Capacity = 2048;
        }

        foreach (var list in lists)
        {
            pool.Return(list);
        }

        for (var i = 0; i < 8; i++)
        {
            pool.Return(new(4096));
        }

        for (var i = 0; i < 8; i++)
        {
            Assert.Equal(4096, pool.Get().Capacity);
        }
    }

    [Fact]
    public void BuilderLists_LargeBuffersAreNotRentedToNamespaceLists()
    {
        var smallLists = TagHelperCollection.TagHelperDescriptorListPool.Default;
        var pool = CreateBuilderPool();
        var large = new List<TagHelperDescriptor>(4096);
        pool.Return(large);

        var small = smallLists.Get();
        Assert.NotSame(large, small);
        Assert.InRange(small.Capacity, 0, 2048);
        smallLists.Return(small);
        Assert.Same(large, pool.Get());
    }

    [Fact]
    public void NamespaceLists_FullPoolPreservesExistingDiscardBehavior()
    {
        var pool = CreateNamespacePool();

        for (var i = 0; i < 128; i++)
        {
            pool.Return(new(2048));
        }

        var discarded = new List<TagHelperDescriptor>(32768) { CreateTagHelper() };
        pool.Return(discarded);

        Assert.Empty(discarded);
        Assert.Equal(2048, discarded.Capacity);
        for (var i = 0; i < 128; i++)
        {
            Assert.NotSame(discarded, pool.Get());
        }

        Assert.Equal(4, pool.Get().Capacity);
    }

    [Fact]
    public void NamespaceLists_PreserveExistingRetentionLimit()
    {
        var pool = CreateNamespacePool();
        var list = new List<TagHelperDescriptor>(4096) { CreateTagHelper() };

        pool.Return(list);

        Assert.Empty(list);
        Assert.Equal(2048, list.Capacity);
        Assert.Same(list, pool.Get());
    }

    [Fact]
    public void Pools_KeepSimultaneousLeasesIndependent()
    {
        var pool = CreateBuilderPool();
        var first = pool.Get();
        var second = pool.Get();
        first.Capacity = 4096;
        second.Capacity = 4096;
        var descriptor = CreateTagHelper();
        first.Add(descriptor);
        second.Add(descriptor);

        pool.Return(second);
        var reused = pool.Get();

        Assert.Same(second, reused);
        Assert.Empty(reused);
        Assert.Same(descriptor, Assert.Single(first));
        pool.Return(first);
        pool.Return(reused);
    }

    [Fact]
    public void Pools_ConcurrentLeasesDoNotShareLists()
    {
        var pool = CreateBuilderPool();
        var descriptor = CreateTagHelper();

        Parallel.For(0, 1000, _ =>
        {
            var list = pool.Get();
            Assert.Empty(list);
            list.Capacity = 4096;
            list.Add(descriptor);
            Assert.Same(descriptor, Assert.Single(list));
            pool.Return(list);
        });
    }

    private static TagHelperCollection.TagHelperDescriptorListPool CreateNamespacePool()
        => new(initialCapacity: 4, maximumCapacity: 2048, poolSize: 128);

    private static TagHelperCollection.Builder.BuilderListPool CreateBuilderPool()
        => new();

    private static TagHelperDescriptor CreateTagHelper()
        => TagHelperDescriptorBuilder.Create("Test", "TestAssembly").Build();
}
