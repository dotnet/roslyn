// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using Microsoft.AspNetCore.Razor.PooledObjects;

namespace Microsoft.AspNetCore.Razor.Language;

public abstract partial class TagHelperObjectBuilder<T> : IPoolableObject
    where T : TagHelperObject<T>
{
    private ImmutableArray<RazorDiagnostic>.Builder? _diagnostics;
    private bool _isBuilt;

    public ImmutableArray<RazorDiagnostic>.Builder Diagnostics
        => _diagnostics ??= ImmutableArray.CreateBuilder<RazorDiagnostic>();

    private protected TagHelperObjectBuilder()
    {
    }

    public T Build()
    {
        if (_isBuilt)
        {
            throw new InvalidOperationException();
        }

        _isBuilt = true;

        var diagnostics = new PooledHashSet<RazorDiagnostic>();
        try
        {
            CollectDiagnostics(ref diagnostics);
            diagnostics.UnionWith(_diagnostics);

            return BuildCore(diagnostics.ToImmutableArray());
        }
        finally
        {
            diagnostics.ClearAndFree();
        }
    }

    private protected abstract T BuildCore(ImmutableArray<RazorDiagnostic> diagnostics);

    private protected virtual void CollectDiagnostics(ref PooledHashSet<RazorDiagnostic> diagnostics)
    {
    }

    private protected abstract void Reset();

    void IPoolableObject.Reset()
    {
        _isBuilt = false;

        const int MaxSize = 32;

        if (_diagnostics is { } diagnostics)
        {
            diagnostics.Clear();

            if (diagnostics.Capacity > MaxSize)
            {
                diagnostics.Capacity = MaxSize;
            }
        }

        Reset();
    }
}
