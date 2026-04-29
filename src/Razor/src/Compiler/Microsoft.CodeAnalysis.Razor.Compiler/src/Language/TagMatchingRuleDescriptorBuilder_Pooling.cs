// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Razor.Language;

public partial class TagMatchingRuleDescriptorBuilder
{
    internal static readonly ObjectPool<TagMatchingRuleDescriptorBuilder> Pool =
        DefaultPool.Create(static () => new TagMatchingRuleDescriptorBuilder());

    internal static TagMatchingRuleDescriptorBuilder GetInstance(TagHelperDescriptorBuilder parent)
    {
        var builder = Pool.Get();

        builder._parent = parent;

        return builder;
    }

    private protected override void Reset()
    {
        _parent = null;

        TagName = null;
        ParentTag = null;
        TagStructure = default;
        Attributes.Clear();
    }
}
