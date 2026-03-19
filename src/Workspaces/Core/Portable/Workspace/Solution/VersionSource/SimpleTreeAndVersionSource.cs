// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis;

/// <summary>
/// Simple implementation of <see cref="ITreeAndVersionSource"/> backed by an opaque <see
/// cref="AsyncLazy{TreeAndVersion}"/>."/>
/// </summary>
internal sealed class SimpleTreeAndVersionSource : ITreeAndVersionSource
{
    private readonly AsyncLazy<TreeAndVersion> _source;

    private SimpleTreeAndVersionSource(AsyncLazy<TreeAndVersion> source)
    {
        _source = source;
    }

    public Task<TreeAndVersion> GetValueAsync(CancellationToken cancellationToken)
        => _source.GetValueAsync(cancellationToken);

    public TreeAndVersion GetValue(CancellationToken cancellationToken)
        => _source.GetValue(cancellationToken);

    public bool TryGetValue([NotNullWhen(true)] out TreeAndVersion? value)
        => _source.TryGetValue(out value);

    public static SimpleTreeAndVersionSource Create<TArg>(
        Func<TArg, CancellationToken, Task<TreeAndVersion>> asynchronousComputeFunction,
        Func<TArg, CancellationToken, TreeAndVersion>? synchronousComputeFunction, TArg arg)
    {
        return new(AsyncLazy<TreeAndVersion>.Create(asynchronousComputeFunction, synchronousComputeFunction, arg));
    }

    public static SimpleTreeAndVersionSource Create(TreeAndVersion source)
        => new(AsyncLazy.Create(source));
}
