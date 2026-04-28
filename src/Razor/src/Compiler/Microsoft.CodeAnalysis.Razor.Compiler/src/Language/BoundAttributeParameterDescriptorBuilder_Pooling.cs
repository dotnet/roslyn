// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Razor.Language;

public partial class BoundAttributeParameterDescriptorBuilder
{
    internal static readonly ObjectPool<BoundAttributeParameterDescriptorBuilder> Pool =
        DefaultPool.Create(static () => new BoundAttributeParameterDescriptorBuilder());

    internal static BoundAttributeParameterDescriptorBuilder GetInstance(BoundAttributeDescriptorBuilder parent)
    {
        var builder = Pool.Get();

        builder._parent = parent;

        return builder;
    }

    private protected override void Reset()
    {
        _parent = null;
        _flags = 0;
        _documentationObject = default;
        _typeNameObject = default;

        Name = null;
        PropertyName = null;
    }
}
