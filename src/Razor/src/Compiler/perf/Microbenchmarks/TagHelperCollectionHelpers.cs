// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Microbenchmarks;

internal static class TagHelperCollectionHelpers
{
    public static ImmutableArray<TagHelperDescriptor> CreateTagHelpers(int count)
    {
        using var result = new PooledArrayBuilder<TagHelperDescriptor>(count);

        for (var i = 0; i < count; i++)
        {
            var builder = TagHelperDescriptorBuilder.Create($"TestTagHelper{i}", "TestAssembly");
            builder.TypeName = $"TestTagHelper{i}";
            builder.TagMatchingRule(rule => rule.TagName = $"test{i}");

            result.Add(builder.Build());
        }

        return result.ToImmutableAndClear();
    }

    public static ImmutableArray<TagHelperDescriptor> CreateTagHelpersWithDuplicates(int count)
    {
        using var result = new PooledArrayBuilder<TagHelperDescriptor>(count);
        var uniqueHelpers = CreateTagHelpers(count / 2);

        for (var i = 0; i < uniqueHelpers.Length; i++)
        {
            result.Add(uniqueHelpers[i]);
        }

        for (var i = uniqueHelpers.Length; i < count; i++)
        {
            result.Add(uniqueHelpers[i % uniqueHelpers.Length]);
        }

        return result.ToImmutableAndClear();
    }

    public static ImmutableArray<TagHelperCollection> CreateTagHelperCollections(int collectionCount, int helpersPerCollection)
    {
        using var result = new PooledArrayBuilder<TagHelperCollection>(collectionCount);
        using var helpers = new PooledArrayBuilder<TagHelperDescriptor>(helpersPerCollection);

        for (var i = 0; i < collectionCount; i++)
        {
            for (var j = 0; j < helpersPerCollection; j++)
            {
                var builder = TagHelperDescriptorBuilder.Create($"Collection{i}TagHelper{j}", "TestAssembly");
                builder.TypeName = $"Collection{i}TagHelper{j}";
                builder.TagMatchingRule(rule => rule.TagName = $"collection{i}test{j}");

                helpers.Add(builder.Build());
            }

            result.Add(TagHelperCollection.Create(helpers.ToImmutableAndClear()));
        }

        return result.ToImmutableAndClear();
    }
}
