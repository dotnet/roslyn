// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Razor.Language;

public partial class AllowedChildTagDescriptorBuilder
{
    internal static readonly ObjectPool<AllowedChildTagDescriptorBuilder> Pool =
        DefaultPool.Create(static () => new AllowedChildTagDescriptorBuilder());

    internal static AllowedChildTagDescriptorBuilder GetInstance(TagHelperDescriptorBuilder parent)
    {
        var builder = Pool.Get();

        builder._parent = parent;

        return builder;
    }

    private protected override void Reset()
    {
        _parent = null;

        Name = null;
        DisplayName = null;
    }
}
