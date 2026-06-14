// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable

using System;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language.Intermediate;

internal abstract class IntermediateNodeBuilder
{
    public static PooledObject<DefaultRazorIntermediateNodeBuilder> GetPooledObject(IntermediateNode root, out IntermediateNodeBuilder builder)
    {
        if (root == null)
        {
            throw new ArgumentNullException(nameof(root));
        }

        var pooled = DefaultRazorIntermediateNodeBuilder.s_pool.GetPooledObject(out var instance);
        instance.Push(root);
        builder = instance;
        return pooled;
    }

    public abstract IntermediateNode Current { get; }

    public abstract void Add(IntermediateNode node);

    public abstract void Insert(int index, IntermediateNode node);

    public abstract IntermediateNode Build();

    public abstract void Push(IntermediateNode node);

    public abstract IntermediateNode Pop();
}
