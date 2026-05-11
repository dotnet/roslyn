// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.PooledObjects;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.AspNetCore.Razor.Language;

public sealed partial class TagHelperObjectBuilderCollection<TObject, TBuilder> : IEnumerable<TBuilder>
    where TObject : TagHelperObject<TObject>
    where TBuilder : TagHelperObjectBuilder<TObject>
{
    private readonly ObjectPool<TBuilder> _builderPool;
    private List<TBuilder>? _builders;

    internal TagHelperObjectBuilderCollection(ObjectPool<TBuilder> builderPool)
    {
        _builderPool = builderPool;
    }

    public int Count
        => _builders?.Count ?? 0;

    public TBuilder this[int index]
        => _builders.AssumeNotNull()[index];

    public Enumerator GetEnumerator()
        => new(this);

    IEnumerator<TBuilder> IEnumerable<TBuilder>.GetEnumerator()
        => GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    internal void Add(TBuilder builder)
    {
        _builders ??= new();
        _builders.Add(builder);
    }

    internal void Clear()
    {
        // Ensure that we return all builders to their pool
        if (_builders is { } builders)
        {
            foreach (var builder in builders)
            {
                _builderPool.Return(builder);
            }

            builders.Clear();
        }
    }

    internal ImmutableArray<TObject> ToImmutable()
    {
        if (_builders is not { Count: > 0 } builders)
        {
            return ImmutableArray<TObject>.Empty;
        }
        else if (builders.Count == 1)
        {
            return [builders[0].Build()];
        }

        using var result = new PooledArrayBuilder<TObject>(capacity: builders.Count);
        using var set = new PooledHashSet<TObject>(capacity: builders.Count);

        foreach (var builder in builders)
        {
            var item = builder.Build();

            if (set.Add(item))
            {
                result.Add(item);
            }
        }

        return result.ToImmutableAndClear();
    }
}
