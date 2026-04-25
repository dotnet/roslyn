// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Razor.Language;

public partial class BoundAttributeDescriptorBuilder
{
    internal static readonly ObjectPool<BoundAttributeDescriptorBuilder> Pool =
        DefaultPool.Create(static () => new BoundAttributeDescriptorBuilder());

    internal static BoundAttributeDescriptorBuilder GetInstance(TagHelperDescriptorBuilder parent)
    {
        var builder = Pool.Get();

        builder._parent = parent;

        return builder;
    }

    private protected override void Reset()
    {
        _parent = null;
        _flags = 0;
        _typeNameObject = default;
        _indexerTypeNameObject = default;
        _documentationObject = default;
        _metadataObject = null;
        _caseSensitiveSet = false;

        Name = null;
        PropertyName = null;
        IndexerAttributeNamePrefix = null;
        DisplayName = null;
        ContainingType = null;
        Parameters.Clear();
    }
}
